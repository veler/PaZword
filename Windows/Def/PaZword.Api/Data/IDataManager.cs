using PaZword.Api.Collections;
using PaZword.Api.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace PaZword.Api.Data
{
    /// <summary>
    /// Provides a set of methods to manage the user's data.
    /// </summary>
    public interface IDataManager : IDisposable
    {
        /// <summary>
        /// Gets the list of categories in the user data.
        /// </summary>
        ConcurrentObservableCollection<Category> Categories { get; }

        /// <summary>
        /// Gets whether a <see cref="UserDataBundle"/> is loaded.
        /// </summary>
        bool HasUserDataBundleLoaded { get; }

        /// <summary>
        /// Deletes the data files on the local machine and clear the categories and account in memory in this instance.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ClearLocalDataAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Tries to load the user data bundle on the local machine and decrypt it but doesn't keep it in memory.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns <code>True</code> if it succeed, or throws an exception if it fails.</returns>
        Task<bool> TryOpenLocalUserDataBundleAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Loads the user data bundle on the local machine, or create a default one.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task LoadOrCreateLocalUserDataBundleAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Saves the user data bundle to the hard drive.
        /// </summary>
        /// <param name="synchronize">Defines whether the data should be synchronized with the cloud once saved.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SaveLocalUserDataBundleAsync(bool synchronize, CancellationToken cancellationToken);

        /// <summary>
        /// Encrypts and saves the specified <paramref name="file"/>'s data to the local hard drive.
        /// </summary>
        /// <param name="fileDataId">The ID of the file, which will be use to name the encrypted generated file.</param>
        /// <param name="file">The file to encrypt.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SaveFileDataAsync(Guid fileDataId, StorageFile file);

        /// <summary>
        /// Load the specified encrypted file from the local hard drive, decrypts it and returns its content.
        /// </summary>
        /// <param name="fileDataId">The ID of the encrypted file.</param>
        /// <returns>The decrypted file content.</returns>
        Task<byte[]> LoadFileDataAsync(Guid fileDataId);

        /// <summary>
        /// Delete permanently the specified file from the local hard drive.
        /// </summary>
        /// <param name="fileDataId">The ID of the file to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteFileDataAsync(Guid fileDataId);

        /// <summary>
        /// Gets a given data from the brown bag.
        /// </summary>
        /// <param name="name">The name of the data.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns <see cref="string.Empty"/> if no data found.</returns>
        Task<string> GetBrownBagDataAsync(string name, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the list of data from the brown bag matching the given <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The contition to match to get the data.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns the list of data matching.</returns>
        Task<IReadOnlyDictionary<string, string>> GetBrownBagDataAsync(Predicate<string> predicate, CancellationToken cancellationToken);

        /// <summary>
        /// Adds, replaces or removes a given data in the brown bag.
        /// </summary>
        /// <remarks>
        /// Use this as a way to keep some settings or non-sensitive data accross several devices
        /// and linked to this <see cref="UserDataBundle"/>.
        /// Only store small data. Do NOT store sensitive data here.
        /// </remarks>
        /// <param name="name">The name of the data</param>
        /// <param name="value">The data.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SetBrownBagDataAsync(string name, string value, CancellationToken cancellationToken);

        /// <summary>
        /// Performs a search into the accounts and returns the matching items.
        /// </summary>
        /// <param name="categoryId">The id of the <see cref="Category"/> where the search must be performed.</param>
        /// <param name="query">The partial non-case sensitive name of the account to search.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>The list of matching accounts</returns>
        Task<ConcurrentObservableCollection<Account>> SearchAsync(Guid categoryId, string query, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a new category to the bundle.
        /// </summary>
        /// <param name="name">The category name.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>The created <see cref="Category"/>.</returns>
        Task<Category> AddNewCategoryAsync(string name, CancellationToken cancellationToken);

        /// <summary>
        /// Renames a category in the bundle.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <param name="name">The category name.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RenameCategoryAsync(Guid id, string name, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a category from the bundle.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the category that match the specified ID.
        /// </summary>
        /// <param name="id">The ID of the category to look for.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns the found <see cref="Category"/> or null if it doesn't find anything.</returns>
        Task<Category> GetCategoryAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a new account to the bundle.
        /// </summary>
        /// <param name="account">The account to add.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddNewAccountAsync(Account account, CancellationToken cancellationToken);

        /// <summary>
        /// Delete the specified account from the bundle.
        /// </summary>
        /// <param name="account">The account to delete</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteAccountAsync(Account account, CancellationToken cancellationToken);

        /// <summary>
        /// Update an existing account by replacing it by the new one.
        /// </summary>
        /// <param name="oldAccount">Old account to remove.</param>
        /// <param name="newAccount">New account to add.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UpdateAccountAsync(Account oldAccount, Account newAccount, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the account that match the specified ID.
        /// </summary>
        /// <param name="id">The ID of the account to look for.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns the found <see cref="Account"/> or null if it doesn't find anything.</returns>
        Task<Account> GetAccountAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Generates a unique ID for a category, account or account data.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns a unique <see cref="Guid"/> that can be used to identify a category, account or account data.</returns>
        Task<Guid> GenerateUniqueIdAsync(CancellationToken cancellationToken);
    }
}
