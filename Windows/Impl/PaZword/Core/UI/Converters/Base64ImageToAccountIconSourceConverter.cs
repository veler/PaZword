using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Core.Threading;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Convert a Base64 <see cref="string"/> representation of an image to a <see cref="ImageSource"/> value that corresponds to an <see cref="Account"/> icon.
    /// </summary>
    internal sealed class Base64ImageToAccountIconSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var valueString = value as string;
            if (string.IsNullOrWhiteSpace(valueString) || !(Windows.UI.Xaml.Application.Current is IApp app))
            {
                return new TaskCompletionNotifier<BitmapImage>(Task.FromResult(new BitmapImage(new Uri("ms-appx://PaZword/Assets/DefaultAccountIcon.png"))));
            }

            return new TaskCompletionNotifier<BitmapImage>(
                app.ExportProvider.GetExport<ISerializationProvider>()
                .Base64ToBitmapImageAsync(valueString));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
