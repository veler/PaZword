using System;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Convert a <see cref="int"/> to a <see cref="Visibility"/> value.
    /// </summary>
    internal sealed class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var valueInt = value as int?;
            var parameterInt = System.Convert.ToInt32(parameter, CultureInfo.CurrentCulture);

            if (valueInt == null)
            {
                return DependencyProperty.UnsetValue;
            }

            if (valueInt.Value == parameterInt)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
