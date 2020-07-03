using PaZword.Api.Security;
using System;
using System.Collections.Generic;
using System.Composition;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace PaZword.Core.Security
{
    [Export(typeof(IEncryptionProvider))]
    [Shared()]
    internal sealed class EncryptionProvider : IEncryptionProvider
    {
        // Changing this will require existing users to re-enter their credentials.
        private const string PaZwordSecretKeysName = "PaZwordSecretKeys";

        private const char IVSeparator = ';';
        private const int IVLength = 32;

        private readonly SymmetricKeyAlgorithmProvider _cryptingProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
        private readonly object _lock = new object();

        private CryptographicKey _cryptographicKey;
        private IBuffer _aesGlobalKey;
        private IBuffer _aesGlobalIV;
        private PasswordCredential _secretKeys;

        public PasswordCredential GenerateSecretKeys()
        {
            string userName = CryptographicBuffer.EncodeToBase64String(CryptographicBuffer.GenerateRandom(_cryptingProvider.BlockLength));
            string password = CryptographicBuffer.EncodeToBase64String(CryptographicBuffer.GenerateRandom(IVLength));

            return new PasswordCredential(PaZwordSecretKeysName, userName, password);
        }

        public string EncodeSecretKeysToBase64(PasswordCredential passwordCredential)
        {
            Arguments.NotNull(passwordCredential, nameof(passwordCredential));

            passwordCredential.RetrievePassword();
            var concatenatedSecretKeys = $"{passwordCredential.UserName} {passwordCredential.Password}";
            return concatenatedSecretKeys.EncodeToBase64();
        }

        public PasswordCredential DecodeSecretKeysFromBase64(string secretKeys)
        {
            Arguments.NotNullOrWhiteSpace(secretKeys, nameof(secretKeys));
            string concatenatedSecretKeys = secretKeys.DecodeFromBase64();
            string[] splittedSecretKeys = concatenatedSecretKeys.Split(' ');
            return new PasswordCredential(PaZwordSecretKeysName, userName: splittedSecretKeys[0].Trim(), password: splittedSecretKeys[1].Trim());
        }

        public bool LoadSecretKeysFromPasswordVault()
        {
            var vault = new PasswordVault();

            try
            {
                PasswordCredential secretKeys = vault.FindAllByResource(PaZwordSecretKeysName)[0];
                SetSecretKeys(secretKeys);
                return true;
            }
            catch
            {
                // FindAllByResource throws if it doesn't find any match.
                return false;
            }
        }

        public void PersistSecretKeysToPasswordVault()
        {
            PasswordCredential newSecretKeys = _secretKeys;
            if (newSecretKeys == null)
            {
                throw new InvalidOperationException("There is no encryption keys existing for this instance.");
            }

            DeleteSecretKeysFromPasswordVault();

            var vault = new PasswordVault();
            vault.Add(newSecretKeys);
        }

        public void DeleteSecretKeysFromPasswordVault()
        {
            var vault = new PasswordVault();

            try
            {
                IReadOnlyList<PasswordCredential> existingSecretKeys = vault.FindAllByResource(PaZwordSecretKeysName);
                for (int i = 0; i < existingSecretKeys.Count; i++)
                {
                    vault.Remove(existingSecretKeys[i]);
                }
            }
            catch
            {
                // FindAllByResource throws if it doesn't find any match.
            }
        }

        public void SetSecretKeys(PasswordCredential secretKeys)
        {
            Arguments.NotNull(secretKeys, nameof(secretKeys));
            if (!string.Equals(secretKeys.Resource, PaZwordSecretKeysName, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Parameter '{nameof(secretKeys)}' is expected to have the PaZword's resource name.");
            }

            secretKeys.RetrievePassword();

            lock (_lock)
            {
                _aesGlobalKey = CryptographicBuffer.DecodeFromBase64String(secretKeys.UserName);
                _aesGlobalIV = CryptographicBuffer.DecodeFromBase64String(secretKeys.Password);

                _cryptographicKey = _cryptingProvider.CreateSymmetricKey(_aesGlobalKey);
                _secretKeys = new PasswordCredential(PaZwordSecretKeysName, secretKeys.UserName, secretKeys.Password);
            }
        }

        public string EncryptString(string data, bool reuseGlobalIV = false)
        {
            Arguments.NotNull(data, nameof(data));

            if (string.IsNullOrEmpty(data))
            {
                return data;
            }

            CryptographicKey cryptographicKey;
            IBuffer iv;

            lock (_lock)
            {
                if (_cryptographicKey == null)
                {
                    throw new InvalidOperationException($"No encryption key found. Invoke '{nameof(LoadSecretKeysFromPasswordVault)}' or '{nameof(SetSecretKeys)}' first.");
                }

                cryptographicKey = _cryptographicKey;

                if (reuseGlobalIV)
                {
                    iv = _aesGlobalIV;
                }
                else
                {
                    iv = CryptographicBuffer.GenerateRandom(IVLength);
                }
            }

            var binaryData = CryptographicBuffer.ConvertStringToBinary(data, BinaryStringEncoding.Utf8);

            IBuffer encryptedBinaryData = CryptographicEngine.Encrypt(cryptographicKey, binaryData, iv);
            string encryptedStringData = CryptographicBuffer.EncodeToBase64String(encryptedBinaryData);

            if (!reuseGlobalIV)
            {
                encryptedStringData = CryptographicBuffer.EncodeToBase64String(iv) + IVSeparator + encryptedStringData;
            }

            return encryptedStringData;
        }

        public string EncryptString(string data, string defaultValue, bool reuseGlobalIV = false)
        {
            return data == null || string.IsNullOrEmpty(data) ? EncryptString(defaultValue, reuseGlobalIV) : EncryptString(data, reuseGlobalIV);
        }

        public string DecryptString(string base64EncryptedData)
        {
            Arguments.NotNullOrWhiteSpace(base64EncryptedData, nameof(base64EncryptedData));

            CryptographicKey cryptographicKey;
            IBuffer iv;

            lock (_lock)
            {
                if (_cryptographicKey == null)
                {
                    throw new InvalidOperationException($"No encryption key found. Invoke '{nameof(LoadSecretKeysFromPasswordVault)}' or '{nameof(SetSecretKeys)}' first.");
                }

                cryptographicKey = _cryptographicKey;
                iv = _aesGlobalIV;
            }

            int ivSeparatorPosition = base64EncryptedData.IndexOf(IVSeparator);
            if (ivSeparatorPosition == -1)
            {
                // retro-compatibility with implementation before https://github.com/veler/PaZword/issues/4
                return DecryptInternal(base64EncryptedData, cryptographicKey, iv);
            }

            iv = CryptographicBuffer.DecodeFromBase64String(base64EncryptedData.Substring(0, ivSeparatorPosition));
            base64EncryptedData = base64EncryptedData.Substring(ivSeparatorPosition + 1);

            return DecryptInternal(base64EncryptedData, cryptographicKey, iv);
        }

        public string DecryptString(string base64EncryptedData, string defaultValue)
        {
            return base64EncryptedData == null
                || string.IsNullOrEmpty(base64EncryptedData)
                ? defaultValue : DecryptString(base64EncryptedData);
        }

        private string DecryptInternal(string base64EncryptedData, CryptographicKey cryptographicKey, IBuffer iv)
        {
            IBuffer encryptedBinaryData = CryptographicBuffer.DecodeFromBase64String(base64EncryptedData);
            IBuffer decryptedData = CryptographicEngine.Decrypt(cryptographicKey, encryptedBinaryData, iv);
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedData);
        }
    }
}
