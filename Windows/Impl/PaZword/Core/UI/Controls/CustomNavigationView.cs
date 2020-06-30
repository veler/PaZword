using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Core.UI.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="CustomNavigationView"/>
    /// </summary>
    public sealed class CustomNavigationView : NavigationView
    {
        private const string OverlayGrid = "OverlayGrid";

        private bool _layoutUpdatedHandled;

        public static readonly DependencyProperty OverlayVisibleProperty = DependencyProperty.Register(
            nameof(OverlayVisible),
            typeof(bool),
            typeof(CustomNavigationView),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the overlay should be visible or not.
        /// </summary>
        public bool OverlayVisible
        {
            get => (bool)GetValue(OverlayVisibleProperty);
            set => SetValue(OverlayVisibleProperty, value);
        }

        /// <summary>
        /// Raised when the user clicks on the overlay.
        /// </summary>
        public event RoutedEventHandler OverlayTapped;

        /// <summary>
        /// Initialize a new instance of the <see cref="CustomNavigationView"/> class.
        /// </summary>
        public CustomNavigationView()
        {
            DefaultStyleKey = typeof(CustomNavigationView);

            LayoutUpdated += CustomNavigationView_LayoutUpdated;
        }

        private void CustomNavigationView_LayoutUpdated(object sender, object e)
        {
            if (_layoutUpdatedHandled)
            {
                return;
            }

            var overlayGrid = (Grid)GetTemplateChild(OverlayGrid);
            if (overlayGrid != null)
            {
                overlayGrid.Tapped += OverlayGrid_Tapped;
                _layoutUpdatedHandled = true;
            }
        }

        private void OverlayGrid_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            OverlayTapped?.Invoke(this, new RoutedEventArgs());
        }
    }
}
