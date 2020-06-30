using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.RemoteStorageProvider.ManualTester
{
    public sealed class MainPageViewModel : INotifyPropertyChanged, IDisposable
    {
        private const string FileContentChunk = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Mauris fringilla ac arcu eget pulvinar.";

        private readonly ISettingsProvider _settingsProvider;

        private bool _isDisposed;
        private bool _isAuthenticatedToRemoteStorageProvider;

        /// <summary>
        /// Gets or sets whether the user is authenticated to a cloud storage provider.
        /// </summary>
        internal bool IsAuthenticatedToRemoteStorageProvider
        {
            get => _isAuthenticatedToRemoteStorageProvider;
            set
            {
                _isAuthenticatedToRemoteStorageProvider = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the list of remote storage providers.
        /// </summary>
        internal IEnumerable<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> RemoteStorageProviders { get; }

        /// <summary>
        /// Gets the current account's email address.
        /// </summary>
        internal TaskCompletionNotifier<string> CurrentAccountUserName
            => new TaskCompletionNotifier<string>(
                GetCurrentRemoteCloudStorageProvider()?.GetUserNameAsync(CancellationToken.None));

        /// <summary>
        /// Gets the current account's email address.
        /// </summary>
        internal TaskCompletionNotifier<string> CurrentAccountEmailAddress
            => new TaskCompletionNotifier<string>(
                GetCurrentRemoteCloudStorageProvider()?.GetUserEmailAddressAsync(CancellationToken.None));

        /// <summary>
        /// Gets the current account's email address.
        /// </summary>
        internal TaskCompletionNotifier<BitmapImage> CurrentAccountProfilePicture
            => new TaskCompletionNotifier<BitmapImage>(
                GetCurrentRemoteCloudStorageProvider()?.GetUserProfilePictureAsync(CancellationToken.None));

        /// <summary>
        /// Gets the name of the current account provider.
        /// </summary>
        internal string CurrentAccountProviderName => GetCurrentRemoteCloudStorageProvider()?.DisplayName;

        internal CompositionHost ExportProvider { get; private set; }


        public event PropertyChangedEventHandler PropertyChanged;

        public MainPageViewModel()
        {
            // Do all the tests in English.
            LanguageManager.Instance.SetCurrentCulture(new CultureInfo("en"));

            var configuration = new ContainerConfiguration()
                .WithAssembly(typeof(MainPageViewModel).Assembly) // this assembly
                .WithAssembly(typeof(Constants).Assembly); // PaZword.Core
            ExportProvider = configuration.CreateContainer();

            _settingsProvider = ExportProvider.GetExport<ISettingsProvider>();
            RemoteStorageProviders = ExportProvider.GetExports<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>>();

            SignInToRemoteStorageServiceCommand = new AsyncActionCommand<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>>(ExecuteSignInToRemoteStorageServiceCommandAsync);
            SignOutCommand = new AsyncActionCommand<object>(ExecuteSignOutCommandAsync);
        }

        ~MainPageViewModel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                ExportProvider.Dispose();
            }

            _isDisposed = true;
        }

        #region SignInToRemoteStorageServiceCommand

        public AsyncActionCommand<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> SignInToRemoteStorageServiceCommand { get; }

        private async Task ExecuteSignInToRemoteStorageServiceCommandAsync(Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata> remoteStorageProvider, CancellationToken cancellationToken)
        {
            // Sign in to the selected provider.
            if (await remoteStorageProvider.Value.SignInAsync(interactive: true, cancellationToken).ConfigureAwait(false))
            {
                _settingsProvider.SetSetting(SettingsDefinitions.SyncDataWithCloud, true);
                _settingsProvider.SetSetting(SettingsDefinitions.RemoteStorageProviderName, remoteStorageProvider.Metadata.ProviderName);

                await UpdateAccountInfoAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region SignOutCommand

        internal AsyncActionCommand<object> SignOutCommand { get; }

        private async Task ExecuteSignOutCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            await GetCurrentRemoteCloudStorageProvider().SignOutAsync().ConfigureAwait(false);

            _settingsProvider.ResetSetting(SettingsDefinitions.SyncDataWithCloud);
            _settingsProvider.ResetSetting(SettingsDefinitions.RemoteStorageProviderName);

            await UpdateAccountInfoAsync(cancellationToken).ConfigureAwait(false);
        }

        #endregion

        internal async void GetFilesButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var result = await GetCurrentRemoteCloudStorageProvider().GetFilesAsync(2000, CancellationToken.None).ConfigureAwait(false);
            Debugger.Break();
        }

        internal async void UploadFileButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("test.txt", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, GenerateFileContent());

            var result = await GetCurrentRemoteCloudStorageProvider().UploadFileAsync(file, CancellationToken.None).ConfigureAwait(false);

            Debugger.Break();
        }

        internal async void DownloadFileButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync("test.txt");
            if (file != null)
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            var result = await GetCurrentRemoteCloudStorageProvider().DownloadFileAsync("test.txt", CancellationToken.None).ConfigureAwait(false);

            Debugger.Break();
        }

        internal async void UploadSmallFileButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("testSmall.txt", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, FileContentChunk);

            var result = await GetCurrentRemoteCloudStorageProvider().UploadFileAsync(file, CancellationToken.None).ConfigureAwait(false);

            Debugger.Break();
        }

        internal async void DownloadSmallFileButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync("testSmall.txt");
            if (file != null)
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            var result = await GetCurrentRemoteCloudStorageProvider().DownloadFileAsync("testSmall.txt", CancellationToken.None).ConfigureAwait(false);

            Debugger.Break();
        }

        internal async void DeleteFileButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await GetCurrentRemoteCloudStorageProvider().DeleteFileAsync("test.txt", CancellationToken.None).ConfigureAwait(false);
            await GetCurrentRemoteCloudStorageProvider().DeleteFileAsync("testSmall.txt", CancellationToken.None).ConfigureAwait(false);
        }

        private async Task UpdateAccountInfoAsync(CancellationToken cancellationToken)
        {
            IRemoteStorageProvider provider = GetCurrentRemoteCloudStorageProvider();
            if (provider != null)
            {
                IsAuthenticatedToRemoteStorageProvider = await provider.SignInAsync(interactive: false, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                IsAuthenticatedToRemoteStorageProvider = false;
            }

            RaisePropertyChanged(nameof(CurrentAccountUserName));
            RaisePropertyChanged(nameof(CurrentAccountEmailAddress));
            RaisePropertyChanged(nameof(CurrentAccountProfilePicture));
            RaisePropertyChanged(nameof(CurrentAccountProviderName));
        }

        private IRemoteStorageProvider GetCurrentRemoteCloudStorageProvider()
        {
            string targetterProviderName = _settingsProvider.GetSetting(SettingsDefinitions.RemoteStorageProviderName);
            return RemoteStorageProviders.SingleOrDefault(m => string.Equals(m.Metadata.ProviderName, targetterProviderName, StringComparison.Ordinal))?.Value;
        }

        internal async void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }).ConfigureAwait(false);
        }

        internal string GenerateFileContent()
        {
            var str = new StringBuilder();

            for (int i = 0; i < 100000; i++)
            {
                str.AppendLine(FileContentChunk);
            }

            return str.ToString();
        }
    }
}
