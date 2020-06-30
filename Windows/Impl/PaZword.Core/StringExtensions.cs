using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace PaZword.Core
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a <see cref="string"/> to a <see cref="SecureString"/>.
        /// Keep in mind a <see cref="SecureString"/> is limited to 65,536 characters.
        /// This method limits to 65000 characters.
        /// </summary>
        /// <param name="value">the <see cref="string"/> to convert.</param>
        /// <returns>Returns a <see cref="SecureString"/>.</returns>
        public static SecureString ToSecureString(this string value)
        {
            var secureString = new SecureString();

            if (value != null)
            {
                if (value.Length > 65000)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The value shouldn't be longer than 65,000 characters.");
                }

                foreach (char c in value)
                {
                    secureString.AppendChar(c);
                }
            }

            secureString.MakeReadOnly();
            return secureString;
        }

        /// <summary>
        /// Converts a <see cref="SecureString"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The <see cref="SecureString"/> to convert.</param>
        /// <param name="disposeValue">Defines whether the <paramref name="value"/> should be disposed or not.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        public static string ToUnsecureString(this SecureString value, bool disposeValue = false)
        {
            if (IsNullOrEmptySecureString(value))
            {
                return string.Empty;
            }

            var valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
                if (disposeValue)
                {
                    value.Dispose();
                }
            }
        }

        /// <summary>
        /// Gets whether a given <see cref="SecureString"/> is null or empty;
        /// </summary>
        /// <param name="secureString">The <see cref="SecureString"/> to test.</param>
        /// <returns>Returns <code>True</code> if it's null or empty.</returns>
        public static bool IsNullOrEmptySecureString(SecureString secureString)
        {
            return secureString == null || secureString.Length == 0;
        }

        /// <summary>
        /// Gets whether two given <see cref="SecureString"/> are equals without exposing they plain text.
        /// </summary>
        /// <param name="left">The secure string to test</param>
        /// <param name="right">The secure string to test</param>
        /// <returns>Returns <code>true</code> if the two <see cref="SecureString"/> are equals.</returns>
        public static bool IsEqualTo(this SecureString left, SecureString right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            if (left.Length != right.Length)
            {
                return false;
            }

            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                bstr1 = Marshal.SecureStringToBSTR(left);
                bstr2 = Marshal.SecureStringToBSTR(right);

                unsafe
                {
                    for (char* ptr1 = (char*)bstr1.ToPointer(), ptr2 = (char*)bstr2.ToPointer();
                        *ptr1 != 0 && *ptr2 != 0;
                         ++ptr1, ++ptr2)
                    {
                        if (*ptr1 != *ptr2)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            finally
            {
                if (bstr1 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr1);
                }

                if (bstr2 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr2);
                }
            }
        }

        /// <summary>
        /// Encodes the string to Base64.
        /// </summary>
        public static string EncodeToBase64(this string value)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Decodes the string to Base64.
        /// </summary>
        public static string DecodeFromBase64(this string value)
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
    }
}
