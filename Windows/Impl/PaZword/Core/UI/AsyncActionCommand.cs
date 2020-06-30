using PaZword.Api;
using PaZword.Core.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PaZword.Core.UI
{
    public sealed class AsyncActionCommand<T> : ICommand, IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _telemetryEvent;
        private readonly Func<T, CancellationToken, Task> _execute;
        private readonly Func<T, bool> _canExecute;
        private readonly Action<Exception> _errorHandler;
        private readonly bool _canExecuteMultipleCallAtOnce;
        private readonly bool _isCancellable;
        private readonly bool _startOnNewThread;
        private readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(true);
        private readonly object _lock = new object();

        private int _isExecuting;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public event EventHandler CanExecuteChanged;

        internal AsyncActionCommand(
            ILogger logger,
            string telemetryEvent,
            Func<T, CancellationToken, Task> execute,
            Func<T, bool> canExecute = null,
            Action<Exception> errorHandler = null,
            bool canExecuteMultipleCallAtOnce = false,
            bool isCancellable = false,
            bool startOnNewThread = false)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _telemetryEvent = Arguments.NotNullOrWhiteSpace(telemetryEvent, nameof(telemetryEvent));
            _execute = Arguments.NotNull(execute, nameof(execute));

            if (canExecuteMultipleCallAtOnce && isCancellable)
            {
                throw new Exception($"Parameters '{nameof(canExecuteMultipleCallAtOnce)}' and '{nameof(isCancellable)}' should not be both true.");
            }

            _canExecute = canExecute;
            _errorHandler = errorHandler;
            _canExecuteMultipleCallAtOnce = canExecuteMultipleCallAtOnce;
            _isCancellable = isCancellable;
            _startOnNewThread = startOnNewThread;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _cancellationTokenSource.Dispose();
                _manualResetEvent.Dispose();
            }
        }

        public bool CanExecute(object parameter)
        {
            if (!_canExecuteMultipleCallAtOnce && !_isCancellable && _isExecuting > 0)
            {
                return false;
            }

            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            lock (_lock)
            {
                _manualResetEvent.Reset();
            }
            ExecuteAsync((T)parameter).ForgetSafely(_errorHandler);
        }

        public void Cancel()
        {
            lock (_lock)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }

        /// <summary>
        /// Waits for the command to run to completion.
        /// This is bad practice in production code but useful in unit tests.
        /// </summary>
        internal void WaitRunToCompletion()
        {
            _manualResetEvent.WaitOne();
        }

        internal void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task ExecuteAsync(T parameter)
        {
            if (!CanExecute(parameter))
            {
                lock (_lock)
                {
                    if (_isExecuting == 0)
                    {
                        _manualResetEvent.Set();
                    }
                }
                return;
            }

            if (_isCancellable)
            {
                Cancel();
            }

            Interlocked.Increment(ref _isExecuting);
            RaiseCanExecuteChanged();

            try
            {
                _logger.LogEvent(_telemetryEvent, string.Empty);
                var token = CancellationToken.None;

                if (_isCancellable)
                {
                    lock (_lock)
                    {
                        token = _cancellationTokenSource.Token;
                    }
                }

                if (_startOnNewThread)
                {
                    await Task.Run(async () => // this makes sure we get out of the UI thread.
                    {
                        TaskHelper.ThrowIfOnUIThread();
                        await _execute(parameter, token).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                else
                {
                    await _execute(parameter, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogFault(_telemetryEvent + ".Fault", string.Empty, ex);
                _errorHandler?.Invoke(ex);
            }
            finally
            {
                Interlocked.Decrement(ref _isExecuting);
                RaiseCanExecuteChanged();
                lock (_lock)
                {
                    if (_isExecuting == 0)
                    {
                        _manualResetEvent.Set();
                    }
                }
            }
        }
    }
}
