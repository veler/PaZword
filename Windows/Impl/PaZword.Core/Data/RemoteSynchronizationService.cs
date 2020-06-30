using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Core.Threading;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace PaZword.Core.Data
{
    [Export(typeof(IRemoteSynchronizationService))]
    [Shared()]
    internal sealed class RemoteSynchronizationService : IRemoteSynchronizationService, IDisposable
    {
        private const string SynchronizeFaultEvent = "SynchronizeWithCloud.Fault";
        private const string SynchronizeCanceledEvent = "SynchronizeWithCloud.Canceled";

        private readonly DisposableSempahore _sempahore = new DisposableSempahore();
        private readonly ISettingsProvider _settingsProvider;
        private readonly ILogger _logger;
        private readonly IEnumerable<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> _remoteStorageProviders;

        private CancellationTokenSource _cancellationTokenSource;

        public bool IsSynchronizing => _sempahore.IsBusy;

        public event EventHandler<EventArgs> SynchronizationStarted;

        public event EventHandler<SynchronizationResultEventArgs> SynchronizationCompleted;

        [ImportingConstructor]
        public RemoteSynchronizationService(
            ISettingsProvider settingsProvider,
            ILogger logger,
            [ImportMany] IEnumerable<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> remoteStorageProviders)
        {
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));
            _logger = Arguments.NotNull(logger, nameof(logger));
            _remoteStorageProviders = Arguments.NotNull(remoteStorageProviders, nameof(remoteStorageProviders));
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _sempahore.Dispose();
        }

        public void QueueSynchronization()
        {
            lock (_sempahore)
            {
                if (_cancellationTokenSource == null)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                Task.Run(async ()
                    => await SynchronizeAsync(_cancellationTokenSource.Token).ConfigureAwait(false)
                    ).Forget();
            }
        }

        public void Cancel()
        {
            lock (_sempahore)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }

        private async Task SynchronizeAsync(CancellationToken cancellationToken)
        {
            // The semaphore acts like queue.
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var synchronizationStarted = false;
                var succeeded = false;
                var requiresReloadLocalData = false;

                try
                {
                    if (!_settingsProvider.GetSetting(SettingsDefinitions.SyncDataWithCloud))
                    {
                        return;
                    }

                    string targetterProviderName = _settingsProvider.GetSetting(SettingsDefinitions.RemoteStorageProviderName);
                    IRemoteStorageProvider remoteStorageProvider = _remoteStorageProviders.SingleOrDefault(m => string.Equals(m.Metadata.ProviderName, targetterProviderName, StringComparison.Ordinal))?.Value;

                    if (remoteStorageProvider == null)
                    {
                        return;
                    }

                    if (!CoreHelper.IsInternetAccess())
                    {
                        return;
                    }

                    SynchronizationStarted?.Invoke(this, EventArgs.Empty);
                    synchronizationStarted = true;

                    if (!await remoteStorageProvider.SignInAsync(interactive: false, cancellationToken).ConfigureAwait(false))
                    {
                        // If fails to authenticate, disables synchronization and sign out.
                        _settingsProvider.SetSetting(SettingsDefinitions.SyncDataWithCloud, false);
                        await remoteStorageProvider.SignOutAsync().ConfigureAwait(false);

                        // TODO: Add a log to notify to let the user know it signed out and he should re-authenticate.

                        // returning here will still trigger the Finally block.
                        return;
                    }

                    // Retrieve the list of online files.
                    IReadOnlyList<RemoteFileInfo> roamingFiles =
                        await remoteStorageProvider.GetFilesAsync(Constants.DataFileCountLimit, cancellationToken).ConfigureAwait(false);

                    RemoteFileInfo roamingUserDataBundleFile = roamingFiles.FirstOrDefault(file
                        => string.Equals(Path.GetFileName(file.FullPath), Constants.UserDataBundleFileName, StringComparison.Ordinal));

                    IEnumerable<RemoteFileInfo> allOtherRoamingFiles = roamingFiles.Where(file
                        => !string.Equals(Path.GetFileName(file.FullPath), Constants.UserDataBundleFileName, StringComparison.Ordinal));

                    // Retrieve the list of local files.
                    var localUserDataFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
                    StorageFile localUserDataBundleFile = await localUserDataFolder.TryGetItemAsync(Constants.UserDataBundleFileName) as StorageFile;

                    IEnumerable<StorageFile> allOtherLocalFiles = (await localUserDataFolder.GetFilesAsync())
                        .Where(file => !string.Equals(file.Name, Constants.UserDataBundleFileName, StringComparison.Ordinal));

                    if (localUserDataBundleFile == null && roamingUserDataBundleFile == RemoteFileInfo.Empty)
                    {
                        // Nothing locally and remotely?

                        succeeded = true;
                        return;
                    }

                    if (localUserDataBundleFile == null
                        || (roamingUserDataBundleFile != RemoteFileInfo.Empty
                            && roamingUserDataBundleFile.CreatedDateTime.ToUniversalTime()
                                > (await localUserDataBundleFile.GetBasicPropertiesAsync()).DateModified.ToUniversalTime()))
                    {
                        // If there is no local user data file, or that the file on the server is more recent than the local one,
                        // then we want to merge by taking the version from the server.

                        await DownloadRoamingDataFromServerAsync(
                            remoteStorageProvider,
                            allOtherRoamingFiles,
                            allOtherLocalFiles,
                            cancellationToken).ConfigureAwait(false);

                        // The local file changed, since we downloaded the one from the server, so let's indicate
                        // that we want to reload (and merge) the local data.
                        requiresReloadLocalData = true;
                    }
                    else
                    {
                        // Else, then it means the local file is more recent than the one on the server,
                        // or that there is simply no file on the server,
                        // so we want to merge by taking the version from the local file.

                        await UploadLocalDataToServerAsync(
                            remoteStorageProvider,
                            localUserDataBundleFile,
                            allOtherRoamingFiles,
                            allOtherLocalFiles,
                            cancellationToken).ConfigureAwait(false);
                    }

                    succeeded = true;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogEvent(SynchronizeCanceledEvent, "The synchronization with the cloud has been canceled.");
                }
                catch (Exception ex)
                {
                    _logger.LogFault(SynchronizeFaultEvent, "Failed to synchronize the data with the cloud.", ex);
                }
                finally
                {
                    if (synchronizationStarted)
                    {
                        RaiseSynchronizationCompleted(succeeded, requiresReloadLocalData);
                    }
                }
            }
        }

        private void RaiseSynchronizationCompleted(bool succeeded, bool requiresReloadLocalData)
        {
            SynchronizationCompleted?.Invoke(this, new SynchronizationResultEventArgs(succeeded, requiresReloadLocalData));
        }

        private static async Task UploadLocalDataToServerAsync(
            IRemoteStorageProvider remoteStorageProvider,
            StorageFile localUserDataBundleFile,
            IEnumerable<RemoteFileInfo> allOtherRoamingFiles,
            IEnumerable<StorageFile> allOtherLocalFiles,
            CancellationToken cancellationToken)
        {
            // Upload the user data bundle file to the server.
            HandleUploadResult(await remoteStorageProvider.UploadFileAsync(localUserDataBundleFile, cancellationToken).ConfigureAwait(false));

            // Delete roaming data files that don't exist locally
            foreach (RemoteFileInfo roamingFile in allOtherRoamingFiles)
            {
                if (!allOtherLocalFiles.Any(localFile
                    => string.Equals(Path.GetFileName(roamingFile.FullPath), localFile.Name, StringComparison.Ordinal)))
                {
                    await remoteStorageProvider.DeleteFileAsync(roamingFile.FullPath, cancellationToken).ConfigureAwait(false);
                }
            }

            // Upload local data files that don't exist remotely
            foreach (StorageFile localFile in allOtherLocalFiles)
            {
                if (!allOtherRoamingFiles.Any(roamingFile
                    => string.Equals(Path.GetFileName(roamingFile.FullPath), localFile.Name, StringComparison.Ordinal)))
                {
                    HandleUploadResult(await remoteStorageProvider.UploadFileAsync(localFile, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        private static async Task DownloadRoamingDataFromServerAsync(
            IRemoteStorageProvider remoteStorageProvider,
            IEnumerable<RemoteFileInfo> allOtherRoamingFiles,
            IEnumerable<StorageFile> allOtherLocalFiles,
            CancellationToken cancellationToken)
        {
            // Download the user data bundle file from the server.
            HandleDownloadResult(await remoteStorageProvider.DownloadFileAsync(Constants.UserDataBundleFileName, cancellationToken).ConfigureAwait(false));

            // Delete local data files that don't exist remotely
            foreach (StorageFile localFile in allOtherLocalFiles)
            {
                if (!allOtherRoamingFiles.Any(roamingFile
                    => string.Equals(Path.GetFileName(roamingFile.FullPath), localFile.Name, StringComparison.Ordinal)))
                {
                    await localFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }

            // Download remote data files that don't exist locally
            foreach (RemoteFileInfo roamingFile in allOtherRoamingFiles)
            {
                if (!allOtherLocalFiles.Any(localFile
                   => string.Equals(Path.GetFileName(roamingFile.FullPath), localFile.Name, StringComparison.Ordinal)))
                {
                    HandleDownloadResult(await remoteStorageProvider.DownloadFileAsync(roamingFile.FullPath, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        private static void HandleUploadResult(bool uploaded)
        {
            if (!uploaded)
            {
                throw new Exception("Unable to upload a file successfully.");
            }
        }

        private static void HandleDownloadResult(StorageFile downloadedFile)
        {
            if (downloadedFile == null)
            {
                throw new Exception("Unable to download a file successfully.");
            }
        }
    }
}
