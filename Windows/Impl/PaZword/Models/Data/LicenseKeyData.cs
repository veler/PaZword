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
    /// Represents the data associated to a software license.
    /// </summary>
    internal sealed class LicenseKeyData : AccountData, IUpgradableAccountData
    {
        [SecurityCritical]
        [JsonProperty(nameof(LicenseTo))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _licenseTo;

        [SecurityCritical]
        [JsonProperty(nameof(LinkedEmailAddress))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _linkedEmailAddress;

        [SecurityCritical]
        [JsonProperty(nameof(Company))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _company;

        [SecurityCritical]
        [JsonProperty(nameof(LicenseKey))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _licenseKey;

        /// <summary>
        /// Gets or sets the name of the person associated to the license.
        /// </summary>
        [JsonIgnore]
        internal SecureString LicenseTo
        {
            get => EncryptionProvider.DecryptString(_licenseTo.ToUnsecureString(), string.Empty).ToSecureString();
            set => _licenseTo = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the email address linked to the license key.
        /// </summary>
        [JsonIgnore]
        internal SecureString LinkedEmailAddress
        {
            get => EncryptionProvider.DecryptString(_linkedEmailAddress.ToUnsecureString(), string.Empty).ToSecureString();
            set => _linkedEmailAddress = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the company name.
        /// </summary>
        [JsonIgnore]
        internal SecureString Company
        {
            get => EncryptionProvider.DecryptString(_company.ToUnsecureString(), string.Empty).ToSecureString();
            set => _company = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the license key.
        /// </summary>
        [JsonIgnore]
        internal SecureString LicenseKey
        {
            get => EncryptionProvider.DecryptString(_licenseKey.ToUnsecureString(), string.Empty).ToSecureString();
            set => _licenseKey = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="LicenseKeyData"/> class.
        /// </summary>
        public LicenseKeyData()
            : base()
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="LicenseKeyData"/> class.
        /// </summary>
        /// <param name="id">The unique ID of the account</param>
        internal LicenseKeyData(Guid id)
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
                _licenseTo?.Dispose();
                _linkedEmailAddress?.Dispose();
                _company?.Dispose();
                _licenseKey?.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool ExactEquals(AccountData other)
        {
            return
                other is LicenseKeyData licenseKeyData
                && licenseKeyData.Id == Id
                && licenseKeyData._licenseTo.IsEqualTo(_licenseTo)
                && licenseKeyData._linkedEmailAddress.IsEqualTo(_linkedEmailAddress)
                && licenseKeyData._company.IsEqualTo(_company)
                && licenseKeyData._licenseKey.IsEqualTo(_licenseKey);
        }

        public Task UpgradeAsync(int oldVersion, int targetVersion)
        {
            if (oldVersion == 1)
            {
                // In Version 1, there was a vulnerability in the encryption engine.
                // Let's fix it by decrypting and re-encrypting all data.

#pragma warning disable CA2245 // Do not assign a property to itself.
                LicenseTo = LicenseTo;
                LinkedEmailAddress = LinkedEmailAddress;
                Company = Company;
                LicenseKey= LicenseKey;
#pragma warning restore CA2245 // Do not assign a property to itself.
            }

            return Task.CompletedTask;
        }
    }
}
