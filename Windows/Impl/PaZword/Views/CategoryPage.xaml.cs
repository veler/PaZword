using PaZword.Api.Models;
using PaZword.Core.Threading;
using PaZword.Models;
using PaZword.ViewModels;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PaZword.Views
{
    /// <summary>
    /// Interaction logic for CategoryPage.xaml
    /// </summary>
    public sealed partial class CategoryPage : Page
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal CategoryPageViewModel ViewModel => (CategoryPageViewModel)DataContext;

        // For unit tests.
        internal Frame AccountFrame => AccountContentFrame;

        public readonly static DependencyProperty RealActualWidthProperty = DependencyProperty.Register(
            nameof(RealActualWidth),
            typeof(double),
            typeof(CategoryPage),
            new PropertyMetadata(0.0));

        /// <summary>
        /// Gets or sets the actual width of the page.
        /// </summary>
        public double RealActualWidth
        {
            get => (double)GetValue(RealActualWidthProperty);
            set => SetValue(RealActualWidthProperty, value);
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="CategoryPage"/> class.
        /// </summary>
        public CategoryPage()
        {
            InitializeComponent();

            RealActualWidth = ActualWidth;

            ViewModel.CommonViewModel.SelectedAccountChanged += CommonViewModel_SelectedAccountChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is CategoryPageNavigationParameter parameters)
            {
                ViewModel.CommonViewModel.Initialize(AccountContentFrame);
                ViewModel.CommonViewModel.ChangeCategory(parameters.Category, refreshGroupsAndAccounts: true);
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RealActualWidth = e.NewSize.Width;
        }

        private void AccountGridViewItemGrid_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            ShowAccountGridViewContextMenu((FrameworkElement)sender);
        }

        private void AccountGridViewItemGrid_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            ShowAccountGridViewContextMenu((FrameworkElement)sender);
        }

        private void CommonViewModel_SelectedAccountChanged(object sender, Core.SelectAccountEventArgs e)
        {
            TaskHelper.RunOnUIThreadAsync(() =>
            {
                try
                {
                    if (e.Account != null)
                    {
                        SemanticZoom.IsZoomedInViewActive = true;

                        if (!IsControlVisible(AccountsGridView.ContainerFromItem(e.Account) as FrameworkElement, AccountsGridView))
                        {
                            AccountsGridView.ScrollIntoView(e.Account, ScrollIntoViewAlignment.Leading);
                        }
                    }
                }
                catch { }
            });
        }

        private void ShowAccountGridViewContextMenu(FrameworkElement sender)
        {
            Account account = (Account)sender.DataContext;
            DeleteMenuFlyoutItem.CommandParameter = account;
            AccountGridViewContextMenu.ShowAt(sender);
        }

        /// <summary>
        /// Check whether a control is visible to the user in the specific container.
        /// </summary>
        /// <param name="element">The control to check.</param>
        /// <param name="container">Its container.</param>
        /// <returns>True if the control is visible.</returns>
        private static bool IsControlVisible(FrameworkElement element, FrameworkElement container)
        {
            if (element == null || container == null)
            {
                return false;
            }

            if (element.Visibility != Visibility.Visible)
            {
                return false;
            }

            var elementBounds = element.TransformToVisual(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            var containerBounds = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);

            return (elementBounds.Top < containerBounds.Bottom && elementBounds.Bottom > containerBounds.Top);
        }
    }
}
