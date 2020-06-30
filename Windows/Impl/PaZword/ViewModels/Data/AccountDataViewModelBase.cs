using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Models;
using PaZword.Api.UI;
using PaZword.Api.ViewModels.Data;
using PaZword.Core;
using PaZword.Core.Threading;
using PaZword.Core.UI;
using PaZword.Core.UI.Controls;
using PaZword.Localization;
using PaZword.Views.Dialog;
using System;
using System.Globalization;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace PaZword.ViewModels.Data
{
    internal abstract class AccountDataViewModelBase : ViewModelBase, IAccountDataViewModel
    {
        private const string GeneratePasswordEvent = "AccountData.GeneratePassword.Command";

        private readonly ISerializationProvider _serializationProvider;
        private readonly IWindowManager _windowManager;

        private bool _isEditing;
        private AccountData _data;
        private AccountData _dataEditMode;

        public abstract FrameworkElement UserInterface { get; }

        public abstract string Title { get; }

        public AccountPageStrings AccountPageStrings => LanguageManager.Instance.AccountPage;

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;

                    if (_isEditing)
                    {
                        // Creates a copy of the object, so it's not the same instance but has the same values.
                        DataEditMode = _serializationProvider.CloneObject(Data);
                    }

                    RaisePropertyChanged();

                    if (!_isEditing)
                    {
                        DataEditMode?.Dispose();
                        DataEditMode = null;
                    }
                }
            }
        }

        public AccountData Data
        {
            get => _data;
            set
            {
                _data = value;
                RaisePropertyChanged();
            }
        }

        public AccountData DataEditMode
        {
            get => _dataEditMode;
            set
            {
                if (_dataEditMode != value)
                {
                    _dataEditMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        public AccountDataViewModelBase(
            ILogger logger,
            ISerializationProvider serializationProvider,
            IWindowManager windowManager)
        {
            _serializationProvider = Arguments.NotNull(serializationProvider, nameof(serializationProvider));
            _windowManager = Arguments.NotNull(windowManager, nameof(windowManager));

            GeneratePasswordCommand = new AsyncActionCommand<EditablePassword>(logger, GeneratePasswordEvent, ExecuteGeneratePasswordCommandAsync);
        }

        public abstract string GenerateSubtitle();

        public virtual Task<bool> ValidateChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public virtual Task SaveAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task DeleteAsync(CancellationToken cancellationToken)
        {
            Data?.Dispose();
            DataEditMode?.Dispose();
            return Task.CompletedTask;
        }

        public virtual Task UnloadingAsync()
        {
            return Task.CompletedTask;
        }

        #region GeneratePasswordCommand

        internal AsyncActionCommand<EditablePassword> GeneratePasswordCommand { get; }

        private async Task ExecuteGeneratePasswordCommandAsync(EditablePassword editablePassword, CancellationToken cancellationToken)
        {
            var canSaveResult = editablePassword != null;

            await TaskHelper.RunOnUIThreadAsync(async () =>
            {
                var passwordGeneratorDialog = new PasswordGeneratorDialog(canSaveResult);
                await passwordGeneratorDialog.ShowAsync();
                if (passwordGeneratorDialog.Result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    editablePassword.TextEditing = passwordGeneratorDialog.GeneratedPassword;
                }
            }).ConfigureAwait(false);
        }

        #endregion

        protected async Task<bool> ValidateFieldLengthAsync(SecureString field, string fieldDisplayName)
        {
            if (field.Length > Constants.StringSizeLimit)
            {
                await _windowManager.ShowMessageDialogAsync(
                    message: LanguageManager.Instance.AccountPage.GetFormattedDataLengthOutOfRangeDescription(
                        fieldDisplayName,
                        Constants.StringSizeLimit.ToString(CultureInfo.CurrentCulture),
                        field.Length.ToString(CultureInfo.CurrentCulture)),
                    closeButtonText: LanguageManager.Instance.AccountPage.DataLengthOutOfRangeCloseButtonText
                    ).ConfigureAwait(false);
                return false;
            }

            return true;
        }
    }
}
