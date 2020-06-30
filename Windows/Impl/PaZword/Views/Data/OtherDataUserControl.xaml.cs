using PaZword.Core;
using PaZword.ViewModels.Data.Other;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Data
{
    /// <summary>
    /// Interaction logic for OtherDataUserControl.xaml
    /// </summary>
    public sealed partial class OtherDataUserControl : UserControl
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal OtherDataViewModel ViewModel => (OtherDataViewModel)DataContext;

        internal OtherDataUserControl(OtherDataViewModel dataContext)
        {
            DataContext = Arguments.NotNull(dataContext, nameof(dataContext));
            InitializeComponent();
        }
    }
}
