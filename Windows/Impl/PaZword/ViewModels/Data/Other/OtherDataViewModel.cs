using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.UI;
using PaZword.Localization;
using PaZword.Models.Data;
using PaZword.Views.Data;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace PaZword.ViewModels.Data.Other
{
    /// <summary>
    /// Interaction logic for <see cref="OtherDataUserControl"/>
    /// </summary>
    internal sealed class OtherDataViewModel : AccountDataViewModelBase
    {
        private OtherDataUserControl _control;

        public OtherDataStrings Strings => LanguageManager.Instance.OtherData;

        public override string Title => Strings.DisplayName;

        public override FrameworkElement UserInterface
        {
            get
            {
                if (_control == null)
                {
                    _control = new OtherDataUserControl(this);
                }
                return _control;
            }
        }

        private OtherData CastedDataEditMode => (OtherData)DataEditMode;

        public OtherDataViewModel(
            OtherData accountData,
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
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.Name, Strings.Name).ConfigureAwait(false)
                && await base.ValidateFieldLengthAsync(CastedDataEditMode.Value, Strings.Value).ConfigureAwait(false);
        }

        public override string GenerateSubtitle()
        {
            return string.Empty;
        }
    }
}
