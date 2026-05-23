using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PaZword.App.Navigation;
using PaZword.App.Pages;
using PaZword.App.Pages.Onboarding;
using PaZword.App.Services;
using PaZword.App.Shell;
using PaZword.Core.Navigation;

namespace PaZword.App;

/// <summary>
/// The application window. Hosts the window-level custom title bar (typed
/// <see cref="TitleBar"/>) and the root navigation <see cref="Frame"/>.
/// Title-bar slot visibility is data-bound to <see cref="IShellChromeService"/>;
/// page actions (Lock/Switch/About) route to the active <see cref="IShellHost"/>.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Chrome service, bound from XAML via <c>{x:Bind Chrome.…}</c>.</summary>
    public IShellChromeService Chrome { get; }

    public MainWindow()
    {
        Chrome = App.Services.GetRequiredService<IShellChromeService>();
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Wire the root navigation service to RootFrame, then register all root-level pages.
        var nav = (NavigationService)App.Services.GetRequiredService<INavigationService>();
        nav.Initialize(RootFrame);
        nav.RegisterPage<OnboardingPage>(PageKeys.Onboarding);
        nav.RegisterPage<WelcomePage>(PageKeys.Welcome);
        nav.RegisterPage<LockedPage>(PageKeys.Locked);
        nav.RegisterPage<MigrationPlaceholderPage>(PageKeys.MigrationPlaceholder);
        nav.RegisterPage<VaultShellPage>(PageKeys.VaultShell);

        // Decide first destination by the OnboardingCompleted flag (D-015).
        nav.Reset(AppSettings.OnboardingCompleted ? PageKeys.Welcome : PageKeys.Onboarding);
    }

    /// <summary>Moves focus into the title-bar search box. Called by Ctrl+F / Ctrl+K from the shell.</summary>
    public void FocusTitleBarSearch() => TitleBarSearch.Focus(FocusState.Programmatic);

    public Visibility FullModeVisibility(bool isFull) => isFull ? Visibility.Visible : Visibility.Collapsed;

    private void OnPaneToggleRequested(TitleBar sender, object args) => Chrome.Host?.TogglePane();

    private void OnLockClicked(object sender, RoutedEventArgs e) => Chrome.Host?.Lock();

    private void OnSwitchClicked(object sender, RoutedEventArgs e) => Chrome.Host?.SwitchVault();

    private void OnAboutClicked(object sender, RoutedEventArgs e) => Chrome.Host?.NavigateAbout();
}

