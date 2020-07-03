using PaZword.Api.Models;
using System.Threading.Tasks;
using Windows.Storage;

namespace PaZword.Api.Data
{
    /// <summary>
    /// Provides a set of methods for upgrate data and settings.
    /// </summary>
    public interface IUpgradeService
    {
        /// <summary>
        /// Gets a number representing the version of user data bundle supported by the current instance of PaZword.
        /// </summary>
        string CurrentUserBundleVersion { get; }

        /// <summary>
        /// Migrates the user data.
        /// </summary>
        /// <param name="userDataBundleFile">The file containing the user data bundle to migrate.</param>
        /// <returns>A up to date user data bundle and a value indicated if it had to be updated.</returns>
        Task<(bool updated, UserDataBundle userDataBundle)> UpgradeUserDataBundleAsync(StorageFile userDataBundleFile);

        /// <summary>
        /// Migrates the application settings.
        /// </summary>
        void UpgradeSettings();
    }
}
