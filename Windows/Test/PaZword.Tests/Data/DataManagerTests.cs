using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.Security;
using PaZword.Core;
using PaZword.Core.Data;
using PaZword.Localization;
using PaZword.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Tests.Data
{
    [TestClass]
    public class DataManagerTests : MefBaseTest
    {
        [TestInitialize]
        public void Initialize()
        {
            var encryptionProvider = ExportProvider.GetExport<IEncryptionProvider>();
            var keys = encryptionProvider.GenerateSecretKeys();
            encryptionProvider.SetSecretKeys(keys);
        }

        [TestMethod]
        public async Task LoadOrCreateLocalDataAsyncTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(5, dataManager.Categories.Count);
            }

            var localFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
            Assert.IsTrue(await localFolder.FileExistsAsync(Constants.UserDataBundleFileName).ConfigureAwait(false));
            Assert.AreEqual(1, (await localFolder.GetFilesAsync()).Count);

            var createdDateTime = (await localFolder.GetFileAsync(Constants.UserDataBundleFileName)).DateCreated;

            using (DataManager dataManager = await CreateDataManagerAsync(clearExistingLocalData: false).ConfigureAwait(false))
            {
                Assert.IsTrue(await localFolder.FileExistsAsync(Constants.UserDataBundleFileName).ConfigureAwait(false));
                Assert.AreEqual(1, (await localFolder.GetFilesAsync()).Count);

                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(5, dataManager.Categories.Count);
            }

            Assert.IsTrue(await localFolder.FileExistsAsync(Constants.UserDataBundleFileName).ConfigureAwait(false));
            Assert.AreEqual(1, (await localFolder.GetFilesAsync()).Count);

            // The file should not has been replaces, so the creation date should be the same.
            Assert.AreEqual(createdDateTime, (await localFolder.GetFileAsync(Constants.UserDataBundleFileName)).DateCreated);
        }

        [TestMethod]
        public async Task SaveLocalDataAsyncTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(5, dataManager.Categories.Count);

                await dataManager.AddNewCategoryAsync("foo", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false);
                await dataManager.SaveLocalUserDataBundleAsync(true, CancellationToken.None).ConfigureAwait(false);
            }

            using (DataManager dataManager = await CreateDataManagerAsync(clearExistingLocalData: false).ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(6, dataManager.Categories.Count);
            }
        }

        [TestMethod]
        public async Task AddNewCategoryAsyncTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                await dataManager.AddNewCategoryAsync("foo", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false);
                await dataManager.AddNewCategoryAsync("bar", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(7, dataManager.Categories.Count);

                // Check they're well sorted.
                Assert.AreEqual(LanguageManager.Instance.Core.CategoryAll, dataManager.Categories[0].Name); // Category "All" is always the first.
                Assert.AreEqual("bar", dataManager.Categories[1].Name);
                Assert.AreEqual(LanguageManager.Instance.Core.CategoryFinancial, dataManager.Categories[2].Name);
                Assert.AreEqual("foo", dataManager.Categories[3].Name);
                Assert.AreEqual(LanguageManager.Instance.Core.CategoryPersonal, dataManager.Categories[4].Name);
                Assert.AreEqual(LanguageManager.Instance.Core.CategoryProfessional, dataManager.Categories[5].Name);
                Assert.AreEqual(LanguageManager.Instance.Core.CategorySocial, dataManager.Categories[6].Name);
            }
        }

        [TestMethod]
        public async Task RenameCategoryAsyncTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                var financialCategory = dataManager.Categories.Single(c => string.Equals(c.Name, LanguageManager.Instance.Core.CategoryFinancial, StringComparison.Ordinal));
                await dataManager.RenameCategoryAsync(financialCategory.Id, "zoo", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual("zoo", dataManager.Categories.Last().Name); // Renaming to re-sort the items.
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task RenameCategoryAllTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                await dataManager.RenameCategoryAsync(new Guid(Constants.CategoryAllId), "foo", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false);

                Assert.Fail();
            }
        }

        [TestMethod]
        public async Task DeleteCategoryAsyncTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                var financialCategory = dataManager.Categories.Single(c => string.Equals(c.Name, LanguageManager.Instance.Core.CategoryFinancial, StringComparison.Ordinal));

                Assert.AreEqual(5, dataManager.Categories.Count);
                await dataManager.DeleteCategoryAsync(financialCategory.Id, CancellationToken.None).ConfigureAwait(false);
                Assert.AreEqual(4, dataManager.Categories.Count);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DeleteCategoryAllTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                await dataManager.DeleteCategoryAsync(new Guid(Constants.CategoryAllId), CancellationToken.None).ConfigureAwait(false);

                Assert.Fail();
            }
        }

        [TestMethod]
        public async Task AddNewAccountAsyncTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                var categoryAllId = new Guid(Constants.CategoryAllId);

                var account1 = new Account(await dataManager.GenerateUniqueIdAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    Title = "Foo",
                    CategoryID = categoryAllId
                };
                var account2 = new Account(await dataManager.GenerateUniqueIdAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    Title = "Bar",
                    CategoryID = categoryAllId
                };

                await dataManager.AddNewAccountAsync(account1, CancellationToken.None).ConfigureAwait(false);
                await dataManager.AddNewAccountAsync(account2, CancellationToken.None).ConfigureAwait(false);

                var allAccounts = await dataManager.SearchAsync(categoryAllId, string.Empty, CancellationToken.None).ConfigureAwait(false);
                Assert.AreEqual(2, allAccounts.Count);

                // Check they're well sorted.
                Assert.AreEqual("Bar", allAccounts[0].Title);
                Assert.AreEqual("Foo", allAccounts[1].Title);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task AddNewAccountAsyncWrongCategoryTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                var account1 = new Account(await dataManager.GenerateUniqueIdAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    Title = "Foo",
                    CategoryID = await dataManager.GenerateUniqueIdAsync(CancellationToken.None).ConfigureAwait(false)
                };

                await dataManager.AddNewAccountAsync(account1, CancellationToken.None).ConfigureAwait(false);

                Assert.Fail();
            }
        }

        [TestMethod]
        public async Task UpdateAccountAsyncTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                var categoryAllId = new Guid(Constants.CategoryAllId);

                var account1 = new Account(await dataManager.GenerateUniqueIdAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    Title = "Foo",
                    CategoryID = categoryAllId
                };
                var account2 = new Account(await dataManager.GenerateUniqueIdAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    Title = "Bar",
                    CategoryID = categoryAllId
                };

                await dataManager.AddNewAccountAsync(account1, CancellationToken.None).ConfigureAwait(false);

                account1 = ExportProvider.GetExport<ISerializationProvider>().CloneObject(account1); // clone the account, so we're sure they're not same object reference.

                await dataManager.UpdateAccountAsync(account1, account2, CancellationToken.None).ConfigureAwait(false);

                var allAccounts = await dataManager.SearchAsync(categoryAllId, string.Empty, CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(1, allAccounts.Count);
                Assert.AreEqual("Bar", allAccounts[0].Title);
            }
        }

        [TestMethod]
        public async Task StressTest()
        {
            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            using (var manualResetEvent = new ManualResetEvent(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                var tasks = new List<Task>();

                for (int i = 0; i < 1000; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        manualResetEvent.WaitOne();
                        await dataManager.AddNewCategoryAsync("foo", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false);
                    }));

                    tasks.Add(Task.Run(async () =>
                    {
                        manualResetEvent.WaitOne();
                        await dataManager.AddNewAccountAsync(
                        new Account(await dataManager.GenerateUniqueIdAsync(CancellationToken.None).ConfigureAwait(false)),
                        CancellationToken.None).ConfigureAwait(false);
                    }));

                    tasks.Add(Task.Run(async () =>
                    {
                        manualResetEvent.WaitOne();
                        await dataManager.SearchAsync(new Guid(Constants.CategoryAllId), string.Empty, CancellationToken.None).ConfigureAwait(false);
                    }));
                }

                manualResetEvent.Set();
                await Task.WhenAll(tasks).ConfigureAwait(false);

                var categoryAllId = new Guid(Constants.CategoryAllId);
                var allAccounts = await dataManager.SearchAsync(categoryAllId, string.Empty, CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(1000, allAccounts.Count);
                Assert.AreEqual(1005, dataManager.Categories.Count);

                await dataManager.SaveLocalUserDataBundleAsync(true, CancellationToken.None).ConfigureAwait(false);
            }

            using (DataManager dataManager = await CreateDataManagerAsync(clearExistingLocalData: false).ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                var allAccounts = await dataManager.SearchAsync(new Guid(Constants.CategoryAllId), string.Empty, CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(1000, allAccounts.Count);
            }
        }

        [TestMethod]
        public async Task MergingAfterSynchronization()
        {
            Guid categoryFoo;
            Guid categoryBar;
            Guid categoryBoo;

            using (DataManager dataManager = await CreateDataManagerAsync().ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                categoryFoo = (await dataManager.AddNewCategoryAsync("foo", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false)).Id;
                categoryBar = (await dataManager.AddNewCategoryAsync("bar", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false)).Id;
                categoryBoo = (await dataManager.AddNewCategoryAsync("boo", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false)).Id;

                await dataManager.SaveLocalUserDataBundleAsync(true, CancellationToken.None).ConfigureAwait(false);

                // backup data file
                var localUserDataFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
                var dataFile = await localUserDataFolder.GetFileAsync(Constants.UserDataBundleFileName);
                await dataFile.CopyAsync(localUserDataFolder, "backup");

                await dataManager.DeleteCategoryAsync(categoryFoo, CancellationToken.None).ConfigureAwait(false);
                await dataManager.DeleteCategoryAsync(categoryBar, CancellationToken.None).ConfigureAwait(false);
                await dataManager.RenameCategoryAsync(categoryBoo, "HelloThere", CategoryIcon.Home, CancellationToken.None).ConfigureAwait(false); // local change is more recent, so "hellothere" should persist after merge.

                await dataManager.SaveLocalUserDataBundleAsync(true, CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(6, dataManager.Categories.Count);
            }

            using (DataManager dataManager = await CreateDataManagerAsync(clearExistingLocalData: false).ConfigureAwait(false))
            {
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(6, dataManager.Categories.Count);

                // restore data file
                var localUserDataFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
                var dataFile = await localUserDataFolder.GetFileAsync("backup");
                await dataFile.RenameAsync(Constants.UserDataBundleFileName, Windows.Storage.NameCollisionOption.ReplaceExisting);

                // Load and do the merge.
                await dataManager.LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(8, dataManager.Categories.Count);

                // Check they're well sorted.
                Assert.AreEqual(LanguageManager.Instance.Core.CategoryAll, dataManager.Categories[0].Name); // Category "All" is always the first.
                Assert.AreEqual("bar", dataManager.Categories[1].Name);
                Assert.AreEqual(LanguageManager.Instance.Core.CategoryFinancial, dataManager.Categories[2].Name);
                Assert.AreEqual("foo", dataManager.Categories[3].Name);
                Assert.AreEqual("HelloThere", dataManager.Categories[4].Name);
                Assert.AreEqual(LanguageManager.Instance.Core.CategoryPersonal, dataManager.Categories[5].Name);
                Assert.AreEqual(LanguageManager.Instance.Core.CategoryProfessional, dataManager.Categories[6].Name);
                Assert.AreEqual(LanguageManager.Instance.Core.CategorySocial, dataManager.Categories[7].Name);
            }
        }

        [TestMethod]
        public void LocalChangeWhileSynchronization()
        {
            // TODO: A test that verifies the behavior when saving the local data file (following a change from the user) while it's synchronizing.
            // Maybe do some manual tests in a VM to see the behavior when the same account get some changes from 2 different machines
            Assert.Fail();
        }

        private async Task<DataManager> CreateDataManagerAsync(bool clearExistingLocalData = true)
        {
            var logger = ExportProvider.GetExport<ILogger>();
            var encryptionProvider = ExportProvider.GetExport<IEncryptionProvider>();
            var serializationProvider = ExportProvider.GetExport<ISerializationProvider>();
            var upgradeService = ExportProvider.GetExport<IUpgradeService>();

#pragma warning disable CA2000 // Dispose objects before losing scope
            var dataManager = new DataManager(
                logger,
                encryptionProvider,
                serializationProvider,
                new MockIRemoteSynchronizationService(),
                upgradeService);
#pragma warning restore CA2000 // Dispose objects before losing scope

            if (clearExistingLocalData)
            {
                await dataManager.ClearLocalDataAsync(CancellationToken.None).ConfigureAwait(false);
            }

            return dataManager;
        }
    }
}
