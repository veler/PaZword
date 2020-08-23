using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Api.Services
{
    /// <summary>
    /// Provides a set of method to manage icons.
    /// </summary>
    public interface IIconService
    {
        /// <summary>
        /// Prompts the user a file open picker to select an image document.
        /// </summary>
        /// <returns>A Base64 representation of the selected image. If no image is selected, an empty string is returned.</returns>
        Task<string> PickUpIconFromLocalFileAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Tries to find an icon online that corresponds to a given <paramref name="entityName"/> or <paramref name="url"/>.
        /// </summary>
        /// <param name="entityName">A potential company name, website name or person.</param>
        /// <param name="url">A potential URL of a website</param>
        /// <returns>A Base64 representation of the finded icon. If no icon is found an empty string is returned.</returns>
        Task<string> ResolveIconOnlineAsync(string entityName, string url, CancellationToken cancellationToken);
    }
}
