using PaZword.Api;
using PaZword.Api.Security;
using PaZword.Api.Settings;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Localization;
using System;
using System.Composition;
using System.Text.RegularExpressions;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace PaZword.ViewModels.Dialog
{
    /// <summary>
    /// Interaction logic for <see cref="SetupTwoFactorAuthenticationDialog"/>
    /// </summary>
    [Export(typeof(SetupTwoFactorAuthenticationDialogViewModel))]
    public sealed class SetupTwoFactorAuthenticationDialogViewModel : ViewModelBase
    {
        private const string SaveEvent = "SetupTwoFactorAuthentication.Save.Command";
        private const string EmailAddressPasteEvent = "SetupTwoFactorAuthentication.EmailAddress.Paste";
        private const string TextBoxKeyDownEvent = "SetupTwoFactorAuthentication.TextBox.KeyDown";

        private readonly ILogger _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly ITwoFactorAuthProvider _twoFactorAuthProvider;
        private readonly DispatcherTimer _timer;
        private readonly Regex _emailRegex = new Regex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private bool _dataValidated;
        private string _verificationCode;
        private string _emailAddress;
        private string _confirmEmailAddress;
        private ImageSource _qrCode;

        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal SetupTwoFactorAuthenticationStrings Strings => LanguageManager.Instance.SetupTwoFactorAuthentication;

        /// <summary>
        /// Gets the size of the QRCode, in pixels.
        /// </summary>
        internal int QRCodeSize => 150;

        /// <summary>
        /// Gets the QRCode to display.
        /// </summary>
        internal ImageSource QRCode
        {
            get => _qrCode;
            private set
            {
                _qrCode = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets a value that definew whether the user data are valid.
        /// </summary>
        internal bool DataValidated
        {
            get => _dataValidated;
            private set
            {
                _dataValidated = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the verification code.
        /// </summary>
        internal string VerificationCode
        {
            get => _verificationCode;
            set
            {
                _verificationCode = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        internal string EmailAddress
        {
            get => _emailAddress;
            set
            {
                _emailAddress = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the email address confirmation.
        /// </summary>
        internal string ConfirmEmailAddress
        {
            get => _confirmEmailAddress;
            set
            {
                _confirmEmailAddress = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Raised when the dialog should close.
        /// </summary>
        internal event EventHandler CloseDialog;

        [ImportingConstructor]
        public SetupTwoFactorAuthenticationDialogViewModel(
            ILogger logger,
            ISettingsProvider settingsProvider,
            ITwoFactorAuthProvider twoFactorAuthProvider)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));
            _twoFactorAuthProvider = Arguments.NotNull(twoFactorAuthProvider, nameof(twoFactorAuthProvider));

            PrimaryButtonClickCommand = new ActionCommand<object>(_logger, SaveEvent, ExecutePrimaryButtonClickCommand);
            EmailAddressBoxPasteCommand = new ActionCommand<TextControlPasteEventArgs>(_logger, EmailAddressPasteEvent, ExecuteEmailAddressBoxPasteCommand);
            TextBoxKeyDownCommand = new ActionCommand<KeyRoutedEventArgs>(_logger, TextBoxKeyDownEvent, ExecuteTextBoxKeyDownCommand);

            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Start();
        }

        #region PrimaryButtonClickCommand

        internal ActionCommand<object> PrimaryButtonClickCommand { get; }

        private void ExecutePrimaryButtonClickCommand(object parameter)
        {
            Save();
        }

        #endregion

        #region EmailAddressBoxPasteCommand

        internal ActionCommand<TextControlPasteEventArgs> EmailAddressBoxPasteCommand { get; }

        private void ExecuteEmailAddressBoxPasteCommand(TextControlPasteEventArgs parameter)
        {
            parameter.Handled = true; // Prevent the user from pasting in the Email/Confirm Email address.
        }

        #endregion

        #region TextBoxKeyDownCommand

        internal ActionCommand<KeyRoutedEventArgs> TextBoxKeyDownCommand { get; }

        private void ExecuteTextBoxKeyDownCommand(KeyRoutedEventArgs parameter)
        {
            if (parameter.Key == VirtualKey.Enter && DataValidated)
            {
                Save();
                CloseDialog?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        internal void Closed()
        {
            _timer.Stop();
        }

        private void Timer_Tick(object sender, object e)
        {
            TaskHelper.ThrowIfNotOnUIThread();

            bool isValidEmail = !string.IsNullOrWhiteSpace(EmailAddress)
                && !string.IsNullOrWhiteSpace(ConfirmEmailAddress)
                && string.Equals(EmailAddress.Trim(), ConfirmEmailAddress.Trim(), StringComparison.Ordinal)
                && _emailRegex.IsMatch(EmailAddress.Trim());

            DataValidated = isValidEmail && _twoFactorAuthProvider.ValidatePin(VerificationCode);

            if (!isValidEmail)
            {
                QRCode = null;
            } 
            else if (QRCode == null)
            {
                QRCode = _twoFactorAuthProvider.GetQRCode(QRCodeSize, QRCodeSize, EmailAddress.Trim());
            }
        }

        private void Save()
        {
            _twoFactorAuthProvider.PersistRecoveryEmailAddressToPasswordVault(EmailAddress);
            _settingsProvider.SetSetting(SettingsDefinitions.UseTwoFactorAuthentication, true);
        }
    }
}
