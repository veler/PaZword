using PaZword.ViewModels.Dialog;
using System.Security;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Dialog
{
    /// <summary>
    /// Interaction logic for PasswordGeneratorDialog.xaml
    /// </summary>
    public sealed partial class PasswordGeneratorDialog : ContentDialog
    {
        /// <summary>
        /// Gets the content dialog's view model.
        /// </summary>
        internal PasswordGeneratorDialogViewModel ViewModel => (PasswordGeneratorDialogViewModel)DataContext;

        internal SecureString GeneratedPassword => ViewModel.GeneratedPassword;

        internal ContentDialogResult Result { get; private set; }

        internal PasswordGeneratorDialog(bool canSaveResult = false)
        {
            InitializeComponent();

            if (canSaveResult)
            {
                PrimaryButtonText = ViewModel.Strings.PrimaryButton;
            }
            else
            {
                PrimaryButtonText = string.Empty;
            }

            ViewModel.CloseDialog += ViewModel_CloseDialog;
        }

        private void ViewModel_CloseDialog(object sender, ContentDialogResult e)
        {
            Result = e;
            Hide();
        }
    }
}
