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

namespace PaZword.ViewModels.Data.LicenseKey
{
    /// <summary>
    /// Interaction logic for <see cref="LicenseKeyDataViewModel"/>
    /// </summary>
    internal sealed class LicenseKeyDataViewModel : AccountDataViewModelBase
    {
        private LicenseKeyDataUserControl _control;

        public LicenseKeyDataStrings Strings => LanguageManager.Instance.LicenseKeyData;

        public override string Title => Strings.DisplayName;

        public override FrameworkElement UserInterface
        {
            get
            {
                if (_control == null)
                {
                    _control = new LicenseKeyDataUserControl(this);
                }
                return _control;
            }
        }

        private LicenseKeyData CastedData => (LicenseKeyData)Data;

        private LicenseKeyData CastedDataEditMode => (LicenseKeyData)DataEditMode;

        public Visibility CompanyFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.Company)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility LicenseKeyFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.LicenseKey)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility LicenseToFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.LicenseTo)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility LinkedEmailAddressFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.LinkedEmailAddress)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public LicenseKeyDataViewModel(
            LicenseKeyData accountData,
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
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.Company, Strings.Company).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.LicenseKey, Strings.LicenseKey).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.LicenseTo, Strings.LicenseTo).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.LinkedEmailAddress, Strings.LinkedEmailAddress).ConfigureAwait(false);
        }

        public override string GenerateSubtitle()
        {
            string licenseTo = CastedDataEditMode.LicenseTo.ToUnsecureString();
            string linkedEmailAddress = CastedDataEditMode.LinkedEmailAddress.ToUnsecureString();
            string company = CastedDataEditMode.Company.ToUnsecureString();

            if (!string.IsNullOrWhiteSpace(licenseTo))
            {
                return licenseTo;
            }
            else if (!string.IsNullOrWhiteSpace(linkedEmailAddress))
            {
                return linkedEmailAddress;
            }
            else if (!string.IsNullOrWhiteSpace(company))
            {
                return company;
            }

            return string.Empty;
        }
    }
}
