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

namespace PaZword.ViewModels.Data.File
{
    [Export(typeof(IAccountDataProvider))]
    [ExportMetadata(nameof(AccountDataProviderMetadata.Order), 5)]
    [ExportMetadata(nameof(AccountDataProviderMetadata.AccountDataType), typeof(FileData))]
    [Shared]
    internal sealed class FileDataProvider : IAccountDataProvider
    {
        private readonly ISerializationProvider _serializationProvider;
        private readonly IDataManager _dataManager;
        private readonly IWindowManager _windowManager;
        private readonly ILogger _logger;

        public string DisplayName => LanguageManager.Instance.FileData.DisplayName;

        [ImportingConstructor]
        public FileDataProvider(
            ISerializationProvider serializationProvider,
            IDataManager dataManager,
            IWindowManager windowManager,
            ILogger logger)
        {
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            _windowManager = Arguments.NotNull(windowManager, nameof(windowManager));
            _logger = Arguments.NotNull(logger, nameof(logger));
        }

        public bool CanCreateAccountData(Account account)
            => true;

        public AccountData CreateAccountData(Guid accountDataId)
        {
            return new FileData(accountDataId);
        }

        public IAccountDataViewModel CreateViewModel(AccountData account)
        {
            return new FileDataViewModel(
                (FileData)account,
                _serializationProvider,
                _dataManager,
                _windowManager,
                _logger);
        }
    }
}
