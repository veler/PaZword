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

        private readonly SymmetricKeyAlgorithmProvider _cryptingProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
        private readonly object _lock = new object();

        private CryptographicKey _cryptographicKey;
        private IBuffer _randomBuffer;
        private IBuffer _randomBufferCBC;
        private PasswordCredential _secretKeys;

        public PasswordCredential GenerateSecretKeys()
        {
            string userName = CryptographicBuffer.EncodeToBase64String(CryptographicBuffer.GenerateRandom(_cryptingProvider.BlockLength));
            string password = CryptographicBuffer.EncodeToBase64String(CryptographicBuffer.GenerateRandom(32));

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
                _randomBuffer = CryptographicBuffer.DecodeFromBase64String(secretKeys.UserName);
                _randomBufferCBC = CryptographicBuffer.DecodeFromBase64String(secretKeys.Password);

                _cryptographicKey = _cryptingProvider.CreateSymmetricKey(_randomBuffer);
                _secretKeys = new PasswordCredential(PaZwordSecretKeysName, secretKeys.UserName, secretKeys.Password);
            }
        }

        public string EncryptString(string data)
        {
            Arguments.NotNull(data, nameof(data));

            var dataString = data;

            Arguments.NotNull(dataString, nameof(data));

            CryptographicKey cryptographicKey;
            IBuffer randomBufferCBC;

            lock (_lock)
            {
                if (_cryptographicKey == null)
                {
                    throw new InvalidOperationException($"No encryption key found. Invoke '{nameof(LoadSecretKeysFromPasswordVault)}' or '{nameof(SetSecretKeys)}' first.");
                }

                cryptographicKey = _cryptographicKey;
                randomBufferCBC = _randomBufferCBC;
            }

            var binaryData = CryptographicBuffer.ConvertStringToBinary(dataString, BinaryStringEncoding.Utf8);

            var encryptedBinaryData = CryptographicEngine.Encrypt(cryptographicKey, binaryData, randomBufferCBC);
            var encryptedStringData = CryptographicBuffer.EncodeToBase64String(encryptedBinaryData);

            if (encryptedStringData == CryptographicBuffer.EncodeToBase64String(binaryData))
            {
                throw new Exception("The data has not been encrypted.");
            }

            return encryptedStringData;
        }

        public string EncryptString(string data, string defaultValue)
        {
            return data == null || string.IsNullOrEmpty(data) ? defaultValue : EncryptString(data);
        }

        public string DecryptString(string base64EncryptedData)
        {
            Arguments.NotNull(base64EncryptedData, nameof(base64EncryptedData));

            CryptographicKey cryptographicKey;
            IBuffer randomBufferCBC;

            lock (_lock)
            {
                if (_cryptographicKey == null)
                {
                    throw new InvalidOperationException($"No encryption key found. Invoke '{nameof(LoadSecretKeysFromPasswordVault)}' or '{nameof(SetSecretKeys)}' first.");
                }

                cryptographicKey = _cryptographicKey;
                randomBufferCBC = _randomBufferCBC;
            }

            var encryptedBinaryData = CryptographicBuffer.DecodeFromBase64String(base64EncryptedData);
            var decryptedData = CryptographicEngine.Decrypt(cryptographicKey, encryptedBinaryData, randomBufferCBC);
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedData);
        }

        public string DecryptString(string base64EncryptedData, string defaultValue)
        {
            return base64EncryptedData == null
                || string.IsNullOrEmpty(base64EncryptedData)
                ? defaultValue : DecryptString(base64EncryptedData);
        }
    }
}
