using Newtonsoft.Json;
using PaZword.Api.Models;
using PaZword.Core;
using PaZword.Core.Json;
using System;
using System.Security;

namespace PaZword.Models.Data
{
    /// <summary>
    /// Represents the data associated to a payment card.
    /// </summary>
    internal sealed class PaymentCardData : AccountData
    {
        [SecurityCritical]
        [JsonProperty(nameof(BankName))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _bankName;

        [SecurityCritical]
        [JsonProperty(nameof(CardHolderName))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _cardHolderName;

        [SecurityCritical]
        [JsonProperty(nameof(CardNumber))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _cardNumber;

        [SecurityCritical]
        [JsonProperty(nameof(CardCryptogram))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _cardCryptogram;

        /// <summary>
        /// Gets or sets the brank name.
        /// </summary>
        [JsonIgnore]
        internal SecureString BankName
        {
            get => EncryptionProvider.DecryptString(_bankName.ToUnsecureString(), string.Empty).ToSecureString();
            set => _bankName = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the type of credit/debit card.
        /// </summary>
        [JsonProperty]
        internal PaymentCardType CardType { get; set; }

        /// <summary>
        /// Gets or sets the card holder name.
        /// </summary>
        [JsonIgnore]
        internal SecureString CardHolderName
        {
            get => EncryptionProvider.DecryptString(_cardHolderName.ToUnsecureString(), string.Empty).ToSecureString();
            set => _cardHolderName = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the credit/debit card number.
        /// </summary>
        [JsonIgnore]
        internal SecureString CardNumber
        {
            get => EncryptionProvider.DecryptString(_cardNumber.ToUnsecureString(), string.Empty).ToSecureString();
            set => _cardNumber = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the card expiration date.
        /// </summary>
        [JsonProperty]
        internal DateTimeOffset CardExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the cryptogram.
        /// </summary>
        [JsonIgnore]
        internal SecureString CardCryptogram
        {
            get => EncryptionProvider.DecryptString(_cardCryptogram.ToUnsecureString(), string.Empty).ToSecureString();
            set => _cardCryptogram = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="PaymentCardData"/> class.
        /// </summary>
        public PaymentCardData()
            : base()
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="PaymentCardData"/> class.
        /// </summary>
        /// <param name="id">The unique ID of the account</param>
        internal PaymentCardData(Guid id)
            : base(id)
        {
            CardExpirationDate = DateTimeOffset.Now;
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                _bankName?.Dispose();
                _cardHolderName?.Dispose();
                _cardNumber?.Dispose();
                _cardCryptogram?.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool ExactEquals(AccountData other)
        {
            return
                other is PaymentCardData paymentCardData
                && paymentCardData.Id == Id
                && paymentCardData._bankName.IsEqualTo(_bankName)
                && paymentCardData._cardHolderName.IsEqualTo(_cardHolderName)
                && paymentCardData._cardNumber.IsEqualTo(_cardNumber)
                && paymentCardData._cardCryptogram.IsEqualTo(_cardCryptogram);
        }
    }
}
