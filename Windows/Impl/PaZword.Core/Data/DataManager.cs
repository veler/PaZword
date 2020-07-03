using PaZword.Api;
using PaZword.Api.Collections;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.Security;
using PaZword.Core.Threading;
using PaZword.Localization;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PaZword.Core.Data
{
    [Export(typeof(IDataManager))]
    [Shared()]
    internal sealed class DataManager : IDataManager
    {
        private const string ClearLocalDataEvent = "DataManager.Clear";
        private const string NewBundleEvent = "DataManager.NewBundle";
        private const string LoadedEvent = "DataManager.Loaded";
        private const string SaveFileEvent = "DataManager.SaveFile";
        private const string LoadFileEvent = "DataManager.LoadFile";
        private const string DeleteFileEvent = "DataManager.DeleteFile";
        private const string SaveLocalDataEvent = "DataManager.Save";
        private const string SaveLocalDataFaultEvent = "DataManager.Save.Fault";
        private const string AddCategoryEvent = "DataManager.Category.Add";
        private const string RenameCategoryEvent = "DataManager.Category.Rename";
        private const string DeleteCategoryEvent = "DataManager.Category.Delete";
        private const string AddAccountEvent = "DataManager.Account.Add";
        private const string UpdateAccountEvent = "DataManager.Account.Update";
        private const string DeleteAccountEvent = "DataManager.Account.Delete";
        private const string SearchAccountEvent = "DataManager.Account.Search";

        private readonly DisposableSempahore _sempahore = new DisposableSempahore();
        private readonly ILogger _logger;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly ISerializationProvider _serializationProvider;
        private readonly IRemoteSynchronizationService _remoteSynchronizationService;
        private readonly IUpgradeService _upgradeService;

        private StorageFolder _localUserDataFolder;
        private UserDataBundle _data;

        public ConcurrentObservableCollection<Category> Categories => _data?.Categories;

        public bool HasUserDataBundleLoaded => _data != null;

        [ImportingConstructor]
        public DataManager(
            ILogger logger,
            IEncryptionProvider encryptionProvider,
            ISerializationProvider serializationProvider,
            IRemoteSynchronizationService remoteSynchronizationService,
            IUpgradeService migrationService)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _encryptionProvider = Arguments.NotNull(encryptionProvider, nameof(encryptionProvider));
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _remoteSynchronizationService = Arguments.NotNull(remoteSynchronizationService, nameof(remoteSynchronizationService));
            _upgradeService = Arguments.NotNull(migrationService, nameof(migrationService));

            _remoteSynchronizationService.SynchronizationCompleted += RemoteSynchronizationService_SynchronizationCompleted;
        }

        public void Dispose()
        {
            _sempahore.Dispose();
            _remoteSynchronizationService.SynchronizationCompleted -= RemoteSynchronizationService_SynchronizationCompleted;
        }

        public async Task ClearLocalDataAsync(CancellationToken cancellationToken)
        {
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await CoreHelper.RetryAsync(async () =>
                {
                    await EnsureInitializedAsync().ConfigureAwait(false);
                    IReadOnlyList<StorageFile> storageFiles = await _localUserDataFolder.GetFilesAsync();

                    for (int i = 0; i < storageFiles.Count; i++)
                    {
                        await storageFiles[i].DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }).ConfigureAwait(false);

                _data = null;
            }

            _logger.LogEvent(ClearLocalDataEvent, string.Empty);
        }

        public async Task<bool> TryOpenLocalUserDataBundleAsync(CancellationToken cancellationToken)
        {
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                // Open and decrypt the file from the hard drive.
                StorageFile dataFile = await _localUserDataFolder.GetFileAsync(Constants.UserDataBundleFileName);

                if (!dataFile.IsAvailable)
                {
                    throw new FileLoadException("The file isn't available.");
                }

                // Loads the user data bundle and migrate it (if needed)
                await _upgradeService.MigrateUserDataBundleAsync(dataFile).ConfigureAwait(false);

                return true;
            }
        }

        public async Task LoadOrCreateLocalUserDataBundleAsync(CancellationToken cancellationToken)
        {
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await EnsureInitializedAsync().ConfigureAwait(false);
                if (await _localUserDataFolder.TryGetItemAsync(Constants.UserDataBundleFileName) == null)
                {
                    // The local data file doesn't exist, let's create one !
                    _logger.LogEvent(NewBundleEvent, string.Empty);
                    CreateNewUserDataBundle();
                    await SaveLocalDataInternalAsync(synchronize: false).ConfigureAwait(false);
                }

                // Open and decrypt the file from the hard drive.
                StorageFile dataFile = await _localUserDataFolder.GetFileAsync(Constants.UserDataBundleFileName);

                if (!dataFile.IsAvailable)
                {
                    throw new FileLoadException("The file isn't available.");
                }

                // Loads the user data bundle and migrate it (if needed)
                (bool updated, UserDataBundle data) = await _upgradeService.MigrateUserDataBundleAsync(dataFile).ConfigureAwait(false);

                _logger.LogEvent(LoadedEvent, string.Empty);
                if (_data == null)
                {
                    // There was no previously loaded data file, let's use the one we loaded.
                    _data = data;
                }
                else
                {
                    // A user data file is already in memory. Maybe we just created it, but usually it probably mean we synchronized the file with the
                    // cloud and should now open the newly downloaded file.
                    // Therefore, we need to "merge" what we have on the hard drive (that comes from the cloud) with
                    // what we already had in RAM.
                    if (MergeUserDataBundle(data))
                    {
                        updated = true;
                    }
                }

                if (updated)
                {
                    // Something has changed during the merge, and/or the data have been migrated from an older version.
                    // So let's save the data to be sure we save the merge and let's (re)synchronize to be
                    // sure the server has the changes from the merge too.
                    await SaveLocalDataInternalAsync(synchronize: true).ConfigureAwait(false);
                }
            }
        }

        public async Task SaveLocalUserDataBundleAsync(bool synchronize, CancellationToken cancellationToken)
        {
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await SaveLocalDataInternalAsync(synchronize).ConfigureAwait(false);
            }
        }

        public async Task SaveFileDataAsync(Guid fileDataId, StorageFile file)
        {
            string fileName = fileDataId.ToString();

            await EnsureInitializedAsync().ConfigureAwait(false);
            if (await _localUserDataFolder.TryGetItemAsync(fileName) != null)
            {
                throw new IOException($"The file '{fileName}' already exists.");
            }

            using (IRandomAccessStreamWithContentType fileStream = await file.OpenReadAsync())
            using (var reader = new DataReader(fileStream.GetInputStreamAt(0)))
            {
                await reader.LoadAsync((uint)fileStream.Size);
                byte[] byteArray = new byte[fileStream.Size];
                reader.ReadBytes(byteArray);

                string encryptedFileContent = _encryptionProvider.EncryptString(Convert.ToBase64String(byteArray), reuseGlobalIV: true);

                await CoreHelper.RetryAsync(async () =>
                {
                    // Create and save the data in the new file.
                    var dataFileCreated = await _localUserDataFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(dataFileCreated, encryptedFileContent);
                }).ConfigureAwait(false);
            }

            _logger.LogEvent(SaveFileEvent, string.Empty);
        }

        public async Task<byte[]> LoadFileDataAsync(Guid fileDataId)
        {
            string fileName = fileDataId.ToString();

            await EnsureInitializedAsync().ConfigureAwait(false);
            if (await _localUserDataFolder.TryGetItemAsync(fileName) == null)
            {
                return Array.Empty<byte>();
            }

            StorageFile file = await _localUserDataFolder.GetFileAsync(fileName);

            if (!file.IsAvailable)
            {
                throw new FileLoadException("The file isn't available.");
            }

            string encryptedFileContent = await FileIO.ReadTextAsync(file);
            byte[] result = Convert.FromBase64String(_encryptionProvider.DecryptString(encryptedFileContent));

            _logger.LogEvent(LoadFileEvent, string.Empty);
            return result;
        }

        public async Task DeleteFileDataAsync(Guid fileDataId)
        {
            string fileName = fileDataId.ToString();

            IStorageItem file = await _localUserDataFolder.TryGetItemAsync(fileName);

            if (file != null)
            {
                await CoreHelper.RetryAsync(async () =>
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }).ConfigureAwait(false);
            }

            _logger.LogEvent(DeleteFileEvent, string.Empty);
        }

        public async Task<string> GetBrownBagDataAsync(string name, CancellationToken cancellationToken)
        {
            Arguments.NotNullOrEmpty(name, nameof(name));

            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_data.BrownBag.TryGetValue(name, out string value))
                {
                    return value;
                }

                return string.Empty;
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> GetBrownBagDataAsync(Predicate<string> predicate, CancellationToken cancellationToken)
        {
            Arguments.NotNull(predicate, nameof(predicate));

            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var result = new Dictionary<string, string>();

                foreach (KeyValuePair<string, string> item in _data.BrownBag)
                {
                    if (predicate(item.Key))
                    {
                        result.Add(item.Key, item.Value);
                    }
                }

                return result;
            }
        }

        public async Task SetBrownBagDataAsync(string name, string value, CancellationToken cancellationToken)
        {
            Arguments.NotNullOrEmpty(name, nameof(name));

            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (string.IsNullOrEmpty(value))
                {
                    _data.BrownBag.TryRemove(name, out _);
                }
                else
                {
                    _data.BrownBag.AddOrUpdate(name, value, (_, __) => value);
                }
            }
        }

        public async Task<Category> AddNewCategoryAsync(string name, CancellationToken cancellationToken)
        {
            Arguments.NotNullOrEmpty(name, nameof(name));

            Category category;
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                Guid uniqueId = GenerateUniqueId();
                category = new Category(uniqueId, name)
                {
                    LastModificationDate = DateTime.Now
                };
                _data.Categories.Add(category);

                SortData();
            }

            _logger.LogEvent(AddCategoryEvent, string.Empty);
            return category;
        }

        public async Task RenameCategoryAsync(Guid id, string name, CancellationToken cancellationToken)
        {
            Arguments.NotNull(id, nameof(id));
            Arguments.NotNullOrEmpty(name, nameof(name));

            if (id == new Guid(Constants.CategoryAllId))
            {
                throw new InvalidOperationException("It is forbidden to rename the 'All' category.");
            }

            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                Category existingCategory = _data.Categories.Single(item => item.Id == id);
                existingCategory.LastModificationDate = DateTime.Now;

                await TaskHelper.RunOnUIThreadAsync(() =>
                {
                    existingCategory.Name = name;
                }).ConfigureAwait(false);

                SortData();
            }

            _logger.LogEvent(RenameCategoryEvent, string.Empty);
        }

        public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
        {
            Arguments.NotNull(id, nameof(id));

            if (id == new Guid(Constants.CategoryAllId))
            {
                throw new InvalidOperationException("It is forbidden to delete the 'All' category.");
            }

            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                // Reassign the accounts of this category to the default category.
                Guid categoryAll = new Guid(Constants.CategoryAllId);

                for (int i = 0; i < _data.Accounts.Count; i++)
                {
                    Account account = _data.Accounts[i];
                    if (account.CategoryID == id)
                    {
                        account.CategoryID = categoryAll;
                    }
                }

                // Delete the category.
                Category categoryToRemove = _data.Categories.Single(item => item.Id == id);
                _data.Categories.Remove(categoryToRemove);
            }

            _logger.LogEvent(DeleteCategoryEvent, string.Empty);
        }

        public async Task<Category> GetCategoryAsync(Guid id, CancellationToken cancellationToken)
        {
            Arguments.NotNull(id, nameof(id));
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                return _data.Categories.SingleOrDefault(item => item.Id == id);
            }
        }

        public async Task AddNewAccountAsync(Account account, CancellationToken cancellationToken)
        {
            Arguments.NotNull(account, nameof(account));

            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                if (!_data.Categories.Any(c => c.Id == account.CategoryID))
                {
                    throw new ArgumentOutOfRangeException(nameof(account), "The account to add doesn't belong to any known category.");
                }

                _data.Accounts.Add(account);

                SortData();
            }

            _logger.LogEvent(AddAccountEvent, string.Empty);
        }

        public async Task UpdateAccountAsync(Account oldAccount, Account newAccount, CancellationToken cancellationToken)
        {
            Arguments.NotNull(oldAccount, nameof(oldAccount));
            Arguments.NotNull(newAccount, nameof(newAccount));

            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                if (!_data.Categories.Any(c => c.Id == newAccount.CategoryID))
                {
                    throw new ArgumentOutOfRangeException(nameof(newAccount), "The new account to add doesn't below to any known category.");
                }

                // It is possible that oldAccount is a clone of what exists in the users data, and that is
                // potentially different. But Account's IEquatable implementation only compares Id, which
                // is what we want here.
                var index = _data.Accounts.IndexOf(oldAccount);
                _data.Accounts[index] = newAccount;

                SortData();
            }

            _logger.LogEvent(UpdateAccountEvent, string.Empty);
        }

        public async Task DeleteAccountAsync(Account account, CancellationToken cancellationToken)
        {
            Arguments.NotNull(account, nameof(account));

            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                // It is possible that oldAccount is a clone of what exists in the users data, and that is
                // potentially different. But Account's IEquatable implementation only compares Id, which
                // is what we want here.
                _data.Accounts.Remove(account);
                account.Dispose();
            }

            _logger.LogEvent(DeleteAccountEvent, string.Empty);
        }

        public async Task<ConcurrentObservableCollection<Account>> SearchAsync(Guid categoryId, string query, CancellationToken cancellationToken)
        {
            Arguments.NotNull(categoryId, nameof(categoryId));

            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                _logger.LogEvent(SearchAccountEvent, string.Empty);

                if (categoryId == new Guid(Constants.CategoryAllId))
                {
                    return SearchInSetOfAccount(_data.Accounts.ToList(), query);
                }

                List<Account> accountsInGivenCategory = _data.Accounts.Where(account => account.CategoryID == categoryId).ToList();
                return SearchInSetOfAccount(accountsInGivenCategory, query);
            }
        }

        public async Task<Account> GetAccountAsync(Guid id, CancellationToken cancellationToken)
        {
            Arguments.NotNull(id, nameof(id));
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                return _data.Accounts.SingleOrDefault(item => item.Id == id);
            }
        }

        public async Task<Guid> GenerateUniqueIdAsync(CancellationToken cancellationToken)
        {
            using (await _sempahore.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                CheckState();

                return GenerateUniqueId();
            }
        }

        private Guid GenerateUniqueId()
        {
            CheckState();

            Guid guid;

            do
            {
                guid = Guid.NewGuid();

                // Makes sure the GUID doesn't exist in any category, account or account's data.
                // It's quite a paranoid test since there's 4 billions of possible GUID.
            } while (_data.Categories.Any(category => category.Id == guid)
                     || _data.Accounts.Any(account => account.Id == guid
                     || account.Data.Any(data => data.Id == guid)));

            return guid;
        }

        /// <summary>
        /// Performs a search into the given accounts and returns the matching items.
        /// </summary>
        /// <param name="accounts">The list of account where we must perform a search.</param
        /// <param name="query">The partial non-case sensitive name of the account to search.</param>
        /// <returns>The list of matching accounts</returns>
        private static ConcurrentObservableCollection<Account> SearchInSetOfAccount(List<Account> accounts, string query)
        {
            Arguments.NotNull(accounts, nameof(accounts));

            if (string.IsNullOrEmpty(query))
            {
                return new ConcurrentObservableCollection<Account>(accounts);
            }

            return new ConcurrentObservableCollection<Account>(
                accounts.Where(account
                    => account.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)));
        }

        /// <summary>
        /// Merges a given <paramref name="dataFromExternalSource"/> with <see cref="_data"/>.
        /// </summary>
        /// <returns>Returns <code>true</code> if something changed following the merge.</returns>
        private bool MergeUserDataBundle(UserDataBundle dataFromExternalSource)
        {
            var differenceDetectedInMerging = false;

            // Remove categories that exist in memory but not in the one coming from an external source.
            List<Category> categoriesToRemove = _data.Categories.Except(dataFromExternalSource.Categories).ToList();
            for (int i = 0; i < categoriesToRemove.Count; i++)
            {
                if (_data.Categories.Remove(categoriesToRemove[i]))
                {
                    differenceDetectedInMerging = true;
                }
            }

            // Update the categories that exist on the both side.
            for (var i = 0; i < _data.Categories.Count; i++)
            {
                Category category = _data.Categories[i];
                Category categoryFromExternalSource = dataFromExternalSource.Categories.FirstOrDefault(item => item == category);

                // If the local category != category from external source and that the one from the external source
                // is more recent, then let's take it.
                if (!category.ExactEquals(categoryFromExternalSource))
                {
                    differenceDetectedInMerging = true;
                    if (categoryFromExternalSource.LastModificationDate.ToUniversalTime() > category.LastModificationDate.ToUniversalTime())
                    {
                        _data.Categories[i] = categoryFromExternalSource;
                    }
                }
            }

            // Add categories that exist in the one coming from an external source but not in memory.
            List<Category> categoriesToAdd = dataFromExternalSource.Categories.Except(_data.Categories).ToList();
            if (categoriesToAdd.Count > 0)
            {
                differenceDetectedInMerging = true;
            }

            for (int i = 0; i < categoriesToAdd.Count; i++)
            {
                _data.Categories.Add(categoriesToAdd[i]);
            }

            // Same but for accounts.

            List<Account> accountsToRemove = _data.Accounts.Except(dataFromExternalSource.Accounts).ToList();
            for (int i = 0; i < accountsToRemove.Count; i++)
            {
                Account account = accountsToRemove[i];
                if (_data.Accounts.Remove(account))
                {
                    differenceDetectedInMerging = true;
                }
                account.Dispose();
            }

            for (var i = 0; i < _data.Accounts.Count; i++)
            {
                Account account = _data.Accounts[i];
                Account accountFromExternalSource = dataFromExternalSource.Accounts.FirstOrDefault(item => item == account);

                // If the local account != account from external source and that the one from the external source
                // is more recent, then let's take it.
                if (!account.ExactEquals(accountFromExternalSource))
                {
                    differenceDetectedInMerging = true;
                    if (accountFromExternalSource.LastModificationDate.ToUniversalTime() > account.LastModificationDate.ToUniversalTime())
                    {
                        _data.Accounts[i] = accountFromExternalSource;
                    }
                }
            }

            List<Account> accountsToAdd = dataFromExternalSource.Accounts.Except(_data.Accounts).ToList();
            if (accountsToAdd.Count > 0)
            {
                differenceDetectedInMerging = true;
            }

            for (int i = 0; i < accountsToAdd.Count; i++)
            {
                _data.Accounts.Add(accountsToAdd[i]);
            }

            if (differenceDetectedInMerging)
            {
                // Sort the data.
                SortData();
            }

            return differenceDetectedInMerging;
        }

        /// <summary>
        /// Creates the default instance of <see cref="UserDataBundle"/>.
        /// </summary>
        private void CreateNewUserDataBundle()
        {
            _data = new UserDataBundle();
            _data.Categories.Add(new Category(new Guid(Constants.CategoryAllId), LanguageManager.Instance.Core.CategoryAll));
            _data.Categories.Add(new Category(GenerateUniqueId(), LanguageManager.Instance.Core.CategoryFinancial));
            _data.Categories.Add(new Category(GenerateUniqueId(), LanguageManager.Instance.Core.CategoryPersonal));
            _data.Categories.Add(new Category(GenerateUniqueId(), LanguageManager.Instance.Core.CategoryProfessional));
            _data.Categories.Add(new Category(GenerateUniqueId(), LanguageManager.Instance.Core.CategorySocial));

            SortData();
        }

        /// <summary>
        /// Sort all the data and categories in alphabethic order and favorite order.
        /// </summary>
        private void SortData()
        {
            CheckState();
            var categoryAllId = new Guid(Constants.CategoryAllId);

            var categories = new List<Category>(_data.Categories
                .OrderBy(item => item.Id != categoryAllId)
                .ThenBy(item => item.Name));

            var accounts = new List<Account>(_data.Accounts
                .OrderBy(item => !item.IsFavorite)
                .ThenBy(item => item.Title));

            // Move the items without overwriting the collections.
            for (int i = 0; i < categories.Count; i++)
            {
                int index = _data.Categories.IndexOf(categories[i]);
                if (index != i)
                {
                    _data.Categories.Move(index, i);
                }
            }

            for (int i = 0; i < accounts.Count; i++)
            {
                int index = _data.Accounts.IndexOf(accounts[i]);
                if (index != i)
                {
                    _data.Accounts.Move(index, i);
                }
            }

            _data.Categories.WaitPendingChangesGetProcessedIfNotOnUIThread();
            _data.Accounts.WaitPendingChangesGetProcessedIfNotOnUIThread();
        }

        /// <summary>
        /// Save the user data in the PaZword's data file.
        /// </summary>
        private async Task SaveLocalDataInternalAsync(bool synchronize)
        {
            try
            {
                CheckState();

                // Encrypt the user data.
                string jsonData = _serializationProvider.SerializeObject(_data);
                string encryptedUserDataBundle =
                    _upgradeService.CurrentUserBundleVersion + ":" + _encryptionProvider.EncryptString(jsonData);

                await CoreHelper.RetryAsync(async () =>
                {
                    // Create and save the data in the new file.
                    await EnsureInitializedAsync().ConfigureAwait(false);
                    StorageFile dataFileCreated = await _localUserDataFolder.CreateFileAsync(Constants.UserDataBundleFileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(dataFileCreated, encryptedUserDataBundle);
                }).ConfigureAwait(false);

                _logger.LogEvent(SaveLocalDataEvent, string.Empty);

                if (synchronize)
                {
                    _remoteSynchronizationService.Cancel();
                    _remoteSynchronizationService.QueueSynchronization();
                }
            }
            catch (Exception ex)
            {
                _logger.LogFault(SaveLocalDataFaultEvent, "Failed to save user's local data on the hard drive.", ex);
            }
        }

        private async Task EnsureInitializedAsync()
        {
            _localUserDataFolder = await CoreHelper.GetOrCreateUserDataStorageFolderAsync().ConfigureAwait(false);
        }

        private void CheckState()
        {
            if (_data == null)
            {
                throw new Exception("No user data bundle available.");
            }
        }

        private void RemoteSynchronizationService_SynchronizationCompleted(object sender, SynchronizationResultEventArgs e)
        {
            if (e.RequiresReloadLocalData)
            {
                LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).Forget();
            }
        }
    }
}
