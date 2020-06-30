using PaZword.Core;
using System.Composition;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace PaZword.ViewModels
{
    /// <summary>
    /// Provides a helper designed to manager the window title bar.
    /// </summary>
    [Export(typeof(TitleBarViewModel))]
    [Shared()]
    public class TitleBarViewModel : ViewModelBase
    {
        private const string SystemAccentColor = "SystemAccentColor";
        private const string PressedAccentColor = "PressedAccentColor";
        private const string SystemControlForegroundBaseHighBrush = "SystemControlForegroundBaseHighBrush";

        private readonly CoreApplicationViewTitleBar _coreTitleBar;

        private Thickness _titlePosition;

        /// <summary>
        /// Gets or sets the position of the title in the window title bar.
        /// </summary>
        public Thickness TitlePosition
        {
            get => _titlePosition;
            private set
            {
                if (value.Left != _titlePosition.Left || value.Top != _titlePosition.Top)
                {
                    _titlePosition = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TitleBarViewModel"/> class.
        /// </summary>
        [ImportingConstructor]
        public TitleBarViewModel()
        {
            _coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            _coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            TitlePosition = CalculateTilebarOffset();
        }

        /// <summary>
        /// Initialize the states of the title bar.
        /// </summary>
        internal void SetupTitleBar()
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            if (titleBar != null)
            {
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveForegroundColor = Colors.LightGray;
                titleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources[SystemAccentColor];
                titleBar.ButtonPressedBackgroundColor = (Color)Application.Current.Resources[PressedAccentColor];
                titleBar.ButtonForegroundColor = ((Windows.UI.Xaml.Media.SolidColorBrush)Application.Current.Resources[SystemControlForegroundBaseHighBrush]).Color;

                _coreTitleBar.ExtendViewIntoTitleBar = true;
            }

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        /// <summary>
        /// Calculates the position of the title of the window depending of the the presence of the Back button in the title bar.
        /// </summary>
        /// <returns>A <see cref="Thickness"/> that corresponds to the position that the title must take.</returns>
        private Thickness CalculateTilebarOffset()
        {
            // top position should be 6 pixels for a 32 pixel high titlebar hence scale by actual height
            var correctHeight = _coreTitleBar.Height / 32 * 6;

            return new Thickness(_coreTitleBar.SystemOverlayLeftInset + 12, correctHeight, 0, 0);
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            TitlePosition = CalculateTilebarOffset();
        }
    }
}
