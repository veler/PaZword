using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.UI;
using PaZword.Core;
using PaZword.Localization;
using PaZword.Models.Data;
using PaZword.Views.Data;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace PaZword.ViewModels.Data.Credential
{
    /// <summary>
    /// Interaction logic for <see cref="CredentialDataUserControl"/>
    /// </summary>
    internal sealed class CredentialDataViewModel : AccountDataViewModelBase
    {
        private CredentialDataUserControl _control;

        public CredentialDataStrings Strings => LanguageManager.Instance.CredentialData;

        public override string Title => Strings.DisplayName;

        public override FrameworkElement UserInterface
        {
            get
            {
                if (_control == null)
                {
                    _control = new CredentialDataUserControl(this);
                }
                return _control;
            }
        }

        public Visibility UserNameFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.Username)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility EmailAddressFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.EmailAddress)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility PasswordFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.Password)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility SecurityQuestionFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.SecurityQuestion)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public Visibility SecurityQuestionAnswerFieldVisibility
            => !IsEditing
            && StringExtensions.IsNullOrEmptySecureString(CastedData.SecurityQuestionAnswer)
            ? Visibility.Collapsed
            : Visibility.Visible;

        private CredentialData CastedData => (CredentialData)Data;

        private CredentialData CastedDataEditMode => (CredentialData)DataEditMode;

        public CredentialDataViewModel(
            CredentialData accountData,
            ISerializationProvider serializationProvider,
            IWindowManager windowManager,
            ILogger logger)
            : base(logger, serializationProvider, windowManager)
        {
            Data = accountData;
        }

        public override async Task<bool> ValidateChangesAsync(CancellationToken cancellationToken)
        {
            return await base.ValidateChangesAsync(cancellationToken).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.Username, Strings.Username).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.EmailAddress, Strings.Email).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.Password, Strings.Password).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.SecurityQuestion, Strings.SecurityQuestion).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.SecurityQuestionAnswer, Strings.SecurityQuestionAnswer).ConfigureAwait(false);
        }

        public override string GenerateSubtitle()
        {
            string userName = CastedDataEditMode.Username.ToUnsecureString();
            string emailAddress = CastedDataEditMode.EmailAddress.ToUnsecureString();

            if (!string.IsNullOrWhiteSpace(userName))
            {
                return userName;
            }

            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                return emailAddress;
            }

            return string.Empty;
        }
    }
}
