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

namespace PaZword.ViewModels.Data.BankAccount
{
    /// <summary>
    /// Interaction logic for <see cref="BankAccountDataUserControl"/>
    /// </summary>
    internal sealed class BankAccountDataViewModel : AccountDataViewModelBase
    {
        private BankAccountDataUserControl _control;

        public BankAccountDataStrings Strings => LanguageManager.Instance.BankAccountData;

        public override string Title => Strings.DisplayName;

        public override FrameworkElement UserInterface
        {
            get
            {
                if (_control == null)
                {
                    _control = new BankAccountDataUserControl(this);
                }
                return _control;
            }
        }

        public Visibility AccountHolderNameFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.AccountHolderName)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility AccountNumberFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.AccountNumber)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility BankNameFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.BankName)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility IbanNumberFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.IbanNumber)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility PinFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.Pin)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility RoutingNumberFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.RoutingNumber)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility SwiftCodeFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.SwiftCode)
            ? Visibility.Collapsed
            : Visibility.Visible;

        private BankAccountData CastedData => (BankAccountData)Data;

        private BankAccountData CastedDataEditMode => (BankAccountData)DataEditMode;

        public BankAccountDataViewModel(
            BankAccountData accountData,
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
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.AccountHolderName, Strings.AccountHolderName).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.AccountNumber, Strings.AccountNumber).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.BankName, Strings.BankName).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.IbanNumber, Strings.IbanNumber).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.Pin, Strings.Pin).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.RoutingNumber, Strings.RoutingNumber).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.SwiftCode, Strings.SwiftCode).ConfigureAwait(false);
        }

        public override string GenerateSubtitle()
        {
            string bankName = CastedDataEditMode.BankName.ToUnsecureString();
            string accountHolderName = CastedDataEditMode.AccountHolderName.ToUnsecureString();
            string accountNumber = CastedDataEditMode.AccountNumber.ToUnsecureString();
            string ibanNumber = CastedDataEditMode.IbanNumber.ToUnsecureString();
            string routingNumber = CastedDataEditMode.RoutingNumber.ToUnsecureString();
            string swiftCode = CastedDataEditMode.SwiftCode.ToUnsecureString();

            if (!string.IsNullOrWhiteSpace(ibanNumber)
                && ibanNumber.Length >= 5)
            {
                return Constants.PasswordMask + ibanNumber.Substring(ibanNumber.Length - 4);
            }

            if (!string.IsNullOrWhiteSpace(swiftCode)
                && swiftCode.Length >= 5)
            {
                return Constants.PasswordMask + swiftCode.Substring(swiftCode.Length - 4);
            }

            if (!string.IsNullOrWhiteSpace(accountNumber)
                && accountNumber.Length >= 5)
            {
                return Constants.PasswordMask + accountNumber.Substring(accountNumber.Length - 4);
            }

            if (!string.IsNullOrWhiteSpace(accountHolderName))
            {
                return accountHolderName;
            }

            if (!string.IsNullOrWhiteSpace(routingNumber))
            {
                return routingNumber;
            }

            if (!string.IsNullOrWhiteSpace(bankName))
            {
                return bankName;
            }

            return string.Empty;
        }
    }
}
