using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaZword.App.Services;
using PaZword.App.ViewModels;
using PaZword.Core.Services;
using PaZword.Core.Theming;

namespace PaZword.App;

/// <summary>
/// Builds and owns the application's <see cref="IServiceProvider"/>.
/// All registrations must be explicit — no assembly scanning (AOT-safe).
/// </summary>
internal static class AppServiceProvider
{
    private static IServiceProvider? _instance;

    /// <summary>Gets the singleton service provider. Call <see cref="Build"/> first.</summary>
    public static IServiceProvider Instance =>
        _instance ?? throw new InvalidOperationException("Call AppServiceProvider.Build() before accessing Instance.");

    /// <summary>
    /// Creates and freezes the DI container. Called once from App.OnLaunched.
    /// </summary>
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(static builder =>
        {
            // Concrete sinks wired in sprint 2 (file logger).
            // Default behaviour: emits to attached debugger via the framework.
        });

        // Core services
        services.AddSingleton<DiagnosticsService>();

        // Theming (Light/Dark/System persistence stub — T-0.4)
        services.AddSingleton<IThemeService, ThemeService>();

        // View-models (transient — pages are short-lived in Frame navigation)
        services.AddTransient<MainPageViewModel>();

        // Navigation (concrete implementation registered by PaZword.App in sprint 2)
        // services.AddSingleton<INavigationService, NavigationService>();

        _instance = services.BuildServiceProvider(validateScopes: true);
        return _instance;
    }
}
