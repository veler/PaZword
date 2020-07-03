using Windows.Security.Credentials;

namespace PaZword.Api.Security
{
    /// <summary>
    /// Provides a set of methods to encrypt data.
    /// </summary>
    public interface IEncryptionProvider
    {
        /// <summary>
        /// Generates new random encryption key.
        /// </summary>
        /// <returns></returns>
        PasswordCredential GenerateSecretKeys();

        /// <summary>
        /// Encodes the given <paramref name="passwordCredential"/> to a Base 64 string.
        /// </summary>
        /// <param name="passwordCredential">The secret keys to encode.</param>
        /// <returns>Returns a Base 64 representation of the keys.</returns>
        string EncodeSecretKeysToBase64(PasswordCredential passwordCredential);

        /// <summary>
        /// Decodes the given <paramref name="secretKeys"/> from a Base 64 string to <see cref="PasswordCredential"/>.
        /// </summary>
        /// <param name="secretKeys">The secret keys to decode.</param>
        /// <returns>Returns a <see cref="PasswordCredential"/>.</returns>
        PasswordCredential DecodeSecretKeysFromBase64(string secretKeys);

        /// <summary>
        /// Loads the user's encryption keys from the credential locker and use them in the current instance.
        /// </summary>
        /// <returns>Returns <code>True</code> if encryption keys have been found.</returns>
        bool LoadSecretKeysFromPasswordVault();

        /// <summary>
        /// Saves the current encryption keys loaded through <see cref="SetSecretKeys"/> or <see cref="LoadSecretKeysFromPasswordVault"/>
        /// in the credential locker.
        /// </summary>
        void PersistSecretKeysToPasswordVault();

        /// <summary>
        /// Delete the encryption keys from the credential locker. Doing this will force the user to enter his recovery key to
        /// authentication in the app the next time he starts it.
        /// </summary>
        void DeleteSecretKeysFromPasswordVault();

        /// <summary>
        /// Set the encryption keys for the current instance of <see cref="IEncryptionProvider"/>.
        /// </summary>
        /// <param name="passwordCredential">The <see cref="PasswordCredential"/> containing the encryption keys.</param>
        void SetSecretKeys(PasswordCredential passwordCredential);

        /// <summary>
        /// Encrypts a given string using AES encryption algorithm and returns it in Base64.
        /// </summary>
        /// <param name="data">Data to be encrypted</param>
        /// <param name="reuseGlobalIV">Defines whether the global initialization vector (IV) should be used to encrypt.</param>
        /// <returns>An encrypted string in Base64</returns>
        string EncryptString(string data, bool reuseGlobalIV = false);

        /// <summary>
        /// Encrypts a given string using AES encryption algorithm and returns it in Base64.
        /// </summary>
        /// <param name="data">Data to be encrypted</param>
        /// <param name="defaultValue">The default value to return if the <paramref name="data"/> is empty.</param>
        /// <param name="reuseGlobalIV">Defines whether the global initialization vector (IV) should be used to encrypt.</param>
        /// <returns>An encrypted string in Base64</returns>
        string EncryptString(string data, string defaultValue, bool reuseGlobalIV = false);

        /// <summary>
        /// Decrypts a string through AES encryption algorithm.
        /// </summary>
        /// <param name="base64EncryptedData">An encrypted string in Unicode. The value should be a Base64 representation of the encrypted data.</param>
        /// <returns>The decrypted string in Unicode</returns>
        string DecryptString(string base64EncryptedData);

        /// <summary>
        /// Decrypts a string through AES encryption algorithm.
        /// </summary>
        /// <param name="base64EncryptedData">An encrypted string in Unicode. The value should be a Base64 representation of the encrypted data.</param>
        /// <param name="defaultValue">The default value to return if the <paramref name="base64EncryptedData"/> is empty.</param>
        /// <returns>The decrypted string in Unicode</returns>
        string DecryptString(string base64EncryptedData, string defaultValue);
    }
}
