using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Security;
using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Localization;
using PaZword.Models;
using PaZword.Views;
using System;
using System.Composition;
using System.Globalization;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.ViewModels.Other
{
    /// <summary>
    /// Interaction logic for <see cref="AuthenticationPage"/>
    /// </summary>
    [Export(typeof(AuthenticationPageViewModel))]
    [Shared]
    public sealed class AuthenticationPageViewModel : ViewModelBase, IDisposable
    {
        private const string AuthenticationStepEvent = "Authentication.Step";
        private const string RetryWindowsHelloEvent = "Authentication.RetryWindowsHello.Command";
        private const string SendRecoveryKeyByEmailEvent = "Authentication.SendRecoveryByEmail.Command";
        private const string RecoveryKeyChangedEvent = "Authentication.RecoveryKey.Changed";

        private readonly ILogger _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IWindowsHelloAuthProvider _windowsHelloAuthProvider;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly ITwoFactorAuthProvider _twoFactorAuthProvider;
        private readonly IDataManager _dataManager;
        private readonly IRemoteSynchronizationService _remoteSynchronizationService;
        private readonly IRecurrentTaskService _recurrentTaskService;
        private readonly object _lock = new object();

        private CancellationTokenSource _loadDataCancellationTokenSource = new CancellationTokenSource();
        private AuthenticationStep _authenticationStep;
        private bool _isWindowsHelloAuthenticationInProgress;
        private bool _isTwoFactorAuthenticationByEmailInProgress;
        private bool _invalidRecoveryKey;
        private bool _userDataLoaded;
        private string _authenticationFailReason;
        private string _twoFactorVerificationCode;
        private string _twoFactorVerificationCodeEmail;

        [SecurityCritical]
        private SecureString _recoveryKey;

        [SecurityCritical]
        private bool _authenticatedWindowsHello;

        [SecurityCritical]
        private bool _authenticatedTwoFactorAuthentication;

        [SecurityCritical]
        private bool _authenticatedRecoveryKey;

        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal AuthenticationPageStrings Strings => LanguageManager.Instance.AuthenticationPage;

        internal string TwoFactorEmailInstruction
            => Strings.GetFormattedTwoFactorEmailInstruction(
            _twoFactorAuthProvider.GetRecoveryEmailAddressFromPassowrdVault(),
            Constants.TwoFactorAuthenticationCodeEmailAllowedInterval.ToString(CultureInfo.CurrentCulture));

        /// <summary>
        /// Gets whether an internet access is available.
        /// </summary>
        internal bool IsInternetAccess => CoreHelper.IsInternetAccess();

        /// <summary>
        /// Gets or sets the authentication step to display in the UI.
        /// </summary>
        internal AuthenticationStep AuthenticationStep
        {
            get => _authenticationStep;
            set
            {
                _authenticationStep = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets a value that defines whether the Windows Hello authentication is in progress or not.
        /// </summary>
        internal bool IsWindowsHelloAuthenticationInProgress
        {
            get => _isWindowsHelloAuthenticationInProgress;
            set
            {
                _isWindowsHelloAuthenticationInProgress = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets a value that defines whether the two factor authentication is in progress or not.
        /// </summary>
        internal bool IsTwoFactorAuthenticationByEmailInProgress
        {
            get => _isTwoFactorAuthenticationByEmailInProgress;
            set
            {
                _isTwoFactorAuthenticationByEmailInProgress = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the two factor authentication verification code.
        /// </summary>
        internal string TwoFactorVerificationCode
        {
            get => _twoFactorVerificationCode;
            set
            {
                _twoFactorVerificationCode = value;
                RaisePropertyChanged();
                AuthenticateTwoFactorAuthentication();
            }
        }

        /// <summary>
        /// Gets or sets the two factor authentication verification code by email.
        /// </summary>
        internal string TwoFactorVerificationCodeEmail
        {
            get => _twoFactorVerificationCodeEmail;
            set
            {
                _twoFactorVerificationCodeEmail = value;
                RaisePropertyChanged();
                AuthenticateTwoFactorAuthenticationEmail();
            }
        }

        /// <summary>
        /// Gets or sets the recovery key to use to authenticate.
        /// </summary>
        internal SecureString RecoveryKey
        {
            get => _recoveryKey;
            set
            {
                _recoveryKey?.Dispose();
                _recoveryKey = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether the entered recovery key is invalid.
        /// </summary>
        internal bool InvalidRecoveryKey
        {
            get => _invalidRecoveryKey;
            set
            {
                _invalidRecoveryKey = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the reason why the authentication failed.
        /// </summary>
        internal string AuthenticationFailReason
        {
            get => _authenticationFailReason;
            set
            {
                _authenticationFailReason = value;
                RaisePropertyChanged();
            }
        }

        [ImportingConstructor]
        public AuthenticationPageViewModel(
            ILogger logger,
            ISettingsProvider settingsProvider,
            IWindowsHelloAuthProvider windowsHelloAuthProvider,
            IEncryptionProvider encryptionProvider,
            ITwoFactorAuthProvider twoFactorAuthProvider,
            IDataManager dataManager,
            IRemoteSynchronizationService remoteSynchronizationService,
            IRecurrentTaskService recurrentTaskService)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));
            _windowsHelloAuthProvider = Arguments.NotNull(windowsHelloAuthProvider, nameof(windowsHelloAuthProvider));
            _encryptionProvider = Arguments.NotNull(encryptionProvider, nameof(encryptionProvider));
            _twoFactorAuthProvider = Arguments.NotNull(twoFactorAuthProvider, nameof(twoFactorAuthProvider));
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            _remoteSynchronizationService = Arguments.NotNull(remoteSynchronizationService, nameof(remoteSynchronizationService));

            _recurrentTaskService = Arguments.NotNull(recurrentTaskService, nameof(recurrentTaskService));

            RetryWindowsHelloCommand = new ActionCommand<object>(_logger, RetryWindowsHelloEvent, ExecuteRetryWindowsHelloCommand);
            SendRecoveryKeyByEmailCommand = new AsyncActionCommand<object>(_logger, SendRecoveryKeyByEmailEvent, ExecuteSendRecoveryKeyByEmailCommandAsync, startOnNewThread: true);
            RecoveryKeyChangedCommand = new AsyncActionCommand<object>(_logger, RecoveryKeyChangedEvent, ExecuteRecoveryKeyChangedCommandAsync, isCancellable: true, startOnNewThread: true);
        }

        internal void Initialize()
        {
            TwoFactorVerificationCode = string.Empty;
            TwoFactorVerificationCodeEmail = string.Empty;
            IsWindowsHelloAuthenticationInProgress = false;
            IsTwoFactorAuthenticationByEmailInProgress = false;
            AuthenticationFailReason = string.Empty;
            InvalidRecoveryKey = false;
            RecoveryKey = null;

            _authenticatedWindowsHello = false;
            _authenticatedRecoveryKey = false;
            _authenticatedTwoFactorAuthentication = false;
            AuthenticationStep = AuthenticationStep.Unknown;

            _recurrentTaskService.Pause();
            DetermineNextAuthenticationStepAsync(loadFromCredentialLocker: true).Forget();
        }

        public void Dispose()
        {
            _loadDataCancellationTokenSource?.Dispose();
            _dataManager?.Dispose();
        }

        #region RetryWindowsHelloCommand

        internal ActionCommand<object> RetryWindowsHelloCommand { get; }

        private void ExecuteRetryWindowsHelloCommand(object parameter)
        {
            AuthenticateWindowsHelloAsync().Forget();
        }

        #endregion

        #region SendRecoveryKeyByEmailCommand

        internal AsyncActionCommand<object> SendRecoveryKeyByEmailCommand { get; }

        private async Task ExecuteSendRecoveryKeyByEmailCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            IsTwoFactorAuthenticationByEmailInProgress = true;
            // TODO: Give focus to text box?

            await _twoFactorAuthProvider.SendPinByEmailAsync().ConfigureAwait(false);
        }

        #endregion

        #region RecoveryKeyChangedCommand

        internal AsyncActionCommand<object> RecoveryKeyChangedCommand { get; }

        private async Task ExecuteRecoveryKeyChangedCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            string recoveryKey = RecoveryKey.ToUnsecureString();
            if (string.IsNullOrWhiteSpace(recoveryKey))
            {
                InvalidRecoveryKey = false;
                return;
            }

            try
            {
                _encryptionProvider.SetSecretKeys(_encryptionProvider.DecodeSecretKeysFromBase64(recoveryKey.Trim()));
            }
            catch
            {
                InvalidRecoveryKey = true;
                return;
            }

            if (await TryLoadDataAndSynchronizeAsync(loadFromCredentialLocker: false, cancellationToken).ConfigureAwait(false))
            {
                _authenticatedRecoveryKey = true;
                InvalidRecoveryKey = false;
                await DetermineNextAuthenticationStepAsync(loadFromCredentialLocker: false).ConfigureAwait(false);
            }
            else
            {
                InvalidRecoveryKey = true;
            }
        }

        #endregion

        private async Task DetermineNextAuthenticationStepAsync(bool loadFromCredentialLocker)
        {
            bool useWindowsHello = _settingsProvider.GetSetting(SettingsDefinitions.UseWindowsHello);
            bool useTwoFactorAuthentication = _settingsProvider.GetSetting(SettingsDefinitions.UseTwoFactorAuthentication);

            if (_settingsProvider.GetSetting(SettingsDefinitions.AskSecretKeyOccasionally)
                && DateTime.Parse(_settingsProvider.GetSetting(SettingsDefinitions.LastTimeAskedSecretKeyToAuthenticate), CultureInfo.InvariantCulture) < DateTime.UtcNow - TimeSpan.FromDays(14))
            {
                // If it's been 2 weeks since the last time we opened the app without asking the recovery key, we ask the recovery key.
                _encryptionProvider.DeleteSecretKeysFromPasswordVault();
                _settingsProvider.SetSetting(SettingsDefinitions.LastTimeAskedSecretKeyToAuthenticate, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            }

            if (!useWindowsHello && !useTwoFactorAuthentication && !_authenticatedRecoveryKey)
            {
                // The recovery key might be in the credential locker (since it's synchronized with the user MSA account
                // and that he may use another device with Windows Hello enabled for example) but if all the additional
                // security feature are disabled, we absolutely want to ask the user for the recovery key.
                _logger.LogEvent(AuthenticationStepEvent, "Recovery Key due to other security features being disabled.");
                AuthenticationStep = AuthenticationStep.RecoveryKey;
                return;
            }

            if (!await TryLoadDataAndSynchronizeAsync(loadFromCredentialLocker, CancellationToken.None).ConfigureAwait(false))
            {
                // Unable to load local data, probably because the recovery key doesn't exist in the credential locker.
                // Let's ask for it.
                _logger.LogEvent(AuthenticationStepEvent, "Recovery Key due impossibility to load local data.");
                AuthenticationStep = AuthenticationStep.RecoveryKey;
                return;
            }
            else if (useWindowsHello || useTwoFactorAuthentication)
            {
                // If loading the data succeeded, and that at least Windows Hello or two factor authentication are enabled,
                // then we persist the secret keys to the credential locker, so we won't ask the secret key the next time.
                _encryptionProvider.PersistSecretKeysToPasswordVault();
            }

            if (useWindowsHello && !_authenticatedWindowsHello)
            {
                _logger.LogEvent(AuthenticationStepEvent, "Windows Hello");
                AuthenticationStep = AuthenticationStep.WindowsHello;
                AuthenticateWindowsHelloAsync().Forget();
                return;
            }

            if (useTwoFactorAuthentication && !_authenticatedTwoFactorAuthentication)
            {
                _logger.LogEvent(AuthenticationStepEvent, "Two Factor Authentication");
                AuthenticationStep = AuthenticationStep.TwoFactorAuthentication;
                return;
            }

            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                _logger.LogEvent(AuthenticationStepEvent, "Authenticated!");
                Frame mainFrame = (Frame)Window.Current.Content;
                mainFrame.Navigate(typeof(MainPage));
            }).ConfigureAwait(false);
        }

        private async Task AuthenticateWindowsHelloAsync()
        {
            await TaskHelper.RunOnUIThreadAsync(async () =>
            {
                IsWindowsHelloAuthenticationInProgress = true;

                if (!await _windowsHelloAuthProvider.IsWindowsHelloEnabledAsync()
                    .ConfigureAwait(true)) // run on the current context.
                {
                    AuthenticationFailReason = Strings.WindowsHelloDisabled;
                    IsWindowsHelloAuthenticationInProgress = false;
                }
                else if (!await _windowsHelloAuthProvider.AuthenticateAsync()
                    .ConfigureAwait(true)) // run on the current context.
                {
                    AuthenticationFailReason = Strings.WindowsHelloFailed;
                    IsWindowsHelloAuthenticationInProgress = false;
                }
                else
                {
                    AuthenticationFailReason = string.Empty;
                    _authenticatedWindowsHello = true;
                    DetermineNextAuthenticationStepAsync(loadFromCredentialLocker: false).Forget();
                }
            }).ConfigureAwait(false);
        }

        private void AuthenticateTwoFactorAuthentication()
        {
            if (_twoFactorAuthProvider.ValidatePin(TwoFactorVerificationCode))
            {
                _authenticatedTwoFactorAuthentication = true;
                IsTwoFactorAuthenticationByEmailInProgress = false;
                DetermineNextAuthenticationStepAsync(loadFromCredentialLocker: false).Forget();
            }
        }

        private void AuthenticateTwoFactorAuthenticationEmail()
        {
            if (_twoFactorAuthProvider.ValidatePin(TwoFactorVerificationCodeEmail, allowedInterval: Constants.TwoFactorAuthenticationCodeEmailAllowedInterval))
            {
                _authenticatedTwoFactorAuthentication = true;
                IsTwoFactorAuthenticationByEmailInProgress = false;
                DetermineNextAuthenticationStepAsync(loadFromCredentialLocker: false).Forget();
            }
        }

        private async Task<bool> TryLoadDataAndSynchronizeAsync(bool loadFromCredentialLocker, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (_userDataLoaded)
                {
                    return true;
                }

                _loadDataCancellationTokenSource.Cancel();
                _loadDataCancellationTokenSource.Dispose();
                _loadDataCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            if (loadFromCredentialLocker && !_encryptionProvider.LoadSecretKeysFromPasswordVault())
            {
                return false;
            }

            try
            {
                await _dataManager.LoadOrCreateLocalUserDataBundleAsync(_loadDataCancellationTokenSource.Token).ConfigureAwait(false);
                _remoteSynchronizationService.Cancel();
                _remoteSynchronizationService.QueueSynchronization();

                lock (_lock)
                {
                    _userDataLoaded = true;
                }
                return true;
            }
            catch
            {
                // if it fails, it likely means that the current secret key in the encryption provider is wrong.
                return false;
            }
        }
    }
}
