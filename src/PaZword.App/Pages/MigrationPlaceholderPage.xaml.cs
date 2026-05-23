using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PaZword.App.Navigation;
using PaZword.App.Shell;
using PaZword.App.ViewModels;
using PaZword.Core.Navigation;

namespace PaZword.App.Pages;

public sealed partial class MigrationPlaceholderPage : Page
{
    public MigrationPlaceholderViewModel ViewModel { get; }

    public MigrationPlaceholderPage()
    {
        ViewModel = App.Services.GetRequiredService<MigrationPlaceholderViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        App.Services.GetRequiredService<IShellChromeService>().SetMode(ShellChromeMode.Brand);
    }
}
