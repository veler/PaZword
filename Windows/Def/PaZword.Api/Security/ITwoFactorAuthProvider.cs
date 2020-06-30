using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace PaZword.Api.Security
{
    /// <summary>
    /// Provides a set of methods to support two-factor authentication.
    /// </summary>
    public interface ITwoFactorAuthProvider
    {
        /// <summary>
        /// Check whether a two factor pin is valid.
        /// </summary>
        /// <param name="pin">The two factor pin.</param>
        /// <param name="allowedInterval">Number of interval of 30sec allowed. By default, 1 will give 1min 30sec.</param>
        /// <returns>Returns <code>True</code> if it is valid.</returns>
        bool ValidatePin(string pin, int allowedInterval = 1);

        /// <summary>
        /// Generates a new two factor authentication pin.
        /// </summary>
        /// <returns>Returns a new pin.</returns>
        string GeneratePin();

        /// <summary>
        /// Generates a QRCode to set up the two factor authentication.
        /// </summary>
        /// <param name="width">The width of the QRCode.</param>
        /// <param name="height">The height of the QRCode.</param>
        /// <param name="emailAddress">The email address used to recover a two-factor authentication pin.</param>
        /// <returns>Returns a <see cref="ImageSource"/> that contains the QRCode.</returns>
        ImageSource GetQRCode(int width, int height, string emailAddress);

        /// <summary>
        /// Defines the email address to use to send a pin when the user can't use his Smartphone.
        /// </summary>
        /// <param name="emailAddress">The email address used to recover a two-factor authentication pin.</param>
        void PersistRecoveryEmailAddressToPasswordVault(string emailAddress);

        /// <summary>
        /// Retrieves the recovery email address.
        /// </summary>
        /// <returns>Returns the recovery email address, or an empty <see cref="string"/>.</returns>
        string GetRecoveryEmailAddressFromPassowrdVault();

        /// <summary>
        /// Calls a service that sends the pin by email to the user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendPinByEmailAsync();
    }
}
