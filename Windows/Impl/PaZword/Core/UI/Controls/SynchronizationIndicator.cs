using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Core.Threading;
using PaZword.Localization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PaZword.Core.UI.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="SynchronizationIndicator"/>
    /// </summary>
    public class SynchronizationIndicator : Control
    {
        private readonly IRemoteSynchronizationService _remoteSynchronizationService;

        protected static readonly DependencyProperty StringsProperty
            = DependencyProperty.Register(
                "Strings",
                typeof(CoreStrings),
                typeof(SynchronizationIndicator),
                new PropertyMetadata(LanguageManager.Instance.Core));

        public static readonly DependencyProperty IsSynchronizingProperty
            = DependencyProperty.Register(
                nameof(IsSynchronizing),
                typeof(bool),
                typeof(SynchronizationIndicator),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the synchronization is in progress.
        /// </summary>
        public bool IsSynchronizing
        {
            get => (bool)GetValue(IsSynchronizingProperty);
            set => SetValue(IsSynchronizingProperty, value);
        }

        public static readonly DependencyProperty SynchronizationSucceededProperty
            = DependencyProperty.Register(
                nameof(SynchronizationSucceeded),
                typeof(bool),
                typeof(SynchronizationIndicator),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the synchronization succeeded.
        /// </summary>
        public bool SynchronizationSucceeded
        {
            get => (bool)GetValue(SynchronizationSucceededProperty);
            set => SetValue(SynchronizationSucceededProperty, value);
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="SynchronizationIndicator"/> class.
        /// </summary>
        public SynchronizationIndicator()
        {
            DefaultStyleKey = typeof(SynchronizationIndicator);

            var app = (IApp)App.Current;
            _remoteSynchronizationService = app.ExportProvider.GetExport<IRemoteSynchronizationService>();
            _remoteSynchronizationService.SynchronizationStarted += RemoteSynchronizationProvider_SynchronizationStarted;
            _remoteSynchronizationService.SynchronizationCompleted += RemoteSynchronizationProvider_SynchronizationCompleted;

            IsSynchronizing = _remoteSynchronizationService.IsSynchronizing;

            Unloaded += SynchronizationIndicator_Unloaded;
        }

        private void SynchronizationIndicator_Unloaded(object sender, RoutedEventArgs e)
        {
            _remoteSynchronizationService.SynchronizationStarted -= RemoteSynchronizationProvider_SynchronizationStarted;
            _remoteSynchronizationService.SynchronizationCompleted -= RemoteSynchronizationProvider_SynchronizationCompleted;
        }

        private void RemoteSynchronizationProvider_SynchronizationCompleted(object sender, SynchronizationResultEventArgs e)
        {
            TaskHelper.RunOnUIThreadAsync(() =>
            {
                IsSynchronizing = false;
                SynchronizationSucceeded = e.Succeeded;
                if (SynchronizationSucceeded)
                {
                    VisualStateManager.GoToState(this, "SynchronizationSucceeded", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "SynchronizationFailed", true);
                }
            });
        }

        private void RemoteSynchronizationProvider_SynchronizationStarted(object sender, System.EventArgs e)
        {
            TaskHelper.RunOnUIThreadAsync(() =>
            {
                IsSynchronizing = true;
                SynchronizationSucceeded = true;
                VisualStateManager.GoToState(this, "Synchronizing", true);
            });
        }
    }
}
