using System;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Converts a <see cref="Guid"/> to a <see cref="bool"/>.
    /// </summary>
    internal sealed class CategoryIdToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return false;
            }

            if (value.GetType() != typeof(Guid))
            {
                throw new ArgumentException($"{nameof(value)} must be a {nameof(Guid)}.");
            }

            var id = (Guid)value;
            if (id == new Guid(Constants.CategoryAllId))
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
