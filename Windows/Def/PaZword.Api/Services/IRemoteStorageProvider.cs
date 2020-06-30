using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Api.Services
{
    /// <summary>
    /// Provides a set of methods to interact with a cloud user service account, such like Microsoft or DropBox account.
    /// </summary>
    public interface IRemoteStorageProvider
    {
        /// <summary>
        /// Gets the localized account provider name to display in the user interface.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets the account provider account to display in the user interface.
        /// </summary>
        BitmapImage ProviderIcon { get; }

        /// <summary>
        /// Retrieves a value that defines whether the user is authenticated or not.
        /// </summary>
        /// <returns>Returns <code>True</code> if the user is logged in.</returns>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// Sign in the user online with information from the cache, or by asking the user to enter its credentials.
        /// </summary>
        /// <param name="interactive">
        /// Defines whether an interactive authentication (with UI) should be used if it
        /// fails to authenticate with the cached information.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns <code>True</code> if it succeeded.</returns>
        Task<bool> SignInAsync(bool interactive, CancellationToken cancellationToken);

        /// <summary>
        /// Sign out the user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SignOutAsync();

        /// <summary>
        /// Gets the user's display name.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>The user name, or an empty string.</returns>
        Task<string> GetUserNameAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the user's display email address.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>The user email address, or an empty string.</returns>
        Task<string> GetUserEmailAddressAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the avatar of the user account to display in the UI.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns a <see cref="BitmapImage"/> corresponding to the user's profile picture, or null.</returns>
        Task<BitmapImage> GetUserProfilePictureAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the list of user data files related to PaZword available on the server.
        /// </summary>
        /// <param name="maxFileCount">Defines the maximum amount of files to retrieve.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns a list of <see cref="RemoteFileInfo"/>.</returns>
        Task<IReadOnlyList<RemoteFileInfo>> GetFilesAsync(int maxFileCount, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads the given file from the server and store it in the local application storage. It replaces the existing local file.
        /// </summary>
        /// <param name="remoteFullPath">The full path to the file on the server.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns the local <see cref="StorageFile"/> that has been created, or null if the task failed to download.</returns>
        Task<StorageFile> DownloadFileAsync(string remoteFullPath, CancellationToken cancellationToken);

        /// <summary>
        /// Uploads the given file to the server and replace the existing one, if exists.
        /// </summary>
        /// <param name="localFile">The local file to upload.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Returns <code>True</code> if the file has been uploaded correctly.</returns>
        Task<bool> UploadFileAsync(StorageFile localFile, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a given file from the server.
        /// </summary>
        /// <param name="remoteFullPath">The full path to the file on the server.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteFileAsync(string remoteFullPath, CancellationToken cancellationToken);
    }
}
