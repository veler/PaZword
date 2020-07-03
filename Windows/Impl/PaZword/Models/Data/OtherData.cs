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
    /// Represents the data for a random type of data.
    /// </summary>
    internal sealed class OtherData : AccountData, IUpgradableAccountData
    {
        [SecurityCritical]
        [JsonProperty(nameof(Name))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _name;

        [SecurityCritical]
        [JsonProperty(nameof(Value))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _value;

        /// <summary>
        /// Gets or sets the name of the field.
        /// </summary>
        [JsonIgnore]
        internal SecureString Name
        {
            get => EncryptionProvider.DecryptString(_name.ToUnsecureString(), string.Empty).ToSecureString();
            set => _name = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the value associated to the field.
        /// </summary>
        [JsonIgnore]
        internal SecureString Value
        {
            get => EncryptionProvider.DecryptString(_value.ToUnsecureString(), string.Empty).ToSecureString();
            set => _value = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="OtherData"/> class.
        /// </summary>
        public OtherData()
            : base()
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="OtherData"/> class.
        /// </summary>
        /// <param name="id">The unique ID of the account</param>
        internal OtherData(Guid id)
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
                _name?.Dispose();
                _value?.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool ExactEquals(AccountData other)
        {
            return
                other is OtherData otherData
                && otherData.Id == Id
                && otherData._name.IsEqualTo(_name)
                && otherData._value.IsEqualTo(_value);
        }

        public Task UpgradeAsync(int oldVersion, int targetVersion)
        {
            if (oldVersion == 1)
            {
                // In Version 1, there was a vulnerability in the encryption engine.
                // Let's fix it by decrypting and re-encrypting all data.

#pragma warning disable CA2245 // Do not assign a property to itself.
                Name = Name;
                Value = Value;
#pragma warning restore CA2245 // Do not assign a property to itself.
            }

            return Task.CompletedTask;
        }
    }
}
