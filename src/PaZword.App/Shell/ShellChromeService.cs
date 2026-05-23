using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PaZword.App.Shell;

/// <summary>
/// What the window-level <c>TitleBar</c> should look like for the current page.
/// </summary>
public enum ShellChromeMode
{
    /// <summary>Brand-only: icon + "PaZword" title. No search, no buttons. Used by Onboarding/Welcome/Locked/MigrationPlaceholder.</summary>
    Brand = 0,

    /// <summary>Full: hamburger + search + title + user-chevron + lock. Used by VaultShell.</summary>
    Full = 1,
}

/// <summary>
/// Implemented by the currently-active shell page so window-level UI (title bar,
/// keyboard accelerators) can delegate user actions back to it without the
/// window holding a reference to the page.
/// </summary>
public interface IShellHost
{
    void TogglePane();
    void Lock();
    void SwitchVault();
    void NavigateAbout();
    void FocusSearch();
}

/// <summary>
/// Drives the window-level <c>TitleBar</c> from the currently-active page.
/// The MainWindow binds its TitleBar slot visibilities to <see cref="Mode"/>;
/// pages call <see cref="SetMode"/> in their <c>OnNavigatedTo</c>.
/// </summary>
/// <remarks>
/// Inherits <see cref="INotifyPropertyChanged"/> so XAML <c>{x:Bind … Mode=OneWay}</c>
/// can subscribe to property updates through the interface type.
/// </remarks>
public interface IShellChromeService : INotifyPropertyChanged
{
    /// <summary>Current chrome mode (OneWay-bindable).</summary>
    ShellChromeMode Mode { get; }

    /// <summary>Vault filename surfaced in the title-bar user chevron flyout. <c>null</c> when no vault is open.</summary>
    string? VaultFileName { get; }

    /// <summary>Bound to title-bar element visibility — <see cref="ShellChromeMode.Full"/> equivalent.</summary>
    bool IsFullMode { get; }

    /// <summary>Currently-active shell host (set by <see cref="Pages.VaultShellPage"/>).</summary>
    IShellHost? Host { get; }

    void SetMode(ShellChromeMode mode);
    void SetVaultFileName(string? fileName);
    void SetHost(IShellHost? host);
}

/// <summary>Singleton DI-registered implementation.</summary>
public sealed partial class ShellChromeService : ObservableObject, IShellChromeService
{
    private ShellChromeMode _mode = ShellChromeMode.Brand;
    private string? _vaultFileName;
    private IShellHost? _host;

    public ShellChromeMode Mode
    {
        get => _mode;
        private set
        {
            if (SetProperty(ref _mode, value))
                OnPropertyChanged(nameof(IsFullMode));
        }
    }

    public bool IsFullMode => _mode == ShellChromeMode.Full;

    public string? VaultFileName
    {
        get => _vaultFileName;
        private set => SetProperty(ref _vaultFileName, value);
    }

    public IShellHost? Host
    {
        get => _host;
        private set => SetProperty(ref _host, value);
    }

    public void SetMode(ShellChromeMode mode) => Mode = mode;
    public void SetVaultFileName(string? fileName) => VaultFileName = fileName;
    public void SetHost(IShellHost? host) => Host = host;
}

