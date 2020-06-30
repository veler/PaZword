using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Api.Data
{
    /// <summary>
    /// Provides a set of methods that help to serialize data.
    /// </summary>
    public interface ISerializationProvider
    {
        /// <summary>
        /// Clone a specified object by returning a new instance of it with the same data inside.
        /// </summary>
        /// <typeparam name="T">The type of data</typeparam>
        /// <param name="data">The data</param>
        /// <returns>The clone of the given data</returns>
        T CloneObject<T>(T data) where T : class;

        /// <summary>
        /// Serialize a data to JSON.
        /// </summary>
        /// <param name="data">The data to serialize</param>
        /// <returns>The data in a JSON format</returns>
        string SerializeObject(object data);

        /// <summary>
        /// Deserialize a JSON data to an object.
        /// </summary>
        /// <typeparam name="T">The expected type of data.</typeparam>
        /// <param name="jsonData">The JSON data.</param>
        /// <returns>An instance of the specified type.</returns>
        T DeserializeObject<T>(string jsonData) where T : class;

        /// <summary>
        /// Gets the MD5 Hash of a string.
        /// </summary>
        /// <param name="data">The string to determine its hash.</param>
        /// <returns>A <see cref="string"/> that corresponds to the MD5 Hash of the data.</returns>
        SecureString GetMD5Hash(string data);

        /// <summary>
        /// Gets the SHA1 Hash of a string.
        /// </summary>
        /// <param name="data">The string to determine its hash.</param>
        /// <returns>A <see cref="string"/> that corresponds to the SHA1 Hash of the data.</returns>
        string GetSha1Hash(string data);

        /// <summary>
        /// Convert a Base64 string representation of an image to a <see cref="BitmapImage"/>.
        /// </summary>
        /// <remarks>
        /// This method should run on the UI thread.
        /// </remarks>
        /// <param name="base64">The Base64 string representation of an image</param>
        /// <returns>The <see cref="BitmapImage"/>.</returns>
        Task<BitmapImage> Base64ToBitmapImageAsync(string base64);

        /// <summary>
        /// Retrieves the dominant color of a picture from its Base64 string representation.
        /// </summary>
        /// <param name="base64">The Base64 string representation of an image</param>
        /// <returns>The <see cref="Color"/>.</returns>
        Task<Color> Base64ToDominantColorAsync(string base64);

        /// <summary>
        /// Convert a <see cref="WriteableBitmap"/> to a Base64 string representation.
        /// </summary>
        /// <remarks>
        /// This method should run on the UI thread.
        /// </remarks>
        /// <param name="bitmap">The <see cref="WriteableBitmap"/> to convert.</param>
        /// <returns>A Base64 string representation of the <see cref="WriteableBitmap"/>.</returns>
        Task<string> WritableBitmapToBase64Async(WriteableBitmap bitmap, CancellationToken cancellationToken);
    }
}
