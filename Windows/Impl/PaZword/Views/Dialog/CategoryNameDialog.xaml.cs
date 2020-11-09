using PaZword.Api.Models;
using PaZword.Localization;
using PaZword.ViewModels.Dialog;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Dialog
{
    /// <summary>
    /// Interaction logic for CategoryNameDialog.xaml
    /// </summary>
    public sealed partial class CategoryNameDialog : ContentDialog
    {
        /// <summary>
        /// Gets the content dialog's view model.
        /// </summary>
        internal CategoryNameDialogViewModel ViewModel => (CategoryNameDialogViewModel)DataContext;

        internal ContentDialogResult Result { get; private set; }

        public CategoryNameDialog(Category existingCategory)
        {
            InitializeComponent();

            if (existingCategory == null)
            {
                Title = ViewModel.Strings.AddCategoryTitle;
                PrimaryButtonText = ViewModel.Strings.AddCategoryPrimaryButton;
            }
            else
            {
                Title = ViewModel.Strings.RenameTitle;
                PrimaryButtonText = ViewModel.Strings.RenamePrimaryButton;
                ViewModel.CategoryName = existingCategory.Name;
                ViewModel.Icon = existingCategory.Icon;
            }

            SecondaryButtonText = LanguageManager.Instance.InputDialog.SecondaryButton;

            ViewModel.CloseDialog += ViewModel_CloseDialog;
        }

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            CategoryNameTextBox.SelectAll();
            CategoryNameTextBox.Focus(FocusState.Keyboard);
        }

        private void ContentDialog_PrimaryButtonClick(object sender, ContentDialogButtonClickEventArgs args)
        {
            Result = ContentDialogResult.Primary;
        }

        private void ContentDialog_SecondaryButtonClick(object sender, ContentDialogButtonClickEventArgs args)
        {
            Result = ContentDialogResult.Secondary;
        }

        private void ViewModel_CloseDialog(object sender, System.EventArgs e)
        {
            Result = ContentDialogResult.Primary;
            Hide();
        }
    }
}
