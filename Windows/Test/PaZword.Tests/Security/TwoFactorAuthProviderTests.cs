using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api.Security;
using System.Threading.Tasks;

namespace PaZword.Tests.Security
{
    [TestClass]
    public class TwoFactorAuthProviderTests : MefBaseTest
    {
        [TestMethod]
        public async Task TwoFactorAuthProviderTest()
        {
            ITwoFactorAuthProvider provider = ExportProvider.GetExport<ITwoFactorAuthProvider>();

            string pin1 = provider.GeneratePin();

            Assert.IsTrue(provider.ValidatePin(pin1));

            await Task.Delay(60000).ConfigureAwait(false);

            string pin2 = provider.GeneratePin();

            Assert.AreNotEqual(pin1, pin2);
            Assert.IsTrue(provider.ValidatePin(pin2));
            Assert.IsFalse(provider.ValidatePin(pin1, allowedInterval: 0));
        }
    }
}
