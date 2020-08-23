using Newtonsoft.Json;
using System;

namespace PaZword.Core.Services.Icons.Favicon
{
    internal sealed class Icon
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }
    }
}
