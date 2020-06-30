using PaZword.Core;
using PaZword.Models.Pwned;
using PaZword.ViewModels.Dialog;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Dialog
{
    /// <summary>
    /// Interaction logic for BreachDiscoveredDialog.xaml
    /// </summary>
    public sealed partial class BreachDiscoveredDialog : ContentDialog
    {
        /// <summary>
        /// Gets the content dialog's view model.
        /// </summary>
        internal BreachDiscoveredDialogViewModel ViewModel => (BreachDiscoveredDialogViewModel)DataContext;

        internal BreachDiscoveredDialog(IReadOnlyList<Breach> breaches)
        {
            InitializeComponent();

            ViewModel.Breaches = Arguments.NotNull(breaches, nameof(breaches));
            ViewModel.CloseDialog += ViewModel_CloseDialog;
        }

        private void ViewModel_CloseDialog(object sender, System.EventArgs e)
        {
            Hide();
        }
    }
}
