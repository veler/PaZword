using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using Dropbox.Api.Users;
using PaZword.Api;
using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Core.Threading;
using PaZword.Localization;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Core.Services.Dropbox
{
    [Export(typeof(IRemoteStorageProvider))]
    [ExportMetadata(nameof(RemoteStorageProviderMetadata.ProviderName), DropBox)]
    [Shared()]
    internal sealed class DropboxStorageProvider : IRemoteStorageProvider, IDisposable
    {
        private const string SignInEvent = "DropBox.SignIn";
        private const string SignInFaultEvent = "DropBox.SignIn.Fault";
        private const string SignInSilentlyFaultEvent = "DropBox.SignInSilently.Fault";
        private const string SignInWithInteractionFaultEvent = "DropBox.SignInWithInteraction.Fault";
        private const string GetProfilePictureFaultEvent = "DropBox.GetProfilePicture.Fault";
        private const string GetProfilePictureEvent = "DropBox.GetProfilePicture";
        private const string GetUserNameEvent = "DropBox.GetUserName";
        private const string GetEmailAddressEvent = "DropBox.GetEmailAddress";
        private const string GetFilesEvent = "DropBox.GetFiles";
        private const string DownloadFileEvent = "DropBox.DownloadFile";
        private const string DownloadFileFaultEvent = "DropBox.DownloadFile.Fault";
        private const string UploadFileEvent = "DropBox.UploadFile";
        private const string UploadFileFaultEvent = "DropBox.UploadFile.Fault";
        private const string DeleteFileEvent = "DropBox.DeleteFile";
        private const string SignOutEvent = "DropBox.SignOut";

        private const string DropBox = "Dropbox";

        private const string UserAgent = "WindowsUWPPaZword";
        private const int TransferBufferSize = 320 * 1024; // 320 KB.

        /// <summary>
        /// The setting definition used to store the user's access token.
        /// </summary>
        private readonly static SettingDefinition<string> DropBoxAccessToken = new SettingDefinition<string>(
            name: nameof(DropBoxAccessToken),
            isRoaming: false,
            defaultValue: string.Empty);

        private readonly DisposableSempahore _semaphore = new DisposableSempahore();
        private readonly ISettingsProvider _settingsProvider;
        private readonly ILogger _logger;

        private DropboxClient _dropboxClient;
        private string _oauth2State;

        public string DisplayName => LanguageManager.Instance.Dropbox.DisplayName;

        public BitmapImage ProviderIcon => new BitmapImage(new Uri("ms-appx://PaZword/Assets/RemoteStorageProviders/Dropbox.png"));

        [ImportingConstructor]
        public DropboxStorageProvider(
            ISettingsProvider settingsProvider,
            ILogger logger)
        {
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));
            _logger = Arguments.NotNull(logger, nameof(logger));
        }

        public void Dispose()
        {
            _semaphore.Dispose();
            _dropboxClient?.Dispose();
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            return Task.FromResult(
                _dropboxClient != null
                && !string.IsNullOrEmpty(_settingsProvider.GetSetting(DropBoxAccessToken)));
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

                    string accessToken = string.Empty;

                    try
                    {
                        _logger.LogEvent(SignInEvent, "Sign in silently");
                        accessToken = await SilentAuthenticateAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (!interactive)
                        {
                            _logger.LogFault(SignInSilentlyFaultEvent, "Unable to authenticate to DropBox silently.", ex);
                            await SignOutInternalAsync().ConfigureAwait(false);
                        }
                    }

                    if (interactive && string.IsNullOrEmpty(accessToken))
                    {
                        try
                        {
                            _oauth2State = Guid.NewGuid().ToString("N");

                            Uri autorizationUri = DropboxOAuth2Helper.GetAuthorizeUri(
                                OAuthResponseType.Token,
                                ServicesKeys.DropBoxAppKey,
                                new Uri(ServicesKeys.DropBoxRedirectUri),
                                state: _oauth2State);

                            WebAuthenticationResult authenticationResult = null;

                            await TaskHelper.RunOnUIThreadAsync(async () =>
                            {
                                // WebAuthenticationBroker.AuthenticateAsync should run on the UI thread.
                                _logger.LogEvent(SignInEvent, "Sign in with interaction");
                                authenticationResult
                                    = await WebAuthenticationBroker.AuthenticateAsync(
                                        WebAuthenticationOptions.None,
                                        autorizationUri,
                                        new Uri(ServicesKeys.DropBoxRedirectUri));
                            }).ConfigureAwait(false);

                            cancellationToken.ThrowIfCancellationRequested();

                            if (authenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
                            {
                                accessToken = DropboxOAuth2Helper.ParseTokenFragment(new Uri(authenticationResult.ResponseData)).AccessToken;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogFault(SignInWithInteractionFaultEvent, "Unable to authenticate to OneDrive with interaction.", ex);
                            await SignOutInternalAsync().ConfigureAwait(false);
                        }
                    }

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        // Authentication seemed to work.
                        _settingsProvider.SetSetting(DropBoxAccessToken, accessToken);
                        _dropboxClient?.Dispose();
                        _dropboxClient = new DropboxClient(accessToken, new DropboxClientConfig(UserAgent));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogFault(SignInFaultEvent, "Unable to sign in to DropBox.", ex);
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
                    FullAccount account = await _dropboxClient.Users.GetCurrentAccountAsync().ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogEvent(GetEmailAddressEvent, string.Empty);
                    return account.Email;
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
                    return await GetUserNameInternalAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            return string.Empty;
        }

        public async Task<BitmapImage> GetUserProfilePictureAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!await IsAuthenticatedAsync().ConfigureAwait(false))
                {
                    return new BitmapImage(new Uri("ms-appx://PaZword/Assets/DefaultProfilePicture.png"));
                }

                using (await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    FullAccount account = await _dropboxClient.Users.GetCurrentAccountAsync().ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrEmpty(account.ProfilePhotoUrl))
                    {
                        return new BitmapImage(new Uri("ms-appx://PaZword/Assets/DefaultProfilePicture.png"));
                    }

                    using (var httpClient = new HttpClient())
                    using (HttpResponseMessage result = await httpClient.GetAsync(new Uri(account.ProfilePhotoUrl), cancellationToken).ConfigureAwait(false))
                    {
                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            using (Stream contentStream = await result.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            using (var ras = contentStream.AsRandomAccessStream())
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
                ListFolderResult files
                    = await _dropboxClient.Files.ListFolderAsync(
                        path: string.Empty, // root of AppFolder.
                        limit: (uint)maxFileCount,
                        includeNonDownloadableFiles: false,
                        includeMediaInfo: true).ConfigureAwait(false);

                var results = new List<RemoteFileInfo>();

                for (int i = 0; i < files.Entries.Count; i++)
                {
                    if (files.Entries[i].IsFile) // if it's a file
                    {
                        FileMetadata file = files.Entries[i].AsFile;
                        results.Add(new RemoteFileInfo(file.Name, file.ServerModified));
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
                    using (IDownloadResponse<FileMetadata> downloadResponse = await _dropboxClient.Files.DownloadAsync("/" + remoteFullPath).ConfigureAwait(false))
                    using (Stream remoteFileContentStream = await downloadResponse.GetContentAsStreamAsync().ConfigureAwait(false))
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
                        FileMetadata uploadResult = null;
                        long localFileSize = localFileContentStream.Length;

                        int chunksCount = (int)Math.Ceiling((double)localFileSize / TransferBufferSize);

                        if (chunksCount <= 1)
                        {
                            // File is too small for uploading by chunks. Let's upload in 1 shot.
                            uploadResult = await _dropboxClient.Files.UploadAsync(
                                new CommitInfo(
                                    path: "/" + localFile.Name,
                                    mode: WriteMode.Overwrite.Instance),
                                localFileContentStream)
                            .ConfigureAwait(false);
                        }
                        else
                        {
                            // For files > 150 MB, UploadAsync won't work, so we use an upload session in case.
                            // It is unlikely that a user data file will be 150 MB but we never know.
                            // In fact, here we use upload session for any file bigger than the TransferBufferSize.
                            uploadResult = await UploadFileInChunksAsync(
                                chunksCount,
                                localFileContentStream,
                                localFile.Name)
                            .ConfigureAwait(false);
                        }

                        return uploadResult != null && uploadResult.Size == (ulong)localFileSize;
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
                await _dropboxClient.Files.DeleteV2Async("/" + remoteFullPath).ConfigureAwait(false);
            }

            _logger.LogEvent(DeleteFileEvent, string.Empty);
        }

        private async Task<string> SilentAuthenticateAsync(CancellationToken cancellationToken)
        {
            string accessToken = _settingsProvider.GetSetting(DropBoxAccessToken);

            if (string.IsNullOrEmpty(accessToken))
            {
                return string.Empty;
            }

            _dropboxClient?.Dispose();
            _dropboxClient = new DropboxClient(accessToken, new DropboxClientConfig(UserAgent));

            if (string.IsNullOrEmpty(await GetUserNameInternalAsync(cancellationToken).ConfigureAwait(false)))
            {
                return string.Empty;
            }

            return accessToken;
        }

        private Task SignOutInternalAsync()
        {
            _dropboxClient?.Dispose();
            _dropboxClient = null;
            _settingsProvider.ResetSetting(DropBoxAccessToken);

            _logger.LogEvent(SignOutEvent, string.Empty);
            return Task.CompletedTask;
        }

        private async Task<string> GetUserNameInternalAsync(CancellationToken cancellationToken)
        {
            FullAccount account = await _dropboxClient.Users.GetCurrentAccountAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogEvent(GetUserNameEvent, string.Empty);
            return account.Name.DisplayName;
        }

        private async Task<FileMetadata> UploadFileInChunksAsync(int chunksCount, Stream localFileContentStream, string fileName)
        {
            byte[] buffer = new byte[TransferBufferSize];
            string sessionId = null;
            FileMetadata uploadResult = null;

            for (var idx = 0; idx < chunksCount; idx++)
            {
                var byteRead = localFileContentStream.Read(buffer, 0, TransferBufferSize);

                using (MemoryStream memStream = new MemoryStream(buffer, 0, byteRead))
                {
                    if (idx == 0)
                    {
                        var result = await _dropboxClient.Files.UploadSessionStartAsync(body: memStream).ConfigureAwait(false);
                        sessionId = result.SessionId;

                        if (string.IsNullOrEmpty(sessionId))
                        {
                            return null;
                        }
                    }
                    else
                    {
                        UploadSessionCursor cursor = new UploadSessionCursor(sessionId, (ulong)(TransferBufferSize * idx));

                        if (idx == chunksCount - 1)
                        {
                            uploadResult = await _dropboxClient.Files.UploadSessionFinishAsync(
                                cursor,
                                new CommitInfo(
                                    path: "/" + fileName,
                                    mode: WriteMode.Overwrite.Instance),
                                memStream)
                            .ConfigureAwait(false);
                        }
                        else
                        {
                            await _dropboxClient.Files.UploadSessionAppendV2Async(cursor, body: memStream).ConfigureAwait(false);
                        }
                    }
                }
            }

            return uploadResult;
        }
    }
}
