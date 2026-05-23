using Microsoft.UI.Xaml;
using PaZword.Core.Theming;
using Windows.Storage;

namespace PaZword.App.Services;

/// <summary>
/// Persists the user's theme preference to <see cref="ApplicationData.LocalSettings"/>
/// and applies it to every open <see cref="Window"/>'s root element.
/// AOT-safe: no reflection, no JSON, no scanning.
/// </summary>
internal sealed class ThemeService : IThemeService
{
    private const string SettingKey = "AppTheme";

    public AppTheme Current
    {
        get
        {
            var raw = ApplicationData.Current.LocalSettings.Values[SettingKey] as int?;
            return raw is int v && Enum.IsDefined(typeof(AppTheme), v)
                ? (AppTheme)v
                : AppTheme.System;
        }
    }

    public void Set(AppTheme theme)
    {
        ApplicationData.Current.LocalSettings.Values[SettingKey] = (int)theme;
        Apply(theme);
    }

    /// <summary>
    /// Applies the given preference to the app's main window root, if available.
    /// Called from <see cref="App"/> on launch and from <see cref="Set"/>.
    /// </summary>
    public static void Apply(AppTheme theme)
    {
        var window = (App.Current as App)?.MainWindow;
        if (window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }
}
