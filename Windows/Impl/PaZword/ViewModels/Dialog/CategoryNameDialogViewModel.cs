using System;
using System.Collections.ObjectModel;
using System.Composition;
using PaZword.Api;
using PaZword.Api.Models;
using PaZword.Core;
using PaZword.Core.UI;
using PaZword.Localization;
using Windows.System;
using Windows.UI.Xaml.Input;

namespace PaZword.ViewModels.Dialog
{
    /// <summary>
    /// Interaction logic for <see cref="CategoryNameDialog"/>
    /// </summary>
    [Export(typeof(CategoryNameDialogViewModel))]
    public sealed class CategoryNameDialogViewModel : ViewModelBase
    {
        private const string CategoryNameTextBoxKeyDownEvent = "CategoryNameDialog.CategoryNameTextBox.KeyDown";

        private string _categoryName;
        private CategoryIcon _icon;

        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal CategoryNameDialogStrings Strings => LanguageManager.Instance.CategoryNameDialog;

        /// <summary>
        /// Gets the ordered list of available icons.
        /// </summary>
        public ObservableCollection<CategoryIcon> IconItemSource { get; }
            = new ObservableCollection<CategoryIcon>()
            {
                CategoryIcon.Default,
                CategoryIcon.UserGroup,
                CategoryIcon.UserGroup2,
                CategoryIcon.Personal,
                CategoryIcon.Personal2,
                CategoryIcon.Professional,
                CategoryIcon.Professional2,
                CategoryIcon.Professional3,
                CategoryIcon.Bank,
                CategoryIcon.Bank2,
                CategoryIcon.BankCard,
                CategoryIcon.Money,
                CategoryIcon.Safe,
                CategoryIcon.Key,
                CategoryIcon.Id,
                CategoryIcon.Id2,
                CategoryIcon.SocialMedia
            };

        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        internal string CategoryName
        {
            get => _categoryName;
            set
            {
                _categoryName = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the icon of the category.
        /// </summary>
        internal CategoryIcon Icon
        {
            get => _icon;
            set
            {
                if (value != _icon)
                {
                    _icon = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Raised when the dialog should close.
        /// </summary>
        internal event EventHandler CloseDialog;

        [ImportingConstructor]
        public CategoryNameDialogViewModel(
            ILogger logger)
        {
            CategoryNameTextBoxKeyDownCommand = new ActionCommand<KeyRoutedEventArgs>(logger, CategoryNameTextBoxKeyDownEvent, ExecutePrimaryButtonClickCommand);
        }

        #region PrimaryButtonClickCommand

        internal ActionCommand<KeyRoutedEventArgs> CategoryNameTextBoxKeyDownCommand { get; }

        private void ExecutePrimaryButtonClickCommand(KeyRoutedEventArgs parameter)
        {
            if (parameter.Key == VirtualKey.Enter && !string.IsNullOrWhiteSpace(CategoryName))
            {
                CloseDialog?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
