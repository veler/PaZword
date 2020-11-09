using System;
using PaZword.Api.Models;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Converts a <see cref="CategoryIcon"/> to a <see cref="BitmapImage"/>.
    /// </summary>
    internal sealed class CategoryIconToBitmapImageConverter : IValueConverter
    {
        private const string IconFolder = "ms-appx://PaZword/Assets/CategoryIcons";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value.GetType() != typeof(CategoryIcon))
            {
                throw new ArgumentException($"{nameof(value)} must be a {nameof(CategoryIcon)}.");
            }

            var icon = (CategoryIcon)value;
            string iconPath = $"{IconFolder}/{icon}.png";

            return new BitmapImage(new Uri(iconPath));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
