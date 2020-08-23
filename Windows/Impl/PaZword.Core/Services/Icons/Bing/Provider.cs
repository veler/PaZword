using Newtonsoft.Json;
using System;

namespace PaZword.Core.Services.Icons.Bing
{
    internal sealed class Provider
    {
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }
}
