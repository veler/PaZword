using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PaZword.RemoteStorageProvider.ManualTester
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal MainPageViewModel ViewModel => (MainPageViewModel)DataContext;

        public MainPage()
        {
            DataContext = new MainPageViewModel();
            this.InitializeComponent();
        }
    }
}
