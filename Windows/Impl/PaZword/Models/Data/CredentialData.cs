using Newtonsoft.Json;
using PaZword.Api.Models;
using PaZword.Core;
using PaZword.Core.Json;
using System;
using System.Security;

namespace PaZword.Models.Data
{
    /// <summary>
    /// Represents the data associated to a login.
    /// </summary>
    internal sealed class CredentialData : AccountData
    {
        [SecurityCritical]
        [JsonProperty(nameof(Username))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _username;

        [SecurityCritical]
        [JsonProperty(nameof(EmailAddress))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _emailAddress;

        [SecurityCritical]
        [JsonProperty(nameof(Password))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _password;

        [SecurityCritical]
        [JsonProperty(nameof(SecurityQuestion))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _securityQuestion;

        [SecurityCritical]
        [JsonProperty(nameof(SecurityQuestionAnswer))]
        [JsonConverter(typeof(SecureStringJsonConverter))]
        private SecureString _securityQuestionAnswer;

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        [JsonIgnore]
        internal SecureString Username
        {
            get => EncryptionProvider.DecryptString(_username.ToUnsecureString(), string.Empty).ToSecureString();
            set => _username = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        [JsonIgnore]
        internal SecureString EmailAddress
        {
            get => EncryptionProvider.DecryptString(_emailAddress.ToUnsecureString(), string.Empty).ToSecureString();
            set => _emailAddress = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [JsonIgnore]
        internal SecureString Password
        {
            get => EncryptionProvider.DecryptString(_password.ToUnsecureString(), string.Empty).ToSecureString();
            set => _password = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the security question.
        /// </summary>
        [JsonIgnore]
        internal SecureString SecurityQuestion
        {
            get => EncryptionProvider.DecryptString(_securityQuestion.ToUnsecureString(), string.Empty).ToSecureString();
            set => _securityQuestion = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Gets or sets the security question's answer.
        /// </summary>
        [JsonIgnore]
        internal SecureString SecurityQuestionAnswer
        {
            get => EncryptionProvider.DecryptString(_securityQuestionAnswer.ToUnsecureString(), string.Empty).ToSecureString();
            set => _securityQuestionAnswer = EncryptionProvider.EncryptString(value.ToUnsecureString(disposeValue: true), string.Empty).ToSecureString();
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="CredentialData"/> class.
        /// </summary>
        public CredentialData()
            : base()
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="CredentialData"/> class.
        /// </summary>
        /// <param name="id">The unique ID of the account</param>
        internal CredentialData(Guid id)
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
                _username?.Dispose();
                _emailAddress?.Dispose();
                _password?.Dispose();
                _securityQuestion?.Dispose();
                _securityQuestionAnswer?.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool ExactEquals(AccountData other)
        {
            return
                other is CredentialData credentialData
                && credentialData.Id == Id
                && credentialData._username.IsEqualTo(_username)
                && credentialData._emailAddress.IsEqualTo(_emailAddress)
                && credentialData._password.IsEqualTo(_password)
                && credentialData._securityQuestion.IsEqualTo(_securityQuestion)
                && credentialData._securityQuestionAnswer.IsEqualTo(_securityQuestionAnswer);
        }
    }
}
