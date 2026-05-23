using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PaZword.App.Shell;
using PaZword.App.ViewModels;

namespace PaZword.App.Pages.Onboarding;

/// <summary>
/// 3-screen first-run flow (D-015). Sets <see cref="ShellChromeMode.Brand"/> on the
/// window title bar so this page renders with a minimal brand chrome.
/// </summary>
public sealed partial class OnboardingPage : Page
{
    public OnboardingViewModel ViewModel { get; }

    public OnboardingPage()
    {
        ViewModel = App.Services.GetRequiredService<OnboardingViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        App.Services.GetRequiredService<IShellChromeService>().SetMode(ShellChromeMode.Brand);
    }

    /// <summary>Pip-indicator brush helper used by {x:Bind}.</summary>
    public Brush PipFill(bool filled)
    {
        if (filled && Application.Current.Resources["BrandPrimaryBrush"] is Brush brand)
            return brand;
        if (Application.Current.Resources["TextFillColorTertiaryBrush"] is Brush dim)
            return dim;
        return new SolidColorBrush(Colors.Gray);
    }

    /// <summary>Hide the Next button on the final screen — the two CTA buttons replace it.</summary>
    public Visibility NextButtonVisibility(bool isFinalScreen) =>
        isFinalScreen ? Visibility.Collapsed : Visibility.Visible;
}
