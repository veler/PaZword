namespace PaZword.Core
{
    /// <summary>
    /// Provides constants.
    /// </summary>
    public static class Constants
    {
        public const string PwnedRecurrentTask = "Pwned";
        public const string InactivityDetectionRecurrentTask = "InactivityDetection";
        public const string RequestRateAndReviewRecurrentTask = "RequestRateAndReview";

        public const string CategoryAllId = "{00000000-0000-0000-0000-000000000000}";

        public const char PasswordMask = '•';
        internal const uint AccountIconSize = 128;

        public const int TwoFactorAuthenticationCodeEmailAllowedInterval = 5; // The user has 5 min to use the pin sent by email.

        // We limit the user input to 40k characters because SecureString has approximately 
        // a limit of 65k and an encrypted 40k string tend to be very close from this limit.
        public const int StringSizeLimit = 40000;

        public const int DataFileCountLimit = 2000;
        public const int DataFileSizeLimit = 4; // 4 MB. 
        public const int DataFileThumbnailSize = 96;
        internal const string UserDataFolderName = "UserData";
        internal const string UserDataBundleFileName = "data.pzd";
    }
}
