using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api.Security;
using PaZword.Core.Security;

namespace PaZword.Tests.Security
{
    [TestClass]
    public class EncryptionProviderTests : MefBaseTest
    {
        [TestMethod]
        public void EncryptionProviderTest()
        {
            var stringToEncrypt = "Hello World";

            IEncryptionProvider encryptionProvider = new EncryptionProvider();
            var encryptionKeys = encryptionProvider.GenerateSecretKeys();
            var recoveryKey = encryptionProvider.EncodeSecretKeysToBase64(encryptionKeys);

            encryptionProvider.SetSecretKeys(encryptionKeys);

            var encryptedString = encryptionProvider.EncryptString(stringToEncrypt, reuseGlobalIV: true);
            var encryptedString2 = encryptionProvider.EncryptString(stringToEncrypt, reuseGlobalIV: true);

            Assert.AreEqual(encryptedString, encryptedString2);
            Assert.AreNotEqual(stringToEncrypt, encryptedString);

            encryptionProvider = new EncryptionProvider();
            encryptionKeys = encryptionProvider.DecodeSecretKeysFromBase64(recoveryKey);
            encryptionProvider.SetSecretKeys(encryptionKeys);

            var decryptedString = encryptionProvider.DecryptString(encryptedString);

            Assert.AreEqual(stringToEncrypt, decryptedString);
        }

        [TestMethod]
        public void EncryptionProviderUniqueIVTest()
        {
            var stringToEncrypt = "Hello World";

            IEncryptionProvider encryptionProvider = new EncryptionProvider();
            var encryptionKeys = encryptionProvider.GenerateSecretKeys();
            var recoveryKey = encryptionProvider.EncodeSecretKeysToBase64(encryptionKeys);

            encryptionProvider.SetSecretKeys(encryptionKeys);

            var encryptedString = encryptionProvider.EncryptString(stringToEncrypt);
            var encryptedString2 = encryptionProvider.EncryptString(stringToEncrypt);

            Assert.AreNotEqual(encryptedString, encryptedString2);
            Assert.AreNotEqual(stringToEncrypt, encryptedString);

            encryptionProvider = new EncryptionProvider();
            encryptionKeys = encryptionProvider.DecodeSecretKeysFromBase64(recoveryKey);
            encryptionProvider.SetSecretKeys(encryptionKeys);

            var decryptedString = encryptionProvider.DecryptString(encryptedString);

            Assert.AreEqual(stringToEncrypt, decryptedString);
        }
    }
}
