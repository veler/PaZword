using PaZword.ViewModels.Other;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Views.Other
{
    /// <summary>
    /// Interaction logic for AuthenticationPage.xaml
    /// </summary>
    public sealed partial class AuthenticationPage : Page
    {
        /// <summary>
        /// Gets the page's view model.
        /// </summary>
        internal AuthenticationPageViewModel ViewModel => (AuthenticationPageViewModel)DataContext;

        /// <summary>
        /// Initialize a new instance of the <see cref="AuthenticationPage"/> class.
        /// </summary>
        public AuthenticationPage()
        {
            InitializeComponent();

            ViewModel.Initialize();

            RecoveryKeyStackPanel.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, VisibilityChanged);
            RetryWindowsHelloStackPanel.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, VisibilityChanged);
            TwoFactorAuthenticationEmailAddressStackPanel.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, VisibilityChanged);
            TwoFactorAuthenticationGrid.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, VisibilityChanged);
        }

        private void VisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is Panel control
                && control.Visibility == Visibility.Visible)
            {
                // Slightly delay setting focus
                Task.Factory.StartNew(async () =>
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        if (control == RecoveryKeyStackPanel)
                        {
                            RecoveryKeyTextBox.Focus(FocusState.Keyboard);
                        }
                        else if (control == RetryWindowsHelloStackPanel)
                        {
                            RetryWindowsHelloAuthenticationButton.Focus(FocusState.Keyboard);
                        }
                        else if (control == TwoFactorAuthenticationGrid)
                        {
                            TwoFactorAuthenticationTextBox.Focus(FocusState.Keyboard);
                        }
                        else if (control == TwoFactorAuthenticationEmailAddressStackPanel)
                        {
                            TwoFactorAuthenticationEmailAddressTextBox.Focus(FocusState.Keyboard);
                        }
                    });
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
            }
        }
    }
}
