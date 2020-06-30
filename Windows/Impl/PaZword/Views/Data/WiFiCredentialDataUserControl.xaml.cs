using PaZword.Core;
using PaZword.ViewModels.Data.WiFiCredential;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Data
{
    /// <summary>
    /// Interaction logic for WiFiCredentialDataUserControl.xaml
    /// </summary>
    public sealed partial class WiFiCredentialDataUserControl : UserControl
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal WiFiCredentialDataViewModel ViewModel => (WiFiCredentialDataViewModel)DataContext;

        internal WiFiCredentialDataUserControl(WiFiCredentialDataViewModel dataContext)
        {
            DataContext = Arguments.NotNull(dataContext, nameof(dataContext));
            InitializeComponent();
        }
    }
}
