using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PaZword.App.ViewModels;

namespace PaZword.App.Pages.VaultShell;

public sealed partial class AllEntriesPage : Page
{
    public AllEntriesViewModel ViewModel { get; }

    public AllEntriesPage()
    {
        ViewModel = App.Services.GetRequiredService<AllEntriesViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Refresh();
    }

    private void OnTreeRootInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        // Sprint 2: only one root. Sprint 3 wires group selection to filter the list.
    }

    public Visibility ListVisibility(bool hasEntries) =>
        hasEntries ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyStateVisibility(bool hasEntries) =>
        hasEntries ? Visibility.Collapsed : Visibility.Visible;

    public Visibility DetailVisibility(bool hasSelection) =>
        hasSelection ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NoSelectionVisibility(bool hasSelection, bool hasEntries) =>
        !hasSelection && hasEntries ? Visibility.Visible : Visibility.Collapsed;
}
