using PaZword.Api.Models;
using PaZword.Api.UI;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace PaZword.Tests.Mocks
{
    internal sealed class MockIWindowManager : IWindowManager
    {
        internal string InputDialogResult { get; set; }

        internal ContentDialogResult MessageDialogResult { get; set; }

        public Task<(bool, string, CategoryIcon)> ShowAddOrRenameCategoryAsync(Category categoryToRename = null)
        {
            if (categoryToRename == null)
            {
                return Task.FromResult((true, "New Category", CategoryIcon.Default));
            }

            return Task.FromResult((true, "Renamed", CategoryIcon.Default));
        }

        public Task<string> ShowInputDialogAsync(string defaultInputValue, string placeHolder, string primaryButtonText, string title = null)
        {
            return Task.FromResult(InputDialogResult);
        }

        public Task<ContentDialogResult> ShowMessageDialogAsync(string message, string closeButtonText, string primaryButtonText = null, string secondaryButtonText = null, string title = null, ContentDialogButton defaultButton = ContentDialogButton.Close)
        {
            return Task.FromResult(MessageDialogResult);
        }
    }
}
