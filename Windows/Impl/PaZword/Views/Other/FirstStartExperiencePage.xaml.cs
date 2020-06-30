using PaZword.ViewModels.Other;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Other
{
    /// <summary>
    /// Interaction logic for FirstStartExperiencePage.xaml
    /// </summary>
    public sealed partial class FirstStartExperiencePage : Page
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal FirstStartExperiencePageViewModel ViewModel => (FirstStartExperiencePageViewModel)DataContext;

        /// <summary>
        /// Initialize a new instance of the <see cref="FirstStartExperiencePage"/> class.
        /// </summary>
        public FirstStartExperiencePage()
        {
            InitializeComponent();
        }
    }
}
