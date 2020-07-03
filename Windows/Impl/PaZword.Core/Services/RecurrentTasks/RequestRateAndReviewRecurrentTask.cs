using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Api.UI;
using PaZword.Core.Threading;
using PaZword.Localization;
using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace PaZword.Core.Services.RecurrentTasks
{
    [Export(typeof(IRecurrentTask))]
    [ExportMetadata(nameof(RecurrentTaskMetadata.Name), Constants.RequestRateAndReviewRecurrentTask)]
    [ExportMetadata(nameof(RecurrentTaskMetadata.Recurrency), TaskRecurrency.Manual)]
    [Shared()]
    internal sealed class RequestRateAndReviewRecurrentTask : IRecurrentTask
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IWindowManager _windowManager;

        [ImportingConstructor]
        public RequestRateAndReviewRecurrentTask(
            ISettingsProvider settingsProvider,
            IWindowManager windowManager)
        {
            _settingsProvider = Arguments.NotNull(settingsProvider, nameof(settingsProvider));
            _windowManager = Arguments.NotNull(windowManager, nameof(windowManager));
        }

        public Task<bool> CanExecuteAsync(CancellationToken cancellationToken)
        {
            int numberOfTimeTheAppStarted = _settingsProvider.GetSetting(SettingsDefinitions.NumberOfTimeTheAppStarted);
            bool lastAppShutdownWasACrash = _settingsProvider.GetSetting(SettingsDefinitions.LastAppShutdownWasCrash);
            bool appIsRated = _settingsProvider.GetSetting(SettingsDefinitions.UserRatedAndReviewedTheApp);

            // If there is an internet connection
            // And that the app isn't rated yet.
            // And that the last time the app closed, it wasn't due to a crash
            // And that the user runs the app for the 5th, 15th, 30th or 50th time
            // then ask the user if he wants to rate and review the app.

            return Task.FromResult(
                CoreHelper.IsInternetAccess()
                && !appIsRated
                && !lastAppShutdownWasACrash
                && (numberOfTimeTheAppStarted == 7
                    || numberOfTimeTheAppStarted == 15
                    || numberOfTimeTheAppStarted == 30
                    || numberOfTimeTheAppStarted == 50));
        }

        public async Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            // Do you enoy the app?
            ContentDialogResult dialogResult = await _windowManager.ShowMessageDialogAsync(
                LanguageManager.Instance.RateAndReviewApp.EnjoyingMessage,
                LanguageManager.Instance.RateAndReviewApp.EnjoyingCloseButtonText,
                LanguageManager.Instance.RateAndReviewApp.EnjoyingPrimaryButtonText,
                title: LanguageManager.Instance.RateAndReviewApp.EnjoyingTitle,
                defaultButton: ContentDialogButton.Primary)
                .ConfigureAwait(true);

            if (dialogResult == ContentDialogResult.Primary)
            {
                // Yes!
                // Would you mind rating it?
                dialogResult = await _windowManager.ShowMessageDialogAsync(
                    LanguageManager.Instance.RateAndReviewApp.RateMessage,
                    LanguageManager.Instance.RateAndReviewApp.RateCloseButtonText,
                    LanguageManager.Instance.RateAndReviewApp.RatePrimaryButtonText,
                    LanguageManager.Instance.RateAndReviewApp.RateSecondaryButtonText,
                    LanguageManager.Instance.RateAndReviewApp.RateTitle,
                    defaultButton: ContentDialogButton.Primary)
                    .ConfigureAwait(true);

                if (dialogResult == ContentDialogResult.Primary)
                {
                    // Yes!
                    StoreContext storeContext = StoreContext.GetDefault();

                    StoreRateAndReviewResult result = await TaskHelper.RunOnUIThreadAsync(async () =>
                    {
                        return await storeContext.RequestRateAndReviewAppAsync();
                    }).ConfigureAwait(false);

                    if (result.Status == StoreRateAndReviewStatus.Succeeded)
                    {
                        // do not ask again.
                        _settingsProvider.SetSetting(SettingsDefinitions.UserRatedAndReviewedTheApp, true);
                    }
                }
                else if (dialogResult == ContentDialogResult.Secondary)
                {
                    // Maybe later
                }
                else
                {
                    // No, thanks
                    // do not ask again.
                    _settingsProvider.SetSetting(SettingsDefinitions.UserRatedAndReviewedTheApp, true);
                }
            }
            else
            {
                // Not really
                // Would you give a feedback?
                dialogResult = await _windowManager.ShowMessageDialogAsync(
                    LanguageManager.Instance.RateAndReviewApp.FeedbackMessage,
                    LanguageManager.Instance.RateAndReviewApp.FeedbackCloseButtonText,
                    LanguageManager.Instance.RateAndReviewApp.FeedbackPrimaryButtonText,
                    title: LanguageManager.Instance.RateAndReviewApp.FeedbackTitle,
                    defaultButton: ContentDialogButton.Primary)
                    .ConfigureAwait(true);

                if (dialogResult == ContentDialogResult.Primary)
                {
                    // Ok, sure
                    await TaskHelper.RunOnUIThreadAsync(async () =>
                    {
                        await Launcher.LaunchUriAsync(new Uri("https://github.com/veler/pazword/issues/new/choose"));
                    }).ConfigureAwait(false);
                }

                // do not ask again.
                _settingsProvider.SetSetting(SettingsDefinitions.UserRatedAndReviewedTheApp, true);
            }

            return null;
        }
    }
}
