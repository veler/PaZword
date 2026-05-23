using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using PaZword.App.Services;
using PaZword.Core.Theming;

namespace PaZword.App;

/// <summary>
/// Application bootstrap. Owns the DI container (built once before any UI is created)
/// and the single top-level <see cref="MainWindow"/>.
/// </summary>
public partial class App : Application
{
    /// <summary>The top-level window. Available after <see cref="OnLaunched"/>.</summary>
    public Window? MainWindow { get; private set; }

    /// <summary>Strongly-typed handle to the top-level window. Available after <see cref="OnLaunched"/>.</summary>
    internal MainWindow? MainWindowImpl => MainWindow as MainWindow;

    /// <summary>Application-scoped service provider. Built before <see cref="OnLaunched"/> returns.</summary>
    public static IServiceProvider Services => AppServiceProvider.Instance;

    public App()
    {
        InitializeComponent();
        AppServiceProvider.Build();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Resolve so MEDI scope is validated (catches missing registrations at startup).
        var theme = Services.GetRequiredService<IThemeService>();

        MainWindow = new MainWindow();
        MainWindow.Activate();

        ThemeService.Apply(theme.Current);
    }
}

