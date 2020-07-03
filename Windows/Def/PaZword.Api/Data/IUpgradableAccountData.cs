using System.Threading.Tasks;

namespace PaZword.Api.Data
{
    /// <summary>
    /// Provides a method that helps to upgrade an <see cref="AccountData"/>.
    /// </summary>
    public interface IUpgradableAccountData
    {
        /// <summary>
        /// Upgrades the current instance of <see cref="IUpgradableAccountData"/> implementation.
        /// </summary>
        /// <param name="oldVersion">The detected version of the user data bundle that has been loaded.</param>
        /// <param name="targetVersion">The version to which the current account data should be upgraded to.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UpgradeAsync(int oldVersion, int targetVersion);
    }
}
