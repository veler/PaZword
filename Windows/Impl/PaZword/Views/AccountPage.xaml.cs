using Microsoft.Toolkit.Uwp.UI.Animations.Expressions;
using PaZword.Core.Threading;
using PaZword.Models;
using PaZword.ViewModels;
using System;
using System.Numerics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;

namespace PaZword.Views
{
    /// <summary>
    /// Interaction logic for AccountPage.xaml
    /// </summary>
    public sealed partial class AccountPage : Page
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        public AccountPageViewModel ViewModel => (AccountPageViewModel)DataContext;

        public AccountPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!(e.Parameter is AccountPageNavigationParameters args))
            {
                throw new ArgumentException("The page parameter isn't an Account.");
            }

            ViewModel.AccountDataAdded += ViewModel_AccountDataAdded;
            ViewModel.InitializeAsync(args).Forget();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeExpressionAnimations();
        }

        private void ViewModel_AccountDataAdded(object sender, EventArgs e)
        {
            TaskHelper.RunOnUIThreadAsync(() =>
            {
                // An AccountData has been added. Scroll to the bottom.
                AccountScrollViewer.UpdateLayout();
                AccountScrollViewer.ChangeView(null, AccountScrollViewer.ScrollableHeight, null, false);
            }).Forget();
        }

        private void InitializeExpressionAnimations()
        {
            try
            {
                // Get the PropertySet that contains the scroll values from MyScrollViewer
                var scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(AccountScrollViewer);
                var compositor = scrollerPropertySet.Compositor;

                // Create a PropertySet that has values to be referenced in the ExpressionAnimations below
                var props = compositor.CreatePropertySet();
                props.InsertScalar("progress", 0);
                props.InsertScalar("clampSize", 88);
                props.InsertScalar("scaleFactor", 0.3f);

                // Get references to our property sets for use with ExpressionNodes
                var scrollingProperties = scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();
                var properties = props.GetReference();
                var progressNode = properties.GetScalarProperty("progress");
                var clampSizeNode = properties.GetScalarProperty("clampSize");
                var scaleFactorNode = properties.GetScalarProperty("scaleFactor");

                // Create and start an ExpressionAnimation to track scroll progress over the desired distance
                var progressAnimation = ExpressionFunctions.Clamp(-scrollingProperties.Translation.Y / clampSizeNode, 0, 1);
                props.StartAnimation("progress", progressAnimation);

                // *** Scaling the header grid ***

                // Get the backing visual for the header so that its properties can be animated
                var headerVisual = ElementCompositionPreview.GetElementVisual(Header);

                // Create and start an ExpressionAnimation to clamp the header's offset to keep it onscreen
                var headerTranslationAnimation = ExpressionFunctions.Conditional(progressNode < 1, 0, -scrollingProperties.Translation.Y - clampSizeNode);
                headerVisual.StartAnimation("Offset.Y", headerTranslationAnimation);

                // Create and start an ExpressionAnimation to scale the header during overpan
                var headerScaleAnimation = ExpressionFunctions.Lerp(1, 1.25f, ExpressionFunctions.Clamp(scrollingProperties.Translation.Y / 50, 0, 1));
                headerVisual.StartAnimation("Scale.X", headerScaleAnimation);
                headerVisual.StartAnimation("Scale.Y", headerScaleAnimation);

                //Set the header's CenterPoint to ensure the overpan scale looks as desired
                headerVisual.CenterPoint = new Vector3((float)(Header.ActualWidth / 2), (float)Header.ActualHeight, 0);

                // *** Animating the account logo ***

                // Get the backing visual for the account logo visual so that its properties can be animated
                var accountLogoVisual = ElementCompositionPreview.GetElementVisual(HeaderAccountLogo);

                // Create and start an ExpressionAnimation to scale the account logo with scroll position
                var accountLogoScaleAnimation = ExpressionFunctions.Lerp(1, scaleFactorNode, progressNode);
                accountLogoVisual.StartAnimation("Scale.X", accountLogoScaleAnimation);
                accountLogoVisual.StartAnimation("Scale.Y", accountLogoScaleAnimation);

                // *** Animating the title ***

                // Get the backing visual for the title visual so that its properties can be animated
                var titleVisual = ElementCompositionPreview.GetElementVisual(HeaderTitle);

                // Create and start an ExpressionAnimation to scale the title with scroll position
                var titleOffsetAnimation = progressNode * -88;
                titleVisual.StartAnimation("Offset.X", titleOffsetAnimation);

                // *** Animating the subtitle ***

                // Get the backing visual for the subtitle visual so that its properties can be animated
                var subtitleVisual = ElementCompositionPreview.GetElementVisual(HeaderSubtitle);

                // Create and start an ExpressionAnimation to scale the subtitle with scroll position
                var subtitleScaleAnimation = ExpressionFunctions.Lerp(1, scaleFactorNode, progressNode);
                subtitleVisual.StartAnimation("Scale.X", subtitleScaleAnimation);
                subtitleVisual.StartAnimation("Scale.Y", subtitleScaleAnimation);

                // Create an ExpressionAnimation that moves between 1 and 0 with scroll progress, to be used for subtitle opacity
                var subtitleOpacityAnimation = ExpressionFunctions.Clamp(1 - (progressNode * 2), 0, 1);
                subtitleVisual.StartAnimation("Opacity", subtitleOpacityAnimation);

                // *** Animating the editable subtitle ***

                // Get the backing visual for the subtitle visual so that its properties can be animated
                var editableSubtitleVisual = ElementCompositionPreview.GetElementVisual(HeaderEditableSubtitle);

                // Create and start an ExpressionAnimation to scale the subtitle with scroll position
                var editableSubtitleScaleAnimation = ExpressionFunctions.Lerp(1, scaleFactorNode, progressNode);
                editableSubtitleVisual.StartAnimation("Scale.X", editableSubtitleScaleAnimation);
                editableSubtitleVisual.StartAnimation("Scale.Y", editableSubtitleScaleAnimation);

                // Create an ExpressionAnimation that moves between 1 and 0 with scroll progress, to be used for subtitle opacity
                var editableSubtitleOpacityAnimation = ExpressionFunctions.Clamp(1 - (progressNode * 2), 0, 1);
                editableSubtitleVisual.StartAnimation("Opacity", editableSubtitleOpacityAnimation);

                // *** Animating the buttons bar ***

                // Get the backing visual for the buttons bar visual so that its properties can be animated
                var buttonsBarVisual = ElementCompositionPreview.GetElementVisual(HeaderButtonsBar);

                // Create and start an ExpressionAnimation to scale the buttons bar with scroll position
                var buttonsBarOffsetAnimation = progressNode * -88;
                buttonsBarVisual.StartAnimation("Offset.Y", buttonsBarOffsetAnimation);

                // Create an ExpressionAnimation that moves between 1 and 0 with scroll progress, to be used for buttons bar opacity
                var buttonsBarOpacityAnimation = ExpressionFunctions.Clamp(1 - (progressNode * 2), 0, 1);
                buttonsBarVisual.StartAnimation("Opacity", buttonsBarOpacityAnimation);

                // *** Animating the header content ***

                // Get the backing visual for the header content visual so that its properties can be animated
                var headerContentVisual = ElementCompositionPreview.GetElementVisual(HeaderContent);

                // When the header stops scrolling it is 88 pixels offscreen. We want the text header to end up with 24 pixels of its content
                // offscreen which means it needs to go from offset 0 to 88 as we traverse through the scrollable region
                var headerContentOffsetAnimation = progressNode * 88;
                headerContentVisual.StartAnimation("Offset.Y", headerContentOffsetAnimation);
            }
            catch
            {

            }
        }
    }
}
