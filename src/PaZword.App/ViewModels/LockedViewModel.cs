using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PaZword.App.Navigation;
using PaZword.App.Recents;
using PaZword.App.Services;
using PaZword.Core.Navigation;
using PaZword.Core.Vault;
using Windows.Storage.Pickers;

namespace PaZword.App.ViewModels;

/// <summary>
/// Backs <see cref="Pages.LockedPage"/>. Drives the unlock flow:
///   - Master password (default-focus, eye toggle off)
///   - Optional key file via Expander
///   - Windows Hello placeholder (disabled until sprint 7)
///   - 400 ms threshold before showing a progress ring (ART-4)
///   - 3 consecutive failures → 2-second debounce (UI disabled)
///   - InfoBar error below the field; never a dialog
/// </summary>
public sealed partial class LockedViewModel : ObservableObject
{
    private const int FailDebounceThreshold = 3;
    private const int FailDebounceMs = 2000;
    private const int ProgressThresholdMs = 400;

    private readonly INavigationService _nav;
    private readonly IVaultService _vault;
    private readonly IVaultSessionService _session;
    private readonly IRecentVaultsService _recents;
    private readonly ILogger<LockedViewModel>? _logger;

    private string _vaultPath = string.Empty;
    private string _password = string.Empty;
    private byte[]? _keyFileBytes;
    private string? _keyFileName;
    private bool _isPasswordRevealed;
    private bool _isBusy;
    private bool _isUnlockingLongRunning;
    private string? _errorMessage;
    private int _failureCount;
    private bool _isDebouncing;

    public string VaultPath
    {
        get => _vaultPath;
        private set
        {
            if (SetProperty(ref _vaultPath, value))
                OnPropertyChanged(nameof(VaultFileName));
        }
    }

    public string VaultFileName =>
        string.IsNullOrEmpty(VaultPath) ? string.Empty : Path.GetFileName(VaultPath);

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
                UnlockCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsPasswordRevealed
    {
        get => _isPasswordRevealed;
        set => SetProperty(ref _isPasswordRevealed, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsInputEnabled));
                UnlockCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsInputEnabled => !IsBusy && !IsDebouncing;

    public bool IsUnlockingLongRunning
    {
        get => _isUnlockingLongRunning;
        private set => SetProperty(ref _isUnlockingLongRunning, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public int FailureCount
    {
        get => _failureCount;
        private set
        {
            if (SetProperty(ref _failureCount, value))
                OnPropertyChanged(nameof(ShowsAttemptCounter));
        }
    }

    public bool ShowsAttemptCounter => FailureCount > 0;

    public bool IsDebouncing
    {
        get => _isDebouncing;
        private set
        {
            if (SetProperty(ref _isDebouncing, value))
            {
                OnPropertyChanged(nameof(IsInputEnabled));
                UnlockCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? KeyFileName
    {
        get => _keyFileName;
        private set
        {
            if (SetProperty(ref _keyFileName, value))
                OnPropertyChanged(nameof(HasKeyFile));
        }
    }

    public bool HasKeyFile => !string.IsNullOrEmpty(_keyFileName);

    public LockedViewModel(
        INavigationService nav,
        IVaultService vault,
        IVaultSessionService session,
        IRecentVaultsService recents,
        ILogger<LockedViewModel>? logger = null)
    {
        _nav = nav;
        _vault = vault;
        _session = session;
        _recents = recents;
        _logger = logger;
    }

    /// <summary>Called by <see cref="Pages.LockedPage"/>.OnNavigatedTo with the vault path.</summary>
    public void Initialize(string vaultPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(vaultPath);
        VaultPath = vaultPath;
        Password = string.Empty;
        ErrorMessage = null;
        FailureCount = 0;
        _keyFileBytes = null;
        KeyFileName = null;
        IsBusy = false;
        IsDebouncing = false;
        IsUnlockingLongRunning = false;
    }

    [RelayCommand]
    private async Task PickKeyFileAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List,
        };
        picker.FileTypeFilter.Add("*");
        WindowInterop.InitializeWithMainWindow(picker);

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        var bytes = await File.ReadAllBytesAsync(file.Path);
        _keyFileBytes = bytes;
        KeyFileName = file.Name;
    }

    [RelayCommand]
    private void ClearKeyFile()
    {
        _keyFileBytes = null;
        KeyFileName = null;
    }

    [RelayCommand(CanExecute = nameof(CanUnlock))]
    private async Task UnlockAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        IsUnlockingLongRunning = false;

        using var threshold = new CancellationTokenSource();
        var showRing = ScheduleRingAsync(threshold.Token);

        try
        {
            using var credentials = VaultCredentials.FromPassword(_password, _keyFileBytes);
            var handle = await Task.Run(() => _vault.OpenAsync(_vaultPath, credentials));
            // success
            _session.SetCurrent(handle);
            _recents.Touch(_vaultPath);
            FailureCount = 0;
            _nav.Reset(PageKeys.VaultShell);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            FailureCount++;
            ErrorMessage = ClassifyUnlockError(ex);
            _logger?.LogWarning("Unlock failed (attempt {Count}): {Type}", FailureCount, ex.GetType().Name);

            if (FailureCount >= FailDebounceThreshold)
                await DebounceAsync();
        }
        finally
        {
            threshold.Cancel();
            try { await showRing; } catch (OperationCanceledException) { }
            IsBusy = false;
            IsUnlockingLongRunning = false;
        }
    }

    private bool CanUnlock() =>
        !IsBusy && !IsDebouncing && !string.IsNullOrEmpty(_password);

    [RelayCommand]
    private void OpenDifferentVault()
    {
        // Drop any pending error / key file. Return to Welcome.
        ErrorMessage = null;
        FailureCount = 0;
        ClearKeyFile();
        _nav.NavigateTo(PageKeys.Welcome, parameter: null, NavigationTransition.Entrance);
    }

    private async Task ScheduleRingAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(ProgressThresholdMs, ct);
            IsUnlockingLongRunning = true;
            // Park until the unlock completes; the caller cancels this token in finally.
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            // Expected — caller cancels when the unlock completes.
        }
    }

    private async Task DebounceAsync()
    {
        IsDebouncing = true;
        try
        {
            await Task.Delay(FailDebounceMs);
        }
        finally
        {
            IsDebouncing = false;
        }
    }

    private static string ClassifyUnlockError(Exception ex) => ex switch
    {
        // PaZword.Kdbx.KdbxIntegrityException is the typical "wrong password" failure
        // (HMAC mismatch after KDF). Other exceptions get a more generic message
        // because exposing the file/format reason can be misleading at the unlock step.
        Exception when ex.GetType().Name == "KdbxIntegrityException" => "Incorrect master password or key file.",
        FileNotFoundException => "Vault file not found.",
        UnauthorizedAccessException => "Cannot read the vault file (permission denied).",
        _ => "Could not open the vault. Check the password and try again.",
    };
}
