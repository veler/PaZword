using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaZword.Core.Services;

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
            builder.AddDebug();
            // File sink will be wired in sprint 2+
        });

        // Core services
        services.AddSingleton<DiagnosticsService>();

        // Navigation (concrete implementation registered by PaZword.App in sprint 2)
        // services.AddSingleton<INavigationService, NavigationService>();

        _instance = services.BuildServiceProvider(validateScopes: true);
        return _instance;
    }
}
