using Microsoft.Extensions.DependencyInjection;
using PaZword.App.Navigation;
using PaZword.App.Recents;
using PaZword.App.Services;
using PaZword.App.Shell;
using PaZword.App.ViewModels;
using PaZword.Core.Navigation;
using PaZword.Core.Services;
using PaZword.Core.Theming;
using PaZword.Core.Vault;

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
    /// Creates and freezes the DI container. Called once from <see cref="App"/> construction.
    /// </summary>
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        // Logging — concrete sinks added when we have a file logger (later sprint).
        services.AddLogging(static _ => { });

        // Core services
        services.AddSingleton<DiagnosticsService>();
        services.AddSingleton<IThemeService, ThemeService>();

        // Vault — Sage's sprint-1 service. Default ctor wires KdbxReader/KdbxWriter.
        services.AddSingleton<IVaultService>(static _ => new VaultService());
        services.AddSingleton<IVaultSessionService, VaultSessionService>();

        // Recents persistence (STJ source-gen → AOT-clean)
        services.AddSingleton<IRecentVaultsService, RecentVaultsService>();

        // Shell chrome + navigation
        services.AddSingleton<IShellChromeService, ShellChromeService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // View-models (transient — pages are short-lived in Frame navigation).
        services.AddTransient<OnboardingViewModel>();
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<LockedViewModel>();
        services.AddTransient<MigrationPlaceholderViewModel>();
        services.AddTransient<VaultShellViewModel>();
        services.AddTransient<AllEntriesViewModel>();
        services.AddTransient<SettingsViewModel>();

        _instance = services.BuildServiceProvider(validateScopes: true);
        return _instance;
    }
}

