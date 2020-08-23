using Newtonsoft.Json;
using System;

namespace PaZword.Core.Services.Icons.Bing
{
    internal sealed class Image
    {
        [JsonProperty("provider")]
        public Provider[] Provider { get; set; }

        [JsonProperty("hostPageUrl")]
        public Uri HostPageUrl { get; set; }
    }
}
