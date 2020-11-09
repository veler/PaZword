using Microsoft.Graph;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.Security;
using PaZword.Api.Settings;
using PaZword.Localization;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Storage;

namespace PaZword.Core.Data
{
    [Export(typeof(IUpgradeService))]
    [Shared]
    internal sealed class UpgradeService : IUpgradeService
    {
        private const string NoUpgradeRequiredEventName = "UpgradeService.NoUpgradeRequired";
        private const string Version1ToVersion2EventName = "UpgradeService.Upgrading.Version1.To.Version2";
        private const string Version2ToVersion3EventName = "UpgradeService.Upgrading.Version2.To.Version3";

        /// <summary>
        /// This should be increased every time a breaking change is made to the
        /// <see cref="UserDataBundle"/> or the encryption engine or anything else that requires
        /// a migration of the user data.
        /// </summary>
        private const int CurrentSupportedUserBundleVersion = 3;

        private readonly ILogger _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly ISerializationProvider _serializationProvider;

        public string CurrentUserBundleVersion => CurrentSupportedUserBundleVersion.ToString(CultureInfo.InvariantCulture);

        [ImportingConstructor]
        public UpgradeService(
            ILogger logger,
            IEncryptionProvider encryptionProvider,
            ISerializationProvider serializationProvider,
            ISettingsProvider settingsProvider)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _encryptionProvider = Arguments.NotNull(encryptionProvider, nameof(encryptionProvider));
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));
        }

        public void UpgradeSettings()
        {
        }

        public async Task<(bool updated, UserDataBundle userDataBundle)> UpgradeUserDataBundleAsync(StorageFile userDataBundleFile)
        {
            string encryptedData = await FileIO.ReadTextAsync(userDataBundleFile);
            int version = GetVersion(encryptedData);
            UserDataBundle userDataBundle = null;
            var updated = false;

            // Migrating to version 2
            if (version == 1)
            {
                updated = true;
                userDataBundle = await UpgradeToVersion2Async(encryptedData).ConfigureAwait(false);
            }

            // Migration to version 3
            if (updated || version == 2)
            {
                updated = true;
                userDataBundle = UpgradeToVersion3(userDataBundle, encryptedData);
            }

            // No migration needed. Just load the data
            if (userDataBundle == null)
            {
                userDataBundle = Load(encryptedData);
                _logger.LogEvent(NoUpgradeRequiredEventName, string.Empty);
            }

            return (updated, userDataBundle);
        }

        /// <summary>
        /// Load data and don't do any upgrade.
        /// </summary>
        private UserDataBundle Load(string encryptedData)
        {
            int lastColonPosition = encryptedData.LastIndexOf(':');
            string jsonData = _encryptionProvider.DecryptString(encryptedData.Substring(lastColonPosition + 1));
            return _serializationProvider.DeserializeObject<UserDataBundle>(jsonData);
        }

        /// <summary>
        /// Migrates from version 1 to 2.
        /// </summary>
        private async Task<UserDataBundle> UpgradeToVersion2Async(string encryptedData)
        {
            // Version 1 can be loaded as version 2.
            // The difference between 1 and 2 is a vulnerability in the encryption engine.
            // To fix it, the data should be decrypted and re-encrypted.
            _logger.LogEvent(Version1ToVersion2EventName, string.Empty);
            const int oldVersion = 1;
            const int newVersion = 2;

            string jsonData = _encryptionProvider.DecryptString(encryptedData);
            UserDataBundle userDataBundle = _serializationProvider.DeserializeObject<UserDataBundle>(jsonData);

            var tasks = new List<Task>();
            for (int i = 0; i < userDataBundle.Accounts.Count; i++)
            {
                Account account = userDataBundle.Accounts[i];
                for (int j = 0; j < account.Data.Count; j++)
                {
                    AccountData accountData = account.Data[j];
                    if (accountData is IUpgradableAccountData upgradableAccountData)
                    {
                        tasks.Add(upgradableAccountData.UpgradeAsync(oldVersion, newVersion));
                    }
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return userDataBundle;
        }

        /// <summary>
        /// Migrates from version 2 to 3.
        /// </summary>
        private UserDataBundle UpgradeToVersion3(UserDataBundle version2UserDataBundle, string encryptedData)
        {
            // Version 2 can be loaded as version 3.
            // The difference between 2 and 3 is the support of custom icons in the categories.
            // The migration consists in defining the default icons of the existing categories.
            _logger.LogEvent(Version2ToVersion3EventName, string.Empty);

            UserDataBundle userDataBundle;

            if (version2UserDataBundle == null)
            {
                userDataBundle = Load(encryptedData);
            }
            else
            {
                userDataBundle = version2UserDataBundle;
            }

            for (int i = 0; i < userDataBundle.Categories.Count; i++)
            {
                Category category = userDataBundle.Categories[i];

                if (category.Id == new Guid(Constants.CategoryAllId))
                {
                    category.Icon = CategoryIcon.Home;
                }
                else if (string.Equals(category.Name, LanguageManager.Instance.Core.CategoryFinancial, StringComparison.OrdinalIgnoreCase))
                {
                    category.Icon = CategoryIcon.BankCard;
                }
                else if (string.Equals(category.Name, LanguageManager.Instance.Core.CategoryPersonal, StringComparison.OrdinalIgnoreCase))
                {
                    category.Icon = CategoryIcon.Personal2;
                }
                else if (string.Equals(category.Name, LanguageManager.Instance.Core.CategoryProfessional, StringComparison.OrdinalIgnoreCase))
                {
                    category.Icon = CategoryIcon.Professional;
                }
                else if (string.Equals(category.Name, LanguageManager.Instance.Core.CategorySocial, StringComparison.OrdinalIgnoreCase))
                {
                    category.Icon = CategoryIcon.SocialMedia;
                }
                else
                {
                    category.Icon = CategoryIcon.Default;
                }
            }

            return userDataBundle;
        }

        private static int GetVersion(string encryptedData)
        {
            int firstColonPosition = encryptedData.IndexOf(':');

            if (firstColonPosition == -1)
            {
                return 1; // Version 1.
            }

            string versionString = encryptedData.Substring(0, firstColonPosition);
            int version = int.Parse(versionString, CultureInfo.InvariantCulture);

            return version;
        }
    }
}
