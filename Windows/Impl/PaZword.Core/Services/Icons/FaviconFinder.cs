using Newtonsoft.Json;
using PaZword.Api;
using PaZword.Core.Services.Icons.Favicon;
using System;
using System.Composition;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Core.Services.Icons
{
    [Export(typeof(FaviconFinder))]
    internal sealed class FaviconFinder
    {
        private const string FaviconFinderFaultEvent = "IconService.FaviconFinder.Fault";

        private const string FaviconFinderApiEndpoint = "https://i.olsh.me/allicons.json?formats=png&url={0}";

        private readonly ILogger _logger;

        [ImportingConstructor]
        public FaviconFinder(ILogger logger)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
        }

        /// <summary>
        /// Query The Favicon Finder API to detect the favicon of a given website in PNG.
        /// </summary>
        /// <returns>A URL to an image.</returns>
        internal async Task<Uri> GetIconUrlAsync(string host, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            try
            {
                var uri = string.Format(
                    CultureInfo.CurrentCulture,
                    FaviconFinderApiEndpoint,
                    WebUtility.UrlEncode(host));

                using (var httpClient = new HttpClient())
                using (HttpResponseMessage result = await httpClient.GetAsync(new Uri(uri), cancellationToken).ConfigureAwait(false))
                {
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        string resultJson = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var faviconFinderResponse = JsonConvert.DeserializeObject<FaviconFinderResponse>(resultJson);

                        if (faviconFinderResponse?.Icons?.Length > 0)
                        {
                            for (int i = 0; i < faviconFinderResponse.Icons.Length; i++)
                            {
                                Icon icon = faviconFinderResponse.Icons[i];
                                if (icon != null && icon.Width >= Constants.AccountIconSize)
                                {
                                    return icon.Url;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogFault(FaviconFinderFaultEvent, $"Unable to detect an image URL throug Bing Entity Search.", ex);
            }

            return null;
        }
    }
}
