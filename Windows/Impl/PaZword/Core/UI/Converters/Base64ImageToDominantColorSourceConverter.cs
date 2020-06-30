using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Core.Threading;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Convert a Base64 <see cref="string"/> representation of an image to the dominant color of the image.
    /// </summary>
    internal sealed class Base64ImageToDominantColorSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (new AccessibilitySettings().HighContrast)
            {
                return new TaskCompletionNotifier<Color>(Task.FromResult((Color)Application.Current.Resources["SystemAccentColor"]));
            }

            var valueString = value as string;
            if (string.IsNullOrWhiteSpace(valueString) || !(Application.Current is IApp app))
            {
                return new TaskCompletionNotifier<Color>(Task.FromResult((Color)Application.Current.Resources["SystemAccentColor"]));
            }

            return new TaskCompletionNotifier<Color>(
                app.ExportProvider.GetExport<ISerializationProvider>()
                .Base64ToDominantColorAsync(valueString));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
