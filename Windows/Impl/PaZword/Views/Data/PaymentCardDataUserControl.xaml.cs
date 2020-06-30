using PaZword.Core;
using PaZword.ViewModels.Data.PaymentCard;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Data
{
    /// <summary>
    /// Interaction logic for PaymentCardDataUserControl.xaml
    /// </summary>
    public sealed partial class PaymentCardDataUserControl : UserControl
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal PaymentCardDataViewModel ViewModel => (PaymentCardDataViewModel)DataContext;

        internal PaymentCardDataUserControl(PaymentCardDataViewModel dataContext)
        {
            DataContext = Arguments.NotNull(dataContext, nameof(dataContext));
            InitializeComponent();
        }
    }
}
