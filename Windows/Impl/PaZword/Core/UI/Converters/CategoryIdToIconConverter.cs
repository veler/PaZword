using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Converts a <see cref="Guid"/> to a <see cref="FontIcon"/>'s Glyph, which is a Segoe MDL2 Assets font character.
    /// </summary>
    internal sealed class CategoryIdToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value.GetType() != typeof(Guid))
            {
                throw new ArgumentException($"{nameof(value)} must be a {nameof(Guid)}.");
            }

            var id = (Guid)value;
            if (id == new Guid(Constants.CategoryAllId))
            {
                return "\xE10F";
            }

            return "\xE179";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
