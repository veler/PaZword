using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.UI;
using PaZword.Api.ViewModels.Data;
using PaZword.Core;
using PaZword.Localization;
using PaZword.Models.Data;
using System;
using System.Composition;

namespace PaZword.ViewModels.Data.WiFiCredential
{
    [Export(typeof(IAccountDataProvider))]
    [ExportMetadata(nameof(AccountDataProviderMetadata.Order), 4)]
    [ExportMetadata(nameof(AccountDataProviderMetadata.AccountDataType), typeof(WiFiCredentialData))]
    [Shared]
    internal sealed class WiFiCredentialDataProvider : IAccountDataProvider
    {
        private readonly ISerializationProvider _serializationProvider;
        private readonly IWindowManager _windowManager;
        private readonly ILogger _logger;

        public string DisplayName => LanguageManager.Instance.WiFiCredentialData.DisplayName;

        [ImportingConstructor]
        public WiFiCredentialDataProvider(
            ISerializationProvider serializationProvider,
            IWindowManager windowManager,
            ILogger logger)
        {
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _windowManager = Arguments.NotNull(windowManager, nameof(windowManager));
            _logger = Arguments.NotNull(logger, nameof(logger));
        }

        public bool CanCreateAccountData(Account account)
            => true;

        public AccountData CreateAccountData(Guid accountDataId)
        {
            return new WiFiCredentialData(accountDataId);
        }

        public IAccountDataViewModel CreateViewModel(AccountData account)
        {
            return new WiFiCredentialDataViewModel(
                (WiFiCredentialData)account,
                _serializationProvider,
                _windowManager,
                _logger);
        }
    }
}
