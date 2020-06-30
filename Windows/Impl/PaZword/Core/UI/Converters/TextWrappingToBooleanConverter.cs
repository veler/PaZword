using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Convert a <see cref="TextWrapping"/> to a <see cref="bool"/> value.
    /// </summary>
    internal sealed class TextWrappingToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var valueWrapping = (TextWrapping)value;

            if (valueWrapping == TextWrapping.NoWrap)
            {
                return false;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
