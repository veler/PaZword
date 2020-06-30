using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Security;
using PaZword.Api.Services;
using PaZword.Core.Services;
using PaZword.Core.Threading;
using PaZword.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Tests.Services
{
    [TestClass]
    public class RecurrentTaskServiceTests : MefBaseTest
    {
        [TestInitialize]
        public void Initialize()
        {
            var encryptionProvider = ExportProvider.GetExport<IEncryptionProvider>();
            var keys = encryptionProvider.GenerateSecretKeys();
            encryptionProvider.SetSecretKeys(keys);
        }

        [TestMethod]
        public async Task RecurrentTaskServiceTest()
        {
            await ExportProvider.GetExport<IDataManager>().ClearLocalDataAsync(CancellationToken.None).ConfigureAwait(false);
            await ExportProvider.GetExport<IDataManager>().LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);


            RecurrentTaskService service = null;
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                service = new RecurrentTaskService(
                    ExportProvider.GetExport<ILogger>(),
                    ExportProvider.GetExport<IDataManager>(),
                    new List<Lazy<IRecurrentTask, RecurrentTaskMetadata>>
                    {
                        new Lazy<IRecurrentTask, RecurrentTaskMetadata>(
                            () => new MockRecurrentTask(),
                            new RecurrentTaskMetadata
                            {
                                Name = "Foo",
                                Recurrency = TaskRecurrency.OneMinute
                            }),
                        new Lazy<IRecurrentTask, RecurrentTaskMetadata>(
                            () => new MockRecurrentTask(),
                            new RecurrentTaskMetadata
                            {
                                Name = "Bar",
                                Recurrency = TaskRecurrency.OneDay
                            }),
                        new Lazy<IRecurrentTask, RecurrentTaskMetadata>(
                            () => new MockRecurrentTask(),
                            new RecurrentTaskMetadata
                            {
                                Name = "Boo",
                                Recurrency = TaskRecurrency.Manual
                            })
                    });
            }).ConfigureAwait(false);

            var fooRunCount = 0;
            var barRunCount = 0;
            var booRunCount = 0;

            service.TaskCompleted += (s, e) =>
            {
                Assert.AreEqual("Hello there", e.Result);

                if (e.TaskName == "Foo")
                {
                    fooRunCount++;
                }
                else if (e.TaskName == "Bar")
                {
                    barRunCount++;
                }
                else if (e.TaskName == "Boo")
                {
                    booRunCount++;
                }
            };

            service.Start();
            await Task.Delay(65000).ConfigureAwait(false); // 1min 5 sec.

            service.Pause();

            Assert.AreEqual(2, fooRunCount);
            Assert.AreEqual(1, barRunCount);
            Assert.AreEqual(0, booRunCount);

            service.Start();
            service.RunTaskExplicitly("Boo");
            await Task.Delay(2000).ConfigureAwait(false);

            Assert.AreEqual(2, fooRunCount);
            Assert.AreEqual(1, barRunCount);
            Assert.AreEqual(1, booRunCount);

            service.Dispose();
        }
    }
}
