using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Core;
using PaZword.Core.Data;
using PaZword.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace PaZword.Tests.Data
{
    [TestClass]
    public class RemoteSynchronizationServiceTests : MefBaseTest
    {
        private readonly MockIRemoteStorageProvider _mockRemoteStorageProvider = new MockIRemoteStorageProvider();

        [TestInitialize]
        public async Task Initialize()
        {
            var localFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
            IReadOnlyList<StorageFile> storageFiles = await localFolder.GetFilesAsync();

            foreach (StorageFile file in storageFiles)
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }

        [TestMethod]
        public async Task SyncNotEnabled()
        {
            var service = CreateRemoteSynchronizationService();
            var settingsProvider = ExportProvider.GetExport<ISettingsProvider>();

            settingsProvider.SetSetting(SettingsDefinitions.SyncDataWithCloud, false);

            service.SynchronizationStarted += (s, e) =>
            {
                Assert.Fail();
            };

            service.SynchronizationCompleted += (s, e) =>
            {
                Assert.Fail();
            };

            service.QueueSynchronization();

            await Task.Delay(1000).ConfigureAwait(false);

            Assert.IsFalse(service.IsSynchronizing);
        }

        [TestMethod]
        public async Task NoRemoteStorageProvider()
        {
            var service = CreateRemoteSynchronizationService();
            var settingsProvider = ExportProvider.GetExport<ISettingsProvider>();

            settingsProvider.SetSetting(SettingsDefinitions.SyncDataWithCloud, true);
            settingsProvider.SetSetting(SettingsDefinitions.RemoteStorageProviderName, string.Empty);

            service.SynchronizationStarted += (s, e) =>
            {
                Assert.Fail();
            };

            service.SynchronizationCompleted += (s, e) =>
            {
                Assert.Fail();
            };

            service.QueueSynchronization();

            await Task.Delay(1000).ConfigureAwait(false);

            Assert.IsFalse(service.IsSynchronizing);
        }

        [TestMethod]
        public async Task SignInFails()
        {
            var service = CreateRemoteSynchronizationService();
            var settingsProvider = ExportProvider.GetExport<ISettingsProvider>();
            _mockRemoteStorageProvider.SignInAsyncResult = false;

            var tcs = new TaskCompletionSource<bool>();
            SynchronizationResultEventArgs resultEventArgs = null;

            service.SynchronizationCompleted += (s, e) =>
            {
                resultEventArgs = e;
                tcs.SetResult(true);
            };

            service.QueueSynchronization();

            await tcs.Task.ConfigureAwait(false);

            Assert.IsNotNull(resultEventArgs);
            Assert.IsFalse(resultEventArgs.Succeeded);
            Assert.IsFalse(resultEventArgs.RequiresReloadLocalData);
            Assert.IsFalse(settingsProvider.GetSetting(SettingsDefinitions.SyncDataWithCloud));
        }

        [TestMethod]
        public async Task NoFileOnServerAndLocal()
        {
            var service = CreateRemoteSynchronizationService();
            var tcs = new TaskCompletionSource<bool>();
            SynchronizationResultEventArgs resultEventArgs = null;

            service.SynchronizationCompleted += (s, e) =>
            {
                resultEventArgs = e;
                tcs.SetResult(true);
            };

            service.QueueSynchronization();

            await tcs.Task.ConfigureAwait(false);

            Assert.IsNotNull(resultEventArgs);
            Assert.IsTrue(resultEventArgs.Succeeded);
            Assert.IsFalse(resultEventArgs.RequiresReloadLocalData);
            Assert.AreEqual(0, (await _mockRemoteStorageProvider.GetFilesAsync(int.MaxValue, CancellationToken.None).ConfigureAwait(false)).Count);
        }

        [TestMethod]
        public async Task NoFileOnServer()
        {                
            var localFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
            await localFolder.CreateFileAsync(Constants.UserDataBundleFileName, CreationCollisionOption.ReplaceExisting);
            await localFolder.CreateFileAsync("file1", CreationCollisionOption.ReplaceExisting);
            await localFolder.CreateFileAsync("file2", CreationCollisionOption.ReplaceExisting);

            var service = CreateRemoteSynchronizationService();
            var tcs = new TaskCompletionSource<bool>();
            SynchronizationResultEventArgs resultEventArgs = null;

            service.SynchronizationCompleted += (s, e) =>
            {
                resultEventArgs = e;
                tcs.SetResult(true);
            };

            service.QueueSynchronization();

            await tcs.Task.ConfigureAwait(false);

            Assert.IsNotNull(resultEventArgs);
            Assert.IsTrue(resultEventArgs.Succeeded);
            Assert.IsFalse(resultEventArgs.RequiresReloadLocalData);
            Assert.AreEqual(3, (await _mockRemoteStorageProvider.GetFilesAsync(int.MaxValue, CancellationToken.None).ConfigureAwait(false)).Count);
        }

        [TestMethod]
        public async Task NoFileOnLocal()
        {
            var localFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
            Assert.AreEqual(0, (await localFolder.GetFilesAsync()).Count);

            _mockRemoteStorageProvider.SetFilesOnServer(
                new RemoteFileInfo(Constants.UserDataBundleFileName, new DateTimeOffset(DateTime.UtcNow)),
                new RemoteFileInfo("file1", new DateTimeOffset(DateTime.UtcNow)),
                new RemoteFileInfo("file2", new DateTimeOffset(DateTime.UtcNow)));

            var service = CreateRemoteSynchronizationService();
            var tcs = new TaskCompletionSource<bool>();
            SynchronizationResultEventArgs resultEventArgs = null;

            service.SynchronizationCompleted += (s, e) =>
            {
                resultEventArgs = e;
                tcs.SetResult(true);
            };

            service.QueueSynchronization();

            await tcs.Task.ConfigureAwait(false);

            Assert.IsNotNull(resultEventArgs);
            Assert.IsTrue(resultEventArgs.Succeeded);
            Assert.IsTrue(resultEventArgs.RequiresReloadLocalData);
            Assert.AreEqual(3, (await localFolder.GetFilesAsync()).Count);
        }

        [TestMethod]
        public async Task LocalMoreRecentThanServer()
        {
            var localFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
            await localFolder.CreateFileAsync(Constants.UserDataBundleFileName, CreationCollisionOption.ReplaceExisting);
            await localFolder.CreateFileAsync("file1", CreationCollisionOption.ReplaceExisting);
            await localFolder.CreateFileAsync("file2", CreationCollisionOption.ReplaceExisting);

            _mockRemoteStorageProvider.SetFilesOnServer(
                new RemoteFileInfo(Constants.UserDataBundleFileName, new DateTimeOffset(DateTime.UtcNow.AddHours(-1))),
                new RemoteFileInfo("file1", new DateTimeOffset(DateTime.UtcNow)));

            var service = CreateRemoteSynchronizationService();
            var tcs = new TaskCompletionSource<bool>();
            SynchronizationResultEventArgs resultEventArgs = null;

            service.SynchronizationCompleted += (s, e) =>
            {
                resultEventArgs = e;
                tcs.SetResult(true);
            };

            service.QueueSynchronization();

            await tcs.Task.ConfigureAwait(false);

            Assert.IsNotNull(resultEventArgs);
            Assert.IsTrue(resultEventArgs.Succeeded);
            Assert.IsFalse(resultEventArgs.RequiresReloadLocalData);
            Assert.AreEqual(3, (await localFolder.GetFilesAsync()).Count);
            Assert.AreEqual(3, (await _mockRemoteStorageProvider.GetFilesAsync(int.MaxValue, CancellationToken.None).ConfigureAwait(false)).Count);
        }

        [TestMethod]
        public async Task ServerMoreRecentThanLocal()
        {
            var localFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
            await localFolder.CreateFileAsync(Constants.UserDataBundleFileName, CreationCollisionOption.ReplaceExisting);
            await localFolder.CreateFileAsync("file1", CreationCollisionOption.ReplaceExisting);
            await localFolder.CreateFileAsync("file2", CreationCollisionOption.ReplaceExisting);

            _mockRemoteStorageProvider.SetFilesOnServer(
                new RemoteFileInfo(Constants.UserDataBundleFileName, new DateTimeOffset(DateTime.UtcNow.AddHours(1))),
                new RemoteFileInfo("file1", new DateTimeOffset(DateTime.UtcNow)));

            var service = CreateRemoteSynchronizationService();
            var tcs = new TaskCompletionSource<bool>();
            SynchronizationResultEventArgs resultEventArgs = null;

            service.SynchronizationCompleted += (s, e) =>
            {
                resultEventArgs = e;
                tcs.SetResult(true);
            };

            service.QueueSynchronization();

            await tcs.Task.ConfigureAwait(false);

            Assert.IsNotNull(resultEventArgs);
            Assert.IsTrue(resultEventArgs.Succeeded);
            Assert.IsTrue(resultEventArgs.RequiresReloadLocalData);
            Assert.AreEqual(2, (await localFolder.GetFilesAsync()).Count);
            Assert.AreEqual(2, (await _mockRemoteStorageProvider.GetFilesAsync(int.MaxValue, CancellationToken.None).ConfigureAwait(false)).Count);
        }

        [TestMethod]
        public async Task SynchronizationCanceled()
        {
            var localFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
            await localFolder.CreateFileAsync(Constants.UserDataBundleFileName, CreationCollisionOption.ReplaceExisting);
            await localFolder.CreateFileAsync("file1", CreationCollisionOption.ReplaceExisting);
            await localFolder.CreateFileAsync("file2", CreationCollisionOption.ReplaceExisting);

            _mockRemoteStorageProvider.SetFilesOnServer(
                new RemoteFileInfo(Constants.UserDataBundleFileName, new DateTimeOffset(DateTime.UtcNow)),
                new RemoteFileInfo("file1", new DateTimeOffset(DateTime.UtcNow)));

            var service = CreateRemoteSynchronizationService();
            var tcs = new TaskCompletionSource<bool>();
            SynchronizationResultEventArgs resultEventArgs = null;

            service.SynchronizationCompleted += (s, e) =>
            {
                resultEventArgs = e;
                tcs.SetResult(true);
            };

            service.QueueSynchronization();

            await Task.Delay(500).ConfigureAwait(false);

            service.Cancel();

            await tcs.Task.ConfigureAwait(false);

            Assert.IsNotNull(resultEventArgs);
            Assert.IsFalse(resultEventArgs.Succeeded);
        }

        private IRemoteSynchronizationService CreateRemoteSynchronizationService()
        {
            var settingsProvider = ExportProvider.GetExport<ISettingsProvider>();
            var logger = ExportProvider.GetExport<ILogger>();

            settingsProvider.SetSetting(SettingsDefinitions.SyncDataWithCloud, true);
            settingsProvider.SetSetting(SettingsDefinitions.RemoteStorageProviderName, nameof(MockIRemoteStorageProvider));

            return new RemoteSynchronizationService(
                settingsProvider,
                logger,
                new List<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>>
                {
                    new Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>(
                        () => _mockRemoteStorageProvider,
                        new RemoteStorageProviderMetadata
                        {
                            ProviderName = nameof(MockIRemoteStorageProvider)
                        })
                });
        }
    }
}
