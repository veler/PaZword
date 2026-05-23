using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PaZword.App.Recents;
using PaZword.App.Shell;
using PaZword.App.ViewModels;

namespace PaZword.App.Pages;

public sealed partial class WelcomePage : Page
{
    public WelcomeViewModel ViewModel { get; }

    public WelcomePage()
    {
        ViewModel = App.Services.GetRequiredService<WelcomeViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        App.Services.GetRequiredService<IShellChromeService>().SetMode(ShellChromeMode.Brand);
        ViewModel.RefreshRecents();
    }

    private void OnRecentClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecentVaultEntry entry)
            ViewModel.OpenRecentCommand.Execute(entry.Path);
    }

    public Visibility RecentsListVisibility(bool hasRecents) =>
        hasRecents ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyRecentsVisibility(bool hasRecents) =>
        hasRecents ? Visibility.Collapsed : Visibility.Visible;
}
