using PaZword.Api;
using PaZword.Api.Collections;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Models;
using PaZword.Views;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Globalization.Collation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace PaZword.ViewModels
{
    /// <summary>
    /// Provides a single-instanced view model shared between other view models.
    /// </summary>
    [Export(typeof(CommonViewModel))]
    [Shared()]
    public sealed class CommonViewModel : ViewModelBase, IDisposable
    {
        private const string EditingOverlayEvent = "Common.EditingOverlay.Command";
        private const string SearchAccountsAndUpdateGroupsAsyncFaultEvent = "Common.SearchAccount.Fault";

        private const char FavoriteGroup = '\u2605'; // ★ <= icon of the favorite accounts group.

        private readonly IDataManager _dataManager;
        private readonly ILogger _logger;

        private NavigationHelper _navigation;
        private Account _selectedAccount;
        private bool _isEmpty;
        private bool _isEditing;
        private string _searchQuery;
        private string _lastSearchQuery;
        private bool _keepSelectionUnchanged;
        private bool _ignoreSelectionChange;
        private CancellationTokenSource _searchCancellationTokenSource;

        /// <summary>
        /// Gets the current displayed category.
        /// </summary>
        internal Guid CurrentCategoryId { get; private set; }

        /// <summary>
        /// Gets the list of accounts.
        /// </summary>
        internal CollectionViewSource Accounts { get; private set; }

        /// <summary>
        /// Gets the selected account.
        /// </summary>
        internal Account SelectedAccount
        {
            get => _selectedAccount;
            set => SetSelectedAccountAsync(value).Forget();
        }

        /// <summary>
        /// Gets whether there is at least one account in the list of not.
        /// </summary>
        internal bool IsEmpty
        {
            get => _isEmpty;
            private set
            {
                _isEmpty = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the query used to search in the list of accounts.
        /// </summary>
        internal string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value that defines whether the current account is in editing mode.
        /// </summary>
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Raised when a component requires to programmatically select to another category in the left menu.
        /// </summary>
        internal event EventHandler<SelectCategoryInMenuEventArgs> SelectCategoryInMenu;

        /// <summary>
        /// Raised when the user clicked on the overlay in edit mode.
        /// </summary>
        internal event EventHandler EditingOverlayClicked;

        /// <summary>
        /// Raised when the selected account has changed.
        /// </summary>
        internal event EventHandler<SelectAccountEventArgs> SelectedAccountChanged;

        /// <summary>
        /// Raised when the selected account is about to change.
        /// </summary>
        internal event EventHandler PreviewSelectedAccountChanged;

        /// <summary>
        /// Raised when the user wants to delete the selected account.
        /// </summary>
        internal event EventHandler DeleteAccount;

        /// <summary>
        /// Raised when the app needs to discard unsaved changes.
        /// </summary>
        internal event EventHandler DiscardUnsavedChanges;

        [ImportingConstructor]
        public CommonViewModel(
            IDataManager dataManager,
            ILogger logger)
        {
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            _logger = Arguments.NotNull(logger, nameof(logger));

            EditingOverlayClickedCommand = new ActionCommand<object>(_logger, EditingOverlayEvent, ExecuteEditingOverlayClickedCommand, CanExecuteEditingOverlayClickedCommand);

            InitializeAccountGroup();
        }

        public void Dispose()
        {
            _searchCancellationTokenSource?.Dispose();
            _dataManager.Dispose();
        }

        #region EditingOverlayClickedCommand

        public ActionCommand<object> EditingOverlayClickedCommand { get; }

        private bool CanExecuteEditingOverlayClickedCommand(object parameter)
        {
            return IsEditing;
        }

        private void ExecuteEditingOverlayClickedCommand(object parameter)
        {
            EditingOverlayClicked?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        /// <summary>
        /// Initialize the view model.
        /// </summary>
        /// <param name="accountContentFrame">The <see cref="Frame"/> that must be used to manage the navigation.</param>
        internal void Initialize(Frame accountContentFrame)
        {
            _navigation = new NavigationHelper(accountContentFrame);
        }

        /// <summary>
        /// Raises an event to indicates the program should programmaticaly select to another category in the left menu.
        /// </summary>
        /// <param name="category"></param>
        internal void RaiseSelectCategoryInMenu(Category category)
        {
            SelectCategoryInMenu?.Invoke(this, new SelectCategoryInMenuEventArgs(category));
        }

        /// <summary>
        /// Raises an event to indicates the users wants to delete the selected account. Generally this happens through a user command from the category page.
        /// </summary>
        internal void RaiseDeleteAccount()
        {
            DeleteAccount?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises an event to indicates we want to discard unsaved changes in the current editing account (if any).
        /// </summary>
        internal void RaiseDiscardUnsavedChanges()
        {
            DiscardUnsavedChanges?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Defines what account should be selected in the list of account, and navigates to it.
        /// </summary>
        /// <param name="account">The account to select.</param>
        /// <param name="shouldSwitchToEditMode">Defines whether the selected account should enters in editing mode.</param>
        internal async Task SetSelectedAccountAsync(Account account, bool shouldSwitchToEditMode = false)
        {
            lock (_dataManager)
            {
                if (_ignoreSelectionChange
                    || _keepSelectionUnchanged
                    || SelectedAccount == account)
                {
                    return;
                }
            }

            var selectionChanged = false;

            PreviewSelectedAccountChanged?.Invoke(this, EventArgs.Empty);

            if (account != null && account != SelectedAccount)
            {
                await _navigation.NavigateToPageAsync<AccountPage>(
                    alwaysNavigate: true,
                    parameter: new AccountPageNavigationParameters(account, shouldSwitchToEditMode))
                    .ConfigureAwait(false);
                selectionChanged = true;
            }

            _selectedAccount = account;
            RaisePropertyChanged(nameof(SelectedAccount));

            if (selectionChanged)
            {
                SelectedAccountChanged?.Invoke(this, new SelectAccountEventArgs(account));
            }
        }

        /// <summary>
        /// Change the category for which accounts should be displayed. This doesn't actually change the selected category in the menu.
        /// </summary>
        /// <param name="categoryToShow">The category for which the accounts should be displayed.</param>
        internal void ChangeCategory(Category categoryToShow, bool refreshGroupsAndAccounts)
        {
            CurrentCategoryId = categoryToShow.Id;
            if (refreshGroupsAndAccounts)
            {
                SearchAccountsAndUpdateGroupsAsync(string.Empty, keepSelectionUnchanged: true).ForgetSafely();
            }
        }

        /// <summary>
        /// Clears the search bar and displays all the accounts for the current category.
        /// </summary>
        /// <param name="keepSelectionUnchanged">Defines whether we should try to keep the current account selection.</param>
        internal async Task ClearSearchAsync(bool keepSelectionUnchanged)
        {
            SearchQuery = string.Empty;
            await SearchAsync(SearchQuery, keepSelectionUnchanged).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a search with the given query.
        /// </summary>
        /// <param name="searchQuery">The query to search.</param>
        /// <param name="keepSelectionUnchanged">Defines whether the selection must stay unchanged if possible.</param>
        internal async Task SearchAsync(string searchQuery, bool keepSelectionUnchanged)
        {
            await SearchAccountsAndUpdateGroupsAsync(searchQuery, keepSelectionUnchanged).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a search to update the list of displayed groups and accounts.
        /// </summary>
        /// <param name="keepSelectionUnchanged">Defines whether the selection must stay unchanged if possible.</param>
        /// <param name="accountToUpdate">Defines an account that should explicitly be replaced in the list of accounts.</param>
        internal async Task RefreshGroupsAndAccountsListAsync(bool keepSelectionUnchanged, Account accountToUpdate = null)
        {
            await SearchAccountsAndUpdateGroupsAsync(_lastSearchQuery, keepSelectionUnchanged, accountToUpdate).ConfigureAwait(false);
        }

        private void InitializeAccountGroup()
        {
            TaskHelper.ThrowIfNotOnUIThread();

            var characterGroupings = new CharacterGroupings();

            List<AccountGroup> groups = characterGroupings
                .Where(x => !string.IsNullOrEmpty(x.Label))
                .Select(x => new AccountGroup(x.Label))
                .ToList();

            groups.Insert(0, new AccountGroup(FavoriteGroup.ToString()));

            Accounts = new CollectionViewSource
            {
                Source = new ConcurrentObservableCollection<AccountGroup>(groups),
                IsSourceGrouped = true,
                ItemsPath = new PropertyPath(nameof(Accounts))
            };
        }

        /// <summary>
        /// Searches the accounts matching the given <paramref name="searchQuery"/>
        /// and update the displayed list of account to only show the results from the search
        /// without reassigning the entire collection.
        /// </summary>
        /// <param name="searchQuery">The query to search.</param>
        /// <param name="keepSelectionUnchanged">Defines whether the selection must stay unchanged if possible.</param>
        private async Task SearchAccountsAndUpdateGroupsAsync(string searchQuery, bool keepSelectionUnchanged = false, Account accountToUpdate = null)
        {
            CancellationToken cancellationToken;
            lock (_dataManager)
            {
                if (_searchCancellationTokenSource != null)
                {
                    _searchCancellationTokenSource.Cancel();
                    _searchCancellationTokenSource.Dispose();
                }

                _searchCancellationTokenSource = new CancellationTokenSource();
                cancellationToken = _searchCancellationTokenSource.Token;
            }

            try
            {
                // Search all the accounts that match the search query.
                ConcurrentObservableCollection<Account> accountsToDisplay = await _dataManager.SearchAsync(CurrentCategoryId, searchQuery, cancellationToken).ConfigureAwait(false);

                // Update the list of displayed account.
                // We'll do it by removing/adding each item one after the other, instead of simply assigning Accounts
                // to a new collection.
                // The reason why we do this is to have a smooth animation when accounts are getting removed/added to the list.
                // It slows down a bit the UI refresh but is more eye catching.

                ConcurrentObservableCollection<AccountGroup> groups = null;
                await TaskHelper.RunOnUIThreadAsync(() =>
                {
                    groups = (ConcurrentObservableCollection<AccountGroup>)Accounts.Source;
                }).ConfigureAwait(false);

                AccountGroup favoriteGroup = groups.First();
                var characterGroupings = new CharacterGroupings();

                cancellationToken.ThrowIfCancellationRequested();

                // Remove displayed accounts that aren't part of the new search result.
                var accountRemoved = false;
                for (int i = 0; i < groups.Count; i++)
                {
                    AccountGroup group = groups[i];
                    for (int j = 0; j < group.Accounts.Count; j++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Account account = group.Accounts[j];
                        if (!accountsToDisplay.Any(a => a == account))
                        {
                            group.Accounts.Remove(account);
                            accountRemoved = true;
                            j--;
                        }
                    }
                }

                if (accountRemoved)
                {
                    EnsureGroupsAndAccountProcessedChanges(groups);
                }

                _keepSelectionUnchanged = keepSelectionUnchanged;

                // Add the new items that aren't already displayed in the UI.
                var anyAccountOrGroupChanged = false;
                for (int i = 0; i < accountsToDisplay.Count; i++)
                {
                    Account account = accountsToDisplay[i];
                    AccountGroup existingGroupHostingAccount = groups.SingleOrDefault(g => g.Accounts.Any(a => a == account));
                    AccountGroup newGroup = null;

                    cancellationToken.ThrowIfCancellationRequested();

                    if (existingGroupHostingAccount == null)
                    {
                        // The item isn't in any group, let's add it.
                        if (account.IsFavorite)
                        {
                            newGroup = favoriteGroup;
                        }
                        else
                        {
                            newGroup = groups.First(group
                                => IsAccountBelongingToGroup(characterGroupings, account, group));
                        }

                        newGroup.Accounts.Add(account);
                        anyAccountOrGroupChanged = true;
                    }
                    else
                    {
                        // The item already exists in a group.
                        // Let's check whether we should move the account to another group (in case if it has been renamed for example).

                        if (account.IsFavorite)
                        {
                            if (existingGroupHostingAccount != favoriteGroup)
                            {
                                // It seems like the account became "Favorite", so we should move it to the favorite group.
                                existingGroupHostingAccount.Accounts.Remove(account); // this may makes SelectedAccount = null;
                                favoriteGroup.Accounts.Add(account);
                                newGroup = favoriteGroup;
                                anyAccountOrGroupChanged = true;
                            }
                            // else, the item is in the right group, don't move it.
                        }
                        else if (!IsAccountBelongingToGroup(characterGroupings, account, existingGroupHostingAccount))
                        {
                            // This account doesn't belong to this group. Let's move it to another one.
                            existingGroupHostingAccount.Accounts.Remove(account); // this may makes SelectedAccount = null;
                            newGroup = groups.First(group
                                => IsAccountBelongingToGroup(characterGroupings, account, group));
                            newGroup.Accounts.Add(account);
                            anyAccountOrGroupChanged = true;
                        }
                        else if (account == accountToUpdate)
                        {
                            // The item doesn't need to be moved. But maybe it's the one we should explicitly update.
                            // Most of the time, the 'accountToUpdate' is the same reference object than 'account'
                            // but replacing it allows to trigger some events in the ConcurrentObservableCollection that will
                            // update the UI of the item in the list. This is useful when the title or subtitle or icon changed.
                            _ignoreSelectionChange = true;
                            existingGroupHostingAccount.Accounts[existingGroupHostingAccount.Accounts.IndexOf(account)] = accountToUpdate;
                            existingGroupHostingAccount.Accounts.WaitPendingChangesGetProcessedIfNotOnUIThread();
                            _ignoreSelectionChange = false;
                        }
                    }

                    if (newGroup != null)
                    {
                        // The account has been added or moved to another group, but it may not be at the right index in the group. 
                        // Let's move it at the right index. Since the list is supposed to be already sorted, except
                        // for the item we just added, no need to resort everything.
                        var j = 0;
                        var itemMoved = false;
                        while (j < newGroup.Accounts.Count && !itemMoved)
                        {
                            Account accountToCompare = newGroup.Accounts[j];
                            int comparisonResult = string.CompareOrdinal(accountToCompare.Title, account.Title);
                            if (comparisonResult > 0)
                            {
                                newGroup.Accounts.Move(newGroup.Accounts.Count - 1, j);
                                itemMoved = true;
                            }

                            j++;
                        }

                        newGroup.Accounts.WaitPendingChangesGetProcessedIfNotOnUIThread();
                    }
                }

                if (anyAccountOrGroupChanged)
                {
                    EnsureGroupsAndAccountProcessedChanges(groups);
                }

                IsEmpty = accountsToDisplay.Count == 0;

                lock (_dataManager)
                {
                    _lastSearchQuery = searchQuery;
                    RaisePropertyChanged(nameof(SelectedAccount));
                    _keepSelectionUnchanged = false;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogFault(SearchAccountsAndUpdateGroupsAsyncFaultEvent, "Unable to search and/or update the list of displayed account.", ex);
            }
            finally
            {
                lock (_dataManager)
                {
                    _keepSelectionUnchanged = false;
                }
            }
        }

        private static void EnsureGroupsAndAccountProcessedChanges(ConcurrentObservableCollection<AccountGroup> groups)
        {
            groups.WaitPendingChangesGetProcessedIfNotOnUIThread();
            for (int i = 0; i < groups.Count; i++)
            {
                AccountGroup group = groups[i];
                group.Accounts.WaitPendingChangesGetProcessedIfNotOnUIThread();
            }
        }

        private static bool IsAccountBelongingToGroup(CharacterGroupings characterGroupings, Account account, AccountGroup accountGroup)
        {
            return string.Equals(accountGroup.GroupName, characterGroupings.Lookup(account.Title), StringComparison.Ordinal);
        }
    }
}
