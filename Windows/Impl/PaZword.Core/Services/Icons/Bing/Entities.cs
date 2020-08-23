using Newtonsoft.Json;

namespace PaZword.Core.Services.Icons.Bing
{
    internal sealed class Entities
    {
        [JsonProperty("value")]
        public ValueElement[] Value { get; set; }
    }
}
