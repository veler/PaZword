using PaZword.Core.Threading;
using PaZword.ViewModels;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PaZword.Views
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal MainPageViewModel ViewModel => (MainPageViewModel)DataContext;

        /// <summary>
        /// Initialize a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += MainPage_CloseRequested;

            Unloaded += MainPage_Unloaded;
            ViewModel.InitializeAsync(ContentFrame).Forget();
        }

        private void MainPage_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            var openedpopups = VisualTreeHelper.GetOpenPopups(Window.Current);
            if (openedpopups.Count > 0)
            {
                // A Popup or content dialog is opened. Do not quit the app.
                e.Handled = true;
                return;
            }

            if (ViewModel.CommonViewModel.EditingOverlayClickedCommand.CanExecute(null))
            {
                // An account is edited.
                e.Handled = true;
                ViewModel.CommonViewModel.EditingOverlayClickedCommand.Execute(null);
                return;
            }

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested -= MainPage_CloseRequested;
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Unload();
        }

        private void SearchBoxKeyboardAccelerator_Invoked(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            SearchBox.Focus(FocusState.Keyboard);
        }
    }
}
