using PaZword.Core.Threading;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Core.UI.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="EditableHyperlink"/>
    /// </summary>
    public sealed class EditableHyperlink : EditableControlBase
    {
        private const string HyperlinkButton = "HyperlinkButton";

        private bool _layoutUpdatedHandled;

        /// <summary>
        /// Initialize a new instance of the <see cref="EditableHyperlink"/> class.
        /// </summary>
        public EditableHyperlink()
        {
            DefaultStyleKey = typeof(EditableHyperlink);

            LayoutUpdated += EditableHyperlink_LayoutUpdated;
        }

        private void EditableHyperlink_LayoutUpdated(object sender, object e)
        {
            if (_layoutUpdatedHandled)
            {
                return;
            }

            var hyperlinkButton = (HyperlinkButton)GetTemplateChild(HyperlinkButton);
            if (hyperlinkButton != null)
            {
                hyperlinkButton.Click += HyperlinkButton_Click;
                _layoutUpdatedHandled = true;
            }
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string uriToLaunch = Text.ToUnsecureString();
                var uri = new Uri(uriToLaunch);

                Windows.System.Launcher.LaunchUriAsync(uri).AsTask().Forget();
            }
            catch { }
        }
    }
}
