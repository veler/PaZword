using Newtonsoft.Json;
using PaZword.Api;
using PaZword.Core.Services.Icons.Bing;
using PaZword.Localization;
using System;
using System.Composition;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Core.Services.Icons
{
    [Export(typeof(BingEntitySearch))]
    internal sealed class BingEntitySearch
    {
        private const string BingEntitySearchFaultEvent = "IconService.BingEntitySearch.Fault";

        // See https://docs.microsoft.com/en-us/rest/api/cognitiveservices-bingsearch/bing-entities-api-v7-reference
        private const int QueryMaxLength = 2048;
        private const string BingEntitySearchApiEndpoint = "https://api.cognitive.microsoft.com/bing/v7.0/entities?safeSearch=Moderate&responseFilter=Entities&ReponseFormat=JSON&mkt={0}&setLang={1}&q={2}";
        private const string AzureApiKeyHeaderName = "Ocp-Apim-Subscription-Key";
        private const string ExpectedProviderType = "Organization";

        private readonly ILogger _logger;

        [ImportingConstructor]
        public BingEntitySearch(ILogger logger)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
        }

        /// <summary>
        /// Query Microsoft Bing Entity Search to detect an organization's icon URL based on the given <paramref name="entityName"/>.
        /// </summary>
        /// <param name="entityName">The name of an account.</param>
        /// <returns>A URL to an image.</returns>
        internal async Task<(Uri organizationWebsite, Uri iconUrl)> GetOrganizationAndIconUrlAsync(string entityName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return (null, null);
            }

            try
            {
                var market = LanguageManager.Instance.GetCurrentCulture().Name;
                var language = LanguageManager.Instance.GetCurrentCulture().TwoLetterISOLanguageName;
                var uri = string.Format(
                    CultureInfo.CurrentCulture,
                    BingEntitySearchApiEndpoint,
                    WebUtility.UrlEncode(market),
                    WebUtility.UrlEncode(language),
                    WebUtility.UrlEncode(entityName));

                if (uri.Length >= QueryMaxLength)
                {
                    return (null, null);
                }

                using (var httpClient = new HttpClient())
                {
                    // Set the Azure API key.
                    httpClient.DefaultRequestHeaders.Add(AzureApiKeyHeaderName, ServicesKeys.MicrosoftAzure);

                    // Search if Bing Entity Search knows something about this entity name.
                    using (HttpResponseMessage result = await httpClient.GetAsync(new Uri(uri), cancellationToken).ConfigureAwait(false))
                    {
                        if (result.StatusCode == HttpStatusCode.OK)
                        {
                            string resultJson = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var bingEntitySearchResponse = JsonConvert.DeserializeObject<BingEntitySearchResponse>(resultJson);

                            if (bingEntitySearchResponse?.Entities?.Value?.Length == 1)
                            {
                                if (bingEntitySearchResponse.Entities.Value[0]?.Image?.Provider?.Length == 1
                                    && string.Equals(bingEntitySearchResponse.Entities.Value[0].Image.Provider[0].Type, ExpectedProviderType, StringComparison.Ordinal))
                                {
                                    return (bingEntitySearchResponse.Entities.Value[0].Url, bingEntitySearchResponse.Entities.Value[0].Image.HostPageUrl);
                                }

                                return (bingEntitySearchResponse.Entities.Value[0].Url, null);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogFault(BingEntitySearchFaultEvent, $"Unable to detect an image URL throug Bing Entity Search.", ex);
            }

            return (null, null);
        }
    }
}
