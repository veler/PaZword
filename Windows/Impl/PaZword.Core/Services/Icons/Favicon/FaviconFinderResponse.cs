using Newtonsoft.Json;

namespace PaZword.Core.Services.Icons.Favicon
{
    internal sealed class FaviconFinderResponse
    {
        [JsonProperty("icons")]
        public Icon[] Icons { get; set; }
    }
}
