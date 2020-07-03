using Microsoft.Toolkit.Uwp.UI.Controls;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Security;
using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Localization;
using PaZword.Views;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Printing;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Printing;

namespace PaZword.ViewModels.Other
{
    /// <summary>
    /// Interaction logic for <see cref="FirstStartExperiencePage"/>
    /// </summary>
    [Export(typeof(FirstStartExperiencePageViewModel))]
    public sealed class FirstStartExperiencePageViewModel : ViewModelBase, IDisposable
    {
        private const string FirstStartEvent = "FirstStart.Initialize";
        private const string BackEvent = "FirstStart.Back.Command";
        private const string ContinueEvent = "FirstStart.Continue.Command";
        private const string NewUserEvent = "FirstStart.NewUser.Command";
        private const string ReturningUserEvent = "FirstStart.ReturningUser.Command";
        private const string ContinueGenerateSecretKeyEvent = "FirstStart.ContinueGenerateSecretKey.Command";
        private const string CopyRecoveryKeyEvent = "FirstStart.SecretKey.Copy.Command";
        private const string ContinueWindowsHelloEvent = "FirstStart.ContinueWindowsHello.Command";
        private const string SaveRecoveryKeyEvent = "FirstStart.SecretKey.Save.Command";
        private const string PrintRecoveryKeyEvent = "FirstStart.SecretKey.Print.Command";
        private const string RegisterToRemoteStorageServiceEvent = "FirstStart.Synchronization.Register.Command";
        private const string DoNotConnectRemoteStorageServiceEvent = "FirstStart.Synchronization.Skip.Command";
        private const string SingInToRemoteStorageServiceEvent = "FirstStart.Synchronization.SignIn.Command";
        private const string MarkdownLinkEvent = "FirstStart.MarkDownLink.Command";
        private const string RecoveryKeyChangedEvent = "FirstStart.SecretKey.Changed";

        internal const int StepNewOrReturningUser = 1;
        internal const int StepGenerateSecretKey = 2;
        internal const int StepRegisterToCloudService = 3;
        internal const int StepSignInToCloudService = 4;
        internal const int StepSynchronizing = 5;
        internal const int StepEnterSecretKey = 6;
        internal const int StepWindowsHello = 7;
        internal const int StepAllSet = 8;

        private readonly ILogger _logger;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IWindowsHelloAuthProvider _windowsHelloAuthProvider;
        private readonly IRemoteSynchronizationService _remoteSynchronizationService;
        private readonly IDataManager _dataManager;
        private readonly DispatcherTimer _timer;
        private readonly Grid _printDocumentPageContent = new Grid();

        private int _backupCurrentStepIndex;
        private int _currentStepIndex;
        private SecureString _recoveryKey;
        private bool _isSigningIn;
        private bool _recoveryKeySaved;
        private bool _invalidRecoveryKey;
        private string _invalidRecoveryKeyReason;
        private bool _windowsHelloIsEnabled;
        private bool _useWindowsHello;
        private IPrintDocumentSource _printDocumentSource;
        private PrintDocument _printDocument;


        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal FirstStartExperiencePageStrings Strings => LanguageManager.Instance.FirstStartExperiencePage;

        /// <summary>
        /// Gets or sets the index of the current displayed step in the wizard.
        /// </summary>
        internal int CurrentStepIndex
        {
            get => _currentStepIndex;
            private set
            {
                _currentStepIndex = value;
                RaisePropertyChanged();
                BackCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether the user is authenticating to a remote storage provider.
        /// </summary>
        internal bool IsSigningIn
        {
            get => _isSigningIn;
            set
            {
                _isSigningIn = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the recovery key.
        /// </summary>
        internal string RecoveryKey
        {
            get
            {
                lock (_printDocumentPageContent)
                {
                    return _recoveryKey.ToUnsecureString();
                }
            }
            set
            {
                lock (_printDocumentPageContent)
                {
                    _recoveryKey?.Dispose();
                    _recoveryKey = value.ToSecureString();
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether the entered recovery key is invalid.
        /// </summary>
        internal bool InvalidRecoveryKey
        {
            get => _invalidRecoveryKey;
            set
            {
                _invalidRecoveryKey = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the reason why the recovery key couldn't work.
        /// </summary>
        internal string InvalidRecoveryKeyReason
        {
            get => _invalidRecoveryKeyReason;
            set
            {
                _invalidRecoveryKeyReason = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the list of remote storage providers.
        /// </summary>
        internal IEnumerable<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> RemoteStorageProviders { get; }

        /// <summary>
        /// Gets the FAQ about remote storage providers in Markdown.
        /// </summary>
        internal TaskCompletionNotifier<string> FaqRemoteStorageProvider { get; } = new TaskCompletionNotifier<string>(LanguageManager.Instance.GetFaqRemoteStorageProviderAsync());

        /// <summary>
        /// Gets a value that defines whether Windows Hello is enabled and that the user defined a PIN.
        /// </summary>
        internal bool WindowsHelloIsEnabled
        {
            get => _windowsHelloIsEnabled;
            set
            {
                _windowsHelloIsEnabled = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether the user must sign in the application with Windows Hello.
        /// </summary>
        internal bool UseWindowsHello
        {
            get => _useWindowsHello;
            set
            {
                _useWindowsHello = value;
                RaisePropertyChanged();
            }
        }

        [ImportingConstructor]
        public FirstStartExperiencePageViewModel(
            ILogger logger,
            IEncryptionProvider encryptionProvider,
            ISettingsProvider settingsProvider,
            IWindowsHelloAuthProvider windowsHelloAuthProvider,
            IRemoteSynchronizationService remoteSynchronizationService,
            IDataManager dataManager,
            IRecurrentTaskService recurrentTaskService,
            [ImportMany] IEnumerable<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> remoteStorageProviders)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _encryptionProvider = Arguments.NotNull(encryptionProvider, nameof(encryptionProvider));
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));
            _windowsHelloAuthProvider = Arguments.NotNull(windowsHelloAuthProvider, nameof(windowsHelloAuthProvider));
            _remoteSynchronizationService = Arguments.NotNull(remoteSynchronizationService, nameof(remoteSynchronizationService));
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            RemoteStorageProviders = Arguments.NotNull(remoteStorageProviders, nameof(remoteStorageProviders));

            Arguments.NotNull(recurrentTaskService, nameof(recurrentTaskService)).Pause();

            BackCommand = new ActionCommand<object>(_logger, BackEvent, ExecuteBackCommand);
            ContinueWelcomeCommand = new ActionCommand<object>(_logger, ContinueEvent, ExecuteContinueWelcomeCommand);
            NewUserCommand = new ActionCommand<object>(_logger, NewUserEvent, ExecuteNewUserCommand);
            ReturningUserCommand = new ActionCommand<object>(_logger, ReturningUserEvent, ExecuteReturningUserCommand);
            ContinueGenerateSecretKeyCommand = new ActionCommand<object>(_logger, ContinueGenerateSecretKeyEvent, ExecuteContinueGenerateSecretKeyCommand, CanExecuteContinueGenerateSecretKeyCommand);
            CopyRecoveryKeyCommand = new ActionCommand<object>(_logger, CopyRecoveryKeyEvent, ExecuteCopyRecoveryKeyCommand);
            SaveRecoveryKeyCommand = new AsyncActionCommand<object>(logger, SaveRecoveryKeyEvent, ExecuteSaveRecoveryKeyCommandAsync);
            PrintRecoveryKeyCommand = new AsyncActionCommand<object>(logger, PrintRecoveryKeyEvent, ExecutePrintRecoveryKeyCommandAsync, CanExecutePrintRecoveryKeyCommand);
            RegisterToRemoteStorageServiceCommand = new AsyncActionCommand<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>>(logger, RegisterToRemoteStorageServiceEvent, ExecuteRegisterToRemoteStorageServiceCommandAsync);
            DoNotConnectRemoteStorageServiceCommand = new AsyncActionCommand<object>(logger, DoNotConnectRemoteStorageServiceEvent, ExecuteDoNotConnectRemoteStorageServiceCommandAsync);
            MarkdownLinkClickedCommand = new AsyncActionCommand<LinkClickedEventArgs>(logger, MarkdownLinkEvent, ExecuteMarkdownLinkClickedCommandAsync);
            ContinueWindowsHelloCommand = new ActionCommand<object>(_logger, ContinueWindowsHelloEvent, ExecuteContinueWindowsHelloCommand);
            ContinueAllSetCommand = new AsyncActionCommand<object>(logger, ContinueEvent, ExecuteContinueAllSetCommandAsync);
            SignInToRemoteStorageServiceCommand = new AsyncActionCommand<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>>(logger, SingInToRemoteStorageServiceEvent, ExecuteSignInToRemoteStorageServiceCommandAsync);
            RecoveryKeyChangedCommand = new AsyncActionCommand<object>(logger, RecoveryKeyChangedEvent, ExecuteRecoveryKeyChangedCommandAsync, isCancellable: true, startOnNewThread: true);

            PrintManager printManager = PrintManager.GetForCurrentView();
            printManager.PrintTaskRequested += PrintManager_PrintTaskRequested;

            // Reset all non-roaming settings.
            _settingsProvider.ResetSetting(SettingsDefinitions.PasswordGeneratorEasyToRead);
            _settingsProvider.ResetSetting(SettingsDefinitions.PasswordGeneratorLength);
            _settingsProvider.ResetSetting(SettingsDefinitions.Theme);
            _settingsProvider.ResetSetting(SettingsDefinitions.UseWindowsHello);

            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Start();

            _remoteSynchronizationService.SynchronizationCompleted += RemoteSynchronizationService_SynchronizationCompleted;

            _logger.LogEvent(FirstStartEvent, "First start experience initialized.");
        }

        public void Dispose()
        {
            if (_recoveryKey != null)
            {
                _recoveryKey.Dispose();
            }

            _timer.Stop();

            _dataManager.Dispose();
            PrintManager printManager = PrintManager.GetForCurrentView();
            printManager.PrintTaskRequested -= PrintManager_PrintTaskRequested;
        }

        #region BackCommand

        internal ActionCommand<object> BackCommand { get; }

        private void ExecuteBackCommand(object parameter)
        {
            if (CurrentStepIndex == StepSignInToCloudService)
            {
                CurrentStepIndex = StepNewOrReturningUser;
            }
            else if (CurrentStepIndex == StepEnterSecretKey || CurrentStepIndex == StepSynchronizing)
            {
                _remoteSynchronizationService.Cancel();
                _settingsProvider.ResetSetting(SettingsDefinitions.SyncDataWithCloud);
                _settingsProvider.ResetSetting(SettingsDefinitions.RemoteStorageProviderName);
                RecoveryKey = string.Empty;
                InvalidRecoveryKey = false;
                CurrentStepIndex = StepSignInToCloudService;
            }
            else if (CurrentStepIndex == StepWindowsHello)
            {
                _settingsProvider.ResetSetting(SettingsDefinitions.UseWindowsHello);
                CurrentStepIndex = _backupCurrentStepIndex;
            }
            else
            {
                CurrentStepIndex--;
            }

            _logger.LogEvent(BackEvent, $"Back button pressed. New step index = {CurrentStepIndex}");
        }

        #endregion

        #region ContinueWelcomeCommand

        internal ActionCommand<object> ContinueWelcomeCommand { get; }

        private void ExecuteContinueWelcomeCommand(object parameter)
        {
            TaskHelper.ThrowIfNotOnUIThread();

            CurrentStepIndex = StepNewOrReturningUser;
            _timer.Start();
        }

        #endregion

        #region NewUserCommand

        internal ActionCommand<object> NewUserCommand { get; }

        private void ExecuteNewUserCommand(object parameter)
        {
            TaskHelper.ThrowIfNotOnUIThread();

            _printDocument = new PrintDocument();
            _printDocument.AddPages += PrintDocument_AddPages;
            _printDocument.GetPreviewPage += PrintDocument_GetPreviewPage;

            _printDocumentSource = _printDocument.DocumentSource;

            PasswordCredential secretKeys = _encryptionProvider.GenerateSecretKeys();
            RecoveryKey = _encryptionProvider.EncodeSecretKeysToBase64(secretKeys);
            _encryptionProvider.SetSecretKeys(secretKeys);

            _recoveryKeySaved = false;
            ContinueGenerateSecretKeyCommand.RaiseCanExecuteChanged();

            CurrentStepIndex = StepGenerateSecretKey;

            _dataManager.ClearLocalDataAsync(CancellationToken.None).Forget();
        }

        private void PrintDocument_AddPages(object sender, AddPagesEventArgs e)
        {
            GeneratePrintDocumentPageContent();

            _printDocument.AddPage(_printDocumentPageContent);
            _printDocument.AddPagesComplete();
        }

        private void PrintDocument_GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            GeneratePrintDocumentPageContent();

            _printDocument.SetPreviewPageCount(1, PreviewPageCountType.Final);
            _printDocument.SetPreviewPage(1, _printDocumentPageContent);
        }

        #endregion

        #region ReturningUserCommand

        internal ActionCommand<object> ReturningUserCommand { get; }

        private void ExecuteReturningUserCommand(object parameter)
        {
            CurrentStepIndex = StepSignInToCloudService;
        }

        #endregion

        #region CopyRecoveryKeyCommand

        internal ActionCommand<object> CopyRecoveryKeyCommand { get; }

        private void ExecuteCopyRecoveryKeyCommand(object parameter)
        {
            _recoveryKeySaved = true;
            ContinueGenerateSecretKeyCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region SaveRecoveryKey

        internal AsyncActionCommand<object> SaveRecoveryKeyCommand { get; }

        private async Task ExecuteSaveRecoveryKeyCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            TaskHelper.ThrowIfNotOnUIThread();

            var saveFileDialog = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = Strings.SecretKeyFileName
            };

            saveFileDialog.FileTypeChoices.Add(Strings.PlainTextFile, new List<string> { ".txt" });

            StorageFile file = await saveFileDialog.PickSaveFileAsync();
            if (file == null)
            {
                return;
            }

            await CoreHelper.RetryAsync(async () =>
            {
                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteTextAsync(file, RecoveryKey);
                await CachedFileManager.CompleteUpdatesAsync(file);
                _recoveryKeySaved = true;
                ContinueGenerateSecretKeyCommand.RaiseCanExecuteChanged();
            }).ConfigureAwait(false);
        }

        #endregion

        #region PrintRecoveryKey

        internal AsyncActionCommand<object> PrintRecoveryKeyCommand { get; }

        private bool CanExecutePrintRecoveryKeyCommand(object parameter)
        {
            return PrintManager.IsSupported();
        }

        private async Task ExecutePrintRecoveryKeyCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            TaskHelper.ThrowIfNotOnUIThread();
            await PrintManager.ShowPrintUIAsync();
        }

        #endregion

        #region ContinueGenerateSecretKeyCommand

        internal ActionCommand<object> ContinueGenerateSecretKeyCommand { get; }

        private bool CanExecuteContinueGenerateSecretKeyCommand(object parameter)
        {
            return _recoveryKeySaved;
        }

        private void ExecuteContinueGenerateSecretKeyCommand(object parameter)
        {
            CurrentStepIndex = StepRegisterToCloudService;
        }

        #endregion

        #region RegisterToRemoteStorageServiceCommand

        public AsyncActionCommand<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> RegisterToRemoteStorageServiceCommand { get; }

        private async Task ExecuteRegisterToRemoteStorageServiceCommandAsync(Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata> remoteStorageProvider, CancellationToken cancellationToken)
        {
            IsSigningIn = true;

            // Sign out from all providers, just in case.
            await SignOutFromAllRemoteStorageProviders().ConfigureAwait(false);

            // Sign in to the selected provider.
            if (await remoteStorageProvider.Value.SignInAsync(interactive: true, cancellationToken).ConfigureAwait(false))
            {
                _settingsProvider.SetSetting(SettingsDefinitions.SyncDataWithCloud, true);
                _settingsProvider.SetSetting(SettingsDefinitions.RemoteStorageProviderName, remoteStorageProvider.Metadata.ProviderName);
                _backupCurrentStepIndex = CurrentStepIndex;
                CurrentStepIndex = StepWindowsHello;
            }

            IsSigningIn = false;
        }

        #endregion

        #region DoNotConnectRemoteStorageServiceCommand

        internal AsyncActionCommand<object> DoNotConnectRemoteStorageServiceCommand { get; }

        private async Task ExecuteDoNotConnectRemoteStorageServiceCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            // Sign out from all providers, just in case.
            await SignOutFromAllRemoteStorageProviders().ConfigureAwait(false);

            _settingsProvider.ResetSetting(SettingsDefinitions.SyncDataWithCloud);
            _settingsProvider.ResetSetting(SettingsDefinitions.RemoteStorageProviderName);
            _backupCurrentStepIndex = CurrentStepIndex;
            CurrentStepIndex = StepWindowsHello;
        }

        #endregion

        #region MarkdownLinkClickedCommand

        internal AsyncActionCommand<LinkClickedEventArgs> MarkdownLinkClickedCommand { get; }

        private async Task ExecuteMarkdownLinkClickedCommandAsync(LinkClickedEventArgs args, CancellationToken cancellationToken)
        {
            string uriToLaunch = args.Link;
            var uri = new Uri(uriToLaunch);

            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        #endregion

        #region SignInToRemoteStorageServiceCommand

        public AsyncActionCommand<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>> SignInToRemoteStorageServiceCommand { get; }

        private async Task ExecuteSignInToRemoteStorageServiceCommandAsync(Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata> remoteStorageProvider, CancellationToken cancellationToken)
        {
            IsSigningIn = true;

            // Sign out from all providers, just in case.
            await SignOutFromAllRemoteStorageProviders().ConfigureAwait(false);

            // Sign in to the selected provider.
            if (await remoteStorageProvider.Value.SignInAsync(interactive: true, cancellationToken).ConfigureAwait(false))
            {
                _settingsProvider.SetSetting(SettingsDefinitions.SyncDataWithCloud, true);
                _settingsProvider.SetSetting(SettingsDefinitions.RemoteStorageProviderName, remoteStorageProvider.Metadata.ProviderName);
                RecoveryKey = string.Empty;
                CurrentStepIndex = StepSynchronizing;
                await _dataManager.ClearLocalDataAsync(cancellationToken).ConfigureAwait(false);
                _remoteSynchronizationService.Cancel();
                _remoteSynchronizationService.QueueSynchronization();
            }

            IsSigningIn = false;
        }

        #endregion

        #region RecoveryKeyChangedCommand

        internal AsyncActionCommand<object> RecoveryKeyChangedCommand { get; }

        private async Task ExecuteRecoveryKeyChangedCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(RecoveryKey))
            {
                InvalidRecoveryKey = false;
                return;
            }

            try
            {
                _encryptionProvider.SetSecretKeys(_encryptionProvider.DecodeSecretKeysFromBase64(RecoveryKey.Trim()));
                await _dataManager.TryOpenLocalUserDataBundleAsync(cancellationToken).ConfigureAwait(false);
                InvalidRecoveryKey = false;
                InvalidRecoveryKeyReason = string.Empty;
                _backupCurrentStepIndex = CurrentStepIndex;
                CurrentStepIndex = StepWindowsHello;
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException)
                {
                    InvalidRecoveryKeyReason = Strings.EnterSecretKeyInvalidKeyReasonFileNotFound;
                }
                else
                {
                    InvalidRecoveryKeyReason = Strings.EnterSecretKeyInvalidKeyReasonInvalidSecretKey;
                }

                InvalidRecoveryKey = true;
            }
        }

        #endregion

        #region ContinueWindowsHelloCommand

        internal ActionCommand<object> ContinueWindowsHelloCommand { get; }

        private void ExecuteContinueWindowsHelloCommand(object parameter)
        {
            CurrentStepIndex = StepAllSet;
        }

        #endregion

        #region ContinueAllSetCommand

        internal AsyncActionCommand<object> ContinueAllSetCommand { get; }

        private async Task ExecuteContinueAllSetCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            _settingsProvider.SetSetting(SettingsDefinitions.UseWindowsHello, UseWindowsHello);

            if (UseWindowsHello)
            {
                // if Windows hello is enable, let's persist the secret keys on the machine so the user won't have to enter them
                // each time he opens the app.
                _encryptionProvider.PersistSecretKeysToPasswordVault();
            }

            await _dataManager.LoadOrCreateLocalUserDataBundleAsync(cancellationToken).ConfigureAwait(false);

            _settingsProvider.SetSetting(SettingsDefinitions.FirstStart, false);

            _remoteSynchronizationService.QueueSynchronization();

            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                Frame mainFrame = (Frame)Window.Current.Content;
                mainFrame.Navigate(typeof(MainPage));
            }).ConfigureAwait(false);
        }

        #endregion

        private void Timer_Tick(object sender, object e)
        {
            _timer.Stop();
            Task.Run(async () =>
            {
                try
                {
                    var oldWindowsHelloIsEnabled = WindowsHelloIsEnabled;
                    WindowsHelloIsEnabled = await _windowsHelloAuthProvider.IsWindowsHelloEnabledAsync().ConfigureAwait(false);

                    if (WindowsHelloIsEnabled && oldWindowsHelloIsEnabled != WindowsHelloIsEnabled)
                    {
                        UseWindowsHello = true;
                    }
                }
                finally
                {
                    await TaskHelper.RunOnUIThreadAsync(() =>
                    {
                        _timer.Start();
                    }).ConfigureAwait(false);
                }
            });
        }

        private void PrintManager_PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            PrintTask printTask = args.Request.CreatePrintTask(Strings.SecretKeyPrintTitle, sourceRequested =>
            {
                sourceRequested.SetSource(_printDocumentSource);
            });

            printTask.Completed += PrintTask_Completed;
        }

        private void PrintTask_Completed(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            if (args.Completion == PrintTaskCompletion.Submitted)
            {
                _recoveryKeySaved = true;
                ContinueGenerateSecretKeyCommand.RaiseCanExecuteChanged();
            }
        }

        private void RemoteSynchronizationService_SynchronizationCompleted(object sender, SynchronizationResultEventArgs e)
        {
            CurrentStepIndex = StepEnterSecretKey;
        }

        private void GeneratePrintDocumentPageContent()
        {
            // TODO: Maybe make it a beautiful User Control?
            var stackPanel = new StackPanel();
            stackPanel.Children.Add(new TextBlock
            {
                Foreground = new SolidColorBrush(Colors.Black),
                Text = Strings.SecretKeyPrintTitle,
                Margin = new Thickness(50, 50, 50, 24),
                TextWrapping = TextWrapping.WrapWholeWords
            });
            stackPanel.Children.Add(new TextBlock
            {
                Foreground = new SolidColorBrush(Colors.Black),
                Text = RecoveryKey,
                Margin = new Thickness(75, 0, 50, 50),
                TextWrapping = TextWrapping.WrapWholeWords
            });

            _printDocumentPageContent.Children.Clear();
            _printDocumentPageContent.Children.Add(stackPanel);

            _printDocumentPageContent.RequestedTheme = ElementTheme.Light;
            _printDocumentPageContent.InvalidateMeasure();
            _printDocumentPageContent.UpdateLayout();
        }

        private async Task SignOutFromAllRemoteStorageProviders()
        {
            var tasks = new List<Task>();
            foreach (Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata> provider in RemoteStorageProviders)
            {
                tasks.Add(provider.Value.SignOutAsync());
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
