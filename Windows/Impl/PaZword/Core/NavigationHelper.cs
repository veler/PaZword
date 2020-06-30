using PaZword.Core.Threading;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Core
{
    /// <summary>
    /// Provides a set of methods designed to manage the navigation into a window.
    /// </summary>
    internal sealed class NavigationHelper
    {
        private readonly Frame _frame;
        private bool _isNavigating;

        /// <summary>
        /// Occurs when the page that is being navigated to has been found and is available from the Frame.Content property, although it may not have completed loading.
        /// </summary>
        internal event EventHandler<NavigationEventArgs> Navigated;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationHelper"/> class.
        /// </summary>
        /// <param name="frame">The <see cref="Frame"/> that must be used to manage the navigation.</param>
        internal NavigationHelper(Frame frame)
        {
            _frame = Arguments.NotNull(frame, nameof(frame));
            _frame.Navigated += Frame_Navigated;
        }

        private void Frame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            RaiseNavigated(sender, e.SourcePageType, e.Content, e.Parameter);
        }

        /// <summary>
        /// Navigates to the specified page.
        /// </summary>
        /// <typeparam name="T">The type of the page.</typeparam>
        /// <param name="alwaysNavigate">Defines whether the navigation should happen even if the current page is of the same type than <typeparamref name="T"/>.</param>
        /// <param name="parameter">(optional) the parameter to send during the navigation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task NavigateToPageAsync<T>(bool alwaysNavigate = false, bool giveFocus = false, object parameter = null)
        {
            if (_isNavigating)
            {
                return;
            }

            _isNavigating = true;
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                if (alwaysNavigate || _frame.CurrentSourcePageType != typeof(T))
                {
                    _frame.Navigate(typeof(T), parameter);
                }
                else
                {
                    // The current page is already the desired one. Let's just raise the event to transfer the parameters.
                    RaiseNavigated(_frame, typeof(T), _frame.Content, parameter);
                }

                if (giveFocus && _frame.Content != null && _frame.Content is Control frameContent)
                {
                    frameContent.Focus(FocusState.Keyboard);
                }
            }).ConfigureAwait(false);
        }

        private void RaiseNavigated(object sender, Type sourcePageType, object content, object parameter)
        {
            TaskHelper.RunOnUIThreadAsync(() =>
            {
                Navigated?.Invoke(sender, new NavigationEventArgs(sourcePageType, content, parameter));
                _isNavigating = false;
            });
        }
    }
}
