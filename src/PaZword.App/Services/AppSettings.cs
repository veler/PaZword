using Windows.Storage;

namespace PaZword.App.Services;

/// <summary>
/// Centralised <see cref="ApplicationData.LocalSettings"/> keys and accessors
/// for simple boolean / scalar flags. Anything more structured goes through
/// a dedicated service (e.g. <see cref="Recents.RecentVaultsService"/>).
/// </summary>
internal static class AppSettings
{
    public const string OnboardingCompletedKey = "OnboardingCompleted";

    public static bool OnboardingCompleted
    {
        get => ApplicationData.Current.LocalSettings.Values[OnboardingCompletedKey] is bool b && b;
        set => ApplicationData.Current.LocalSettings.Values[OnboardingCompletedKey] = value;
    }
}
