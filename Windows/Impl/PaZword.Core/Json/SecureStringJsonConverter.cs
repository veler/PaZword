using Newtonsoft.Json;
using System;
using System.Security;

namespace PaZword.Core.Json
{
    public sealed class SecureStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SecureString);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }

            string value = reader.Value.ToString();
            return value.ToSecureString();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                var secureValue = (SecureString)value;
                writer.WriteValue(secureValue.ToUnsecureString());
            }
        }
    }
}
