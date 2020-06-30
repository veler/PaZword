using PaZword.Api.Models;
using PaZword.Api.ViewModels.Data;
using PaZword.Core;
using PaZword.Core.Threading;
using System;
using System.Collections.Generic;

namespace PaZword.ViewModels.Data
{
    /// <summary>
    /// Represents a bridge between <see cref="AccountPageViewModel"/> and the <see cref="IAccountDataViewModel"/> to enable
    /// some interaction between both, in particular to know when <see cref="AccountPageViewModel.IsEditing"/> mode changes.
    /// </summary>
    public sealed class AccountPageToAccountDataViewModelBridge
    {
        private readonly List<IAccountDataViewModel> _viewModels = new List<IAccountDataViewModel>();
        private readonly List<IAccountDataViewModel> _viewModelsForDeletion = new List<IAccountDataViewModel>();
        private readonly AccountPageViewModel _accountPageViewModel;

        /// <summary>
        /// Gets a value that defines whether the current account is in editing mode.
        /// </summary>
        internal bool IsEditing => _accountPageViewModel.IsEditing;

        /// <summary>
        /// Gets the of registred view models.
        /// </summary>
        internal IReadOnlyList<IAccountDataViewModel> ViewModels => _viewModels;

        /// <summary>
        /// Gets the of registred view models to delete.
        /// </summary>
        internal IReadOnlyList<IAccountDataViewModel> ViewModelsForDeletion => _viewModelsForDeletion;

        /// <summary>
        /// Raised when <see cref="RegisterViewModel"/> or <see cref="UnregisterViewModel"/> are called.
        /// </summary>
        internal event EventHandler<EventArgs> OnViewModelListChanged;

        /// <summary>
        /// Initialize a new instance of the <see cref="AccountPageToAccountDataViewModelBridge"/> class.
        /// </summary>
        /// <param name="accountPageViewModel">The <see cref="AccountPageViewModel"/>.</param>
        internal AccountPageToAccountDataViewModelBridge(AccountPageViewModel accountPageViewModel)
        {
            _accountPageViewModel = Arguments.NotNull(accountPageViewModel, nameof(accountPageViewModel));
            _accountPageViewModel.PropertyChanged += AccountPageViewModel_PropertyChanged;
        }

        private void AccountPageViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_accountPageViewModel.IsEditing))
            {
                for (int i = 0; i < ViewModels.Count; i++)
                {
                    ViewModels[i].IsEditing = IsEditing;
                }
            }
        }

        /// <summary>
        /// Registers a view model to keep in memory.
        /// </summary>
        /// <param name="viewModel">The view model to register.</param>
        internal void RegisterViewModel(IAccountDataViewModel viewModel)
        {
            Arguments.NotNull(viewModel, nameof(viewModel));

            viewModel.IsEditing = IsEditing;
            _viewModels.Add(viewModel);
            OnViewModelListChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Unregisters the specific view model.
        /// </summary>
        /// <param name="viewModel">The view model to unregister.</param>
        internal void UnregisterViewModel(IAccountDataViewModel viewModel)
        {
            Arguments.NotNull(viewModel, nameof(viewModel));

            _viewModels.Remove(viewModel);
            viewModel.UnloadingAsync().ForgetSafely();

            OnViewModelListChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Registers a view model to a list of account data to delete.
        /// This is used in the scenario where the user delete an account data in editing mode then
        /// save the changes. When this happens, we will want to iterate through these view models
        /// to call the <see cref="IAccountDataViewModel.DeleteAsync(System.Threading.CancellationToken)"/> method.
        /// </summary>
        /// <param name="viewModel">The view model to register.</param>
        internal void RegisterViewModelForDeletion(IAccountDataViewModel viewModel)
        {
            Arguments.NotNull(viewModel, nameof(viewModel));
            _viewModels.Remove(viewModel);
            _viewModelsForDeletion.Add(viewModel);
        }

        /// <summary>
        /// Clears the registry of account data to be deleted.
        /// </summary>
        internal void ClearViewModelsForDeletion()
        {
            _viewModelsForDeletion.Clear();
        }

        /// <summary>
        /// Retrieves the account in editing mode.
        /// </summary>
        /// <returns>The <see cref="Account"/> in editing mode.</returns>
        internal Account GetAccountEditMode()
        {
            return _accountPageViewModel.AccountEditMode;
        }
    }
}
