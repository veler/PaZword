using Newtonsoft.Json;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Core;
using PaZword.Core.Json;
using System;
using System.Security;
using System.Threading.Tasks;

namespace PaZword.Models.Data
{
    /// <summary>
    /// Represents the data associated to a bank account.
    /// </summary>
    internal sealed class BankAccountData : AccountData, IUpgradableAccountData
    {
        [SecurityCritical]
        [JsonProperty(nameof(BankName))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _bankName;

        [SecurityCritical]
        [JsonProperty(nameof(AccountHolderName))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _accountHolderName;

        [SecurityCritical]
        [JsonProperty(nameof(RoutingNumber))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _routingNumber;

        [SecurityCritical]
        [JsonProperty(nameof(AccountNumber))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _accountNumber;

        [SecurityCritical]
        [JsonProperty(nameof(SwiftCode))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _swiftCode;

        [SecurityCritical]
        [JsonProperty(nameof(IbanNumber))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _ibanNumber;

        [SecurityCritical]
        [JsonProperty(nameof(Pin))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _pin;

        /// <summary>
        /// Gets or sets the bank name.
        /// </summary>
        [JsonIgnore]
        internal SecureString BankName
        {
            get => EncryptionProvider.DecryptString(_bankName.ToUnsecureString(), string.Empty).ToSecureString();
            set => _bankName = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the account holder name.
        /// </summary>
        [JsonIgnore]
        internal SecureString AccountHolderName
        {
            get => EncryptionProvider.DecryptString(_accountHolderName.ToUnsecureString(), string.Empty).ToSecureString();
            set => _accountHolderName = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the routing number.
        /// </summary>
        [JsonIgnore]
        internal SecureString RoutingNumber
        {
            get => EncryptionProvider.DecryptString(_routingNumber.ToUnsecureString(), string.Empty).ToSecureString();
            set => _routingNumber = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the account number.
        /// </summary>
        [JsonIgnore]
        internal SecureString AccountNumber
        {
            get => EncryptionProvider.DecryptString(_accountNumber.ToUnsecureString(), string.Empty).ToSecureString();
            set => _accountNumber = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the SWIFT code.
        /// </summary>
        [JsonIgnore]
        internal SecureString SwiftCode
        {
            get => EncryptionProvider.DecryptString(_swiftCode.ToUnsecureString(), string.Empty).ToSecureString();
            set => _swiftCode = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the IBAN number.
        /// </summary>
        [JsonIgnore]
        internal SecureString IbanNumber
        {
            get => EncryptionProvider.DecryptString(_ibanNumber.ToUnsecureString(), string.Empty).ToSecureString();
            set => _ibanNumber = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the PIN code.
        /// </summary>
        [JsonIgnore]
        internal SecureString Pin
        {
            get => EncryptionProvider.DecryptString(_pin.ToUnsecureString(), string.Empty).ToSecureString();
            set => _pin = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="BankAccountData"/> class.
        /// </summary>
        public BankAccountData()
            : base()
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="BankAccountData"/> class.
        /// </summary>
        /// <param name="id">The unique ID of the account</param>
        internal BankAccountData(Guid id)
            : base(id)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                _accountHolderName?.Dispose();
                _accountNumber?.Dispose();
                _bankName?.Dispose();
                _ibanNumber?.Dispose();
                _pin?.Dispose();
                _routingNumber?.Dispose();
                _swiftCode?.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool ExactEquals(AccountData other)
        {
            return
                other is BankAccountData bankAccountData
                && bankAccountData.Id == Id
                && bankAccountData._accountHolderName.IsEqualTo(_accountHolderName)
                && bankAccountData._accountNumber.IsEqualTo(_accountNumber)
                && bankAccountData._bankName.IsEqualTo(_bankName)
                && bankAccountData._ibanNumber.IsEqualTo(_ibanNumber)
                && bankAccountData._pin.IsEqualTo(_pin)
                && bankAccountData._routingNumber.IsEqualTo(_routingNumber)
                && bankAccountData._swiftCode.IsEqualTo(_swiftCode);
        }

        public Task UpgradeAsync(int oldVersion, int targetVersion)
        {
            if (oldVersion == 1)
            {
                // In Version 1, there was a vulnerability in the encryption engine.
                // Let's fix it by decrypting and re-encrypting all data.

#pragma warning disable CA2245 // Do not assign a property to itself.
                AccountHolderName = AccountHolderName;
                AccountNumber = AccountNumber;
                BankName = BankName;
                IbanNumber = IbanNumber;
                Pin = Pin;
                RoutingNumber = RoutingNumber;
                SwiftCode = SwiftCode;
#pragma warning restore CA2245 // Do not assign a property to itself.
            }

            return Task.CompletedTask;
        }
    }
}
