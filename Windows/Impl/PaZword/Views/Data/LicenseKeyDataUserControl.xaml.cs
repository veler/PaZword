using PaZword.Core;
using PaZword.ViewModels.Data.LicenseKey;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Data
{
    /// <summary>
    /// Interaction logic for LicenseKeyDataUserControl.xaml
    /// </summary>
    public sealed partial class LicenseKeyDataUserControl : UserControl
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal LicenseKeyDataViewModel ViewModel => (LicenseKeyDataViewModel)DataContext;

        internal LicenseKeyDataUserControl(LicenseKeyDataViewModel dataContext)
        {
            DataContext = Arguments.NotNull(dataContext, nameof(dataContext));
            InitializeComponent();
        }
    }
}
