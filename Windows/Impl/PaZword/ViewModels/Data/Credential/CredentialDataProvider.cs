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

namespace PaZword.ViewModels.Data.Credential
{
    [Export(typeof(IAccountDataProvider))]
    [ExportMetadata(nameof(AccountDataProviderMetadata.Order), 0)]
    [ExportMetadata(nameof(AccountDataProviderMetadata.AccountDataType), typeof(CredentialData))]
    [Shared]
    internal sealed class CredentialDataProvider : IAccountDataProvider
    {
        private readonly ISerializationProvider _serializationProvider;
        private readonly IWindowManager _windowManager;
        private readonly ILogger _logger;

        public string DisplayName => LanguageManager.Instance.CredentialData.DisplayName;

        [ImportingConstructor]
        public CredentialDataProvider(
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
            return new CredentialData(accountDataId);
        }

        public IAccountDataViewModel CreateViewModel(AccountData account)
        {
            return new CredentialDataViewModel(
                (CredentialData)account,
                _serializationProvider,
                _windowManager,
                _logger);
        }
    }
}
