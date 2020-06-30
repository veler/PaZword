using PaZword.ViewModels;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal SettingsPageViewModel ViewModel => (SettingsPageViewModel)DataContext;

        public SettingsPage()
        {
            InitializeComponent();
        }
    }
}
