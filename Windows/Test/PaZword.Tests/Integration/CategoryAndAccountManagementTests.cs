using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.Security;
using PaZword.Api.ViewModels.Data;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI.Controls;
using PaZword.Localization;
using PaZword.Models.Data;
using PaZword.Tests.Mocks;
using PaZword.ViewModels;
using PaZword.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace PaZword.Tests.Integration
{
    [TestClass]
    public class CategoryAndAccountManagementTests
    {
        private readonly MockIWindowManager _windowManager = new MockIWindowManager();

        private Frame _mainPageFrame;
        private IApp _app;
        private ViewModelLocator _viewModelLocator;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                // Do all the tests in English.
                LanguageManager.Instance.SetCurrentCulture(new CultureInfo("en"));

                _app = (IApp)Windows.UI.Xaml.Application.Current;

                _app.ResetMef();

                _viewModelLocator = (ViewModelLocator)App.Current.Resources["ViewModelLocator"];

                _mainPageFrame = new Frame();

                _viewModelLocator.MainPage.WindowManager = _windowManager;
                _viewModelLocator.CategoryPage.WindowManager = _windowManager;
            }).ConfigureAwait(false);

            var encryption = _app.ExportProvider.GetExport<IEncryptionProvider>();
            encryption.SetSecretKeys(encryption.GenerateSecretKeys());

            await _app.ExportProvider.GetExport<IDataManager>().ClearLocalDataAsync(CancellationToken.None).ConfigureAwait(false);
            await _app.ExportProvider.GetExport<IDataManager>().LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

            await _viewModelLocator.MainPage.InitializeAsync(_mainPageFrame).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AddCategoryAsync()
        {
            Assert.AreEqual(5, _viewModelLocator.MainPage.Categories.Count);

            var mainPage = _viewModelLocator.MainPage;
            mainPage.AddACategoryNavigationViewItemTappedCommand.Execute(null);
            mainPage.AddACategoryNavigationViewItemTappedCommand.WaitRunToCompletion();

            Assert.AreEqual(6, mainPage.Categories.Count);

            var newCategory = mainPage.Categories.Single(c => c.Name == "New Category");
            Assert.AreEqual(2, mainPage.Categories.IndexOf(newCategory));
            Assert.AreEqual(newCategory, mainPage.SelectedMenu);

            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                Assert.IsInstanceOfType(_mainPageFrame.Content, typeof(CategoryPage));
                Assert.AreEqual(newCategory.Id, ((CategoryPage)_mainPageFrame.Content).ViewModel.CommonViewModel.CurrentCategoryId);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public void RenameCategory()
        {
            var mainPage = _viewModelLocator.MainPage;

            // Can't rename the "All" category.
            Assert.IsFalse(mainPage.RenameCategoryCommand.CanExecute(mainPage.SelectedMenu));

            var categoryToRename = mainPage.Categories[1];
            Assert.AreEqual("Financial", categoryToRename.Name);
            Assert.AreNotEqual(mainPage.SelectedMenu, categoryToRename);

            Assert.IsTrue(mainPage.RenameCategoryCommand.CanExecute(categoryToRename));
            mainPage.RenameCategoryCommand.Execute(categoryToRename);
            mainPage.RenameCategoryCommand.WaitRunToCompletion();

            Assert.AreEqual(mainPage.SelectedMenu, categoryToRename);
            Assert.AreEqual("Renamed", categoryToRename.Name);
            Assert.AreEqual(3, mainPage.Categories.IndexOf(categoryToRename));
        }

        [TestMethod]
        public void DeleteCategory()
        {
            _windowManager.MessageDialogResult = ContentDialogResult.Primary;
            var mainPage = _viewModelLocator.MainPage;

            // Can't delete the "All" category.
            Assert.IsFalse(mainPage.DeleteCategoryCommand.CanExecute(mainPage.SelectedMenu));

            var categoryToRename = mainPage.Categories[1];
            Assert.AreEqual("Financial", categoryToRename.Name);

            Assert.IsTrue(mainPage.DeleteCategoryCommand.CanExecute(categoryToRename));
            mainPage.DeleteCategoryCommand.Execute(categoryToRename);
            mainPage.DeleteCategoryCommand.WaitRunToCompletion();

            Assert.AreEqual(4, mainPage.Categories.Count);
        }

        [TestMethod]
        public async Task AddNewAccountAsync()
        {
            var mainPage = _viewModelLocator.MainPage;
            var categoryPage = _viewModelLocator.CategoryPage;

            var dataManager = _app.ExportProvider.GetExport<IDataManager>();
            var searchResult = await dataManager.SearchAsync(((Category)mainPage.SelectedMenu).Id, string.Empty, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(0, searchResult.Count);

            // Add account to the category All.
            _windowManager.InputDialogResult = "Account 1";
            Assert.IsTrue(categoryPage.AddAccountCommand.CanExecute(null));
            categoryPage.AddAccountCommand.Execute(null);
            categoryPage.AddAccountCommand.WaitRunToCompletion();

            searchResult = await dataManager.SearchAsync(((Category)mainPage.SelectedMenu).Id, string.Empty, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, searchResult.Count);
            Assert.AreEqual(new Guid(Constants.CategoryAllId), searchResult[0].CategoryID);
            Assert.AreEqual("Account 1", searchResult[0].Title);

            // Navigate to another category
            await mainPage.ChangeSelectedMenuToCategoryAsync(mainPage.Categories[1]).ConfigureAwait(false);

            // Add account to the category All.
            _windowManager.InputDialogResult = "Account 2";
            Assert.IsTrue(categoryPage.AddAccountCommand.CanExecute(null));
            categoryPage.AddAccountCommand.Execute(null);
            categoryPage.AddAccountCommand.WaitRunToCompletion();

            searchResult = await dataManager.SearchAsync(((Category)mainPage.SelectedMenu).Id, string.Empty, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, searchResult.Count);
            Assert.AreEqual(((Category)mainPage.SelectedMenu).Id, searchResult[0].CategoryID);
            Assert.AreEqual("Account 2", searchResult[0].Title);

            // Move to All category
            await mainPage.ChangeSelectedMenuToCategoryAsync(mainPage.Categories[0]).ConfigureAwait(false);

            searchResult = await dataManager.SearchAsync(((Category)mainPage.SelectedMenu).Id, string.Empty, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(2, searchResult.Count);
            Assert.AreEqual(((Category)mainPage.SelectedMenu).Id, searchResult[0].CategoryID);
            Assert.AreEqual("Account 1", searchResult[0].Title);
            Assert.AreEqual(mainPage.Categories[1].Id, searchResult[1].CategoryID);
            Assert.AreEqual("Account 2", searchResult[1].Title);
        }

        [TestMethod]
        public async Task SaveAccountWithEmptyNameAsync()
        {
            AccountPageViewModel accountPage = await AddAccountAsync("Account 1").ConfigureAwait(false);

            Assert.IsTrue(accountPage.IsEditing);

            // Clear the title field.
            accountPage.AccountEditMode.Title = string.Empty;

            // Try to save.
            accountPage.SaveChangesCommand.Execute(null);
            accountPage.SaveChangesCommand.WaitRunToCompletion();

            // The action has been canceled because there is no title. Therefore the account should still be in Editing mode.
            Assert.IsTrue(accountPage.IsEditing);
        }

        [TestMethod]
        public void RemoveAFileAccountDataAndCancelWontRemoveFiles()
        {
            // TODO: integration test
            Assert.Fail();
        }

        [TestMethod]
        public async Task AddAccountData()
        {
            AccountPageViewModel accountPage = await AddAccountAsync("Account 1").ConfigureAwait(false);

            // Adding 3 account data.
            await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false);
            await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false);
            await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false);

            var accountData = accountPage.AccountPageToAccountDataViewModelBridge.ViewModels;

            Assert.AreEqual(3, accountData.Count);
            Assert.IsTrue(accountData[0].IsEditing);
            Assert.IsTrue(accountData[1].IsEditing);
            Assert.IsTrue(accountData[2].IsEditing);
        }

        [TestMethod]
        public async Task MoveAccountDataDown()
        {
            AccountPageViewModel accountPage = await AddAccountAsync("Account 1").ConfigureAwait(false);

            // Adding 3 account data.
            var controls = new List<AccountDataControl>
            {
                await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false),
                await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false),
                await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false)
            };

            var originalListOfData = accountPage.AccountEditMode.Data.ToList();

            // Move the first item down.
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                Assert.IsTrue(controls[0].MoveDownCommand.CanExecute(null));
                controls[0].MoveDownCommand.Execute(null);
            }).ConfigureAwait(false);

            // The middle item is now at the top.
            Assert.AreEqual(originalListOfData[1].Id, accountPage.AccountEditMode.Data[0].Id);

            // The first item is now in the middle.
            Assert.AreEqual(originalListOfData[0].Id, accountPage.AccountEditMode.Data[1].Id);

            // The last item didn't move.
            Assert.AreEqual(originalListOfData[2].Id, accountPage.AccountEditMode.Data[2].Id);
        }

        [TestMethod]
        public async Task MoveAccountDataUp()
        {
            AccountPageViewModel accountPage = await AddAccountAsync("Account 1").ConfigureAwait(false);

            // Adding 3 account data.
            var controls = new List<AccountDataControl>
            {
                await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false),
                await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false),
                await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false)
            };

            var originalListOfData = accountPage.AccountEditMode.Data.ToList();

            // Move the last item up.
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                Assert.IsTrue(controls[2].MoveUpCommand.CanExecute(null));
                controls[2].MoveUpCommand.Execute(null);
            }).ConfigureAwait(false);

            // The first item didn't move.
            Assert.AreEqual(originalListOfData[0].Id, accountPage.AccountEditMode.Data[0].Id);

            // The middle item is now the last.
            Assert.AreEqual(originalListOfData[1].Id, accountPage.AccountEditMode.Data[2].Id);

            // The last item is now in the middle.
            Assert.AreEqual(originalListOfData[2].Id, accountPage.AccountEditMode.Data[1].Id);
        }

        [TestMethod]
        public async Task DeleteAccountData()
        {
            AccountPageViewModel accountPage = await AddAccountAsync("Account 1").ConfigureAwait(false);

            // Adding 3 account data.
            var controls = new List<AccountDataControl>
            {
                await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false),
                await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false),
                await AddAccountDataAsync(accountPage, typeof(CredentialData)).ConfigureAwait(false)
            };

            // Save changes.
            accountPage.SaveChangesCommand.Execute(null);
            accountPage.SaveChangesCommand.WaitRunToCompletion();

            Assert.AreEqual(3, accountPage.Account.Data.Count);

            // Go to Edit mode.
            accountPage.EditAccountCommand.Execute(null);
            accountPage.EditAccountCommand.WaitRunToCompletion();

            Assert.AreEqual(3, accountPage.AccountEditMode.Data.Count);

            // Delete the first item.
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                Assert.IsTrue(controls[0].DeleteCommand.CanExecute(null));
                controls[0].DeleteCommand.Execute(null);
            }).ConfigureAwait(false);

            Assert.AreEqual(3, accountPage.Account.Data.Count);
            Assert.AreEqual(2, accountPage.AccountEditMode.Data.Count);

            // Cancel the change.
            accountPage.DiscardChangesCommand.Execute(null);

            Assert.AreEqual(3, accountPage.Account.Data.Count);

            // Go to Edit mode.
            accountPage.EditAccountCommand.Execute(null);
            accountPage.EditAccountCommand.WaitRunToCompletion();

            Assert.AreEqual(3, accountPage.AccountEditMode.Data.Count);

            // Delete the first item.
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                Assert.IsTrue(controls[0].DeleteCommand.CanExecute(null));
                controls[0].DeleteCommand.Execute(null);
            }).ConfigureAwait(false);

            Assert.AreEqual(3, accountPage.Account.Data.Count);
            Assert.AreEqual(2, accountPage.AccountEditMode.Data.Count);

            // Save changes.
            accountPage.SaveChangesCommand.Execute(null);
            accountPage.SaveChangesCommand.WaitRunToCompletion();

            Assert.AreEqual(2, accountPage.Account.Data.Count);
        }

        [TestMethod]
        public void PassingFromACategoryToAllCategoryShouldNotLoseSelectedAccount()
        {
            // TODO: integration test
            Assert.Fail();
        }

        private async Task<AccountPageViewModel> AddAccountAsync(string accountTitle)
        {
            var categoryPage = _viewModelLocator.CategoryPage;

            // Add account to the category All.
            _windowManager.InputDialogResult = accountTitle;
            Assert.IsTrue(categoryPage.AddAccountCommand.CanExecute(null));
            categoryPage.AddAccountCommand.Execute(null);
            categoryPage.AddAccountCommand.WaitRunToCompletion();

            await Task.Delay(1000).ConfigureAwait(false); // Wait to be sure the AccountPageViewModel got initialized.

            AccountPageViewModel accountPage = null;
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                var ui = ((AccountPage)((CategoryPage)_mainPageFrame.Content).AccountFrame.Content);
                ui.UpdateLayout();
                accountPage = ui.ViewModel;
                accountPage.WindowManager = _windowManager;
            }).ConfigureAwait(false);

            Assert.IsTrue(accountPage.IsEditing);

            return accountPage;
        }

        private async Task<AccountDataControl> AddAccountDataAsync(AccountPageViewModel accountPage, Type accountDataType)
        {
            var accountDataProviders = _app.ExportProvider.GetExports<Lazy<IAccountDataProvider, AccountDataProviderMetadata>>();
            var provider = accountDataProviders.Single(p => p.Metadata.AccountDataType == accountDataType);

            // Adding 3 account data.
            Assert.IsTrue(accountPage.AddAccountDataCommand.CanExecute(provider.Value));
            accountPage.AddAccountDataCommand.Execute(provider.Value);
            accountPage.AddAccountDataCommand.WaitRunToCompletion();

            AccountDataControl ui = null;
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                ui = new AccountDataControl();
                ui.AccountPageToAccountDataViewModelBridge = accountPage.AccountPageToAccountDataViewModelBridge;
                ui.DataContext = accountPage.AccountEditMode.Data.Last();
            }).ConfigureAwait(false);

            return ui;
        }
    }
}
