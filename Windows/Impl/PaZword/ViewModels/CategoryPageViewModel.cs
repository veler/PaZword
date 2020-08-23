using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.UI;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Localization;
using PaZword.Views;
using PaZword.Views.Dialog;
using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace PaZword.ViewModels
{
    /// <summary>
    /// Interaction logic for <see cref="CategoryPage"/>
    /// </summary>
    [Export(typeof(CategoryPageViewModel))]
    [Shared()]
    public sealed class CategoryPageViewModel : ViewModelBase, IDisposable
    {
        private const string AddAccountEvent = "Category.AddAccount.Command";
        private const string GeneratePasswordEvent = "Category.GeneratePassword.Command";
        private const string DeleteAccountEvent = "Category.DeleteAccount.Command";
        private const string PaneClosingEvent = "Category.Pane.Closing";

        private readonly ILogger _logger;
        private readonly IDataManager _dataManager;

        internal IWindowManager WindowManager { get; set; }

        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal CategoryPageStrings Strings => LanguageManager.Instance.CategoryPage;

        internal CommonViewModel CommonViewModel { get; }

        [ImportingConstructor]
        public CategoryPageViewModel(
            ILogger logger,
            IDataManager dataManager,
            IWindowManager windowManager,
            CommonViewModel commonViewModel)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            CommonViewModel = Arguments.NotNull(commonViewModel, nameof(commonViewModel));
            WindowManager = Arguments.NotNull(windowManager, nameof(windowManager));

            AddAccountCommand = new AsyncActionCommand<object>(_logger, AddAccountEvent, ExecuteAddAccountCommandAsync);
            GeneratePasswordCommand = new AsyncActionCommand<object>(_logger, GeneratePasswordEvent, ExecuteGeneratePasswordCommandAsync);
            DeleteAccountCommand = new AsyncActionCommand<Account>(_logger, DeleteAccountEvent, ExecuteDeleteAccountCommandAsync);
            PaneClosingCommand = new ActionCommand<SplitViewPaneClosingEventArgs>(_logger, PaneClosingEvent, ExecutePaneClosingCommand);
        }

        public void Dispose()
        {
            _dataManager.Dispose();
        }

        #region AddAccountCommand

        internal AsyncActionCommand<object> AddAccountCommand { get; }

        private async Task ExecuteAddAccountCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            string input = await WindowManager.ShowInputDialogAsync(
                defaultInputValue: null,
                placeHolder: LanguageManager.Instance.InputDialog.AccountTitlePlaceholder,
                primaryButtonText: LanguageManager.Instance.InputDialog.AddAccountPrimaryButton,
                title: LanguageManager.Instance.InputDialog.AddAccountTitle).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(input))
            {
                Guid accountId = await _dataManager.GenerateUniqueIdAsync(cancellationToken).ConfigureAwait(false);
                var account = new Account(accountId)
                {
                    Title = input,
                    CategoryID = CommonViewModel.CurrentCategoryId,
                    IconMode = IconMode.Automatic,
                    CreationDate = DateTime.Now,
                    LastModificationDate = DateTime.Now
                };

                await _dataManager.AddNewAccountAsync(account, cancellationToken).ConfigureAwait(false);

                // Clear the search bar (which makes all the items being displayed).
                await CommonViewModel.ClearSearchAsync(keepSelectionUnchanged: true).ConfigureAwait(false);

                // Select the account we created.
                account = await _dataManager.GetAccountAsync(accountId, cancellationToken).ConfigureAwait(false);
                await CommonViewModel.SetSelectedAccountAsync(account, shouldSwitchToEditMode: true).ConfigureAwait(false);

                _dataManager.SaveLocalUserDataBundleAsync(synchronize: true, cancellationToken).ForgetSafely();
            }
        }

        #endregion

        #region GeneratePasswordCommand

        internal AsyncActionCommand<object> GeneratePasswordCommand { get; }

        private async Task ExecuteGeneratePasswordCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            await TaskHelper.RunOnUIThreadAsync(async () =>
            {
                var passwordGeneratorDialog = new PasswordGeneratorDialog();
                await passwordGeneratorDialog.ShowAsync();
            }).ConfigureAwait(false);
        }

        #endregion

        #region DeleteAccountCommand

        internal AsyncActionCommand<Account> DeleteAccountCommand { get; }

        private async Task ExecuteDeleteAccountCommandAsync(Account parameter, CancellationToken cancellationToken)
        {
            await CommonViewModel.SetSelectedAccountAsync(parameter, shouldSwitchToEditMode: false).ConfigureAwait(false);
            CommonViewModel.RaiseDeleteAccount();
        }

        #endregion

        #region PaneClosingCommand

        internal ActionCommand<SplitViewPaneClosingEventArgs> PaneClosingCommand { get; }

        private void ExecutePaneClosingCommand(SplitViewPaneClosingEventArgs parameter)
        {
        }

        #endregion
    }
}
