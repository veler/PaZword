using PaZword.Core;
using PaZword.ViewModels.Data.File;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Data
{
    /// <summary>
    /// Interaction logic for FileDataUserControl.xaml
    /// </summary>
    public sealed partial class FileDataUserControl : UserControl
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal FileDataViewModel ViewModel => (FileDataViewModel)DataContext;

        internal FileDataUserControl(FileDataViewModel dataContext)
        {
            DataContext = Arguments.NotNull(dataContext, nameof(dataContext));
            InitializeComponent();
        }
    }
}
