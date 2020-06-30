using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.Security;
using PaZword.Core;
using PaZword.Core.RecurrentTasks;
using PaZword.Models.Data;
using PaZword.Models.Pwned;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Tests.RecurrentTasks
{
    [TestClass]
    public class PwnedRecurrentTaskTests
    {
        private IApp _app;


        [TestInitialize]
        public void Initialize()
        {
            _app = (IApp)Windows.UI.Xaml.Application.Current;

            _app.ResetMef();

            var encryptionProvider = _app.ExportProvider.GetExport<IEncryptionProvider>();
            var keys = encryptionProvider.GenerateSecretKeys();
            encryptionProvider.SetSecretKeys(keys);
        }

        [TestMethod]
        public async Task PwnedRecurrentTaskCompromised()
        {
            await _app.ExportProvider.GetExport<IDataManager>().ClearLocalDataAsync(CancellationToken.None).ConfigureAwait(false);
            await _app.ExportProvider.GetExport<IDataManager>().LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);
            var account = new Account()
            {
                Title = "Account"
            };
            account.Data.Add(new CredentialData()
            {
                EmailAddress = "test@outlook.com".ToSecureString()
            });
            await _app.ExportProvider.GetExport<IDataManager>().AddNewAccountAsync(account, CancellationToken.None).ConfigureAwait(false);

            var service = new PwnedRecurrentTask(
                _app.ExportProvider.GetExport<ILogger>(),
                _app.ExportProvider.GetExport<IDataManager>(),
                _app.ExportProvider.GetExport<ISerializationProvider>());

            Assert.IsTrue(await service.CanExecuteAsync(CancellationToken.None).ConfigureAwait(false));

            Dictionary<SecureString, IReadOnlyList<Breach>> result = await service.ExecuteAsync(CancellationToken.None).ConfigureAwait(false) as Dictionary<SecureString, IReadOnlyList<Breach>>;

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.First().Value.Count >= 25);

            Assert.IsTrue(await service.CanExecuteAsync(CancellationToken.None).ConfigureAwait(false));

            result = await service.ExecuteAsync(CancellationToken.None).ConfigureAwait(false) as Dictionary<SecureString, IReadOnlyList<Breach>>;

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task PwnedRecurrentTaskNotCompromised()
        {
            await _app.ExportProvider.GetExport<IDataManager>().ClearLocalDataAsync(CancellationToken.None).ConfigureAwait(false);
            await _app.ExportProvider.GetExport<IDataManager>().LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);
            var account = new Account()
            {
                Title = "Account"
            };
            account.Data.Add(new CredentialData()
            {
                EmailAddress = "fghfdghdf8g67hfg5hd6gj7fghjfghj@outlook.com".ToSecureString()
            });
            await _app.ExportProvider.GetExport<IDataManager>().AddNewAccountAsync(account, CancellationToken.None).ConfigureAwait(false);

            var service = new PwnedRecurrentTask(
                _app.ExportProvider.GetExport<ILogger>(),
                _app.ExportProvider.GetExport<IDataManager>(),
                _app.ExportProvider.GetExport<ISerializationProvider>());

            Assert.IsTrue(await service.CanExecuteAsync(CancellationToken.None).ConfigureAwait(false));

            Dictionary<SecureString, IReadOnlyList<Breach>> result = await service.ExecuteAsync(CancellationToken.None).ConfigureAwait(false) as Dictionary<SecureString, IReadOnlyList<Breach>>;

            Assert.AreEqual(0, result.Count);
        }
    }
}
