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

namespace PaZword.ViewModels.Data.Other
{
    [Export(typeof(IAccountDataProvider))]
    [ExportMetadata(nameof(AccountDataProviderMetadata.Order), int.MaxValue)]
    [ExportMetadata(nameof(AccountDataProviderMetadata.AccountDataType), typeof(OtherData))]
    [Shared]
    internal sealed class OtherDataProvider : IAccountDataProvider
    {
        private readonly ISerializationProvider _serializationProvider;
        private readonly IWindowManager _windowManager;
        private readonly ILogger _logger;

        public string DisplayName => LanguageManager.Instance.OtherData.DisplayName;

        [ImportingConstructor]
        public OtherDataProvider(
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
            return new OtherData(accountDataId);
        }

        public IAccountDataViewModel CreateViewModel(AccountData account)
        {
            return new OtherDataViewModel(
                (OtherData)account,
                _serializationProvider,
                _windowManager,
                _logger);
        }
    }
}
