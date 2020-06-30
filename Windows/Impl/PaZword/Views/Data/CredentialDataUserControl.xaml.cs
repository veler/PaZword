using PaZword.Core;
using PaZword.ViewModels.Data.Credential;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Data
{
    /// <summary>
    /// Interaction logic for CredentialDataUserControl.xaml
    /// </summary>
    public sealed partial class CredentialDataUserControl : UserControl
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal CredentialDataViewModel ViewModel => (CredentialDataViewModel)DataContext;

        internal CredentialDataUserControl(CredentialDataViewModel dataContext)
        {
            DataContext = Arguments.NotNull(dataContext, nameof(dataContext));
            InitializeComponent();
        }
    }
}
