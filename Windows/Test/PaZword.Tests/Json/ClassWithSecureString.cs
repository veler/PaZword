using Newtonsoft.Json;
using PaZword.Core.Json;
using System.Security;

namespace PaZword.Tests.Json
{
    class ClassWithSecureString
    {
        [JsonConverter(typeof(SecureStringJsonConverter))]
        public SecureString Secret { get; set; }
    }
}
