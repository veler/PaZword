using Microsoft.Toolkit.Uwp.UI.Controls;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Security;
using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Localization;
using PaZword.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.ViewModels
{
    /// <summary>
    /// Interaction logic for <see cref="SettingsPage"/>
    /// </summary>
    [Export(typeof(SettingsPageViewModel))]
    [Shared()]
    public sealed class SettingsPageViewModel : ViewModelBase
    {
        private const string SignInRemoteStorageProviderEvent = "Settings.Synchronization.SignIn.Command";
        private const string SignOutRemoteStorageProviderEvent = "Settings.Synchronization.SignOut.Command";
        private const string SyncNowEvent = "Settings.Synchronization.SyncNow.Command";
        private const string MarkdownLinkEvent = "Settings.MarkdownLink.Command";

        private readonly ISettingsProvider _settingsProvider;
        private readonly IWindowsHelloAuthProvider _windowsHelloAuthProvider;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly IRemoteSynchronizationService _remoteSynchronizationService;
        private readonly ILogger _logger;
        private readonly DispatcherTimer _timer;
        private readonly object _lock = new object();

        private bool _isAuthenticatedToRemoteStorageProvider;
        private bool _showSecurityWarning;
        private string _copyRightsDetail = string.Empty;
        private CancellationTokenSource _accountInfoCancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal SettingsPageStrings Strings => LanguageManager.Instance.SettingsPage;

        internal ElementTheme Theme
        {
            get => _settingsProvider.GetSetting(SettingsDefinitions.Theme);
            set
            {
                _settingsProvider.SetSetting(SettingsDefinitions.Theme, value);
                ((IApp)App.Current).UpdateColorTheme();
            }
        }

        /// <summary>
        /// Gets or sets whether the user is authenticated to a cloud storage provider.
        /// </summary>
        internal bool IsAuthenticatedToRemoteStorageProvider
        {
            get => _isAuthenticatedToRemoteStorageProvider;
            set
            {
                _isAuthenticatedToRemoteStorageProvider = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the list of remote storage providers.
        /// </summary>
        internal IEnumerable<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> RemoteStorageProviders { get; }

        /// <summary>
        /// Gets the current account's email address.
        /// </summary>
        internal TaskCompletionNotifier<string> CurrentAccountUserName
            => new TaskCompletionNotifier<string>(
                GetCurrentRemoteCloudStorageProvider()?.GetUserNameAsync(_accountInfoCancellationTokenSource.Token));

        /// <summary>
        /// Gets the current account's email address.
        /// </summary>
        internal TaskCompletionNotifier<string> CurrentAccountEmailAddress
            => new TaskCompletionNotifier<string>(
                GetCurrentRemoteCloudStorageProvider()?.GetUserEmailAddressAsync(_accountInfoCancellationTokenSource.Token));

        /// <summary>
        /// Gets the current account's email address.
        /// </summary>
        internal TaskCompletionNotifier<BitmapImage> CurrentAccountProfilePicture
            => new TaskCompletionNotifier<BitmapImage>(
                GetCurrentRemoteCloudStorageProvider()?.GetUserProfilePictureAsync(_accountInfoCancellationTokenSource.Token));

        /// <summary>
        /// Gets the name of the current account provider.
        /// </summary>
        internal string CurrentAccountProviderName => GetCurrentRemoteCloudStorageProvider()?.DisplayName;

        /// <summary>
        /// Gets or sets whether the application must synchronize the data with the Cloud.
        /// </summary>
        internal bool SynchronizeCloud
        {
            get => _settingsProvider.GetSetting(SettingsDefinitions.SyncDataWithCloud);
            set
            {
                _settingsProvider.SetSetting(SettingsDefinitions.SyncDataWithCloud, value);
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the FAQ about remote storage providers in Markdown.
        /// </summary>
        internal TaskCompletionNotifier<string> FaqRemoteStorageProvider { get; } = new TaskCompletionNotifier<string>(LanguageManager.Instance.GetFaqRemoteStorageProviderAsync());

        /// <summary>
        /// Gets the Third-party software notice.
        /// </summary>
        internal TaskCompletionNotifier<string> ThirdPartyNotices { get; } = new TaskCompletionNotifier<string>(LanguageManager.GetThirdPartyNoticesAsync());

        /// <summary>
        /// Gets the license.
        /// </summary>
        internal TaskCompletionNotifier<string> License { get; } = new TaskCompletionNotifier<string>(LanguageManager.GetLicenseAsync());

        /// <summary>
        /// Gets the privacy statement.
        /// </summary>
        internal TaskCompletionNotifier<string> PrivacyStatement { get; } = new TaskCompletionNotifier<string>(LanguageManager.Instance.GetPrivacyStatementAsync());

        /// <summary>
        /// Gets or sets whether the warning about all the security features disabled should be displayed.
        /// </summary>
        internal bool ShowSecurityWarning
        {
            get => _showSecurityWarning;
            set
            {
                _showSecurityWarning = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets a value that defines whether Windows Hello is enabled and that the user defined a PIN.
        /// </summary>
        internal bool WindowsHelloIsEnabled { get; private set; }

        /// <summary>
        /// Gets or sets whether the user must sign in the application with Windows Hello.
        /// </summary>
        internal bool UseWindowsHello
        {
            get => _settingsProvider.GetSetting(SettingsDefinitions.UseWindowsHello);
            set => SetUseWindowsHelloAsync(value).Forget();
        }

        /// <summary>
        /// Gets or sets whether the user must sign in the application with his trusted device or email address.
        /// </summary>
        internal bool UseTwoFactorAuthentication
        {
            get => _settingsProvider.GetSetting(SettingsDefinitions.UseTwoFactorAuthentication);
            set => SetUseTwoFactorAuthentication(value).Forget();
        }

        /// <summary>
        /// Gets or sets whether the user wants to be asked to enter its recovery key every occasionally to authenticate.
        /// </summary>
        internal bool AskSecretKeyOccasionally
        {
            get => _settingsProvider.GetSetting(SettingsDefinitions.AskSecretKeyOccasionally);
            set => _settingsProvider.SetSetting(SettingsDefinitions.AskSecretKeyOccasionally, value);
        }

        /// <summary>
        /// Gets or sets after how long does PaZword should lock following a period of inactivity.
        /// </summary>
        internal InactivityTime LockAfterInactivity
        {
            get => _settingsProvider.GetSetting(SettingsDefinitions.LockAfterInactivity);
            set => _settingsProvider.SetSetting(SettingsDefinitions.LockAfterInactivity, value);
        }

        /// <summary>
        /// Gets the version of the application.
        /// </summary>
        internal string Version => typeof(App).GetTypeInfo().Assembly.GetName().Version.ToString();

        /// <summary>
        /// Gets the copyright of the application.
        /// </summary>
        internal string Copyright
        {
            get
            {
                Assembly assembly = Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    object[] customAttributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                    if ((customAttributes != null) && (customAttributes.Length > 0))
                    {
                        _copyRightsDetail = ((AssemblyCopyrightAttribute)customAttributes[0]).Copyright;
                    }
                    if (string.IsNullOrEmpty(_copyRightsDetail))
                    {
                        _copyRightsDetail = string.Empty;
                    }
                }
                return _copyRightsDetail;
            }
        }

        /// <summary>
        /// Gets all the logs.
        /// </summary>
        internal string Logs => _logger.GetAllLogs().ToString();

        [ImportingConstructor]
        public SettingsPageViewModel(
            ISettingsProvider settingsProvider,
            IWindowsHelloAuthProvider windowsHelloAuthProvider,
            IEncryptionProvider encryptionProvider,
            IRemoteSynchronizationService remoteSynchronizationService,
            ILogger logger,
            [ImportMany] IEnumerable<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> remoteStorageProviders)
        {
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));
            _windowsHelloAuthProvider = Arguments.NotNull(windowsHelloAuthProvider, nameof(windowsHelloAuthProvider));
            _encryptionProvider = Arguments.NotNull(encryptionProvider, nameof(encryptionProvider));
            _remoteSynchronizationService = Arguments.NotNull(remoteSynchronizationService, nameof(remoteSynchronizationService));
            _logger = Arguments.NotNull(logger, nameof(logger));
            RemoteStorageProviders = Arguments.NotNull(remoteStorageProviders, nameof(remoteStorageProviders));

            logger.LogsChanged += Logger_LogsChanged;

            SignInToRemoteStorageServiceCommand = new AsyncActionCommand<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>>(_logger, SignInRemoteStorageProviderEvent, ExecuteSignInToRemoteStorageServiceCommandAsync);
            SignOutCommand = new AsyncActionCommand<object>(_logger, SignOutRemoteStorageProviderEvent, ExecuteSignOutCommandAsync);
            SyncNowCommand = new ActionCommand<object>(_logger, SyncNowEvent, ExecuteSyncNowCommand);
            MarkdownLinkClickedCommand = new AsyncActionCommand<LinkClickedEventArgs>(_logger, MarkdownLinkEvent, ExecuteMarkdownLinkClickedCommandAsync);

            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(2);
        }

        internal void Load()
        {
            _timer.Start();

            Timer_Tick(null, null);

            UpdatePasswordVault();
            UpdateAccountInfoAsync(_accountInfoCancellationTokenSource.Token).Forget();
        }

        internal void Unload()
        {
            _timer.Stop();
        }

        #region SignInToRemoteStorageServiceCommand

        public AsyncActionCommand<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> SignInToRemoteStorageServiceCommand { get; }

        private async Task ExecuteSignInToRemoteStorageServiceCommandAsync(Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata> remoteStorageProvider, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                _accountInfoCancellationTokenSource?.Cancel();
                _accountInfoCancellationTokenSource?.Dispose();
                _accountInfoCancellationTokenSource = new CancellationTokenSource();
                cancellationToken = _accountInfoCancellationTokenSource.Token;
            }

            // Sign in to the selected provider.
            if (await remoteStorageProvider.Value.SignInAsync(interactive: true, cancellationToken).ConfigureAwait(false))
            {
                _settingsProvider.SetSetting(SettingsDefinitions.SyncDataWithCloud, true);
                _settingsProvider.SetSetting(SettingsDefinitions.RemoteStorageProviderName, remoteStorageProvider.Metadata.ProviderName);

                await UpdateAccountInfoAsync(cancellationToken).ConfigureAwait(false);

                _remoteSynchronizationService.Cancel();
                _remoteSynchronizationService.QueueSynchronization();
            }
        }

        #endregion

        #region SignOutCommand

        internal AsyncActionCommand<object> SignOutCommand { get; }

        private async Task ExecuteSignOutCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                _accountInfoCancellationTokenSource?.Cancel();
                _accountInfoCancellationTokenSource?.Dispose();
                _accountInfoCancellationTokenSource = new CancellationTokenSource();
            }

            await GetCurrentRemoteCloudStorageProvider().SignOutAsync().ConfigureAwait(false);

            _settingsProvider.ResetSetting(SettingsDefinitions.SyncDataWithCloud);
            _settingsProvider.ResetSetting(SettingsDefinitions.RemoteStorageProviderName);

            await UpdateAccountInfoAsync(_accountInfoCancellationTokenSource.Token).ConfigureAwait(false);
        }

        #endregion

        #region SyncNowCommand

        internal ActionCommand<object> SyncNowCommand { get; }

        private void ExecuteSyncNowCommand(object parameter)
        {
            _remoteSynchronizationService.Cancel();
            _remoteSynchronizationService.QueueSynchronization();
        }

        #endregion

        #region MarkdownLinkClickedCommand

        internal AsyncActionCommand<LinkClickedEventArgs> MarkdownLinkClickedCommand { get; }

        private async Task ExecuteMarkdownLinkClickedCommandAsync(LinkClickedEventArgs args, CancellationToken cancellationToken)
        {
            try
            {
                string uriToLaunch = args.Link;
                var uri = new Uri(uriToLaunch);

                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            catch { }
        }

        #endregion  

        private void Logger_LogsChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(Logs));
        }

        private void Timer_Tick(object sender, object e)
        {
            _timer.Stop();
            Task.Run(async () =>
            {
                try
                {
                    WindowsHelloIsEnabled = await _windowsHelloAuthProvider.IsWindowsHelloEnabledAsync().ConfigureAwait(false);
                    RaisePropertyChanged(nameof(WindowsHelloIsEnabled));
                }
                finally
                {
                    await TaskHelper.RunOnUIThreadAsync(() =>
                    {
                        _timer.Start();
                    }).ConfigureAwait(false);
                }
            });
        }

        private async Task SetUseWindowsHelloAsync(bool useWindowsHello)
        {
            await TaskHelper.RunOnUIThreadAsync(async () =>
            {
                if (UseWindowsHello && !useWindowsHello)
                {
                    // If the user tries to disabled the authentication, verify his identity.
                    if (await _windowsHelloAuthProvider.AuthenticateAsync()
                        .ConfigureAwait(true)) // run on the current context.
                    {
                        _settingsProvider.SetSetting(SettingsDefinitions.UseWindowsHello, useWindowsHello);
                    }
                }
                else
                {
                    _settingsProvider.SetSetting(SettingsDefinitions.UseWindowsHello, useWindowsHello);
                }

                UpdatePasswordVault();
                RaisePropertyChanged(nameof(UseWindowsHello));
            }).ConfigureAwait(false);
        }

        private async Task SetUseTwoFactorAuthentication(bool useTwoFactorAuthentication)
        {
            if (!UseTwoFactorAuthentication && useTwoFactorAuthentication)
            {
                // If the user tries to enable the authentication
                var confirmationDialog = new SetupTwoFactorAuthenticationDialog();
                await confirmationDialog.ShowAsync();
            }
            else
            {
                _settingsProvider.SetSetting(SettingsDefinitions.UseTwoFactorAuthentication, useTwoFactorAuthentication);
            }

            UpdatePasswordVault();
            RaisePropertyChanged(nameof(UseTwoFactorAuthentication));
        }

        private void UpdatePasswordVault()
        {
            if (!UseWindowsHello && !UseTwoFactorAuthentication)
            {
                // Since all the additional security features are disabled,
                // we delete the user recovery key from the credential locker, which will force
                // the user to enter his recovery key the next time he opens the app, on all his devices 
                // (as a side effect since the credential locker is sync accrossed devices),
                // until he enable at least one feature.
                _encryptionProvider.DeleteSecretKeysFromPasswordVault();
                ShowSecurityWarning = true;
            }
            else
            {
                // Brings the user recovery key back to the credential locker.
                _encryptionProvider.PersistSecretKeysToPasswordVault();
                ShowSecurityWarning = false;
            }
        }

        private async Task UpdateAccountInfoAsync(CancellationToken cancellationToken)
        {
            IRemoteStorageProvider provider = GetCurrentRemoteCloudStorageProvider();
            if (provider != null)
            {
                IsAuthenticatedToRemoteStorageProvider = await provider.SignInAsync(interactive: false, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                IsAuthenticatedToRemoteStorageProvider = false;
            }

            RaisePropertyChanged(nameof(CurrentAccountUserName));
            RaisePropertyChanged(nameof(CurrentAccountEmailAddress));
            RaisePropertyChanged(nameof(CurrentAccountProfilePicture));
            RaisePropertyChanged(nameof(CurrentAccountProviderName));
            RaisePropertyChanged(nameof(SynchronizeCloud));
        }

        private IRemoteStorageProvider GetCurrentRemoteCloudStorageProvider()
        {
            string targetterProviderName = _settingsProvider.GetSetting(SettingsDefinitions.RemoteStorageProviderName);
            return RemoteStorageProviders.SingleOrDefault(m => string.Equals(m.Metadata.ProviderName, targetterProviderName, StringComparison.Ordinal))?.Value;
        }
    }
}
