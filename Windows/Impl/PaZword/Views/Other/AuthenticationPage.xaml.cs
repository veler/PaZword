using PaZword.ViewModels.Other;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Other
{
    /// <summary>
    /// Interaction logic for AuthenticationPage.xaml
    /// </summary>
    public sealed partial class AuthenticationPage : Page
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal AuthenticationPageViewModel ViewModel => (AuthenticationPageViewModel)DataContext;

        /// <summary>
        /// Initialize a new instance of the <see cref="AuthenticationPage"/> class.
        /// </summary>
        public AuthenticationPage()
        {
            InitializeComponent();

            ViewModel.Initialize();
        }
    }
}
