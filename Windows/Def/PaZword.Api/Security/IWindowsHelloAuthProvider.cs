using System.Threading.Tasks;

namespace PaZword.Api.Security
{
    /// <summary>
    /// Provides a set of methods to use Windows Hello.
    /// </summary>
    public interface IWindowsHelloAuthProvider
    {
        /// <summary>
        /// Determines whether Windows Hello is enabled on the current device.
        /// </summary>
        /// <returns>Returns <code>True</code> if Windows Hello is enabled.</returns>
        Task<bool> IsWindowsHelloEnabledAsync();

        /// <summary>
        /// Tries to authenticates the user with Windows Hello.
        /// </summary>
        /// <remarks>
        /// Be sure to run this method on the UI context otherwise Windows Hello will appear on a separated window in the
        /// task bar and won't lock the app's UI.
        /// </remarks>
        /// <returns>Returns <code>True</code> if the authentication succeeded. Returns <code>False</code> if it fails or if Windows Hello is disabled.</returns>
        Task<bool> AuthenticateAsync();
    }
}
