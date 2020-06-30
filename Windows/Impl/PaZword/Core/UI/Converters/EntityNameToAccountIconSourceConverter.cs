using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Services;
using PaZword.Core.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.Core.UI.Converters
{
    /// <summary>
    /// Convert a entity name to a <see cref="ImageSource"/> value through the <see cref="IIconService"/>.
    /// </summary>
    internal sealed class EntityNameToAccountIconSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                var valueString = value as string;
                if (string.IsNullOrWhiteSpace(valueString) || !(Windows.UI.Xaml.Application.Current is IApp app))
                {
                    return new TaskCompletionNotifier<BitmapImage>(Task.FromResult(new BitmapImage(new Uri("ms-appx://PaZword/Assets/DefaultAccountIcon.png"))));
                }

                var task = Task.Run(async () =>
                {
                    string base64Icon = await app.ExportProvider.GetExport<IIconService>()
                        .ResolveIconOnlineAsync(valueString, string.Empty, CancellationToken.None).ConfigureAwait(false);

                    return await TaskHelper.RunOnUIThreadAsync(async ()
                        => await app.ExportProvider.GetExport<ISerializationProvider>()
                            .Base64ToBitmapImageAsync(base64Icon).ConfigureAwait(false))
                    .ConfigureAwait(false);
                });

                return new TaskCompletionNotifier<BitmapImage>(task);
            }
            catch
            {
                return new TaskCompletionNotifier<BitmapImage>(Task.FromResult(new BitmapImage(new Uri("ms-appx://PaZword/Assets/DefaultAccountIcon.png"))));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
