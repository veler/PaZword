using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.UI;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Localization;
using PaZword.Models.Data;
using PaZword.Views.Data;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace PaZword.ViewModels.Data.File
{
    /// <summary>
    /// Interaction logic for <see cref="FileDataUserControl"/>
    /// </summary>
    internal sealed class FileDataViewModel : AccountDataViewModelBase
    {
        private const string ViewFileEvent = "FileData.ViewFile.Command";
        private const string SelectFileButtonEvent = "FileData.SelectFile.Command";
        private const string DragOverEvent = "FileData.DragOver";
        private const string DropEvent = "FileData.Drop";

        private const uint FileAttributesReadOnly = 1;

        private readonly ISerializationProvider _serializationProvider;
        private readonly IDataManager _dataManager;
        private readonly IWindowManager _windowManager;
        private readonly ILogger _logger;

        private StorageFile _newSelectedStorageFile;
        private bool _lastSelectedFileIsTooLarge;
        private bool _tempFileCreated;
        private FileDataUserControl _control;

        public FileDataStrings Strings => LanguageManager.Instance.FileData;

        internal string FileSizeLimit
            => Strings.GetFormattedFileSizeLimit(Constants.DataFileSizeLimit.ToString(CultureInfo.CurrentCulture));

        public override string Title => Strings.DisplayName;

        public override FrameworkElement UserInterface
        {
            get
            {
                if (_control == null)
                {
                    _control = new FileDataUserControl(this);
                }
                return _control;
            }
        }

        public Visibility DescriptionFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.FileName)
            ? Visibility.Collapsed
            : Visibility.Visible;

        /// <summary>
        /// Defines whether a file is present.
        /// </summary>
        public bool FileIsPresent => !StringExtensions.IsNullOrEmptySecureString(CastedData.FileName);

        /// <summary>
        /// Defines whether a file is present in editing mode.
        /// </summary>
        public bool FileIsPresentEditing => CastedDataEditMode != null && !StringExtensions.IsNullOrEmptySecureString(CastedDataEditMode.FileName);

        /// <summary>
        /// Gets whether the last selected file is too large or not.
        /// </summary>
        public bool LastSelectedFileIsTooLarge
        {
            get => _lastSelectedFileIsTooLarge;
            private set
            {
                _lastSelectedFileIsTooLarge = value;
                RaisePropertyChanged();
            }
        }

        private FileData CastedData => (FileData)Data;

        private FileData CastedDataEditMode => (FileData)DataEditMode;

        public FileDataViewModel(
            FileData accountData,
            ISerializationProvider serializationProvider,
            IDataManager dataManager,
            IWindowManager windowManager,
            ILogger logger)
            : base(logger, serializationProvider, windowManager)
        {
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            _windowManager = Arguments.NotNull(windowManager, nameof(windowManager));
            _logger = Arguments.NotNull(logger, nameof(logger));

            Data = accountData;

            ViewFileCommand = new AsyncActionCommand<object>(_logger, ViewFileEvent, ExecuteViewFileCommandAsync);
            SelectFileButtonCommand = new AsyncActionCommand<object>(_logger, SelectFileButtonEvent, ExecuteSelectFileButtonCommandAsync);
            SelectFileGridDragOverCommand = new ActionCommand<DragEventArgs>(_logger, DragOverEvent, ExecuteSelectFileGridDragOverCommand);
            SelectFileGridDropCommand = new AsyncActionCommand<DragEventArgs>(_logger, DropEvent, ExecuteSelectFileGridDropCommandAsync);
        }

        #region ViewFileCommand

        internal AsyncActionCommand<object> ViewFileCommand { get; }

        private async Task ExecuteViewFileCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            // Load an decrypt the file.
            byte[] fileContent = await _dataManager.LoadFileDataAsync(Data.Id).ConfigureAwait(false);

            var fileName = GetTempFileName();
            var tempFolder = ApplicationData.Current.TemporaryFolder;

            if (await tempFolder.TryGetItemAsync(fileName) == null)
            {
                await CoreHelper.RetryAsync(async () =>
                {
                    // Create a temporary file.
                    var tempFile = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
                    await FileIO.WriteBytesAsync(tempFile, fileContent);

                    _tempFileCreated = true;

                    // Make the file read only
                    var key = "System.FileAttributes";
                    var retrieveList = new string[] { key };
                    var props = await tempFile.Properties.RetrievePropertiesAsync(retrieveList);
                    if (props != null)
                    {
                        var temp = (uint)props[key] | FileAttributesReadOnly;
                        props[key] = temp;
                    }
                    else
                    {
                        props = new Windows.Foundation.Collections.PropertySet
                        {
                            { key, FileAttributesReadOnly }
                        };
                    }

                    await tempFile.Properties.SavePropertiesAsync(props);
                }).ConfigureAwait(false);
            }

            // Open the file with the default editor.
            var fileToOpen = await tempFolder.GetFileAsync(fileName);

            TaskHelper.RunOnUIThreadAsync(() =>
            {
                Windows.System.Launcher.LaunchFileAsync(
                    fileToOpen,
                    new Windows.System.LauncherOptions()
                    {
                        TreatAsUntrusted = false
                    }).AsTask().Forget();
            }).Forget();
        }

        #endregion

        #region SelectFileButtonCommand

        internal AsyncActionCommand<object> SelectFileButtonCommand { get; }

        private async Task ExecuteSelectFileButtonCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            await TaskHelper.RunOnUIThreadAsync(async () =>
            {
                LastSelectedFileIsTooLarge = false;

                var fileOpenPicker = new Windows.Storage.Pickers.FileOpenPicker
                {
                    ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
                    SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
                };
                fileOpenPicker.FileTypeFilter.Add("*");

                StorageFile storageFile = await fileOpenPicker.PickSingleFileAsync();
                if (storageFile != null)
                {
                    await SelectFileAsync(storageFile, cancellationToken).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        #endregion

        #region SelectFileGridDragOverCommand

        internal ActionCommand<DragEventArgs> SelectFileGridDragOverCommand { get; }

        private void ExecuteSelectFileGridDragOverCommand(DragEventArgs parameter)
        {
            if (parameter.DataView.Contains(StandardDataFormats.StorageItems))
            {
                LastSelectedFileIsTooLarge = false;
                parameter.AcceptedOperation = DataPackageOperation.Copy;
                parameter.Handled = true;
            }
        }

        #endregion

        #region SelectFileGridDropCommand

        internal AsyncActionCommand<DragEventArgs> SelectFileGridDropCommand { get; }

        private async Task ExecuteSelectFileGridDropCommandAsync(DragEventArgs parameter, CancellationToken cancellationToken)
        {
            await TaskHelper.RunOnUIThreadAsync(async () =>
            {
                if (!parameter.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    return;
                }

                var files = await parameter.DataView.GetStorageItemsAsync();
                if (files.Count != 1)
                {
                    return;
                }

                if (files[0] is StorageFile storageFile)
                {
                    await SelectFileAsync(storageFile, cancellationToken).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        #endregion

        public override async Task<bool> ValidateChangesAsync(CancellationToken cancellationToken)
        {
            if (_newSelectedStorageFile != null)
            {
                // Maybe the file has been removed since it has been selected by the user.
                try
                {
                    using (IRandomAccessStreamWithContentType fileStream = await _newSelectedStorageFile.OpenReadAsync())
                    { }
                }
                catch
                {
                    await _windowManager.ShowMessageDialogAsync(
                        message: Strings.GetFormattedFileNotFoundMessage(_newSelectedStorageFile.Path),
                        closeButtonText: Strings.FileNotFoundMessageCloseButtonText
                        ).ConfigureAwait(false);

                    _newSelectedStorageFile = null;
                    CastedDataEditMode.FileExtension = null;
                    CastedDataEditMode.FileName = null;
                    CastedDataEditMode.Base64Thumbnail = null;

                    RaisePropertyChanged(nameof(DataEditMode));
                    RaisePropertyChanged(nameof(FileIsPresentEditing));

                    return false;
                }
            }

            return await base.ValidateChangesAsync(cancellationToken).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.FileName, Strings.Description).ConfigureAwait(false);
        }

        public override async Task SaveAsync(CancellationToken cancellationToken)
        {
            if (_newSelectedStorageFile != null)
            {
                await _dataManager.SaveFileDataAsync(DataEditMode.Id, _newSelectedStorageFile).ConfigureAwait(false);
            }

            await base.SaveAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task DeleteAsync(CancellationToken cancellationToken)
        {
            await _dataManager.DeleteFileDataAsync(Data.Id).ConfigureAwait(false);
            await base.DeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task UnloadingAsync()
        {
            if (!_tempFileCreated)
            {
                return;
            }

            // Delete temporary file.
            var fileName = GetTempFileName();
            var tempFile = ApplicationData.Current.TemporaryFolder;

            var temporaryFile = await tempFile.TryGetItemAsync(fileName);
            if (temporaryFile != null)
            {
                await CoreHelper.RetryAsync(async () =>
                {
                    await temporaryFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }).ConfigureAwait(false);
            }
        }

        public override string GenerateSubtitle()
        {
            return string.Empty;
        }

        private async Task SelectFileAsync(StorageFile storageFile, CancellationToken cancellationToken)
        {
            Arguments.NotNull(storageFile, nameof(storageFile));

            // Check whether the file is under the size limit.
            ulong sizeLimitInMegaByte = 1000000 * Constants.DataFileSizeLimit;
            if ((await storageFile.GetBasicPropertiesAsync()).Size > sizeLimitInMegaByte)
            {
                LastSelectedFileIsTooLarge = true;
                return;
            }

            // Generate the thumbnail.
            StorageItemThumbnail fileThumbnail = await storageFile.GetThumbnailAsync(ThumbnailMode.SingleItem, Constants.DataFileThumbnailSize);
            BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(fileThumbnail);
            var transform = new BitmapTransform();
            byte[] pixelData = (await bitmapDecoder.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.ColorManageToSRgb)).DetachPixelData();
            var bitmap = new WriteableBitmap((int)bitmapDecoder.PixelWidth, (int)bitmapDecoder.PixelHeight);
            using (Stream stream = bitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixelData, 0, pixelData.Length).ConfigureAwait(false);
            }

            CastedDataEditMode.Base64Thumbnail = await _serializationProvider.WritableBitmapToBase64Async(bitmap, cancellationToken).ConfigureAwait(true);
            CastedDataEditMode.FileName = storageFile.DisplayName.ToSecureString();

            CastedDataEditMode.FileExtension
                = storageFile.FileType
                .ToUpper(CultureInfo.CurrentCulture)
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .ToSecureString();

            _newSelectedStorageFile = storageFile;

            RaisePropertyChanged(nameof(DataEditMode));
            RaisePropertyChanged(nameof(FileIsPresentEditing));
        }

        private string GetTempFileName()
        {
            return $"{Data.Id}.{CastedData.FileExtension.ToUnsecureString().ToLower(CultureInfo.CurrentCulture)}";
        }
    }
}
