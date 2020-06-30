using System;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Converts the ActualWidth of a <see cref="Page"/> to a width that dynamically defines the length of a SplitView's pane.
    /// </summary>
    internal sealed class PageWidthToOpenPaneLengthConverter : IValueConverter
    {
        public int PaneWidth { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value.GetType() != typeof(double))
            {
                throw new ArgumentException($"{nameof(value)} must be a {nameof(Double)}.");
            }

            var actualWidth = (double)value;
            if (actualWidth < PaneWidth * 2)
            {
                return actualWidth;
            }

            return Math.Floor(actualWidth - PaneWidth);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
