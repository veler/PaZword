using System;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Converts a null value to a <see cref="bool"/> value.
    /// </summary>
    internal sealed class NullToBooleanConverter : IValueConverter
    {
        public bool IsInverted { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value?.GetType() == typeof(string))
            {
                if (IsInverted)
                {
                    return string.IsNullOrEmpty((string)value) ? false : true;
                }
                return string.IsNullOrEmpty((string)value) ? true : false;
            }

            if (IsInverted)
            {
                return value == null ? false : true;
            }
            return value == null ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
