using PaZword.Api;
using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Core.Threading;
using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PaZword.Core.Services.RecurrentTasks
{
    [Export(typeof(IRecurrentTask))]
    [ExportMetadata(nameof(RecurrentTaskMetadata.Name), Constants.InactivityDetectionRecurrentTask)]
    [ExportMetadata(nameof(RecurrentTaskMetadata.Recurrency), TaskRecurrency.FiveMinutes)]
    [Shared()]
    internal sealed class InactivityDetectionRecurrentTask : IRecurrentTask
    {
        private const string InactivityDetectedEvent = "InactivityDetectionRecurrentTask.InactivityDetected";

        private readonly ILogger _logger;
        private readonly ISettingsProvider _settingsProvider;

        private DateTime _lastUserInput;

        [ImportingConstructor]
        public InactivityDetectionRecurrentTask(
            ILogger logger,
            ISettingsProvider settingsProvider)
        {
            _logger = Arguments.NotNull(logger, nameof(logger));
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));

            _lastUserInput = DateTime.Now;

            TaskHelper.RunOnUIThreadAsync(() =>
            {
                CoreApplication.MainView.CoreWindow.KeyUp += CoreWindow_KeyUp;
                CoreApplication.MainView.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
            }).Forget();
        }

        public Task<bool> CanExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_settingsProvider.GetSetting(SettingsDefinitions.LockAfterInactivity) != InactivityTime.Never);
        }

        public Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            InactivityTime inactivityTimeSetting = _settingsProvider.GetSetting(SettingsDefinitions.LockAfterInactivity);
            var now = DateTime.Now;

            switch (inactivityTimeSetting)
            {
                case InactivityTime.FiveMinutes:
                    if (_lastUserInput < now - TimeSpan.FromMinutes(4))
                    {
                        _logger.LogEvent(InactivityDetectedEvent, "5 minutes");
                        return Task.FromResult((object)true);
                    }
                    break;

                case InactivityTime.TenMinutes:
                    if (_lastUserInput < now - TimeSpan.FromMinutes(9))
                    {
                        _logger.LogEvent(InactivityDetectedEvent, "10 minutes");
                        return Task.FromResult((object)true);
                    }
                    break;

                case InactivityTime.FifteenMinutes:
                    if (_lastUserInput < now - TimeSpan.FromMinutes(14))
                    {
                        _logger.LogEvent(InactivityDetectedEvent, "15 minutes");
                        return Task.FromResult((object)true);
                    }
                    break;

                case InactivityTime.ThirtyMinutes:
                    if (_lastUserInput < now - TimeSpan.FromMinutes(29))
                    {
                        _logger.LogEvent(InactivityDetectedEvent, "30 minutes");
                        return Task.FromResult((object)true);
                    }
                    break;

                case InactivityTime.OneHour:
                    if (_lastUserInput < now - TimeSpan.FromMinutes(59))
                    {
                        _logger.LogEvent(InactivityDetectedEvent, "1 hour");
                        return Task.FromResult((object)true);
                    }
                    break;

                case InactivityTime.Never:
                default:
                    break;
            }

            return Task.FromResult((object)false);
        }

        private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            _lastUserInput = DateTime.Now;
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            _lastUserInput = DateTime.Now;
        }
    }
}
