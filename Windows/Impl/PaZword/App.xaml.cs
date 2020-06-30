using PaZword.Api;
using PaZword.Api.Settings;
using PaZword.Core;
using PaZword.ViewModels;
using PaZword.Views.Other;
using System;
using System.Composition.Hosting;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PaZword
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application, IApp, IDisposable
    {
        private readonly MefHost _mefHost = new MefHost();

        /// <summary>
        /// Gets the MEF export provider.
        /// </summary>
        public CompositionHost ExportProvider => _mefHost.ExportProvider;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            _mefHost.InitializeMef(); // Initialize MEF.

            InitializeComponent();
            DebugSettings.BindingFailed += DebugSettings_BindingFailed;

            UnhandledException += OnUnhandledException;
            Suspending += OnSuspending;

            UpdateColorTheme();
        }

        public void Dispose()
        {
            _mefHost.Dispose();
        }

        public void ResetMef()
        {
            throw new InvalidOperationException($"{nameof(ResetMef)} should only be called in Unit Tests.");
        }

        public void UpdateColorTheme()
        {
            if (new AccessibilitySettings().HighContrast)
            {
                return;
            }

            ElementTheme theme = ExportProvider.GetExport<ISettingsProvider>().GetSetting(SettingsDefinitions.Theme);

            if (theme == ElementTheme.Default)
            {
                // Detects the Windows's theme
                var uiSettings = new UISettings();
                var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
                if (color == Colors.Black)
                {
                    theme = ElementTheme.Dark;
                }
                else
                {
                    theme = ElementTheme.Light;
                }
            }

            SetColorTheme(theme);
        }

        private void SetColorTheme(ElementTheme theme)
        {
            if (Window.Current.Content is FrameworkElement frameworkElement)
            {
                frameworkElement.RequestedTheme = theme;
                ExportProvider.GetExport<TitleBarViewModel>().SetupTitleBar();
            }
            else
            {
                if (theme == ElementTheme.Light)
                {
                    RequestedTheme = ApplicationTheme.Light;
                }
                else if (theme == ElementTheme.Dark)
                {
                    RequestedTheme = ApplicationTheme.Dark;
                }
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            OpenMainWindow(e);

            ExportProvider.GetExport<TitleBarViewModel>().SetupTitleBar();
        }

        private void OpenMainWindow(LaunchActivatedEventArgs e)
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    if (ExportProvider.GetExport<ISettingsProvider>().GetSetting(SettingsDefinitions.FirstStart))
                    {
                        rootFrame.Navigate(typeof(FirstStartExperiencePage), e.Arguments);
                    }
                    else
                    {
                        rootFrame.Navigate(typeof(AuthenticationPage), e.Arguments);
                        //rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    }
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // TODO
        }

        private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
        {
            // TODO: Log that
        }
    }
}
