using PaZword.Core.Threading;
using PaZword.Localization;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;

namespace PaZword.Core
{
    /// <summary>
    /// Provides a basic <see cref="INotifyPropertyChanged"/> implementation.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal async void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }).ConfigureAwait(false);
        }

        public ViewModelBase()
        {
            if (DesignMode.DesignModeEnabled || DesignMode.DesignMode2Enabled)
            {
                LanguageManager.Instance.SetCurrentCulture(new CultureInfo("en"));
            }
        }
    }
}
