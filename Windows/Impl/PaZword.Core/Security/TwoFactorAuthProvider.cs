using PaZword.Api;
using PaZword.Api.Security;
using PaZword.Localization;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using ZXing;
using ZXing.Common;

namespace PaZword.Core.Security
{
    [Export(typeof(ITwoFactorAuthProvider))]
    [Shared()]
    internal sealed class TwoFactorAuthProvider : ITwoFactorAuthProvider
    {
        private const string AuthenticateEvent = "TwoFactorAuthProvider.Authenticate";
        private const string PersistRecoveryEmailAddressToPasswordVaultFaultEvent = "TwoFactorAuthProvider.PersistRecoveryEmailAddressToPassword.Fault";
        private const string SendPinByEmailAsyncFaultEvent = "TwoFactorAuthProvider.SendPinByEmailAsync.Fault";

        // Changing this will break the two factor authentication and users won't be able to retrieve it
        // until they reset/repair the application to factory default. Just, DON'T !
        private const string PaZwordTwoFactorAuthSecretKeysName = "PaZwordTwoFactorAuthSecretKeys";

        private const byte DIGITS = 6;
        private const long EPOCH = 621355968000000000;
        private const int INTERVAL = 30000;

        private readonly ILogger _logger;
        private readonly object _lock = new object();

        private CryptographicKey _cryptographicKey;

        /// <summary>
        /// Initialize a new instance of the <see cref="TwoFactorAuthProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        public TwoFactorAuthProvider(ILogger logger)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
        }

        public bool ValidatePin(string pin, int allowedInterval = 1)
        {
            if (string.IsNullOrWhiteSpace(pin))
            {
                _logger.LogEvent(AuthenticateEvent, "Success == False; No pin");
                return false;
            }

            pin = pin.Replace(" ", string.Empty, StringComparison.Ordinal);

            if (pin.Equals(Generate(TimeSource()), StringComparison.Ordinal))
            {
                _logger.LogEvent(AuthenticateEvent, "Success == True");
                return true;
            }

            for (int i = 1; i <= allowedInterval; i++)
            {
                if (pin.Equals(Generate(TimeSource() + i), StringComparison.Ordinal))
                {
                    _logger.LogEvent(AuthenticateEvent, "Success == True");
                    return true;
                }

                if (pin.Equals(Generate(TimeSource() - i), StringComparison.Ordinal))
                {
                    _logger.LogEvent(AuthenticateEvent, "Success == True");
                    return true;
                }
            }

            _logger.LogEvent(AuthenticateEvent, "Success == False; Wrong pin");
            return false;
        }

        public string GeneratePin()
        {
            return Generate(TimeSource());
        }

        public ImageSource GetQRCode(int width, int height, string emailAddress)
        {
            Arguments.NotNullOrWhiteSpace(emailAddress, nameof(emailAddress));

            EnsureInitialized();
            using (SecureString secretKey = GetOrCreateSecretKey())
            {
                string label = Package.Current.DisplayName;

                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions
                    {
                        Height = height,
                        Width = width
                    }
                };

                var qrCodeData = $"otpauth://totp/{label}:{Uri.EscapeDataString(emailAddress)}?issuer={label}&secret={secretKey.ToUnsecureString()}";
                return writer.Write(qrCodeData);
            }
        }

        public void PersistRecoveryEmailAddressToPasswordVault(string emailAddress)
        {
            Arguments.NotNullOrWhiteSpace(emailAddress, nameof(emailAddress));

            EnsureInitialized();
            var vault = new PasswordVault();

            try
            {
                PasswordCredential passwordCredential = vault.FindAllByResource(PaZwordTwoFactorAuthSecretKeysName)[0];
                passwordCredential.RetrievePassword();
                using (SecureString password = passwordCredential.Password.ToSecureString())
                {
                    IReadOnlyList<PasswordCredential> existingSecretKeys = vault.FindAllByResource(PaZwordTwoFactorAuthSecretKeysName);
                    for (int i = 0; i < existingSecretKeys.Count; i++)
                    {
                        vault.Remove(existingSecretKeys[i]);
                    }

                    vault.Add(new PasswordCredential(
                        PaZwordTwoFactorAuthSecretKeysName,
                        emailAddress.Trim(),
                        password.ToUnsecureString()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogFault(PersistRecoveryEmailAddressToPasswordVaultFaultEvent, "Probably: an existing two factor authentication key was expected.", ex);
                throw;
            }
        }

        public string GetRecoveryEmailAddressFromPassowrdVault()
        {
            EnsureInitialized();
            var vault = new PasswordVault();

            try
            {
                PasswordCredential passwordCredential = vault.FindAllByResource(PaZwordTwoFactorAuthSecretKeysName)[0];
                passwordCredential.RetrievePassword();
                if (!string.Equals(passwordCredential.UserName, PaZwordTwoFactorAuthSecretKeysName, StringComparison.Ordinal))
                {
                    return passwordCredential.UserName;
                }
            }
            catch
            {
                // FindAllByResource throws if it doesn't find any match.
            }

            return string.Empty;
        }

        public async Task SendPinByEmailAsync()
        {
            try
            {
                // Send verification code by email
                var param = new StringBuilder();

                param.Append("send=" + WebUtility.UrlEncode("true") + "&");
                param.Append("email=" + WebUtility.UrlEncode(GetRecoveryEmailAddressFromPassowrdVault()) + "&");
                param.Append("key=" + WebUtility.UrlEncode(GeneratePin()) + "&");
                param.Append("lang=" + LanguageManager.Instance.GetCurrentCulture().TwoLetterISOLanguageName);

                var ascii = new ASCIIEncoding();
                var postBytes = ascii.GetBytes(param.ToString());
                var request = (HttpWebRequest)WebRequest.Create(new Uri(ServicesKeys.TwoFactorAuthenticationServiceUrl));

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers["Content-Length"] = postBytes.Length.ToString(CultureInfo.InvariantCulture);
                request.Headers["User-Agent"] = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

                using (var postStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    postStream.Write(postBytes, 0, postBytes.Length);
                    postStream.Flush();
                }

                await request.GetResponseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogFault(SendPinByEmailAsyncFaultEvent, "Unable to send the two factor authentication PIN by email.", ex);
            }
        }

        private void EnsureInitialized()
        {
            lock (_lock)
            {
                if (_cryptographicKey != null)
                {
                    return;
                }

                MacAlgorithmProvider provider = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);

                using (SecureString secretKey = GetOrCreateSecretKey())
                {
                    IBuffer keyMaterial = CryptographicBuffer.CreateFromByteArray(ToBytesBase32(secretKey));
                    _cryptographicKey = provider.CreateKey(keyMaterial);
                }
            }
        }

        private string Generate(long iterationNumber)
        {
            EnsureInitialized();

            byte[] code = BitConverter.GetBytes(iterationNumber);

            if (BitConverter.IsLittleEndian)
            {
                code = Reverse(code);
            }

            byte[] hash = HmacSha1(code);

            // the last 4 bits of the mac say where the code starts (e.g. if last 4 bit are 1100, we start at byte 12)
            int start = hash[19] & 0x0f;

            // extract those 4 bytes
            var bytes = new byte[4];
            Array.Copy(hash, start, bytes, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                bytes = Reverse(bytes);
            }

            uint fullcode = BitConverter.ToUInt32(bytes, 0) & 0x7fffffff;

            // we use the last x DIGITS of this code in radix 10
            double codemask = (uint)Math.Pow(10, DIGITS);

            string totp = (fullcode % codemask).ToString(CultureInfo.InvariantCulture);

            // .NETmf has no required format string
            while (totp.Length != DIGITS)
            {
                totp = "0" + totp;
            }

            return totp;
        }

        private byte[] HmacSha1(byte[] value)
        {
            IBuffer data = CryptographicBuffer.CreateFromByteArray(value);
            IBuffer buffer = CryptographicEngine.Sign(_cryptographicKey, data);

            CryptographicBuffer.EncodeToHexString(buffer);

            CryptographicBuffer.CopyToByteArray(buffer, out byte[] hash);
            return hash;
        }

        private static long TimeSource()
        {
            return ((DateTime.UtcNow.Ticks - EPOCH) / TimeSpan.TicksPerMillisecond) / INTERVAL;
        }

        private static byte[] Reverse(byte[] src)
        {
            Array.Reverse(src);
            return src;
        }

        private static byte[] ToBytesBase32(SecureString input)
        {
            Arguments.NotNull(input, nameof(input));

            string inputString = input.ToUnsecureString();

            Arguments.NotNullOrEmpty(inputString, nameof(input));

            inputString = inputString.TrimEnd('='); //remove padding characters
            int byteCount = inputString.Length * 5 / 8; //this must be TRUNCATED
            var returnArray = new byte[byteCount];

            byte curByte = 0;
            byte bitsRemaining = 8;
            var arrayIndex = 0;

            char[] upperCharacters = inputString.ToUpper(CultureInfo.InvariantCulture).ToCharArray();
            for (int i = 0; i < upperCharacters.Length; i++)
            {
                char c = upperCharacters[i];
                int cValue = CharToValue(c);

                int mask;
                if (bitsRemaining > 5)
                {
                    mask = cValue << (bitsRemaining - 5);
                    curByte = (byte)(curByte | mask);
                    bitsRemaining -= 5;
                }
                else
                {
                    mask = cValue >> (5 - bitsRemaining);
                    curByte = (byte)(curByte | mask);
                    returnArray[arrayIndex++] = curByte;
                    curByte = (byte)(cValue << (3 + bitsRemaining));
                    bitsRemaining += 3;
                }
            }

            //if we didn't end with a full byte
            if (arrayIndex != byteCount)
            {
                returnArray[arrayIndex] = curByte;
            }

            return returnArray;
        }

        private static int CharToValue(char c)
        {
            int value = c;

            //65-90 == uppercase letters
            if (value < 91 && value > 64)
            {
                return value - 65;
            }

            //50-55 == numbers 2-7
            if (value < 56 && value > 49)
            {
                return value - 24;
            }

            //97-122 == lowercase letters
            if (value < 123 && value > 96)
            {
                return value - 97;
            }

            throw new ArgumentException("Character is not a Base32 character.", nameof(c));
        }

        private static SecureString GetOrCreateSecretKey()
        {
            var vault = new PasswordVault();

            try
            {
                // try to find existing key in credential locker.
                PasswordCredential passwordCredential = vault.FindAllByResource(PaZwordTwoFactorAuthSecretKeysName)[0];
                passwordCredential.RetrievePassword();
                return passwordCredential.Password.ToSecureString();
            }
            catch
            {
                // FindAllByResource throws if it doesn't find any match.
            }

            // Generate a key.
            var buffer = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            string password = Convert.ToBase64String(buffer)
                .Substring(0, 15) // Make sure the password is a Base32 string. See https://en.wikipedia.org/wiki/Base32#RFC_4648_Base32_alphabet
                .Replace('1', '3')
                .Replace('0', '2')
                .Replace('9', '7')
                .Replace('8', '6')
                .Replace('/', '2')
                .Replace('+', '7')
                .ToUpper(CultureInfo.InvariantCulture);

            // save it in credential locker.
            vault.Add(new PasswordCredential(
                PaZwordTwoFactorAuthSecretKeysName,
                PaZwordTwoFactorAuthSecretKeysName,
                password));

            return password.ToSecureString();
        }
    }
}
