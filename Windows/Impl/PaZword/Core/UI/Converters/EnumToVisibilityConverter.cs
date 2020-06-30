using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Convert a <see cref="Enum"/> to a <see cref="Visibility"/> value.
    /// </summary>
    internal sealed class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || parameter == null || !(value is Enum))
            {
                return Visibility.Collapsed;
            }

            var currentState = value.ToString();
            var stateStrings = parameter.ToString();

            string[] stateStringsSplitted = stateStrings.Split(',');
            for (int i = 0; i < stateStringsSplitted.Length; i++)
            {
                if (string.Equals(currentState, stateStringsSplitted[i].Trim(), StringComparison.Ordinal))
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
