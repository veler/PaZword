using PaZword.Api.Models;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace PaZword.Api.ViewModels.Data
{
    /// <summary>
    /// Provides a set of methods and properties to implement an account data's view model.
    /// </summary>
    public interface IAccountDataViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the localized title of the account data to show in the list.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets or sets a value that defines whether the current account is in editing mode.
        /// </summary>
        bool IsEditing { get; set; }

        /// <summary>
        /// Gets the user interface for this account data.
        /// </summary>
        FrameworkElement UserInterface { get; }

        /// <summary>
        /// Gets or sets the data to manage/display.
        /// </summary>
        AccountData Data { get; set; }

        /// <summary>
        /// Gets or sets the data to manager/display in the editing mode.
        /// </summary>
        AccountData DataEditMode { get; set; }

        /// <summary>
        /// Generates the sub title of the account. This sub title will be displayed below the account title in the list of accounts.
        /// </summary>
        /// <remarks>
        /// This method is called in Edit mode.
        /// The string shouldn't be longer than 64 characters. A longer string will be trimmed.
        /// DO NOT return very sensitive data clearly, such like a credit card number for example.
        /// </remarks>
        /// <returns>Returns a localized sub title, or an empty string.</returns>
        string GenerateSubtitle();

        /// <summary>
        /// Validates that user inputs are correct before <see cref="SaveAsync"/> is called.
        /// </summary>
        /// <remarks>This method is called in Edit mode.</remarks>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns <code>True</code> if everything is good.</returns>
        Task<bool> ValidateChangesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Called when the user is saving an account. This method can be use to perform additional treatment when validating a data.
        /// </summary>
        /// <remarks>This method is called in Edit mode.</remarks>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SaveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Called when the user is deleting an account or the account data (and that the user saves the changes).
        /// This method can be use to perform additional treatment when deleting a data.
        /// </summary>
        /// <remarks>This method is called in Edit mode and when deleting an entire account.</remarks>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Calls when the view models won't be used anymore
        /// either because it's not displayed in the UI anymore or because it will be deleted through <see cref="DeleteAsync(CancellationToken)"/>.
        /// This is different from a <see cref="IDisposable"/> because this method
        /// should NOT dispose the <see cref="Data"/> and <see cref="DataEditMode"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UnloadingAsync();
    }
}
