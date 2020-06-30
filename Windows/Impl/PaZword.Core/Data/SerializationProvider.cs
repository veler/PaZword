using ColorThiefDotNet;
using Newtonsoft.Json;
using PaZword.Api.Data;
using PaZword.Core.Threading;
using System;
using System.Composition;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Core.Data
{
    [Export(typeof(ISerializationProvider))]
    [Shared()]
    internal sealed class SerializationProvider : ISerializationProvider
    {
        public T CloneObject<T>(T data) where T : class
            => DeserializeObject<T>(SerializeObject(data));

        public string SerializeObject(object data)
        {
            return JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            });
        }

        public T DeserializeObject<T>(string jsonData) where T : class
        {
            return JsonConvert.DeserializeObject<T>(jsonData, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        public SecureString GetMD5Hash(string data)
        {
            string algorithmName = HashAlgorithmNames.Md5;
            IBuffer dataBuffer = CryptographicBuffer.ConvertStringToBinary(data, BinaryStringEncoding.Utf8);
            HashAlgorithmProvider algorithmprovider = HashAlgorithmProvider.OpenAlgorithm(algorithmName);
            IBuffer hashBuffer = algorithmprovider.HashData(dataBuffer);

            if (hashBuffer.Length != algorithmprovider.HashLength)
            {
                throw new Exception("There was an error creating the hash");
            }

            return CryptographicBuffer.EncodeToHexString(hashBuffer).ToSecureString();
        }

        public string GetSha1Hash(string data)
        {
            string algorithmName = HashAlgorithmNames.Sha1;
            IBuffer dataBuffer = CryptographicBuffer.ConvertStringToBinary(data, BinaryStringEncoding.Utf8);
            HashAlgorithmProvider algorithmprovider = HashAlgorithmProvider.OpenAlgorithm(algorithmName);
            IBuffer hashBuffer = algorithmprovider.HashData(dataBuffer);

            if (hashBuffer.Length != algorithmprovider.HashLength)
            {
                throw new Exception("There was an error creating the hash");
            }

            return CryptographicBuffer.EncodeToHexString(hashBuffer);
        }

        public async Task<BitmapImage> Base64ToBitmapImageAsync(string base64)
        {
            Arguments.NotNullOrWhiteSpace(base64, nameof(base64));
            TaskHelper.ThrowIfNotOnUIThread();

            try
            {
                byte[] bytes = Convert.FromBase64String(base64);

                using (InMemoryRandomAccessStream memoryStream = new InMemoryRandomAccessStream())
                {
                    using (DataWriter writer = new DataWriter(memoryStream.GetOutputStreamAt(0)))
                    {
                        writer.WriteBytes(bytes);
                        await writer.StoreAsync();
                    }

                    var image = new BitmapImage();
                    image.SetSource(memoryStream);
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<Windows.UI.Color> Base64ToDominantColorAsync(string base64)
        {
            Arguments.NotNullOrWhiteSpace(base64, nameof(base64));

            try
            {
                var bytes = Convert.FromBase64String(base64);

                using (InMemoryRandomAccessStream memoryStream = new InMemoryRandomAccessStream())
                {
                    using (DataWriter writer = new DataWriter(memoryStream.GetOutputStreamAt(0)))
                    {
                        writer.WriteBytes(bytes);
                        await writer.StoreAsync();
                    }

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(memoryStream);

                    var colorThief = new ColorThief();
                    QuantizedColor quantizedColor = await colorThief.GetColor(decoder).ConfigureAwait(false);

                    return Windows.UI.Color.FromArgb(255, quantizedColor.Color.R, quantizedColor.Color.G, quantizedColor.Color.B);
                }
            }
            catch
            {
                return Windows.UI.Color.FromArgb(255, 255, 255, 255);
            }
        }

        public async Task<string> WritableBitmapToBase64Async(WriteableBitmap bitmap, CancellationToken cancellationToken)
        {
            Arguments.NotNull(bitmap, nameof(bitmap));
            TaskHelper.ThrowIfNotOnUIThread();

            using (var memoryStream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memoryStream);

                using (SoftwareBitmap softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(bitmap.PixelBuffer, BitmapPixelFormat.Rgba8, bitmap.PixelWidth, bitmap.PixelHeight))
                {
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    await encoder.FlushAsync();
                }

                var bytes = new byte[memoryStream.Size];
                using (Stream stream = memoryStream.AsStream())
                {
                    await stream.ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }

                return Convert.ToBase64String(bytes);
            }
        }
    }
}
