using PaZword.Api;
using PaZword.Api.Security;
using System;
using System.Composition;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;

namespace PaZword.Core.Security
{
    [Export(typeof(IWindowsHelloAuthProvider))]
    [Shared()]
    internal sealed class WindowsHelloAuthProvider : IWindowsHelloAuthProvider
    {
        private const string AuthenticateEvent = "WindowsHello.Authenticate";

        private const string KeyCredentialManagerName = "PaZword";

        private readonly ILogger _logger;

        [ImportingConstructor]
        public WindowsHelloAuthProvider(
            ILogger logger)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
        }

        public async Task<bool> IsWindowsHelloEnabledAsync()
            => await KeyCredentialManager.IsSupportedAsync();

        public async Task<bool> AuthenticateAsync()
        {
            if (!await IsWindowsHelloEnabledAsync()
                .ConfigureAwait(true)) // run on the current context.
            {
                _logger.LogEvent(AuthenticateEvent, "Success == False; Windows Hello isn't enabled.");
                return false;
            }

            // Get credentials for current user and app
            KeyCredentialRetrievalResult result = await KeyCredentialManager.OpenAsync(KeyCredentialManagerName);
            if (result.Credential != null)
            {
                // Prompt the user to authenticate.
                KeyCredentialOperationResult signResult = await result.Credential.RequestSignAsync(CryptographicBuffer.ConvertStringToBinary(KeyCredentialManagerName, BinaryStringEncoding.Utf8));
                if (signResult.Status == KeyCredentialStatus.Success)
                {
                    _logger.LogEvent(AuthenticateEvent, "Success == True");
                    return true;
                }

                _logger.LogEvent(AuthenticateEvent, "Success == False");
                return false;
            }

            // No previous saved credentials found, let's create them (this will also prompt the user to authenticate).
            KeyCredentialRetrievalResult creationResult = await KeyCredentialManager.RequestCreateAsync(KeyCredentialManagerName, KeyCredentialCreationOption.ReplaceExisting);

            _logger.LogEvent(AuthenticateEvent, $"Success == {creationResult.Status == KeyCredentialStatus.Success}");
            return creationResult.Status == KeyCredentialStatus.Success;
        }
    }
}
