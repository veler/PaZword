namespace PaZword.Core.Theming;

/// <summary>
/// User-facing theme preference. <see cref="System"/> defers to the OS setting.
/// </summary>
public enum AppTheme
{
    System = 0,
    Light = 1,
    Dark = 2
}

/// <summary>
/// Abstraction over theme preference storage and application.
/// Lives in Core so view-models and services can read/write the preference
/// without depending on WinUI types.
/// </summary>
public interface IThemeService
{
    /// <summary>Currently persisted preference.</summary>
    AppTheme Current { get; }

    /// <summary>Persist a new preference and apply it to the live UI.</summary>
    void Set(AppTheme theme);
}
