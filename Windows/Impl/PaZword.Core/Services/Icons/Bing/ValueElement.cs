using Newtonsoft.Json;
using System;

namespace PaZword.Core.Services.Icons.Bing
{
    internal sealed class ValueElement
    {
        [JsonProperty("image")]
        public Image Image { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }
}
