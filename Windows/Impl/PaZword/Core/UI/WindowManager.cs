using PaZword.Api.Models;
using PaZword.Api.UI;
using PaZword.Core.Threading;
using PaZword.Views.Dialog;
using System;
using System.Composition;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;

namespace PaZword.Core.UI
{
    [Export(typeof(IWindowManager))]
    [Shared()]
    internal sealed class WindowManager : IWindowManager
    {
        public async Task<string> ShowInputDialogAsync(
            string placeHolder,
            string primaryButtonText,
            string defaultInputValue = null,
            string title = null)
        {
            return await TaskHelper.RunOnUIThreadAsync(async () =>
            {
                var dialog = new InputDialog
                {
                    Title = title ?? Package.Current.DisplayName,
                    PrimaryButtonText = Arguments.NotNullOrWhiteSpace(primaryButtonText, nameof(primaryButtonText)),
                    Placeholder = Arguments.NotNullOrWhiteSpace(placeHolder, nameof(placeHolder)),
                    InputValue = defaultInputValue
                };

                await dialog.ShowAsync();

                if (dialog.Result == ContentDialogResult.Primary)
                {
                    return dialog.InputValue;
                }

                return string.Empty;
            }).ConfigureAwait(false);
        }

        public async Task<ContentDialogResult> ShowMessageDialogAsync(
            string message,
            string closeButtonText,
            string primaryButtonText = null,
            string secondaryButtonText = null,
            string title = null,
            ContentDialogButton defaultButton = ContentDialogButton.Close)
        {
            return await TaskHelper.RunOnUIThreadAsync(async () =>
            {
                var confirmationDialog = new ContentDialog
                {
                    Title = title ?? Package.Current.DisplayName,
                    Content = Arguments.NotNullOrWhiteSpace(message, nameof(message)),
                    CloseButtonText = Arguments.NotNullOrWhiteSpace(closeButtonText, nameof(closeButtonText)),
                    DefaultButton = defaultButton
                };

                if (!string.IsNullOrEmpty(primaryButtonText))
                {
                    confirmationDialog.PrimaryButtonText = primaryButtonText;
                }

                if (!string.IsNullOrEmpty(secondaryButtonText))
                {
                    confirmationDialog.SecondaryButtonText = secondaryButtonText;
                }

                return await confirmationDialog.ShowAsync();
            }).ConfigureAwait(false);
        }

        public async Task<(bool, string, CategoryIcon)> ShowAddOrRenameCategoryAsync(Category categoryToRename = null)
        {
            return await TaskHelper.RunOnUIThreadAsync(async () =>
            {
                var categoryNameDialog = new CategoryNameDialog(categoryToRename);
                await categoryNameDialog.ShowAsync();

                if (categoryNameDialog.Result == ContentDialogResult.Primary)
                {
                    return
                        (true,
                        categoryNameDialog.ViewModel.CategoryName,
                        categoryNameDialog.ViewModel.Icon);
                }

                return (false, string.Empty, CategoryIcon.Default);
            }).ConfigureAwait(false);
        }
    }
}
