using PaZword.Api.Services;
using System;
using System.Globalization;
using Windows.UI.Xaml;

namespace PaZword.Api.Settings
{
    public static class SettingsDefinitions
    {
        /// <summary>
        /// Whether the user wants to authenticate with Windows Hello.
        /// </summary>
        public readonly static SettingDefinition<bool> UseWindowsHello = new SettingDefinition<bool>(
            name: nameof(UseWindowsHello),
            isRoaming: false,
            defaultValue: false);

        /// <summary>
        /// Whether the user wants to authenticate with Two Factor Authentication.
        /// </summary>
        public readonly static SettingDefinition<bool> UseTwoFactorAuthentication = new SettingDefinition<bool>(
            name: nameof(UseTwoFactorAuthentication),
            isRoaming: true,
            defaultValue: false);

        /// <summary>
        /// Whether the user wants to be asked to enter its recovery key every occasionally to authenticate.
        /// </summary>
        public readonly static SettingDefinition<bool> AskSecretKeyOccasionally = new SettingDefinition<bool>(
            name: nameof(AskSecretKeyOccasionally),
            isRoaming: true,
            defaultValue: false);

        /// <summary>
        /// The last time that PaZword asked the user to authenticate with his recovery key.
        /// </summary>
        public readonly static SettingDefinition<string> LastTimeAskedSecretKeyToAuthenticate = new SettingDefinition<string>(
            name: nameof(LastTimeAskedSecretKeyToAuthenticate),
            isRoaming: true,
            defaultValue: DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// Whether the app should lock after a certain amount of time of inactivity.
        /// </summary>
        public readonly static SettingDefinition<InactivityTime> LockAfterInactivity = new SettingDefinition<InactivityTime>(
            name: nameof(LockAfterInactivity),
            isRoaming: true,
            defaultValue: InactivityTime.TenMinutes);

        /// <summary>
        /// Whether the user data are synchronized with the cloud or not.
        /// </summary>
        public readonly static SettingDefinition<bool> SyncDataWithCloud = new SettingDefinition<bool>(
            name: nameof(SyncDataWithCloud),
            isRoaming: true,
            defaultValue: false);

        /// <summary>
        /// The <see cref="RemoteStorageProviderMetadata.ProviderName"/> the user uses to synchronize his data with the cloud.
        /// </summary>
        public readonly static SettingDefinition<string> RemoteStorageProviderName = new SettingDefinition<string>(
            name: nameof(RemoteStorageProviderName),
            isRoaming: true,
            defaultValue: string.Empty);

        /// <summary>
        /// Whether it's the first time the application starts or not.
        /// </summary>
        public readonly static SettingDefinition<bool> FirstStart = new SettingDefinition<bool>(
            name: nameof(FirstStart),
            isRoaming: false,
            defaultValue: true);

        /// <summary>
        /// The color theme of the application.
        /// </summary>
        public readonly static SettingDefinition<ElementTheme> Theme = new SettingDefinition<ElementTheme>(
            name: nameof(Theme),
            isRoaming: false,
            defaultValue: ElementTheme.Default);

        /// <summary>
        /// The desired length of passwords to generate
        /// </summary>
        public readonly static SettingDefinition<int> PasswordGeneratorLength = new SettingDefinition<int>(
            name: nameof(PasswordGeneratorLength),
            isRoaming: true,
            defaultValue: 12);

        /// <summary>
        /// Whether the generated password should be easy to read or not.
        /// </summary>
        public readonly static SettingDefinition<bool> PasswordGeneratorEasyToRead = new SettingDefinition<bool>(
            name: nameof(PasswordGeneratorEasyToRead),
            isRoaming: false,
            defaultValue: false);

        /// <summary>
        /// Whether the last time the app closed was due to a crash or not.
        /// </summary>
        public readonly static SettingDefinition<bool> LastAppShutdownWasCrash = new SettingDefinition<bool>(
            name: nameof(LastAppShutdownWasCrash),
            isRoaming: true,
            defaultValue: false);

        /// <summary>
        /// The number of time the app has been started.
        /// </summary>
        public readonly static SettingDefinition<int> NumberOfTimeTheAppStarted = new SettingDefinition<int>(
            name: nameof(NumberOfTimeTheAppStarted),
            isRoaming: true,
            defaultValue: 0);

        /// <summary>
        /// Whether the user rated and reviewed the app or not.
        /// </summary>
        public readonly static SettingDefinition<bool> UserRatedAndReviewedTheApp = new SettingDefinition<bool>(
            name: nameof(UserRatedAndReviewedTheApp),
            isRoaming: true,
            defaultValue: false);
    }
}
