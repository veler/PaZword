using CommunityToolkit.Mvvm.ComponentModel;
using PaZword.Core.Theming;

namespace PaZword.App.ViewModels;

/// <summary>
/// Backs <see cref="Pages.VaultShell.SettingsPage"/>. Sprint 2 wires only the
/// theme selector through <see cref="IThemeService"/>; other settings show a
/// "Coming soon" placeholder.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _theme;
    private int _selectedThemeIndex;

    public IReadOnlyList<string> ThemeOptions { get; } = new[] { "Use system theme", "Light", "Dark" };

    public int SelectedThemeIndex
    {
        get => _selectedThemeIndex;
        set
        {
            if (SetProperty(ref _selectedThemeIndex, value))
                _theme.Set((AppTheme)value);
        }
    }

    public SettingsViewModel(IThemeService theme)
    {
        _theme = theme;
        _selectedThemeIndex = (int)_theme.Current;
    }
}
