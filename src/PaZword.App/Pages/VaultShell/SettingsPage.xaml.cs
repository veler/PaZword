using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PaZword.App.ViewModels;

namespace PaZword.App.Pages.VaultShell;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }
}
