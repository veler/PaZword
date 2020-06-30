using System;
using System.Security;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Convert a <see cref="SecureString"/> to a <see cref="string"/> value.
    /// </summary>
    internal sealed class SecureStringToStringConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value that defines whether the converter behavior is inverted or not
        /// </summary>
        public bool IsInverted { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (IsInverted)
            {
                if (value == null || value is string)
                {
                    return (value as string).ToSecureString();
                }

                throw new InvalidCastException($"The value should be a {nameof(String)}.");
            }

            if (value == null || value is SecureString)
            {
                return (value as SecureString).ToUnsecureString();
            }

            throw new InvalidCastException($"The value should be a {nameof(SecureString)}.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (IsInverted)
            {
                if (value == null || value is SecureString)
                {
                    return (value as SecureString).ToUnsecureString();
                }

                throw new InvalidCastException($"The value should be a {nameof(SecureString)}.");
            }

            if (value == null || value is string)
            {
                return (value as string).ToSecureString();
            }

            throw new InvalidCastException($"The value should be a {nameof(String)}.");
        }
    }
}
