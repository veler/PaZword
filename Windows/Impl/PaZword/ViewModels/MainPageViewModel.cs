using Microsoft.Graph;
using PaZword.Api;
using PaZword.Api.Collections;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.Services;
using PaZword.Api.UI;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Localization;
using PaZword.Models;
using PaZword.Models.Pwned;
using PaZword.Views;
using PaZword.Views.Dialog;
using PaZword.Views.Other;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Constants = PaZword.Core.Constants;

namespace PaZword.ViewModels
{
    /// <summary>
    /// Interaction logic for <see cref="MainPage"/>
    /// </summary>
    [Export(typeof(MainPageViewModel))]
    [Shared()]
    public sealed class MainPageViewModel : ViewModelBase
    {
        private const string MenuSelectedEvent = "Main.MenuSelected.Command";
        private const string SelectedMenuChangedEvent = "Main.SelectedMenu.Changed";
        private const string SearchBoxChangedEvent = "Main.SearchBox.TextChanged";
        private const string AddCategoryEvent = "Main.AddCategory.Command";
        private const string RenameCategoryEvent = "Main.RenameCategory.Command";
        private const string DeleteCategoryEvent = "Main.DeleteCategory.Command";

        private readonly ILogger _logger;
        private readonly IDataManager _dataManager;
        private readonly IRecurrentTaskService _recurrentTaskService;

        private NavigationHelper _navigation;
        private object _selectedMenu;
        private object _settingsViewItem;

        internal IWindowManager WindowManager { get; set; }

        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        /// <remarks>This property is Public because other it isn't accessible from data templates and context menu</remarks>
        public MainPageStrings Strings => LanguageManager.Instance.MainPage;

        internal CommonViewModel CommonViewModel { get; }

        /// <summary>
        /// Gets the list of categories.
        /// </summary>
        internal ConcurrentObservableCollection<Category> Categories => _dataManager.Categories;

        /// <summary>
        /// Gets or sets the selected menu in the navigation view.
        /// </summary>
        internal object SelectedMenu
        {
            get => _selectedMenu;
            set
            {
                _selectedMenu = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
        /// </summary>
        [ImportingConstructor]
        public MainPageViewModel(
            ILogger logger,
            IDataManager dataManager,
            IRecurrentTaskService recurrentTaskService,
            IWindowManager windowManager,
            CommonViewModel commonViewModel)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            _recurrentTaskService = Arguments.NotNull(recurrentTaskService, nameof(recurrentTaskService));
            CommonViewModel = Arguments.NotNull(commonViewModel, nameof(commonViewModel));
            WindowManager = Arguments.NotNull(windowManager, nameof(windowManager));

            NavigationViewItemInvokedCommand = new ActionCommand<NavigationViewItemInvokedEventArgs>(_logger, MenuSelectedEvent, ExecuteNavigationViewItemInvokedCommand);
            AutoSuggestBoxTextChangedCommand = new ActionCommand<object>(_logger, SearchBoxChangedEvent, ExecuteAutoSuggestBoxQuerySubmittedCommand);
            AddACategoryNavigationViewItemKeyDownCommand = new ActionCommand<KeyRoutedEventArgs>(_logger, AddCategoryEvent, ExecuteAddACategoryNavigationViewItemKeyDownCommand, CanExecuteAddACategoryNavigationViewItemKeyDownCommand);
            AddACategoryNavigationViewItemTappedCommand = new AsyncActionCommand<object>(_logger, AddCategoryEvent, ExecuteAddACategoryNavigationViewItemTappedCommandAsync);
            RenameCategoryCommand = new AsyncActionCommand<Category>(_logger, RenameCategoryEvent, ExecuteRenameCategoryCommandAsync, CanExecuteRenameCategoryCommand);
            DeleteCategoryCommand = new AsyncActionCommand<Category>(_logger, DeleteCategoryEvent, ExecuteDeleteCategoryCommandAsync, CanExecuteDeleteCategoryCommand);

            CommonViewModel.SelectCategoryInMenu += CommonViewModel_SelectCategoryInMenu;
        }

        /// <summary>
        /// Initialize the view model.
        /// </summary>
        /// <param name="frame">The <see cref="Frame"/> that must be used to manage the navigation.</param>
        internal async Task InitializeAsync(Frame contentFrame)
        {
            _recurrentTaskService.TaskCompleted += RecurrentTaskService_TaskCompleted;
            _recurrentTaskService.Start();

            if (_navigation != null)
            {
                _navigation.Navigated -= Navigation_Navigated;
            }

            _navigation = new NavigationHelper(Arguments.NotNull(contentFrame, nameof(contentFrame)));
            _navigation.Navigated += Navigation_Navigated;

            if (SelectedMenu == null && Categories.Count > 0)
            {
                Category defaultCategory = await _dataManager.GetCategoryAsync(new Guid(Constants.CategoryAllId), CancellationToken.None).ConfigureAwait(false);
                await ChangeSelectedMenuToCategoryAsync(defaultCategory).ConfigureAwait(false);
            }
        }

        internal void Unload()
        {
            _recurrentTaskService.TaskCompleted -= RecurrentTaskService_TaskCompleted;
            SelectedMenu = null;
        }

        #region NavigationViewItemInvokedCommand

        /// <summary>
        /// Command executed when an item from the navigation view is invoked.
        /// </summary>
        internal ActionCommand<NavigationViewItemInvokedEventArgs> NavigationViewItemInvokedCommand { get; }

        private void ExecuteNavigationViewItemInvokedCommand(NavigationViewItemInvokedEventArgs parameter)
        {
            ChangeSelectedMenuAsync(parameter.IsSettingsInvoked, parameter.InvokedItem, changeProgrammatically: false).Forget();
        }

        #endregion

        #region AutoSuggestBoxQuerySubmittedCommand

        /// <summary>
        /// Command executed when the user submit the search query in the search bar a the top left of the UI.
        /// </summary>
        internal ActionCommand<object> AutoSuggestBoxTextChangedCommand { get; }

        private void ExecuteAutoSuggestBoxQuerySubmittedCommand(object paramter)
        {
            if (SelectedMenu != null && SelectedMenu.GetType() == typeof(Category))
            {
                CommonViewModel.SearchAsync(CommonViewModel.SearchQuery, keepSelectionUnchanged: true).ForgetSafely();
            }
        }

        #endregion

        #region AddACategoryNavigationViewItemKeyDownCommand

        /// <summary>
        /// Command executed when the user press a key on Add A Category menu.
        /// </summary>
        internal ActionCommand<KeyRoutedEventArgs> AddACategoryNavigationViewItemKeyDownCommand { get; }

        private bool CanExecuteAddACategoryNavigationViewItemKeyDownCommand(KeyRoutedEventArgs parameter)
        {
            return parameter.Key == Windows.System.VirtualKey.Enter || parameter.Key == Windows.System.VirtualKey.Space;
        }

        private void ExecuteAddACategoryNavigationViewItemKeyDownCommand(KeyRoutedEventArgs parameter)
        {
            AddACategoryNavigationViewItemTappedCommand.Execute(null);
        }

        #endregion

        #region AddACategoryNavigationViewItemTappedCommand

        /// <summary>
        /// Command executed when the user click on Add a Category.
        /// </summary>
        internal AsyncActionCommand<object> AddACategoryNavigationViewItemTappedCommand { get; }

        private async Task ExecuteAddACategoryNavigationViewItemTappedCommandAsync(object parameter, CancellationToken cancellationToken)
        {
            string input = await WindowManager.ShowInputDialogAsync(
                defaultInputValue: null,
                placeHolder: LanguageManager.Instance.InputDialog.CategoryNamePlaceholder,
                primaryButtonText: LanguageManager.Instance.InputDialog.AddCategoryPrimaryButton,
                title: LanguageManager.Instance.InputDialog.AddCategoryTitle).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(input))
            {
                var category = await _dataManager.AddNewCategoryAsync(input, cancellationToken).ConfigureAwait(false);

                await ChangeSelectedMenuToCategoryAsync(category, changeProgrammatically: false).ConfigureAwait(false);

                _dataManager.SaveLocalUserDataBundleAsync(synchronize: true, cancellationToken).ForgetSafely();
            }
        }

        #endregion

        #region RenameCategoryCommand

        /// <summary>
        /// Command executed when the user clicked on the Rename category context menu.
        /// </summary>
        /// <remarks>This property is Public because other it isn't accessible from data templates and context menu</remarks>
        public AsyncActionCommand<Category> RenameCategoryCommand { get; }

        private bool CanExecuteRenameCategoryCommand(Category category)
        {
            return category != null && category.Id != new Guid(Constants.CategoryAllId);
        }

        private async Task ExecuteRenameCategoryCommandAsync(Category category, CancellationToken cancellationToken)
        {
            string input = await WindowManager.ShowInputDialogAsync(
                defaultInputValue: category.Name,
                placeHolder: LanguageManager.Instance.InputDialog.CategoryNamePlaceholder,
                primaryButtonText: LanguageManager.Instance.InputDialog.RenamePrimaryButton,
                title: LanguageManager.Instance.InputDialog.RenameTitle).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(input))
            {
                await _dataManager.RenameCategoryAsync(category.Id, input, cancellationToken).ConfigureAwait(false);

                RaisePropertyChanged(nameof(Categories));
                category = Categories.Single(c => c == category);

                if (!await ChangeSelectedMenuToCategoryAsync(category).ConfigureAwait(false))
                {
                    SelectedMenu = category;
                }

                _dataManager.SaveLocalUserDataBundleAsync(synchronize: true, cancellationToken).ForgetSafely();
            }
        }

        #endregion

        #region DeleteCategoryCommand

        /// <summary>
        /// Command executed when the user clicked on the Delete category context menu.
        /// </summary>
        /// <remarks>This property is Public because other it isn't accessible from data templates and context menu</remarks>
        public AsyncActionCommand<Category> DeleteCategoryCommand { get; }

        private bool CanExecuteDeleteCategoryCommand(Category category)
        {
            return category != null && category.Id != new Guid(Constants.CategoryAllId);
        }

        private async Task ExecuteDeleteCategoryCommandAsync(Category category, CancellationToken cancellationToken)
        {
            var dialogResult = await WindowManager.ShowMessageDialogAsync(
                message: Strings.DeleteConfirmationDescription,
                closeButtonText: Strings.No,
                primaryButtonText: Strings.Yes,
                title: Strings.GetFormattedDeleteConfirmationTitle(category.Name)).ConfigureAwait(false);

            if (dialogResult == ContentDialogResult.Primary)
            {
                await _dataManager.DeleteCategoryAsync(category.Id, cancellationToken).ConfigureAwait(false);

                Category defaultCategory = await _dataManager.GetCategoryAsync(new Guid(Constants.CategoryAllId), cancellationToken).ConfigureAwait(false);
                await ChangeSelectedMenuToCategoryAsync(defaultCategory).ConfigureAwait(false);

                _dataManager.SaveLocalUserDataBundleAsync(synchronize: true, cancellationToken).ForgetSafely();
            }
        }

        #endregion   

        private void RecurrentTaskService_TaskCompleted(object sender, RecurrentTaskEventArgs e)
        {
            switch (e.TaskName)
            {
                case Constants.PwnedRecurrentTask:
                    if (e.Result is IReadOnlyList<Breach> breaches)
                    {
                        NotifyUserBreachAsync(breaches).Forget();
                    }
                    break;

                case Constants.InactivityDetectionRecurrentTask:
                    if (e.Result is bool isInactive
                        && isInactive
                        && Window.Current.Content is Frame frame)
                    {
                        // Hide all the content dialog.
                        IReadOnlyList<Popup> popups = VisualTreeHelper.GetOpenPopups(Window.Current);
                        for (int i = 0; i < popups.Count; i++)
                        {
                            Popup popup = popups[i];
                            if (popup.Child is ContentDialog contentDialog)
                            {
                                contentDialog.Hide();
                            }
                        }

                        frame.Navigate(typeof(AuthenticationPage), null);
                    }
                    break;

                default:
                    break;
            }
        }

        private void Navigation_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(SettingsPage))
            {
                SelectedMenu = _settingsViewItem;
            }
            else if (e.Parameter is CategoryPageNavigationParameter parameters && e.SourcePageType == typeof(CategoryPage))
            {
                SelectedMenu = parameters.Category;

                if (parameters.ShouldClearSelection)
                {
                    CommonViewModel.SelectedAccount = null;
                }

                CommonViewModel.ChangeCategory(parameters.Category, refreshGroupsAndAccounts: false);
                CommonViewModel.ClearSearchAsync(keepSelectionUnchanged: parameters.NavigatedProgrammatically).ForgetSafely();
            }
        }

        private void CommonViewModel_SelectCategoryInMenu(object sender, SelectCategoryInMenuEventArgs e)
        {
            ChangeSelectedMenuToCategoryAsync(e.Category).Forget();
        }

        /// <summary>
        /// Move to the specified category.
        /// </summary>
        /// <param name="category">The category to go to.</param>
        /// <param name="changeProgrammatically">Indicates whether this action has been initiated by the user by explicitly selecting a menu or not.</param>
        /// <returns>Returns <code>False</code> if the <paramref name="category"/> is the current <see cref="SelectedMenu"/>.</returns>
        internal Task<bool> ChangeSelectedMenuToCategoryAsync(Category category, bool changeProgrammatically = true)
            => ChangeSelectedMenuAsync(false, category, changeProgrammatically);

        /// <summary>
        /// Update the <see cref="SelectedMenu"/> and navigation frame.
        /// </summary>
        /// <param name="isSettingsInvoked">Defines whether the settings have been invoked.</param>
        /// <param name="invokedItem">The invoked item.</param>
        /// <param name="changeProgrammatically">Indicates whether this action has been initiated by the user by explicitly selecting a menu or not.</param>
        /// <returns>Returns <code>False</code> if the <paramref name="invokedItem"/> is the current <see cref="SelectedMenu"/>.</returns>
        private async Task<bool> ChangeSelectedMenuAsync(bool isSettingsInvoked, object invokedItem, bool changeProgrammatically = true)
        {
            if (SelectedMenu == invokedItem)
            {
                return false;
            }

            var previousMenuWasSettings = SelectedMenu == _settingsViewItem;

            if (isSettingsInvoked)
            {
                _logger.LogEvent(SelectedMenuChangedEvent, $"'Settings' menu selected.");
                _settingsViewItem = invokedItem;
                await _navigation.NavigateToPageAsync<SettingsPage>(giveFocus: true).ConfigureAwait(false);
            }
            else if (invokedItem is Category category)
            {
                _logger.LogEvent(SelectedMenuChangedEvent, $"'Category' menu selected. The selected category is 'All' == {category.Id == new Guid(Constants.CategoryAllId)}");
                await _navigation.NavigateToPageAsync<CategoryPage>(giveFocus: !changeProgrammatically,
                    parameter: new CategoryPageNavigationParameter(
                        category,
                        changeProgrammatically,
                        shouldClearSelection: previousMenuWasSettings))
                    .ConfigureAwait(false);
            }

            return true;
        }

        /// <summary>
        /// Notifies the user about a breach on one of its account.
        /// </summary>
        private async Task NotifyUserBreachAsync(IReadOnlyList<Breach> breaches)
        {
            if (breaches == null || breaches.Count == 0)
            {
                return;
            }

            var breacheDisoveredDialog = new BreachDiscoveredDialog(breaches);
            await breacheDisoveredDialog.ShowAsync();
        }
    }
}
