using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PaZword.App.ViewModels;

namespace PaZword.App;

/// <summary>
/// The main content page. Resolves its view-model from DI in a parameterless
/// constructor so it remains compatible with WinUI <see cref="Frame"/> navigation
/// (which instantiates pages via parameterless <c>Activator</c> calls — but for AOT
/// we rely on the page type being statically referenced from <c>RootFrame.Navigate</c>,
/// which keeps the constructor reachable without reflection).
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainPageViewModel>();
        InitializeComponent();
    }
}
