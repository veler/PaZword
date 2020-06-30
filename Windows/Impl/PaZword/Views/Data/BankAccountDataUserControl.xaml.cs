using PaZword.Core;
using PaZword.ViewModels.Data.BankAccount;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Data
{
    /// <summary>
    /// Interaction logic for BankAccountDataUserControl.xaml
    /// </summary>
    public sealed partial class BankAccountDataUserControl : UserControl
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal BankAccountDataViewModel ViewModel => (BankAccountDataViewModel)DataContext;

        internal BankAccountDataUserControl(BankAccountDataViewModel dataContext)
        {
            DataContext = Arguments.NotNull(dataContext, nameof(dataContext));
            InitializeComponent();
        }
    }
}
