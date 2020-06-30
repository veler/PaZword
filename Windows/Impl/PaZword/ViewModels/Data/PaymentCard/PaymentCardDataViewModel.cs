using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.UI;
using PaZword.Core;
using PaZword.Localization;
using PaZword.Models.Data;
using PaZword.Views.Data;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace PaZword.ViewModels.Data.PaymentCard
{
    /// <summary>
    /// Interaction logic for <see cref="PaymentCardDataUserControl"/>
    /// </summary>
    internal sealed class PaymentCardDataViewModel : AccountDataViewModelBase
    {
        private int _cardTypeEditingIndex = -1;
        private PaymentCardDataUserControl _control;

        public PaymentCardDataStrings Strings => LanguageManager.Instance.PaymentCardData;

        public override string Title => Strings.DisplayName;

        public override FrameworkElement UserInterface
        {
            get
            {
                if (_control == null)
                {
                    _control = new PaymentCardDataUserControl(this);
                }
                return _control;
            }
        }

        /// <summary>
        /// Gets the list of card types.
        /// </summary>
        public ObservableCollection<PaymentCardTypeItem> CardTypeItemSource { get; }
            = new ObservableCollection<PaymentCardTypeItem>()
            {
                PaymentCardTypeItems.AmericanExpress,
                PaymentCardTypeItems.Discovery,
                PaymentCardTypeItems.Mastercard,
                PaymentCardTypeItems.Visa,
                PaymentCardTypeItems.Other
            };

        /// <summary>
        /// Gets the current <see cref="PaymentCardType"/> as an <see cref="int"/> value.
        /// </summary>
        public int CardTypeIndex => (int)CastedData.CardType;

        /// <summary>
        /// Gets or sets the current <see cref="PaymentCardType"/> in editing mode as an <see cref="int"/> value.
        /// </summary>
        public int CardTypeEditingIndex
        {
            get => _cardTypeEditingIndex;
            set
            {
                if (value != -1 && _cardTypeEditingIndex != value)
                {
                    _cardTypeEditingIndex = value;
                    if (IsEditing && (int)CastedDataEditMode.CardType != value)
                    {
                        CastedDataEditMode.CardType = (PaymentCardType)value;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current year.
        /// </summary>
        public DateTimeOffset TodaysYear => DateTimeOffset.Now;

        public Visibility BankNameFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.BankName)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility CardCryptogramFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.CardCryptogram)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility CardHolderNameFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.CardHolderName)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility CardNumberFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.CardNumber)
            ? Visibility.Collapsed
            : Visibility.Visible;

        private PaymentCardData CastedData => (PaymentCardData)Data;

        private PaymentCardData CastedDataEditMode => (PaymentCardData)DataEditMode;

        public PaymentCardDataViewModel(
            PaymentCardData accountData,
            ISerializationProvider serializationProvider,
            IWindowManager windowManager,
            ILogger logger)
            : base(logger, serializationProvider, windowManager)
        {
            Data = accountData;

            CardTypeEditingIndex = CardTypeIndex;
            PropertyChanged += PaymentCardDataViewModel_PropertyChanged;
        }

        private void PaymentCardDataViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataEditMode) && IsEditing)
            {
                CardTypeEditingIndex = (int)CastedDataEditMode.CardType;
            }
        }

        public override async Task<bool> ValidateChangesAsync(CancellationToken cancellationToken)
        {
            return await base.ValidateChangesAsync(cancellationToken).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.BankName, Strings.BankName).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.CardHolderName, Strings.CardHolder).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.CardNumber, Strings.CardNumber).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.CardCryptogram, Strings.CardCryptogram).ConfigureAwait(false);
        }

        public override string GenerateSubtitle()
        {
            string bankName = CastedDataEditMode.BankName.ToUnsecureString();
            string cardHolderName = CastedDataEditMode.CardHolderName.ToUnsecureString();
            string cardNumber = CastedDataEditMode.CardNumber.ToUnsecureString();

            if (!string.IsNullOrWhiteSpace(cardNumber)
                && cardNumber.Length == 19
                && !cardNumber.Contains("_", StringComparison.Ordinal))
            {
                return cardNumber.Substring(0, 4)
                    + '-'
                    + new string(Constants.PasswordMask, 4)
                    + '-'
                    + new string(Constants.PasswordMask, 4)
                    + '-'
                    + cardNumber.Substring(15);
            }

            if (!string.IsNullOrWhiteSpace(cardHolderName))
            {
                return cardHolderName;
            }

            if (!string.IsNullOrWhiteSpace(bankName))
            {
                return bankName;
            }

            return string.Empty;
        }
    }
}
