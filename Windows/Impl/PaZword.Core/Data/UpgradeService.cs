using Microsoft.Graph;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.Security;
using PaZword.Api.Settings;
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

        /// <summary>
        /// This should be increased every time a breaking change is made to the
        /// <see cref="UserDataBundle"/> or the encryption engine or anything else that requires
        /// a migration of the user data.
        /// </summary>
        private const int CurrentSupportedUserBundleVersion = 1;

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

        public void MigrateSettings()
        {
        }

        public async Task<(bool updated, UserDataBundle userDataBundle)> MigrateUserDataBundleAsync(StorageFile userDataBundleFile)
        {
            string encryptedData = await FileIO.ReadTextAsync(userDataBundleFile);
            int version = GetVersion(encryptedData);

            // No migration needed. Just load the data
            UserDataBundle userDataBundle = Load(encryptedData);
            _logger.LogEvent(NoUpgradeRequiredEventName, string.Empty);
            return (updated: false, userDataBundle);
        }

        /// <summary>
        /// Load data and don't do any upgrade.
        /// </summary>
        private UserDataBundle Load(string encryptedData)
        {
            string jsonData = _encryptionProvider.DecryptString(encryptedData);
            return _serializationProvider.DeserializeObject<UserDataBundle>(jsonData);
        }

        private int GetVersion(string encryptedData)
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
