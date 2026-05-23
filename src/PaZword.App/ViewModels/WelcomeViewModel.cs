using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PaZword.App.Navigation;
using PaZword.App.Recents;
using PaZword.App.Services;
using PaZword.Core.Navigation;
using Windows.Storage.Pickers;

namespace PaZword.App.ViewModels;

/// <summary>
/// Backs <see cref="Pages.WelcomePage"/>. Three tiles + recent-vaults list.
/// </summary>
public sealed partial class WelcomeViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly IRecentVaultsService _recents;
    private bool _showCreateNewBanner;

    public ObservableCollection<RecentVaultEntry> Recents { get; } = new();

    public bool HasRecents => Recents.Count > 0;

    public bool ShowCreateNewBanner
    {
        get => _showCreateNewBanner;
        set => SetProperty(ref _showCreateNewBanner, value);
    }

    public WelcomeViewModel(INavigationService nav, IRecentVaultsService recents)
    {
        _nav = nav;
        _recents = recents;
        _recents.Changed += (_, _) => RefreshRecents();
        RefreshRecents();
    }

    /// <summary>Called from <see cref="Pages.WelcomePage"/>.OnNavigatedTo.</summary>
    public void RefreshRecents()
    {
        Recents.Clear();
        foreach (var entry in _recents.GetAll()) Recents.Add(entry);
        OnPropertyChanged(nameof(HasRecents));
    }

    [RelayCommand]
    private void CreateNew()
    {
        // Real vault-creation wizard arrives in sprint 3 (entry CRUD).
        ShowCreateNewBanner = true;
    }

    [RelayCommand]
    private async Task OpenExistingAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List,
        };
        picker.FileTypeFilter.Add(".kdbx");
        WindowInterop.InitializeWithMainWindow(picker);

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        _nav.NavigateTo(PageKeys.Locked, file.Path, NavigationTransition.DrillIn);
    }

    [RelayCommand]
    private void Migrate()
    {
        // Migrate tile is always visible per D-015. Sprint 5 replaces the placeholder.
        _nav.NavigateTo(PageKeys.MigrationPlaceholder, parameter: null, NavigationTransition.DrillIn);
    }

    [RelayCommand]
    private void OpenRecent(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        _nav.NavigateTo(PageKeys.Locked, path, NavigationTransition.DrillIn);
    }

    [RelayCommand]
    private void ForgetRecent(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        _recents.Remove(path);
    }
}
