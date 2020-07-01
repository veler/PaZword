using Microsoft.Graph;
using PaZword.Api;
using PaZword.Api.Security;
using PaZword.Core.Threading;
using System;
using System.Composition;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.UI.Core;

namespace PaZword.Core.Security
{
    [Export(typeof(IWindowsHelloAuthProvider))]
    [Shared()]
    internal sealed class WindowsHelloAuthProvider : IWindowsHelloAuthProvider
    {
        private const string AuthenticateEvent = "WindowsHello.Authenticate";

        private const string KeyCredentialManagerName = "PaZword";

        private readonly ILogger _logger;

        private TaskCompletionSource<bool> _windowFocusAwaiter = new TaskCompletionSource<bool>();

        [ImportingConstructor]
        public WindowsHelloAuthProvider(
            ILogger logger)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));

            TaskHelper.RunOnUIThreadAsync(() =>
            {
                CoreApplication.MainView.CoreWindow.Activated += CoreWindow_Activated;
            }).Forget();
        }

        public async Task<bool> IsWindowsHelloEnabledAsync()
            => await KeyCredentialManager.IsSupportedAsync();

        public async Task<bool> AuthenticateAsync()
        {
            // Waits that the app's window gets the focus. This allows to avoid having the icon in the task bar
            // blinking orange when windows hello starts. It might distracts the user.
            await _windowFocusAwaiter.Task.ConfigureAwait(false);

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

        private void CoreWindow_Activated(CoreWindow sender, WindowActivatedEventArgs args)
        {
            _windowFocusAwaiter.TrySetResult(true);
            if (args.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                _windowFocusAwaiter = new TaskCompletionSource<bool>();
            }
        }
    }
}
