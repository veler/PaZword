using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Services;
using PaZword.Core.Threading;
using System;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Core.Services.Icons
{
    [Export(typeof(IIconService))]
    internal sealed class IconService : IIconService
    {
        private const string DownloadAccountIconBase64AsyncFaultEvent = "IconService.DownloadAccountIconBase64Async.Fault";
        private const string RiteKiteEvent = "IconService.ResolveIconOnlineAsync.RiteKit";
        private const string ClearBitEvent = "IconService.ResolveIconOnlineAsync.ClearBit";
        private const string BingEntitySearchEvent = "IconService.ResolveIconOnlineAsync.BingEntitySearch";
        private const string FaviconFinderEvent = "IconService.ResolveIconOnlineAsync.FaviconFinder";
        private const string NoResultEvent = "IconService.ResolveIconOnlineAsync.NoResult";

        private const string RiteKitEndpoint = "https://api.ritekit.com/v1/images/logo?domain={1}&client_id={0}";
        private const string ClearBitEndpoint = "http://logo.clearbit.com/{0}?size={1}&format=png";

        private readonly ISerializationProvider _serializationProvider;
        private readonly ILogger _logger;
        private readonly BingEntitySearch _bingEntitySearch;
        private readonly FaviconFinder _faviconFinder;

        [ImportingConstructor]
        public IconService(
            ISerializationProvider serializationProvider,
            ILogger logger,
            BingEntitySearch bingEntitySearch,
            FaviconFinder faviconFinder)
        {
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _logger = Arguments.NotNull(logger, nameof(logger));
            _bingEntitySearch = Arguments.NotNull(bingEntitySearch, nameof(bingEntitySearch));
            _faviconFinder = Arguments.NotNull(faviconFinder, nameof(faviconFinder));
        }

        public async Task<string> ResolveIconOnlineAsync(string entityName, string url, CancellationToken cancellationToken)
        {
            if (!CoreHelper.IsInternetAccess())
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(entityName) && string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                // Try with Bing Entity Search by searching for the entity name.
                (string base64Icon, Uri organizationWebsite) info = await GetOrganizationAndUrlFromBingEntitySearch(entityName, shouldBeSquareShaped: true, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(info.base64Icon))
                {
                    return info.base64Icon;
                }

                url = info.organizationWebsite?.OriginalString;
            }

            string host = GenerateCompanyName(entityName, url);

            if (!string.IsNullOrEmpty(host))
            {
                // Try with The Favicon Finder
                var base64Icon = await GetIconFromFaviconFinder(host, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(base64Icon))
                {
                    return base64Icon;
                }

                // Try with RiteKit
                base64Icon = await GetIconFromRiteKit(host, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(base64Icon))
                {
                    return base64Icon;
                }

                // Retry with Bing Entity Search by searching for the generated domain name.
                (string base64Icon, Uri organizationWebsite) info = await GetOrganizationAndUrlFromBingEntitySearch(host, shouldBeSquareShaped: false, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(info.base64Icon))
                {
                    return info.base64Icon;
                }

                // Try with ClearBit
                base64Icon = await GetIconFromClearBit(host, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(base64Icon))
                {
                    return base64Icon;
                }
            }

            _logger.LogEvent(NoResultEvent, $"No result found for '{host}'.");
            return string.Empty;
        }

        private async Task<(string base64Icon, Uri organizationWebsite)> GetOrganizationAndUrlFromBingEntitySearch(string query, bool shouldBeSquareShaped, CancellationToken cancellationToken)
        {
            _logger.LogEvent(BingEntitySearchEvent, $"Querying Bing Entity Search for '{query}'.");
            (Uri organizationWebsite, Uri iconUrl) info = await _bingEntitySearch.GetOrganizationAndIconUrlAsync(query, cancellationToken).ConfigureAwait(false);

            if (info.iconUrl != null)
            {
                var base64Icon = await DownloadAccountIconBase64Async(info.iconUrl, shouldBeSquareShaped, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(base64Icon))
                {
                    _logger.LogEvent(BingEntitySearchEvent, $"Bing Entity Search query succeeded.");
                    return (base64Icon, null);
                }
            }

            return (string.Empty, info.organizationWebsite);
        }

        private async Task<string> GetIconFromFaviconFinder(string host, CancellationToken cancellationToken)
        {
            _logger.LogEvent(FaviconFinderEvent, $"Querying The Favicon Finder for '{host}'.");
            Uri iconUrl = await _faviconFinder.GetIconUrlAsync(host, cancellationToken).ConfigureAwait(false);

            if (iconUrl != null)
            {
                var base64Icon = await DownloadAccountIconBase64Async(iconUrl, shouldBeSquareShaped: true, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(base64Icon))
                {
                    _logger.LogEvent(FaviconFinderEvent, $"The Favicon Finder query succeeded.");
                    return base64Icon;
                }
            }

            return string.Empty;
        }

        private async Task<string> GetIconFromRiteKit(string host, CancellationToken cancellationToken)
        {
            _logger.LogEvent(RiteKiteEvent, $"Querying RiteKit for '{host}'.");
            var requestUri = new Uri(string.Format(
                CultureInfo.CurrentCulture,
                RiteKitEndpoint,
                ServicesKeys.RiteKitClientId,
                WebUtility.UrlEncode(host)));

            var base64Icon = await DownloadAccountIconBase64Async(requestUri, shouldBeSquareShaped: true, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(base64Icon))
            {
                _logger.LogEvent(RiteKiteEvent, $"RiteKit query succeeded.");
                return base64Icon;
            }

            return string.Empty;
        }

        private async Task<string> GetIconFromClearBit(string host, CancellationToken cancellationToken)
        {
            _logger.LogEvent(RiteKiteEvent, $"Querying ClearBit for '{host}'.");
            var requestUri = new Uri(string.Format(
                CultureInfo.CurrentCulture,
                ClearBitEndpoint,
                WebUtility.UrlEncode(host),
                Constants.AccountIconSize));

            var base64Icon = await DownloadAccountIconBase64Async(requestUri, shouldBeSquareShaped: true, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(base64Icon))
            {
                _logger.LogEvent(ClearBitEvent, $"ClearBit query succeeded.");
                return base64Icon;
            }

            return string.Empty;
        }

        /// <summary>
        /// Download a picture from a <see cref="Uri"/>, resize it too the <see cref="Constants.AccountIconSize"/> and return it as a Base64 string representation.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> of the image to download.</param>
        /// <returns>A Base64 string representation of the image, or an empty string if something wrong happened.</returns>
        private async Task<string> DownloadAccountIconBase64Async(Uri uri, bool shouldBeSquareShaped, CancellationToken cancellationToken)
        {
            Arguments.NotNull(uri, nameof(uri));

            try
            {
                using (var httpClient = new HttpClient())
                using (HttpResponseMessage result = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream contentStream = await result.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            var bitmapDecoder = await BitmapDecoder.CreateAsync(contentStream.AsRandomAccessStream());
                            if (bitmapDecoder.PixelWidth >= Constants.AccountIconSize && bitmapDecoder.PixelHeight > 0)
                            {
                                if (shouldBeSquareShaped)
                                {
                                    // verify the icon is approximately square-like shape (tolerance of 25%).
                                    var differenceInPercent = 100 / ((double)bitmapDecoder.PixelHeight / bitmapDecoder.PixelWidth);
                                    if (differenceInPercent < 75 || differenceInPercent > 125)
                                    {
                                        return string.Empty;
                                    }
                                }

                                var transform = new BitmapTransform();
                                var pixelData = (await bitmapDecoder.GetPixelDataAsync(
                                    BitmapPixelFormat.Rgba8,
                                    BitmapAlphaMode.Straight,
                                    transform,
                                    ExifOrientationMode.IgnoreExifOrientation,
                                    ColorManagementMode.ColorManageToSRgb)).DetachPixelData();

                                return await TaskHelper.RunOnUIThreadAsync(async () =>
                                {
                                    var bitmap = new WriteableBitmap((int)bitmapDecoder.PixelWidth, (int)bitmapDecoder.PixelHeight);
                                    using (var stream = bitmap.PixelBuffer.AsStream())
                                    {
                                        await stream.WriteAsync(pixelData, 0, pixelData.Length).ConfigureAwait(true);
                                    }

                                    if (bitmapDecoder.PixelWidth > Constants.AccountIconSize + 50)
                                    {
                                        // If we judge the image is too big, we resize it. This improves the image quality because the resize is bilinear,
                                        // and it reduce the size of the user data bundle file.
                                        double propotion = bitmapDecoder.PixelWidth / (double)bitmapDecoder.PixelHeight;
                                        bitmap = bitmap.Resize(
                                            (int)Constants.AccountIconSize,
                                            (int)(Constants.AccountIconSize / propotion),
                                            WriteableBitmapExtensions.Interpolation.Bilinear);
                                    }

                                    return await _serializationProvider.WritableBitmapToBase64Async(bitmap, cancellationToken).ConfigureAwait(true);
                                }).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogFault(DownloadAccountIconBase64AsyncFaultEvent, $"Unable to download an icon from the url '{uri.OriginalString}'.", ex);
            }

            return string.Empty;
        }

        private static string GenerateCompanyName(string entityName, string url)
        {
            var host = string.Empty;

            if (!string.IsNullOrWhiteSpace(url))
            {
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "http://" + url;
                }

                bool isUri = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp
                        || uriResult.Scheme == Uri.UriSchemeHttps);

                if (isUri)
                {
                    host = uriResult.Host;
                }
            }

            if (string.IsNullOrEmpty(host)
                && !string.IsNullOrWhiteSpace(entityName))
            {
                entityName = entityName
                    .Trim()
                    .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("&", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("/", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("?", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("%", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("#", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("@", string.Empty, StringComparison.OrdinalIgnoreCase);
                host = $"{entityName.ToLower(CultureInfo.CurrentCulture)}";
                if (!host.Contains(".", StringComparison.OrdinalIgnoreCase))
                {
                    host += ".com";
                }
            }

            return host;
        }
    }
}
