using PaZword.Api;
using PaZword.Api.Collections;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.Services;
using PaZword.Api.UI;
using PaZword.Api.ViewModels.Data;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Localization;
using PaZword.Models;
using PaZword.ViewModels.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace PaZword.ViewModels
{
    /// <summary>
    /// Interaction logic for <see cref="AccountPage"/>
    /// </summary>
    [Export(typeof(AccountPageViewModel))]
    public sealed class AccountPageViewModel : ViewModelBase
    {
        private const string HeaderTitleUpdatedEvent = "Account.HeaderTitle.Changed";
        private const string EditAccountEvent = "Account.Edit.Command";
        private const string DeleteAccountEvent = "Account.Delete.Command";
        private const string AddAccountDataEvent = "Account.AddAccountData.Command";
        private const string SaveEvent = "Account.Save.Command";
        private const string DiscardChangesEvent = "Account.DiscardChanges.Command";
        private const string IconAutoDetectEvent = "Account.IconAutoDetect.Command";
        private const string IconDefaultEvent = "Account.IconDefault.Command";
        private const string IconSelectFileEvent = "Account.IconSelectFile.Command";

        private const int MaximumSubtitleLength = 64;

        private readonly IOrderedEnumerable<Lazy<IAccountDataProvider, AccountDataProviderMetadata>> _orderedAccountDataProviders;
        private readonly ILogger _logger;
        private readonly ISerializationProvider _serializationProvider;
        private readonly IDataManager _dataManager;
        private readonly IIconService _iconService;
        private readonly IRecurrentTaskService _recurrentTaskService;
        private readonly CommonViewModel _commonViewModel;
        private readonly object _lock = new object();

        private bool _isAskingUserAboutUnsavedChanges;
        private bool _isEditing;
        private bool _isLoadingIcon;
        private Account _account;
        private Account _accountEdit;
        private int _categoryIndex = -1;
        private CancellationTokenSource _findIconOnlineCancellationTokenSource;

        internal IWindowManager WindowManager { get; set; }

        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal AccountPageStrings Strings => LanguageManager.Instance.AccountPage;

        /// <summary>
        /// Gets the <see cref="MenuFlyout"/> displayed in the user interface that allows the user to add a <see cref="AccountData"/>.
        /// </summary>
        internal MenuFlyout AddAccountDataContextMenu { get; private set; }

        /// <summary>
        /// Gets the formatted text that corresponds to the LastModification resource.
        /// </summary>
        internal string FormattedLastModificationString => Strings.GetFormattedLastModification(Account.LastModificationDate.ToShortDateString(), Account.LastModificationDate.ToShortTimeString());

        /// <summary>
        /// Gets or sets the current account.
        /// </summary>
        internal Account Account
        {
            get => _account;
            set
            {
                _account = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the current account in editing mode.
        /// </summary>
        internal Account AccountEditMode
        {
            get => _accountEdit;
            set
            {
                if (_accountEdit != null)
                {
                    _accountEdit.Data.CollectionChanged -= AccountEditMode_Data_CollectionChanged;
                }

                _accountEdit = value;

                if (_accountEdit != null)
                {
                    _accountEdit.Data.CollectionChanged += AccountEditMode_Data_CollectionChanged;
                }

                RaisePropertyChanged(string.Empty);
            }
        }

        /// <summary>
        /// Gets or sets a value that defines whether the current account is in editing mode.
        /// </summary>
        public bool IsEditing
        {
            get => _isEditing;
            private set
            {
                _isEditing = value;
                _commonViewModel.IsEditing = value;
                if (value)
                {
                    _commonViewModel.EditingOverlayClicked += CommonViewModel_EditingOverlayClicked;
                }
                else
                {
                    _commonViewModel.EditingOverlayClicked -= CommonViewModel_EditingOverlayClicked;
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether the account is a favorite or not.
        /// </summary>
        internal bool IsFavorite
        {
            get => Account.IsFavorite;
            set
            {
                Account.IsFavorite = value;
                _dataManager.UpdateAccountAsync(Account, Account, CancellationToken.None)
                    .ContinueWith(_ =>
                    {
                        // Refresh the UI to move in/out the account from the Favorite group.
                        _commonViewModel.RefreshGroupsAndAccountsListAsync(keepSelectionUnchanged: true).ForgetSafely();

                        // Save the data.
                        _dataManager.SaveLocalUserDataBundleAsync(synchronize: true, CancellationToken.None).ForgetSafely();
                    }, TaskScheduler.Default).ForgetSafely();
            }
        }

        /// <summary>
        /// Gets the account icon.
        /// </summary>
        internal string Base64Icon
        {
            get
            {
                if (IsEditing)
                {
                    return AccountEditMode.Base64Icon;
                }
                else
                {
                    return Account.Base64Icon;
                }
            }
        }

        /// <summary>
        /// Gets whether the icon of the account is loaded.
        /// </summary>
        internal bool IsLoadingIcon
        {
            get => _isLoadingIcon;
            private set
            {
                _isLoadingIcon = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the list of categories.
        /// </summary>
        internal ConcurrentObservableCollection<Category> Categories => GetCategories();

        /// <summary>
        /// Gets or sets the current category Id in editing mode as an <see cref="int"/> value.
        /// </summary>
        public int CategoryIndex
        {
            get => _categoryIndex;
            set
            {
                if (value != -1 && _categoryIndex != value)
                {
                    _categoryIndex = value;
                    if (AccountEditMode != null)
                    {
                        AccountEditMode.CategoryID = _dataManager.Categories[value].Id;
                        RaisePropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets whether there is at least one account data in the list of not.
        /// </summary>
        internal bool IsEmpty => AccountEditMode.Data.Count == 0;

        /// <summary>
        /// Gets a bridge between <see cref="AccountPageViewModel"/> and the <see cref="IAccountDataViewModel"/> to enable
        /// some interaction between both, in particular to know when <see cref="IsEditing"/> mode changes.
        /// </summary>
        public AccountPageToAccountDataViewModelBridge AccountPageToAccountDataViewModelBridge { get; }

        /// <summary>
        /// Raised to notify that an <see cref="AccountData"/> has been added to the <see cref="Account"/> in edit mode.
        /// </summary>
        internal event EventHandler<EventArgs> AccountDataAdded;

        [ImportingConstructor]
        public AccountPageViewModel(
            ILogger logger,
            ISerializationProvider serializationProvider,
            IDataManager dataManager,
            IIconService iconService,
            IWindowManager windowManager,
            IRecurrentTaskService recurrentTaskService,
            CommonViewModel commonViewModel,
            [ImportMany] IEnumerable<Lazy<IAccountDataProvider, AccountDataProviderMetadata>> accountDataProviders)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            _iconService = Arguments.NotNull(iconService, nameof(iconService));
            _commonViewModel = Arguments.NotNull(commonViewModel, nameof(commonViewModel));
            WindowManager = Arguments.NotNull(windowManager, nameof(windowManager));
            _recurrentTaskService = Arguments.NotNull(recurrentTaskService, nameof(recurrentTaskService));
            _orderedAccountDataProviders = Arguments.NotNull(accountDataProviders, nameof(accountDataProviders)).OrderBy(l => l.Metadata.Order);

            AccountPageToAccountDataViewModelBridge = new AccountPageToAccountDataViewModelBridge(this);

            HeaderTitleUpdatedCommand = new ActionCommand<object>(_logger, HeaderTitleUpdatedEvent, ExecuteHeaderTitleUpdatedCommand);
            EditAccountCommand = new AsyncActionCommand<object>(_logger, EditAccountEvent, ExecuteEditAccountCommandAsync);
            DeleteAccountCommand = new AsyncActionCommand<object>(_logger, DeleteAccountEvent, ExecuteDeleteAccountCommandAsync);
            AddAccountDataCommand = new AsyncActionCommand<IAccountDataProvider>(_logger, AddAccountDataEvent, ExecuteAddAccountDataCommandAsync, CanExecuteAddAccountDataCommand);
            SaveChangesCommand = new AsyncActionCommand<object>(_logger, SaveEvent, ExecuteSaveChangesCommandAsync);
            DiscardChangesCommand = new ActionCommand<object>(_logger, DiscardChangesEvent, ExecuteDiscardChangesCommand);
            IconAutoDetectCommand = new ActionCommand<object>(_logger, IconAutoDetectEvent, ExecuteIconAutoDetectCommand);
            IconDefaultCommand = new ActionCommand<object>(_logger, IconDefaultEvent, ExecuteIconDefaultCommand);
            IconSelectFileCommand = new AsyncActionCommand<object>(_logger, IconSelectFileEvent, ExecuteIconSelectFileCommandAsync);

            InitializeAddAccountDataContextMenu();

            _commonViewModel.DeleteAccount += CommonViewModel_DeleteAccount;
            _commonViewModel.PreviewSelectedAccountChanged += CommonViewModel_PreviewSelectedAccountChanged;
            _commonViewModel.DiscardUnsavedChanges += CommonViewModel_DiscardChanges;
        }

        internal async Task InitializeAsync(AccountPageNavigationParameters args)
        {
            Arguments.NotNull(args, nameof(args));

            Account = args.Account;
            AccountEditMode = _serializationProvider.CloneObject(Account);
            Category accountCategory = await _dataManager.GetCategoryAsync(Account.Id, CancellationToken.None).ConfigureAwait(false);
            CategoryIndex = _dataManager.Categories.IndexOf(accountCategory);

            if (args.ShouldSwitchToEditMode)
            {
                EditAccountCommand.Execute(null);
            }
        }

        private void InitializeAddAccountDataContextMenu()
        {
            var orderedAccountDataProviders = _orderedAccountDataProviders.Select(l => l.Value);
            var menu = new MenuFlyout();
            menu.Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom;

            foreach (IAccountDataProvider accountDataProvider in orderedAccountDataProviders)
            {
                var menuItem = new MenuFlyoutItem
                {
                    DataContext = this,
                    CommandParameter = accountDataProvider
                };

                menuItem.SetBinding(MenuFlyoutItem.TextProperty, new Binding
                {
                    Path = new PropertyPath($"{nameof(MenuFlyoutItem.CommandParameter)}.{nameof(IAccountDataProvider.DisplayName)}"),
                    RelativeSource = new RelativeSource
                    {
                        Mode = RelativeSourceMode.Self
                    },
                    Mode = BindingMode.OneWay
                });

                menuItem.SetBinding(MenuFlyoutItem.CommandProperty, new Binding
                {
                    Path = new PropertyPath(nameof(AddAccountDataCommand)),
                    Mode = BindingMode.OneTime
                });

                menu.Items.Add(menuItem);
            }

            AddAccountDataContextMenu = menu;
        }

        #region HeaderTitleUpdatedCommand

        internal ActionCommand<object> HeaderTitleUpdatedCommand { get; }

        private void ExecuteHeaderTitleUpdatedCommand(object parameter)
        {
            if (AccountEditMode.IconMode == IconMode.Automatic)
            {
                FindIcon();
            }
        }

        #endregion

        #region EditAccountCommand

        internal AsyncActionCommand<object> EditAccountCommand { get; }

        private async Task ExecuteEditAccountCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            Category category = await _dataManager.GetCategoryAsync(Account.CategoryID, cancellationToken).ConfigureAwait(false);

            IsEditing = true;
            AccountEditMode = _serializationProvider.CloneObject(Account);
            CategoryIndex = _dataManager.Categories.IndexOf(category);
            RaisePropertyChanged(nameof(Base64Icon));

            if (AccountEditMode.IconMode == IconMode.Automatic
                && string.IsNullOrEmpty(AccountEditMode.Base64Icon))
            {
                FindIcon();
            }
        }

        #endregion

        #region DeleteAccountCommand

        internal AsyncActionCommand<object> DeleteAccountCommand { get; }

        private async Task ExecuteDeleteAccountCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            var dialogResult = await WindowManager.ShowMessageDialogAsync(
                message: Strings.DeleteConfirmationDescription,
                closeButtonText: Strings.No,
                primaryButtonText: Strings.Yes,
                title: Strings.GetFormattedDeleteConfirmationTitle(Account.Title)).ConfigureAwait(false);

            if (dialogResult == ContentDialogResult.Primary)
            {
                lock (_lock)
                {
                    // Stop the icon automatic detection, if any.
                    _findIconOnlineCancellationTokenSource?.Cancel();
                    _findIconOnlineCancellationTokenSource?.Dispose();
                    _findIconOnlineCancellationTokenSource = null;
                }

                for (int i = 0; i < AccountPageToAccountDataViewModelBridge.ViewModels.Count; i++)
                {
                    await AccountPageToAccountDataViewModelBridge.ViewModels[i].UnloadingAsync().ConfigureAwait(false);
                    await AccountPageToAccountDataViewModelBridge.ViewModels[i].DeleteAsync(cancellationToken).ConfigureAwait(false);
                }

                await _dataManager.DeleteAccountAsync(Account, cancellationToken).ConfigureAwait(false);

                await _commonViewModel.RefreshGroupsAndAccountsListAsync(keepSelectionUnchanged: false).ConfigureAwait(false);

                _dataManager.SaveLocalUserDataBundleAsync(synchronize: true, cancellationToken).ForgetSafely();
            }
        }

        #endregion

        #region AddAccountDataCommand

        public AsyncActionCommand<IAccountDataProvider> AddAccountDataCommand { get; }

        private bool CanExecuteAddAccountDataCommand(IAccountDataProvider parameter)
        {
            return IsEditing && parameter.CanCreateAccountData(AccountEditMode);
        }

        private async Task ExecuteAddAccountDataCommandAsync(IAccountDataProvider provider, CancellationToken cancellationToken)
        {
            // Generate a unique ID.
            Guid id = await _dataManager.GenerateUniqueIdAsync(cancellationToken).ConfigureAwait(false);

            // Add the item and be sure to process the change before raising the event.
            AccountEditMode.Data.Add(provider.CreateAccountData(id));
            AccountEditMode.Data.WaitPendingChangesGetProcessedIfNotOnUIThread();

            // Raise this event to notify the view that it should scroll to this item.
            AccountDataAdded?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region SaveChangesCommand

        public AsyncActionCommand<object> SaveChangesCommand { get; }

        private async Task ExecuteSaveChangesCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            // Check whether the account still has a title.
            if (string.IsNullOrWhiteSpace(AccountEditMode.Title))
            {
                await WindowManager.ShowMessageDialogAsync(
                    message: Strings.TitleIsEmptyDescription,
                    closeButtonText: Strings.No,
                    title: Strings.TitleIsEmptyTitle).ConfigureAwait(false);

                return;
            }

            // Copy the list of view models we will work with in case they changes because of the user action.
            IReadOnlyList<IAccountDataViewModel> viewModels = AccountPageToAccountDataViewModelBridge.ViewModels.ToList();
            IReadOnlyList<IAccountDataViewModel> viewModelsForDeletion = AccountPageToAccountDataViewModelBridge.ViewModelsForDeletion.ToList();

            // Asks to each account data if they're correctly filled.
            for (int i = 0; i < viewModels.Count; i++)
            {
                if (!await viewModels[i].ValidateChangesAsync(cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            lock (_lock)
            {
                // Stop the icon automatic detection, if any.
                _findIconOnlineCancellationTokenSource?.Cancel();
                _findIconOnlineCancellationTokenSource?.Dispose();
                _findIconOnlineCancellationTokenSource = null;
            }

            // Detects if the user changed the category of the account.
            var categoryChanged = AccountEditMode.CategoryID != Account.CategoryID;

            // The "new" account we will generate/keep corresponds to the AccountEditMode one.
            var newAccount = AccountEditMode;

            // Creates a copy of the object, so it's not the same instance but has the same values.
            // This allows us to do some ultimate changes without updating the UI or being impacted by user's behavior.
            newAccount = _serializationProvider.CloneObject(newAccount);

            // Proceed to delete the data of the account data that have been removed by the user.
            for (int i = 0; i < viewModelsForDeletion.Count; i++)
            {
                await viewModelsForDeletion[i].UnloadingAsync().ConfigureAwait(false);
                await viewModelsForDeletion[i].DeleteAsync(cancellationToken).ConfigureAwait(false);
            }

            // The data in account data in the view models are likely different from the ones in the account in the current view model.
            // So we replace the ones of the current view models by the ones in the UI.
            newAccount.Data.Clear();
            for (int i = 0; i < viewModels.Count; i++)
            {
                IAccountDataViewModel viewModel = viewModels[i];
                await viewModel.SaveAsync(cancellationToken).ConfigureAwait(false);
                newAccount.Data.Add(_serializationProvider.CloneObject(viewModel.DataEditMode));
            }

            // We update the sub title of the account that will be displayed in the list of accounts.
            newAccount.AccountSubtitle = GenerateAccountSubtitle(viewModels);

            // did the user actually change something?
            bool anythingChanged = !newAccount.ExactEquals(Account);

            if (anythingChanged)
            {
                // Update the date of last modification.
                newAccount.LastModificationDate = DateTime.Now;

                // Replaces the old account by the new one we made.
                await _dataManager.UpdateAccountAsync(Account, newAccount, cancellationToken).ConfigureAwait(false);
                _dataManager.SaveLocalUserDataBundleAsync(synchronize: true, cancellationToken).ForgetSafely();

                AccountPageToAccountDataViewModelBridge.ClearViewModelsForDeletion();

                Account = newAccount;
                IsEditing = false;
                AccountEditMode = newAccount;

                if (categoryChanged)
                {
                    // Changing the actual selected category in the menu will trigger a displayed account list refresh.
                    Category category = await _dataManager.GetCategoryAsync(newAccount.CategoryID, cancellationToken).ConfigureAwait(false);
                    _commonViewModel.RaiseSelectCategoryInMenu(category);
                }
                else
                {
                    await _commonViewModel.RefreshGroupsAndAccountsListAsync(keepSelectionUnchanged: true, accountToUpdate: Account).ConfigureAwait(false);
                }

                // Check if the account has been pwned.
                _recurrentTaskService.RunTaskExplicitly(Constants.PwnedRecurrentTask);
            }
            else
            {
                DiscardChangesCommand.Execute(null);
            }
        }

        #endregion

        #region DiscardChangesCommand

        internal ActionCommand<object> DiscardChangesCommand { get; }

        private void ExecuteDiscardChangesCommand(object parameter)
        {
            lock (_lock)
            {
                _findIconOnlineCancellationTokenSource?.Cancel();
                _findIconOnlineCancellationTokenSource?.Dispose();
                _findIconOnlineCancellationTokenSource = null;
            }

            if (IsEditing)
            {
                IsEditing = false;
                AccountPageToAccountDataViewModelBridge.ClearViewModelsForDeletion();
                AccountEditMode = _serializationProvider.CloneObject(Account);
            }
        }

        #endregion

        #region IconAutoDetect

        internal ActionCommand<object> IconAutoDetectCommand { get; }

        private void ExecuteIconAutoDetectCommand(object parameter)
        {
            FindIcon();
        }

        #endregion

        #region IconSelectFile

        internal AsyncActionCommand<object> IconSelectFileCommand { get; }

        private async Task ExecuteIconSelectFileCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                _findIconOnlineCancellationTokenSource?.Cancel();
                _findIconOnlineCancellationTokenSource?.Dispose();
                _findIconOnlineCancellationTokenSource = new CancellationTokenSource();
            }

            string base64Icon = await _iconService.PickUpIconFromLocalFileAsync(_findIconOnlineCancellationTokenSource.Token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(base64Icon))
            {
                AccountEditMode.Base64Icon = base64Icon;
                AccountEditMode.IconMode = IconMode.Browse;
                RaisePropertyChanged(nameof(Base64Icon));
            }
        }

        #endregion

        #region IconDefault

        internal ActionCommand<object> IconDefaultCommand { get; }

        private void ExecuteIconDefaultCommand(object parameter)
        {
            lock (_lock)
            {
                _findIconOnlineCancellationTokenSource?.Cancel();
                _findIconOnlineCancellationTokenSource?.Dispose();
                _findIconOnlineCancellationTokenSource = null;
            }

            AccountEditMode.Base64Icon = string.Empty;
            AccountEditMode.IconMode = IconMode.DefaultIcon;
            RaisePropertyChanged(nameof(Base64Icon));
        }

        #endregion

        private void AccountEditMode_Data_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(IsEmpty));
        }

        private void CommonViewModel_PreviewSelectedAccountChanged(object sender, EventArgs e)
        {
            _commonViewModel.DeleteAccount -= CommonViewModel_DeleteAccount;
            _commonViewModel.DiscardUnsavedChanges -= CommonViewModel_DiscardChanges;
            _commonViewModel.PreviewSelectedAccountChanged -= CommonViewModel_PreviewSelectedAccountChanged;
        }

        private void CommonViewModel_EditingOverlayClicked(object sender, EventArgs e)
        {
            EditingOverlayClickedAsync().ForgetSafely();
        }

        private void CommonViewModel_DeleteAccount(object sender, EventArgs e)
        {
            DeleteAccountCommand.Execute(null);
        }

        private void CommonViewModel_DiscardChanges(object sender, EventArgs e)
        {
            DiscardChangesCommand.Execute(null);
        }

        /// <summary>
        /// In Edit mode, asks the user whether he wants to save of not the changes when clicking on the dark overlay
        /// that is over the category and account list, on the left of the UI.
        /// </summary>
        private async Task EditingOverlayClickedAsync()
        {
            lock (_lock)
            {
                if (_isAskingUserAboutUnsavedChanges)
                {
                    return;
                }

                _isAskingUserAboutUnsavedChanges = true;
            }

            var dialogResult = await WindowManager.ShowMessageDialogAsync(
                message: Strings.DiscardUnsavedChangesConfirmationDescription,
                closeButtonText: Strings.Cancel,
                primaryButtonText: Strings.SaveChanges,
                secondaryButtonText: Strings.DiscardChanges,
                title: Strings.DiscardUnsavedChangesConfirmationTitle).ConfigureAwait(false);

            if (dialogResult == ContentDialogResult.Primary)
            {
                SaveChangesCommand.Execute(null);
                SaveChangesCommand.WaitRunToCompletion();
            }
            else if (dialogResult == ContentDialogResult.Secondary)
            {
                DiscardChangesCommand.Execute(null);
            }

            lock (_lock)
            {
                _isAskingUserAboutUnsavedChanges = false;
            }

            // TODO: Detect when there are actual unsaved changes, instead of asking even if the user didn't change anything.
        }

        /// <summary>
        /// Gets the list of <see cref="Category"/>.
        /// </summary>
        /// <returns>A list of <see cref="Category"/></returns>
        private ConcurrentObservableCollection<Category> GetCategories()
        {
            var result = _serializationProvider.CloneObject(_dataManager.Categories);
            result[0].Name = Strings.CategoryUncategorized;
            return result;
        }

        /// <summary>
        /// Generates the sub title of the account. It will be displayed in the list of accounts, under the account name.
        /// </summary>
        /// <param name="viewModels">The account data.</param>
        /// <returns>Returns the generated sub title.</returns>
        private string GenerateAccountSubtitle(IReadOnlyList<IAccountDataViewModel> viewModels)
        {
            var subtitle = string.Empty;

            if (viewModels.Count > 0)
            {
                List<Lazy<IAccountDataProvider, AccountDataProviderMetadata>> accountProviders = _orderedAccountDataProviders.ToList();
                var i = 0;

                while (i < accountProviders.Count && string.IsNullOrEmpty(subtitle))
                {
                    IAccountDataViewModel viewModel = viewModels.FirstOrDefault(vm
                        => vm.DataEditMode.GetType() == accountProviders[i].Metadata.AccountDataType);

                    if (viewModel != null)
                    {
                        subtitle = viewModel.GenerateSubtitle();
                    }

                    i++;
                }
            }

            if (string.IsNullOrEmpty(subtitle))
            {
                var isUri = Uri.TryCreate(AccountEditMode.Url, UriKind.Absolute, out Uri uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp
                        || uriResult.Scheme == Uri.UriSchemeHttps
                        || uriResult.Scheme == Uri.UriSchemeFtp
                        || uriResult.Scheme == Uri.UriSchemeMailto);
                if (isUri)
                {
                    subtitle = uriResult.Host;
                }
            }

            if (subtitle.Length > MaximumSubtitleLength)
            {
                subtitle = subtitle.Substring(0, MaximumSubtitleLength) + "...";
            }

            return subtitle;
        }

        /// <summary>
        /// Try to find the best icon that matches with the current account.
        /// </summary>
        private void FindIcon()
        {
            lock (_lock)
            {
                _findIconOnlineCancellationTokenSource?.Cancel();
                _findIconOnlineCancellationTokenSource?.Dispose();
                _findIconOnlineCancellationTokenSource = new CancellationTokenSource();
            }

            Task.Run(async () =>
            {
                TaskHelper.ThrowIfOnUIThread();

                if (!CoreHelper.IsInternetAccess())
                {
                    RaisePropertyChanged(nameof(Base64Icon));
                    return;
                }

                try
                {
                    IsLoadingIcon = true;

                    string base64Icon;
                    if (IsEditing)
                    {
                        base64Icon = await _iconService.ResolveIconOnlineAsync(AccountEditMode.Title, AccountEditMode.Url, _findIconOnlineCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        base64Icon = await _iconService.ResolveIconOnlineAsync(Account.Title, Account.Url, _findIconOnlineCancellationTokenSource.Token).ConfigureAwait(false);
                    }

                    _findIconOnlineCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // The status might changed during the process to find an icon.
                    if (IsEditing && !string.Equals(base64Icon, AccountEditMode.Base64Icon, StringComparison.Ordinal))
                    {
                        AccountEditMode.Base64Icon = base64Icon;
                        AccountEditMode.IconMode = IconMode.Automatic;
                        RaisePropertyChanged(nameof(Base64Icon));
                    }
                    else if (!string.Equals(base64Icon, Account.Base64Icon, StringComparison.Ordinal))
                    {
                        Account.Base64Icon = base64Icon;
                        AccountEditMode.IconMode = IconMode.Automatic;
                        RaisePropertyChanged(nameof(Base64Icon));
                    }
                }
                finally
                {
                    IsLoadingIcon = false;
                }
            });
        }
    }
}
