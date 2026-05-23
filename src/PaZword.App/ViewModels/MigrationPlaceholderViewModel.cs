using CommunityToolkit.Mvvm.Input;
using PaZword.App.Navigation;
using PaZword.Core.Navigation;

namespace PaZword.App.ViewModels;

/// <summary>
/// Trivial VM for <see cref="Pages.MigrationPlaceholderPage"/>. Sprint 5 replaces
/// both this page and its VM with the real migration wizard.
/// </summary>
public sealed partial class MigrationPlaceholderViewModel
{
    private readonly INavigationService _nav;

    public MigrationPlaceholderViewModel(INavigationService nav)
    {
        _nav = nav;
    }

    [RelayCommand]
    private void BackToWelcome() =>
        _nav.NavigateTo(PageKeys.Welcome, parameter: null, NavigationTransition.Entrance);
}
