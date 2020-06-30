using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Convert a <see cref="Enum"/> to a <see cref="int"/> value.
    /// </summary>
    internal sealed class EnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || !(value is Enum))
            {
                return false;
            }

            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var valueInt= value as int?;
            if (valueInt == null)
            {
                return DependencyProperty.UnsetValue;
            }

            return Enum.ToObject(targetType, valueInt);
        }
    }
}
