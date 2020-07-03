using PaZword.ViewModels.Dialog;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Dialog
{
    /// <summary>
    /// Interaction logic for SetupTwoFactorAuthenticationDialog.xaml
    /// </summary>
    public sealed partial class SetupTwoFactorAuthenticationDialog : ContentDialog
    {
        /// <summary>
        /// Gets the content dialog's view model.
        /// </summary>
        internal SetupTwoFactorAuthenticationDialogViewModel ViewModel => (SetupTwoFactorAuthenticationDialogViewModel)DataContext;

        public SetupTwoFactorAuthenticationDialog()
        {
            InitializeComponent();

            ViewModel.CloseDialog += ViewModel_CloseDialog;

            Closed += SetupTwoFactorAuthenticationDialog_Closed;
        }

        private void SetupTwoFactorAuthenticationDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            ViewModel.Closed();
        }

        private void ViewModel_CloseDialog(object sender, System.EventArgs e)
        {
            Hide();
        }
    }
}
