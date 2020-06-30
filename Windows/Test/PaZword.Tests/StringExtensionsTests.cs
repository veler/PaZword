using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Core;
using System.Security;

namespace PaZword.Tests
{
    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        public void SecureStringConvertionTest()
        {
            var str = "Hello World";
            using (SecureString secureStr = str.ToSecureString())
            {
                Assert.IsTrue(secureStr.IsReadOnly());
                Assert.AreEqual("Hello World", secureStr.ToUnsecureString());
            }
        }

        [TestMethod]
        public void IsNullOrEmptySecureStringTest()
        {
            var str = "Hello World";
            using (SecureString secureStr = str.ToSecureString())
            {
                Assert.IsFalse(StringExtensions.IsNullOrEmptySecureString(secureStr));
            }

            str = string.Empty;
            using (SecureString secureStr = str.ToSecureString())
            {
                Assert.IsTrue(StringExtensions.IsNullOrEmptySecureString(secureStr));
            }
        }

        [TestMethod]
        public void SecureStringIsEqualToTest()
        {
            var str = "Hello World";
            using (SecureString secureStr = str.ToSecureString())
            {
                str = "Hello World";
                using (SecureString secureStr2 = str.ToSecureString())
                {
                    Assert.IsTrue(secureStr.IsEqualTo(secureStr2));
                }

                str = "Hello World2";
                using (SecureString secureStr2 = str.ToSecureString())
                {
                    Assert.IsFalse(secureStr.IsEqualTo(secureStr2));
                }
            }
        }

        [TestMethod]
        public void Base64ConvertionTest()
        {
            var str = "Hello World";
            var base64 = str.EncodeToBase64();
            Assert.AreEqual(str, base64.DecodeFromBase64());
        }
    }
}
