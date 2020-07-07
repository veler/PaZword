using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace PaZword.Localization
{
    public partial class LanguageManager
    {
        private const string FaqRemoteStorageProviderFileName = "FAQ-RemoteStorageProvider.md";
        private const string PrivacyStatementFileName = "PrivacyStatement.md";
        private const string ThirdPartyNoticesFileName = "ThirdPartyNotices.md";
        private const string LicenseFileName = "LICENSE.md";

        public async Task<string> GetFaqRemoteStorageProviderAsync()
        {
            string result = await GetLocalFileContentAsync($"{GetCurrentCulture().Name}\\{FaqRemoteStorageProviderFileName}").ConfigureAwait(false);

            if (string.IsNullOrEmpty(result))
            {
                result = await GetLocalFileContentAsync($"{GetCurrentCulture().TwoLetterISOLanguageName}\\{FaqRemoteStorageProviderFileName}").ConfigureAwait(false);
            }

            return result;
        }

        public async Task<string> GetPrivacyStatementAsync()
        {
            string result = await GetLocalFileContentAsync($"{GetCurrentCulture().Name}\\{PrivacyStatementFileName}").ConfigureAwait(false);

            if (string.IsNullOrEmpty(result))
            {
                result = await GetLocalFileContentAsync($"{GetCurrentCulture().TwoLetterISOLanguageName}\\{PrivacyStatementFileName}").ConfigureAwait(false);
            }

            return result;
        }

        public static Task<string> GetThirdPartyNoticesAsync()
        {
            return GetLocalFileContentAsync(ThirdPartyNoticesFileName);
        }

        public static Task<string> GetLicenseAsync()
        {
            return GetLocalFileContentAsync(LicenseFileName);
        }

        private static async Task<string> GetLocalFileContentAsync(string filePath)
        {
            StorageFolder installationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            var path = $"Strings\\{filePath}";
            IStorageItem file = await installationFolder.TryGetItemAsync(path);
            if (file != null)
            {
                return StringEncodingHelper.DetectAndReadTextWithEncoding(file.Path);
            }

            return string.Empty;
        }
    }
}
