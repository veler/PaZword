using Microsoft.Graph;
using Microsoft.Identity.Client;
using PaZword.Api;
using PaZword.Api.Services;
using PaZword.Core.Threading;
using PaZword.Localization;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Core.Services.OneDrive
{
    [Export(typeof(IRemoteStorageProvider))]
    [ExportMetadata(nameof(RemoteStorageProviderMetadata.ProviderName), OneDrive)]
    [Shared()]
    internal sealed class OneDriveStorageProvider : IRemoteStorageProvider, IDisposable
    {
        private const string SignInEvent = "OneDrive.SignIn";
        private const string SignInFaultEvent = "OneDrive.SignIn.Fault";
        private const string SignInSilentlyFaultEvent = "OneDrive.SignInSilently.Fault";
        private const string SignInWithInteractionFaultEvent = "OneDrive.SignInWithInteraction.Fault";
        private const string GetProfilePictureEvent = "OneDrive.GetProfilePicture";
        private const string GetProfilePictureFaultEvent = "OneDrive.GetProfilePicture.Fault";
        private const string GetUserNameEvent = "OneDrive.GetUserName";
        private const string GetEmailAddressEvent = "OneDrive.GetEmailAddress";
        private const string GetFilesEvent = "OneDrive.GetFiles";
        private const string DownloadFileEvent = "OneDrive.DownloadFile";
        private const string DownloadFileFaultEvent = "OneDrive.DownloadFile.Fault";
        private const string UploadFileEvent = "OneDrive.UploadFile";
        private const string UploadFileFaultEvent = "OneDrive.UploadFile.Fault";
        private const string DeleteFileEvent = "OneDrive.DeleteFile";
        private const string SignOutEvent = "OneDrive.SignOut";

        private const string OneDrive = "OneDrive";

        private const string GraphBaseUrl = "https://graph.microsoft.com/beta"; // https://graph.microsoft.com/v1.0
        private const string AuthenticationHeaderScheme = "bearer";
        private const int TokenExpirationMargin = 10; // minutes
        private const int TransferBufferSize = 320 * 1024; // 320 KB. This is an API limitation. The buffer should be a multiple of 320.

        public static readonly string[] Scopes =
        {
            "User.Read",
            "User.ReadBasic.All",
            "Files.ReadWrite.AppFolder"
        };

        private readonly DisposableSempahore _semaphore = new DisposableSempahore();
        private readonly ILogger _logger;

        private readonly IPublicClientApplication _identityClientApp = PublicClientApplicationBuilder
            .Create(ServicesKeys.OneDriveClientId)
            .WithRedirectUri(ServicesKeys.OneDriveRedirectUri)
            .Build();

        private GraphServiceClient _graphClient = null;
        private DateTimeOffset _userTokenExpiration;
        private SecureString _userToken;

        public string DisplayName => LanguageManager.Instance.OneDrive.DisplayName;

        public BitmapImage ProviderIcon => new BitmapImage(new Uri("ms-appx://PaZword/Assets/RemoteStorageProviders/OneDrive.png"));

        [ImportingConstructor]
        public OneDriveStorageProvider(ILogger logger)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
        }

        public void Dispose()
        {
            _semaphore.Dispose();
            _userToken?.Dispose();
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            return Task.FromResult(
                _graphClient != null
                && !StringExtensions.IsNullOrEmptySecureString(_userToken)
                && _userTokenExpiration > DateTimeOffset.UtcNow.AddMinutes(TokenExpirationMargin)); // if the token expires in more than N min.
        }

        public async Task<bool> SignInAsync(bool interactive, CancellationToken cancellationToken)
        {
            using (await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    if (await IsAuthenticatedAsync().ConfigureAwait(false))
                    {
                        // Already authenticated.
                        _logger.LogEvent(SignInEvent, "Already signed in");
                        return true;
                    }

                    if (_graphClient == null)
                    {
                        _graphClient = new GraphServiceClient(
                            GraphBaseUrl,
                            new DelegateAuthenticationProvider(AuthenticateRequestAsync));
                    }

                    AuthenticationResult authenticationResult = null;

                    try
                    {
                        _logger.LogEvent(SignInEvent, "Sign in silently");
                        authenticationResult = await SilentAuthenticateAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (!interactive)
                        {
                            _logger.LogFault(SignInSilentlyFaultEvent, "Unable to authenticate to OneDrive silently.", ex);
                            await SignOutInternalAsync().ConfigureAwait(false);
                        }
                    }

                    if (interactive
                        && (authenticationResult == null
                            || string.IsNullOrEmpty(authenticationResult.AccessToken)
                            || authenticationResult.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(TokenExpirationMargin))) // if the token expires in less than N min
                    {
                        try
                        {
                            _logger.LogEvent(SignInEvent, "Sign in with interaction");
                            authenticationResult = await InteractiveAuthenticationAsync(cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogFault(SignInWithInteractionFaultEvent, "Unable to authenticate to OneDrive with interaction.", ex);
                            await SignOutInternalAsync().ConfigureAwait(false);
                        }
                    }

                    if (authenticationResult != null)
                    {
                        // Authentication seemed to work.
                        _userToken = authenticationResult.AccessToken.ToSecureString();
                        _userTokenExpiration = authenticationResult.ExpiresOn;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogFault(SignInFaultEvent, "Unable to sign in to OneDrive.", ex);
                }

                bool isAuthenticated = await IsAuthenticatedAsync().ConfigureAwait(false);
                if (isAuthenticated)
                {
                    _logger.LogEvent(SignInEvent, "Signed in successfully");
                }
                else
                {
                    _logger.LogEvent(SignInFaultEvent, "It seemed to failed without exception.");
                }

                return isAuthenticated;
            }
        }

        public async Task SignOutAsync()
        {
            using (await _semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                await SignOutInternalAsync().ConfigureAwait(false);
            }
        }

        public async Task<string> GetUserEmailAddressAsync(CancellationToken cancellationToken)
        {
            if (await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                using (await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    User user = await _graphClient.Me.Request().GetAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogEvent(GetEmailAddressEvent, string.Empty);
                    return user.UserPrincipalName;
                }
            }

            return string.Empty;
        }

        public async Task<string> GetUserNameAsync(CancellationToken cancellationToken)
        {
            if (await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                using (await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    User user = await _graphClient.Me.Request().GetAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogEvent(GetUserNameEvent, string.Empty);
                    return user.DisplayName;
                }
            }

            return string.Empty;
        }

        public async Task<BitmapImage> GetUserProfilePictureAsync(CancellationToken cancellationToken)
        {
            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                return new BitmapImage(new Uri("ms-appx://PaZword/Assets/DefaultProfilePicture.png"));
            }

            try
            {
                using (await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    User user = await _graphClient.Me.Request().GetAsync(cancellationToken).ConfigureAwait(false);

                    using (Stream stream = await (_graphClient
                        .Users[user.UserPrincipalName]?
                        .Photo?
                        .Content?
                        .Request()
                        .WithMaxRetry(3)
                        .GetAsync(cancellationToken)).ConfigureAwait(false))
                    using (var ras = stream.AsRandomAccessStream())
                    {
                        return await TaskHelper.RunOnUIThreadAsync(async () =>
                        {
                            var bitmap = new BitmapImage();
                            await bitmap.SetSourceAsync(ras);
                            _logger.LogEvent(GetProfilePictureEvent, string.Empty);
                            return bitmap;
                        }).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogFault(
                    GetProfilePictureFaultEvent,
                    "Unable to retrieve the profile picture from a Microsoft account, probably because it is a personal account and not a work or school account.",
                    ex);
            }

            return new BitmapImage(new Uri("ms-appx://PaZword/Assets/DefaultProfilePicture.png"));
        }

        public async Task<IReadOnlyList<RemoteFileInfo>> GetFilesAsync(int maxFileCount, CancellationToken cancellationToken)
        {
            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                return Array.Empty<RemoteFileInfo>();
            }

            using (await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                IDriveItemChildrenCollectionPage files
                    = await _graphClient.Drive.Special.AppRoot.Children
                        .Request()
                        .Top(maxFileCount)
                        .GetAsync(cancellationToken).ConfigureAwait(false);

                var results = new List<RemoteFileInfo>();

                for (int i = 0; i < files.Count; i++)
                {
                    DriveItem file = files[i];
                    if (file.File != null) // if it's a file
                    {
                        results.Add(new RemoteFileInfo(file.Name, file.LastModifiedDateTime.GetValueOrDefault()));
                    }
                }

                _logger.LogEvent(GetFilesEvent, string.Empty);

                return results;
            }
        }

        public async Task<StorageFile> DownloadFileAsync(string remoteFullPath, CancellationToken cancellationToken)
        {
            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                return null;
            }

            try
            {
                StorageFile result = await CoreHelper.RetryAsync(async () =>
                {
                    using (await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false))
                    using (Stream remoteFileContentStream = await _graphClient.Drive.Special.AppRoot
                        .ItemWithPath(remoteFullPath)
                        .Content.Request().GetAsync(cancellationToken).ConfigureAwait(false))
                    {
                        string fileName = Path.GetFileName(remoteFullPath);
                        var localUserDataFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
                        StorageFile localFile = await localUserDataFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                        using (IRandomAccessStream localFileContent = await localFile.OpenAsync(FileAccessMode.ReadWrite))
                        using (Stream localFileContentStream = localFileContent.AsStreamForWrite())
                        {
                            await remoteFileContentStream.CopyToAsync(localFileContentStream, bufferSize: TransferBufferSize, cancellationToken).ConfigureAwait(false);
                            await localFileContentStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                            await localFileContent.FlushAsync();
                        }

                        return localFile;
                    }
                }).ConfigureAwait(false);

                _logger.LogEvent(DownloadFileEvent, $"File downloaded.");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogFault(DownloadFileFaultEvent, "Unable to download a file.", ex);
                return null;
            }
        }

        public async Task<bool> UploadFileAsync(StorageFile localFile, CancellationToken cancellationToken)
        {
            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                return false;
            }

            try
            {
                bool uploadSucceeded = await CoreHelper.RetryAsync(async () =>
                {
                    using (await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false))
                    using (IRandomAccessStream localFileContent = await localFile.OpenReadAsync())
                    using (Stream localFileContentStream = localFileContent.AsStreamForRead())
                    {
                        // the main data file may be > 4 MB, so we use an upload session in case.
                        UploadSession uploadSession = await _graphClient.Drive.Special.AppRoot
                            .ItemWithPath(localFile.Name)
                            .CreateUploadSession().Request()
                            .PostAsync(cancellationToken).ConfigureAwait(false);

                        var largeFileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, localFileContentStream, TransferBufferSize);

                        UploadResult<DriveItem> uploadedFile = await largeFileUploadTask.UploadAsync().ConfigureAwait(false);

                        return uploadedFile.UploadSucceeded;
                    }
                }).ConfigureAwait(false);

                _logger.LogEvent(UploadFileEvent, $"Upload succeeded == {uploadSucceeded}");

                return uploadSucceeded;
            }
            catch (Exception ex)
            {
                _logger.LogFault(UploadFileFaultEvent, "Unable to upload a file.", ex);
                return false;
            }
        }

        public async Task DeleteFileAsync(string remoteFullPath, CancellationToken cancellationToken)
        {
            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                return;
            }

            using (await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await _graphClient.Drive.Special.AppRoot
                .ItemWithPath(remoteFullPath).Request()
                .DeleteAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogEvent(DeleteFileEvent, string.Empty);
        }

        private async Task<AuthenticationResult> SilentAuthenticateAsync(CancellationToken cancellationToken)
        {
            // Try to authenticate silently by using the token from the cache.
            IAccount account = (await _identityClientApp.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();

            if (account == null)
            {
                return null;
            }

            return await CoreHelper.RetryAsync(async () =>
            {
                return await _identityClientApp
                    .AcquireTokenSilent(Scopes, account)
                    .WithForceRefresh(true)
                    .ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> InteractiveAuthenticationAsync(CancellationToken cancellationToken)
        {
            // Try to authenticate by asking the user to enter its credentials.
            return await _identityClientApp
                .AcquireTokenInteractive(Scopes)
                .ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        private Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationHeaderScheme, _userToken.ToUnsecureString());
            return Task.CompletedTask;
        }

        private async Task SignOutInternalAsync()
        {
            IEnumerable<IAccount> accounts = await _identityClientApp.GetAccountsAsync().ConfigureAwait(false);

            foreach (IAccount account in accounts)
            {
                // Clear all the tokens in cache.
                await _identityClientApp.RemoveAsync(account).ConfigureAwait(false);
            }

            _graphClient = null;
            _userToken?.Dispose();
            _userToken = null;
            _logger.LogEvent(SignOutEvent, string.Empty);
        }
    }
}
