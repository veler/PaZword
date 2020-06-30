using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.UI;
using PaZword.Core;
using PaZword.Localization;
using PaZword.Models.Data;
using PaZword.Views.Data;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace PaZword.ViewModels.Data.WiFiCredential
{
    /// <summary>
    /// Interaction logic for <see cref="WiFiCredentialDataUserControl"/>
    /// </summary>
    internal sealed class WiFiCredentialDataViewModel : AccountDataViewModelBase
    {
        private WiFiCredentialDataUserControl _control;

        public WiFiCredentialDataStrings Strings => LanguageManager.Instance.WiFiCredentialData;

        public override string Title => Strings.DisplayName;

        public override FrameworkElement UserInterface
        {
            get
            {
                if (_control == null)
                {
                    _control = new WiFiCredentialDataUserControl(this);
                }
                return _control;
            }
        }

        public Visibility SsidFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.Ssid)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility PasswordFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.Password)
            ? Visibility.Collapsed
            : Visibility.Visible;

        private WiFiCredentialData CastedData => (WiFiCredentialData)Data;

        private WiFiCredentialData CastedDataEditMode => (WiFiCredentialData)DataEditMode;

        public WiFiCredentialDataViewModel(
            WiFiCredentialData accountData,
            ISerializationProvider serializationProvider,
            IWindowManager windowManager,
            ILogger logger)
            : base(logger, serializationProvider, windowManager)
        {
            Data = accountData;
        }

        public override async Task<bool> ValidateChangesAsync(CancellationToken cancellationToken)
        {
            return await base.ValidateChangesAsync(cancellationToken).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.Ssid, Strings.Ssid).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.Password, Strings.Password).ConfigureAwait(false);
        }

        public override string GenerateSubtitle()
        {
            string ssid = CastedDataEditMode.Ssid.ToUnsecureString();

            if (!string.IsNullOrWhiteSpace(ssid))
            {
                return ssid;
            }

            return string.Empty;
        }
    }
}
