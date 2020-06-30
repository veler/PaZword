using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PaZword.Core;

namespace PaZword.Tests.Json
{
    [TestClass]
    public class SecureStringJsonConverterTests
    {
        [TestMethod]
        public void SerializesSecureString()
        {
            var obj = new ClassWithSecureString { Secret = "SECRET".ToSecureString() };

            var json = JsonConvert.SerializeObject(obj);

            Assert.AreEqual(@"{""Secret"":""SECRET""}", json);
        }

        [TestMethod]
        public void SerializesNullValue()
        {
            var obj = new ClassWithSecureString { Secret = null };

            var json = JsonConvert.SerializeObject(obj);

            Assert.AreEqual(@"{""Secret"":null}", json);
        }

        [TestMethod]
        public void DeserializesSecureString()
        {
            var json = @"{""Secret"":""SECRET""}";

            var obj = JsonConvert.DeserializeObject<ClassWithSecureString>(json);

            Assert.AreEqual("SECRET", obj.Secret.ToUnsecureString());
        }

        [TestMethod]
        public void DeserializesNullValue()
        {
            var json = @"{""Secret"":null}";

            var obj = JsonConvert.DeserializeObject<ClassWithSecureString>(json);

            Assert.IsNull(obj.Secret);
        }
    }
}
