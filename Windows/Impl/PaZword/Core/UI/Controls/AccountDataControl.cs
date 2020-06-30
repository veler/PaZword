using PaZword.Api;
using PaZword.Api.Models;
using PaZword.Api.ViewModels.Data;
using PaZword.ViewModels.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Core.UI.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="AccountDataControl"/>
    /// </summary>
    public class AccountDataControl : ContentControl
    {
        private const string MoveUpEvent = "AccountData.MoveUp.Command";
        private const string MoveDownEvent = "AccountData.MoveDown.Command";
        private const string DeleteEvent = "AccountData.Delete.Command";

        private readonly IEnumerable<Lazy<IAccountDataProvider, AccountDataProviderMetadata>> _accountDataProviders;

        private IAccountDataViewModel _viewModel;

        public static readonly DependencyProperty AccountPageToAccountDataViewModelBridgeProperty = DependencyProperty.Register(
            nameof(AccountPageToAccountDataViewModelBridge),
            typeof(AccountPageToAccountDataViewModelBridge),
            typeof(AccountDataControl),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets a value that defines whether the current account is in editing mode.
        /// </summary>
        public AccountPageToAccountDataViewModelBridge AccountPageToAccountDataViewModelBridge
        {
            get => (AccountPageToAccountDataViewModelBridge)GetValue(AccountPageToAccountDataViewModelBridgeProperty);
            set => SetValue(AccountPageToAccountDataViewModelBridgeProperty, value);
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="AccountDataControl"/> class.
        /// </summary>
        public AccountDataControl()
        {
            DefaultStyleKey = typeof(AccountDataControl);

            if (Application.Current is IApp app)
            {
                _accountDataProviders = app.ExportProvider.GetExports<Lazy<IAccountDataProvider, AccountDataProviderMetadata>>();
            }
            else
            {
                throw new ApplicationException($"Unable to convert Application to {nameof(IApp)}");
            }

            Unloaded += AccountDataControl_Unloaded;
            DataContextChanged += AccountDataControl_DataContextChanged;

            var logger = app.ExportProvider.GetExport<ILogger>();
            MoveUpCommand = new ActionCommand<object>(logger, MoveUpEvent, ExecuteMoveUpCommand, CanExecuteMoveUpCommand);
            MoveDownCommand = new ActionCommand<object>(logger, MoveDownEvent, ExecuteMoveDownCommand, CanExecuteMoveDownCommand);
            DeleteCommand = new ActionCommand<object>(logger, DeleteEvent, ExecuteDeleteCommand);
        }

        #region MoveUpCommand

        public static readonly DependencyProperty MoveUpCommandProperty = DependencyProperty.Register(
            nameof(MoveUpCommand),
            typeof(ICommand),
            typeof(AccountDataControl),
            new PropertyMetadata(null));

        public ActionCommand<object> MoveUpCommand
        {
            get => (ActionCommand<object>)GetValue(MoveUpCommandProperty);
            set => SetValue(MoveUpCommandProperty, value);
        }

        private bool CanExecuteMoveUpCommand(object parameter)
        {
            Account accountInEditMode = AccountPageToAccountDataViewModelBridge.GetAccountEditMode();
            AccountData accountDataInEditMode = _viewModel.DataEditMode;

            return AccountPageToAccountDataViewModelBridge.IsEditing
                && accountInEditMode.Data.IndexOf(accountDataInEditMode) > 0; // This will only compare ID of AccountData, which is what we want.
        }

        private void ExecuteMoveUpCommand(object parameter)
        {
            Account accountInEditMode = AccountPageToAccountDataViewModelBridge.GetAccountEditMode();
            AccountData accountDataInEditMode = _viewModel.DataEditMode;
            int oldIndex = accountInEditMode.Data.IndexOf(accountDataInEditMode);

            // Moving the item will refresh the UI, and make us losing DataEditMode.
            // Let's backup them. This is safe to do since accountInEditMode will be
            // discard if the user cancel the changes.
            for (int i = 0; i < AccountPageToAccountDataViewModelBridge.ViewModels.Count; i++)
            {
                accountInEditMode.Data[i] = AccountPageToAccountDataViewModelBridge.ViewModels[i].DataEditMode;
            }

            accountInEditMode.Data.Move(oldIndex, oldIndex - 1);
        }

        #endregion

        #region MoveDownCommand

        public static readonly DependencyProperty MoveDownCommandProperty = DependencyProperty.Register(
            nameof(MoveDownCommand),
            typeof(ICommand),
            typeof(AccountDataControl),
            new PropertyMetadata(null));

        public ActionCommand<object> MoveDownCommand
        {
            get => (ActionCommand<object>)GetValue(MoveDownCommandProperty);
            set => SetValue(MoveDownCommandProperty, value);
        }

        private bool CanExecuteMoveDownCommand(object parameter)
        {
            Account accountInEditMode = AccountPageToAccountDataViewModelBridge.GetAccountEditMode();
            AccountData accountDataInEditMode = _viewModel.DataEditMode;

            return AccountPageToAccountDataViewModelBridge.IsEditing
                && accountInEditMode.Data.IndexOf(accountDataInEditMode) < accountInEditMode.Data.Count - 1; // This will only compare ID of AccountData, which is what we want.
        }

        private void ExecuteMoveDownCommand(object parameter)
        {
            Account accountInEditMode = AccountPageToAccountDataViewModelBridge.GetAccountEditMode();
            AccountData accountDataInEditMode = _viewModel.DataEditMode;
            int oldIndex = accountInEditMode.Data.IndexOf(accountDataInEditMode);

            // Moving the item will refresh the UI, and make us losing DataEditMode.
            // Let's backup them. This is safe to do since accountInEditMode will be
            // discard if the user cancel the changes. 
            for (int i = 0; i < AccountPageToAccountDataViewModelBridge.ViewModels.Count; i++)
            {
                accountInEditMode.Data[i] = AccountPageToAccountDataViewModelBridge.ViewModels[i].DataEditMode;
            }

            accountInEditMode.Data.Move(oldIndex, oldIndex + 1);
        }

        #endregion

        #region DeleteCommand

        public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register(
            nameof(DeleteCommand),
            typeof(ICommand),
            typeof(AccountDataControl),
            new PropertyMetadata(null));

        public ActionCommand<object> DeleteCommand
        {
            get => (ActionCommand<object>)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        private void ExecuteDeleteCommand(object parameter)
        {
            Account accountInEditMode = AccountPageToAccountDataViewModelBridge.GetAccountEditMode();
            AccountData accountDataInEditMode = _viewModel.DataEditMode;

            // Register the view model to the register of view models for deletion.
            // When the user deletes an account data in editing mode, we don't want to immediately
            // call the DeleteAsync method because this method might delete some files on the hard drive
            // that won't be recovered if the user Cancels the changes. So we keep this view model for later.
            AccountPageToAccountDataViewModelBridge.RegisterViewModelForDeletion(_viewModel);

            // Remove the account data from the UI.
            // This will only compare ID of AccountData, which is what we want.
            // Doing this will refresh the list in the UI and therefore unload the current control.
            accountInEditMode.Data.RemoveAt(accountInEditMode.Data.IndexOf(accountDataInEditMode));
        }

        #endregion

        private void AccountDataControl_Unloaded(object sender, RoutedEventArgs e)
        {
            AccountPageToAccountDataViewModelBridge.UnregisterViewModel(_viewModel);
        }

        private void AccountDataControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue is AccountData accountData)
            {
                // When we add a AccountData to an Account, what is sent to the list is an AccountData.
                // Here, we replace the AccountData by the appripriate IAccountDataViewModel
                // and add a few binding to make the link between states.

                IAccountDataProvider accountDataProvider = _accountDataProviders
                    .Single(l => l.Metadata.AccountDataType == accountData.GetType()).Value;

                IAccountDataViewModel viewModel = accountDataProvider.CreateViewModel(accountData);
                _viewModel = viewModel;
                DataContext = viewModel;
                Content = viewModel.UserInterface;

                AccountPageToAccountDataViewModelBridge.RegisterViewModel(viewModel);
                AccountPageToAccountDataViewModelBridge.OnViewModelListChanged += AccountPageToAccountDataViewModelBridge_OnViewModelListChanged;
            }
        }

        private void AccountPageToAccountDataViewModelBridge_OnViewModelListChanged(object sender, EventArgs e)
        {
            MoveDownCommand.RaiseCanExecuteChanged();
            MoveUpCommand.RaiseCanExecuteChanged();
        }
    }
}
