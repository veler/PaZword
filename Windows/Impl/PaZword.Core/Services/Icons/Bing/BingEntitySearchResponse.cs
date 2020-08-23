using Newtonsoft.Json;

namespace PaZword.Core.Services.Icons.Bing
{
    internal sealed class BingEntitySearchResponse
    {
        [JsonProperty("entities")]
        public Entities Entities { get; set; }
    }
}
