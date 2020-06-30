using PaZword.Api.Services;
using PaZword.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Tests.Mocks
{
    class MockIRemoteStorageProvider : IRemoteStorageProvider
    {
        private readonly List<RemoteFileInfo> _filesOnTheServer = new List<RemoteFileInfo>();
        private bool _isAuthenticated;

        public bool SignInAsyncResult { get; set; } = true;

        public string DisplayName => "Foo";

        public BitmapImage ProviderIcon => null;

        public async Task DeleteFileAsync(string remoteFullPath, CancellationToken cancellationToken)
        {
            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                return;
            }

            // simulate latency.
            await Task.Delay(250).ConfigureAwait(false);

            var fileToDelete = _filesOnTheServer.FirstOrDefault(f => string.Equals(f.FullPath, remoteFullPath, StringComparison.Ordinal));
            _filesOnTheServer.Remove(fileToDelete);

            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task<StorageFile> DownloadFileAsync(string remoteFullPath, CancellationToken cancellationToken)
        {
            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                return null;
            }

            var fileToDownload = _filesOnTheServer.FirstOrDefault(f => string.Equals(f.FullPath, remoteFullPath, StringComparison.Ordinal));
            if (fileToDownload == RemoteFileInfo.Empty)
            {
                return null;
            }

            // simulate latency.
            await Task.Delay(250).ConfigureAwait(false);

            string fileName = Path.GetFileName(fileToDownload.FullPath);
            var localFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
            StorageFile localFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            cancellationToken.ThrowIfCancellationRequested();
            return localFile;
        }

        public async Task<IReadOnlyList<RemoteFileInfo>> GetFilesAsync(int maxFileCount, CancellationToken cancellationToken)
        {
            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                return Array.Empty<RemoteFileInfo>();
            }

            // simulate latency.
            await Task.Delay(250).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return _filesOnTheServer;
        }

        public Task<string> GetUserEmailAddressAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserNameAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<BitmapImage> GetUserProfilePictureAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            return Task.FromResult(_isAuthenticated);
        }

        public Task<bool> SignInAsync(bool interactive, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _isAuthenticated = SignInAsyncResult;
            return Task.FromResult(SignInAsyncResult);
        }

        public Task SignOutAsync()
        {
            _isAuthenticated = false;
            return Task.CompletedTask;
        }

        public async Task<bool> UploadFileAsync(StorageFile localFile, CancellationToken cancellationToken)
        {
            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                return false;
            }

            // simulate latency.
            await Task.Delay(250).ConfigureAwait(false);

            await DeleteFileAsync(localFile.Name, cancellationToken).ConfigureAwait(false);
            _filesOnTheServer.Add(new RemoteFileInfo(localFile.Name, new DateTimeOffset(DateTime.UtcNow)));

            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }

        internal void SetFilesOnServer(params RemoteFileInfo[] remoteFileInfos)
        {
            _filesOnTheServer.Clear();
            _filesOnTheServer.AddRange(remoteFileInfos);
        }
    }
}
