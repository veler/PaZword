using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace PaZword.Api.UI
{
    /// <summary>
    /// Provides a set of methods to manager windows and dialogs.
    /// </summary>
    public interface IWindowManager
    {
        /// <summary>
        /// Prompt a message dialog to the user.
        /// </summary>
        /// <param name="message">The message to show</param>
        /// <param name="closeButtonText">The text of the close button.</param>
        /// <param name="primaryButtonText">The text of the primary button. If null or empty, the button will be hidden.</param>
        /// <param name="secondaryButtonText">The text of the secondary button. If null or empty, the button will be hidden.</param>
        /// <param name="title">The title of the message. If null, use the application name.</param>
        /// <param name="defaultButton">The button that should have the focus by default.</param>
        /// <returns>Returns a <see cref="ContentDialogResult"/> based on what the user clicked.</returns>
        Task<ContentDialogResult> ShowMessageDialogAsync(
            string message,
            string closeButtonText,
            string primaryButtonText = null,
            string secondaryButtonText = null,
            string title = null,
            ContentDialogButton defaultButton = ContentDialogButton.Close);

        /// <summary>
        /// Prompt a dialog to the user that asks they to type a string value.
        /// </summary>
        /// <param name="defaultInputValue">The default value to display</param>
        /// <param name="placeHolder">The placeholder to show in the text input control.</param>
        /// <param name="primaryButtonText">The text of the validation button.</param>
        /// <param name="title">The title of the message. If null, use the application name.</param>
        /// <returns>Returns the value that the user typed. Returns empty string if the user canceled the action.</returns>
        Task<string> ShowInputDialogAsync(
            string placeHolder,
            string primaryButtonText,
            string defaultInputValue = null,
            string title = null);
    }
}
