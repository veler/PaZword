using PaZword.Api;
using PaZword.Core;
using PaZword.Core.UI;
using PaZword.Localization;
using PaZword.Models.Pwned;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;

namespace PaZword.ViewModels.Dialog
{
    /// <summary>
    /// Interaction logic for <see cref="BreachDiscoveredDialog"/>
    /// </summary>
    [Export(typeof(BreachDiscoveredDialogViewModel))]
    public sealed class BreachDiscoveredDialogViewModel : ViewModelBase
    {
        private const string PrimaryButtonEvent = "BreachDiscovered.PrimaryButton.Command";

        private IReadOnlyList<Breach> _breaches;

        /// <summary>
        /// Gets the texts for this view.
        /// </summary>
        internal BreachDiscoveredDialogStrings Strings => LanguageManager.Instance.BreachDiscoveredDialog;

        /// <summary>
        /// Gets the formatted text that corresponds to the WarningIntroduction resource.
        /// </summary>
        internal string FormattedWarningIntroduction
            => Strings.GetFormattedWarningIntroduction(
                Breaches.Count.ToString(CultureInfo.CurrentCulture));

        /// <summary>
        /// gets or sets the list of breaches to display.
        /// </summary>
        internal IReadOnlyList<Breach> Breaches
        {
            get => _breaches;
            set
            {
                _breaches = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FormattedWarningIntroduction));
            }
        }

        /// <summary>
        /// Raised when the dialog should close.
        /// </summary>
        internal event EventHandler CloseDialog;

        [ImportingConstructor]
        public BreachDiscoveredDialogViewModel(ILogger logger)
        {
            PrimaryButtonClickCommand = new ActionCommand<object>(logger, PrimaryButtonEvent, ExecutePrimaryButtonClickCommand);
        }

        #region PrimaryButtonClickCommand

        internal ActionCommand<object> PrimaryButtonClickCommand { get; }

        private void ExecutePrimaryButtonClickCommand(object parameter)
        {
            foreach (Breach breach in Breaches)
            {
                breach.EmailAddress.Dispose();
            }

            CloseDialog?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
