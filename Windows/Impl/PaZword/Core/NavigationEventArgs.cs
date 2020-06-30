using System;
using Windows.UI.Xaml;

namespace PaZword.Core
{
    internal sealed class NavigationEventArgs : RoutedEventArgs
    {
        internal Type SourcePageType { get; }

        internal object Parameter { get; }

        internal object Content { get; }

        public NavigationEventArgs(Type sourcePageType, object content, object parameter)
        {
            SourcePageType = Arguments.NotNull(sourcePageType, nameof(sourcePageType));
            Content = Arguments.NotNull(content, nameof(content));
            Parameter = parameter;
        }
    }
}
