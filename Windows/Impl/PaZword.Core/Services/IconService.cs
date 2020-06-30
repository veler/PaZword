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

namespace PaZword.Core.Services
{
    [Export(typeof(IIconService))]
    internal sealed class IconService : IIconService
    {
        private const string DownloadAccountIconBase64AsyncFaultEvent = "IconService.DownloadAccountIconBase64Async.Fault";
        private const string RiteKiteEvent = "IconService.ResolveIconOnlineAsync.RiteKit";
        private const string ClearBitEvent = "IconService.ResolveIconOnlineAsync.ClearBit";
        private const string NoResultEvent = "IconService.ResolveIconOnlineAsync.NoResult";

        private const string RiteKitEndpoint = "https://api.ritekit.com/v1/images/logo?domain={1}&client_id={0}";
        private const string ClearBitEndpoint = "http://logo.clearbit.com/{0}?size={1}&format=png";

        private readonly ISerializationProvider _serializationProvider;
        private readonly ILogger _logger;

        [ImportingConstructor]
        public IconService(
            ISerializationProvider serializationProvider,
            ILogger logger)
        {
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _logger = Arguments.NotNull(logger, nameof(logger));
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

            if (!string.IsNullOrEmpty(host))
            {
                // Try with RiteKit => https://ritekit.com/api-demo/company-logo
                _logger.LogEvent(RiteKiteEvent, $"Querying RiteKit for '{host}'.");
                var requestUri = new Uri(string.Format(CultureInfo.CurrentCulture, RiteKitEndpoint, ServicesKeys.RiteKitClientId, WebUtility.UrlEncode(host)));
                var base64Icon = await DownloadAccountIconBase64Async(requestUri, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(base64Icon))
                {
                    _logger.LogEvent(RiteKiteEvent, $"RiteKit query succeeded.");
                    return base64Icon;
                }

                // Try with ClearBit => https://clearbit.com/logo
                _logger.LogEvent(RiteKiteEvent, $"Querying ClearBit for '{host}'.");
                requestUri = new Uri(string.Format(CultureInfo.CurrentCulture, ClearBitEndpoint, WebUtility.UrlEncode(host), Constants.AccountIconSize));
                base64Icon = await DownloadAccountIconBase64Async(requestUri, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(base64Icon))
                {
                    _logger.LogEvent(ClearBitEvent, $"ClearBit query succeeded.");
                    return base64Icon;
                }

                // TODO: Try to get the Favicon of the website?
            }

            _logger.LogEvent(NoResultEvent, $"No result found for '{host}'.");
            return string.Empty;
        }

        /// <summary>
        /// Download a picture from a <see cref="Uri"/>, resize it too the <see cref="Consts.AccountIconSize"/> and return it as a Base64 string representation.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> of the image to download.</param>
        /// <returns>A Base64 string representation of the image, or an empty string if something wrong happened.</returns>
        private async Task<string> DownloadAccountIconBase64Async(Uri uri, CancellationToken cancellationToken)
        {
            Arguments.NotNull(uri, nameof(uri));

            try
            {
                using (var httpClient = new HttpClient())
                using (HttpResponseMessage result = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        using (Stream contentStream = await result.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            var bitmapDecoder = await BitmapDecoder.CreateAsync(contentStream.AsRandomAccessStream());
                            if (bitmapDecoder.PixelWidth >= Constants.AccountIconSize)
                            {
                                var differenceInPercent = 100 / ((double)bitmapDecoder.PixelHeight / bitmapDecoder.PixelWidth);
                                if (differenceInPercent > 95 && differenceInPercent < 105)
                                {
                                    var transform = new BitmapTransform();
                                    var pixelData = (await bitmapDecoder.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.ColorManageToSRgb)).DetachPixelData();

                                    return await TaskHelper.RunOnUIThreadAsync(async () =>
                                    {
                                        var bitmap = new WriteableBitmap((int)bitmapDecoder.PixelWidth, (int)bitmapDecoder.PixelHeight);
                                        using (var stream = bitmap.PixelBuffer.AsStream())
                                        {
                                            await stream.WriteAsync(pixelData, 0, pixelData.Length).ConfigureAwait(true);
                                        }

                                        return await _serializationProvider.WritableBitmapToBase64Async(bitmap, cancellationToken).ConfigureAwait(true);
                                    }).ConfigureAwait(false);
                                }
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
    }
}
