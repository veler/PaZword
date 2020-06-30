using PaZword.Api;
using System;
using System.Windows.Input;

namespace PaZword.Core.UI
{
    public sealed class ActionCommand<T> : ICommand
    {
        private readonly ILogger _logger;
        private readonly string _telemetryEvent;
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        internal ActionCommand(ILogger logger, string telemetryEvent, Action<T> execute, Func<T, bool> canExecute = null)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _telemetryEvent = Arguments.NotNullOrWhiteSpace(telemetryEvent, nameof(telemetryEvent));
            _execute = Arguments.NotNull(execute, nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _logger.LogEvent(_telemetryEvent, string.Empty);
            try
            {
                _execute((T)parameter);
            }
            catch (Exception ex)
            {
                _logger.LogFault(_telemetryEvent + ".Fault", string.Empty, ex);
            }

            RaiseCanExecuteChanged();
        }

        internal void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
