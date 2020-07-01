using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Services;
using PaZword.Core.Threading;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace PaZword.Core.Services
{
    [Export(typeof(IRecurrentTaskService))]
    [Shared]
    internal sealed class RecurrentTaskService : IRecurrentTaskService, IDisposable
    {
        private const string RunTaskFaultEvent = "RecurrentTaskService.RunRecurrentTask.Fault";
        private const string RunTaskEvent = "RecurrentTaskService.RunRecurrentTask";

        private const string BrownBagRecurrentTaskNamePrefix = "RecurrentTask_";

        private readonly object _lock = new object();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly Dictionary<string, DateTime> _lastRunTaskTime = new Dictionary<string, DateTime>();
        private readonly ILogger _logger;
        private readonly IDataManager _dataManager;
        private readonly IEnumerable<Lazy<IRecurrentTask, RecurrentTaskMetadata>> _recurrentTasks;

        private readonly Dictionary<TaskRecurrency, TimeSpan> _taskRecurrencyTime = new Dictionary<TaskRecurrency, TimeSpan>
        {
            { TaskRecurrency.OneMinute, TimeSpan.FromMinutes(1) },
            { TaskRecurrency.FiveMinutes, TimeSpan.FromMinutes(5) },
            { TaskRecurrency.TenMinutes, TimeSpan.FromMinutes(10) },
            { TaskRecurrency.OneHour, TimeSpan.FromHours(1) },
            { TaskRecurrency.OneDay, TimeSpan.FromDays(1) }
        };

        private bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<RecurrentTaskEventArgs> TaskCompleted;

        [ImportingConstructor]
        public RecurrentTaskService(
            ILogger logger,
            IDataManager dataManager,
            [ImportMany] IEnumerable<Lazy<IRecurrentTask, RecurrentTaskMetadata>> recurrentTasks)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _dataManager = Arguments.NotNull(dataManager, nameof(dataManager));
            _recurrentTasks = Arguments.NotNull(recurrentTasks, nameof(recurrentTasks));

            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += Timer_Tick;
        }

        public void Dispose()
        {
            Pause();
            _dataManager.Dispose();
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    return;
                }

                _isRunning = true;

                TaskHelper.RunOnUIThreadAsync(() =>
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    Timer_Tick(this, null);
                    _timer.Start();
                }).Forget();
            }
        }

        public void Pause()
        {
            lock (_lock)
            {
                if (!_isRunning)
                {
                    return;
                }

                _isRunning = false;

                TaskHelper.RunOnUIThreadAsync(() =>
                {
                    _timer.Stop();

                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }).Forget();
            }
        }

        public void RunTaskExplicitly(string taskName)
        {
            lock (_lock)
            {
                if (!_isRunning)
                {
                    return;
                }

                Task.Run(async ()
                    => await RunRecurrentTaskAsync(
                        _recurrentTasks
                        .First(
                            task => string.Equals(task.Metadata.Name, taskName, StringComparison.Ordinal)))
                    .ConfigureAwait(false));
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            Task.Run(async () => await TimerTickAsync().ConfigureAwait(false));
        }

        private async Task TimerTickAsync()
        {
            foreach (Lazy<IRecurrentTask, RecurrentTaskMetadata> recurrentTask in _recurrentTasks)
            {
                if (recurrentTask.Metadata.Recurrency == TaskRecurrency.Manual)
                {
                    continue;
                }

                // If we never ran the task or if we ran it
                // or if it's been too long ago we ran it,
                // then run it.
                if (recurrentTask.Metadata.Recurrency == TaskRecurrency.OneMinute
                    || recurrentTask.Metadata.Recurrency == TaskRecurrency.FiveMinutes
                    || recurrentTask.Metadata.Recurrency == TaskRecurrency.TenMinutes)
                {
                    if (!_lastRunTaskTime.TryGetValue(recurrentTask.Metadata.Name, out DateTime lastRunDate)
                        || lastRunDate < DateTime.Now - _taskRecurrencyTime[recurrentTask.Metadata.Recurrency])
                    {
                        _lastRunTaskTime[recurrentTask.Metadata.Name] = DateTime.Now;
                        RunRecurrentTaskAsync(recurrentTask).ForgetSafely();
                    }
                }
                else if (_dataManager.HasUserDataBundleLoaded)
                {
                    string lastRunDataString = await _dataManager.GetBrownBagDataAsync(BrownBagRecurrentTaskNamePrefix + recurrentTask.Metadata.Name, _cancellationTokenSource.Token).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(lastRunDataString)
                         || (DateTime.TryParse(lastRunDataString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime lastRunDate)
                             && lastRunDate < DateTime.UtcNow - _taskRecurrencyTime[recurrentTask.Metadata.Recurrency]))
                    {
                        await _dataManager.SetBrownBagDataAsync(
                            BrownBagRecurrentTaskNamePrefix + recurrentTask.Metadata.Name,
                            DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                            _cancellationTokenSource.Token).ConfigureAwait(false);

                        RunRecurrentTaskAsync(recurrentTask).ForgetSafely();
                    }
                }
            }
        }

        private async Task RunRecurrentTaskAsync(Lazy<IRecurrentTask, RecurrentTaskMetadata> recurrentTask)
        {
            object result = null;
            var executed = false;
            try
            {
                if (await recurrentTask.Value.CanExecuteAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
                {
                    _logger.LogEvent(RunTaskEvent, $"Task: {recurrentTask.Metadata.Name}");
                    executed = true;
                    result = await recurrentTask.Value.ExecuteAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogFault(RunTaskFaultEvent, $"Failed to run recurrent task '{recurrentTask.Metadata.Name}'.", ex);
            }
            finally
            {
                if (executed)
                {
                    await TaskHelper.RunOnUIThreadAsync(() =>
                    {
                        TaskCompleted?.Invoke(this, new RecurrentTaskEventArgs(recurrentTask.Metadata.Name, result));
                    }).ConfigureAwait(false);
                }
            }
        }
    }
}
