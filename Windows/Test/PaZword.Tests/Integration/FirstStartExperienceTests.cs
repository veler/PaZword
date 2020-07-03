using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaZword.Api;
using PaZword.Api.Data;
using PaZword.Api.Security;
using PaZword.Api.Services;
using PaZword.Api.Settings;
using PaZword.Core.Data;
using PaZword.Core.Threading;
using PaZword.Localization;
using PaZword.Tests.Mocks;
using PaZword.ViewModels.Other;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace PaZword.Tests.Integration
{
    [TestClass]
    public class FirstStartExperienceTests
    {
        private IApp _app;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                // Do all the tests in English.
                LanguageManager.Instance.SetCurrentCulture(new CultureInfo("en"));

                _app = (IApp)Windows.UI.Xaml.Application.Current;

                _app.ResetMef();
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NewUserAsync()
        {
            var viewModel = await GetViewModelAsync().ConfigureAwait(false);

            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                viewModel.ContinueWelcomeCommand.Execute(null);
                Assert.AreEqual(string.Empty, viewModel.RecoveryKey);

                viewModel.NewUserCommand.Execute(null);
                Assert.AreNotEqual(string.Empty, viewModel.RecoveryKey);

                viewModel.BackCommand.Execute(null);
                Assert.AreEqual(FirstStartExperiencePageViewModel.StepNewOrReturningUser, viewModel.CurrentStepIndex);

                viewModel.NewUserCommand.Execute(null);
                Assert.IsFalse(viewModel.ContinueGenerateSecretKeyCommand.CanExecute(null));

                var recoveryKey = viewModel.RecoveryKey;
                viewModel.CopyRecoveryKeyCommand.Execute(null);
                Assert.IsTrue(viewModel.ContinueGenerateSecretKeyCommand.CanExecute(null));

                viewModel.ContinueGenerateSecretKeyCommand.Execute(null);

                viewModel.BackCommand.Execute(null);
                Assert.AreEqual(FirstStartExperiencePageViewModel.StepGenerateSecretKey, viewModel.CurrentStepIndex);
                Assert.IsTrue(viewModel.ContinueGenerateSecretKeyCommand.CanExecute(null));

                viewModel.BackCommand.Execute(null);
                Assert.AreEqual(FirstStartExperiencePageViewModel.StepNewOrReturningUser, viewModel.CurrentStepIndex);

                viewModel.NewUserCommand.Execute(null);
                Assert.IsFalse(viewModel.ContinueGenerateSecretKeyCommand.CanExecute(null));
                Assert.AreNotEqual(recoveryKey, viewModel.RecoveryKey);

                viewModel.CopyRecoveryKeyCommand.Execute(null);
                viewModel.ContinueGenerateSecretKeyCommand.Execute(null);

                viewModel.DoNotConnectRemoteStorageServiceCommand.Execute(null);
                viewModel.DoNotConnectRemoteStorageServiceCommand.WaitRunToCompletion();
                Assert.AreEqual(FirstStartExperiencePageViewModel.StepWindowsHello, viewModel.CurrentStepIndex);

                viewModel.BackCommand.Execute(null);
                Assert.AreEqual(FirstStartExperiencePageViewModel.StepRegisterToCloudService, viewModel.CurrentStepIndex);
            }).ConfigureAwait(false);

            viewModel.RegisterToRemoteStorageServiceCommand.Execute(
                new Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>(
                    () => new MockIRemoteStorageProvider(),
                    new RemoteStorageProviderMetadata
                    {
                        ProviderName = "Foo"
                    }));

            viewModel.RegisterToRemoteStorageServiceCommand.WaitRunToCompletion();

            Assert.AreEqual(FirstStartExperiencePageViewModel.StepWindowsHello, viewModel.CurrentStepIndex);
            Assert.IsFalse(viewModel.UseWindowsHello);

            viewModel.BackCommand.Execute(null);
            Assert.AreEqual(FirstStartExperiencePageViewModel.StepRegisterToCloudService, viewModel.CurrentStepIndex);

            viewModel.RegisterToRemoteStorageServiceCommand.Execute(
                new Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>(
                    () => new MockIRemoteStorageProvider(),
                    new RemoteStorageProviderMetadata
                    {
                        ProviderName = "Foo"
                    }));

            viewModel.RegisterToRemoteStorageServiceCommand.WaitRunToCompletion();

            viewModel.ContinueWindowsHelloCommand.Execute(null);

            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                viewModel.Dispose();
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ReturningUserAsync()
        {
            var encryption = _app.ExportProvider.GetExport<IEncryptionProvider>();
            var secretKeys = encryption.GenerateSecretKeys();
            encryption.SetSecretKeys(secretKeys);

            await _app.ExportProvider.GetExport<IDataManager>().ClearLocalDataAsync(CancellationToken.None).ConfigureAwait(false);
            await _app.ExportProvider.GetExport<IDataManager>().LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

            var viewModel = await GetViewModelAsync().ConfigureAwait(false);

            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                viewModel.ContinueWelcomeCommand.Execute(null);
                Assert.AreEqual(string.Empty, viewModel.RecoveryKey);

                viewModel.ReturningUserCommand.Execute(null);
                Assert.AreEqual(string.Empty, viewModel.RecoveryKey);
                Assert.AreEqual(FirstStartExperiencePageViewModel.StepSignInToCloudService, viewModel.CurrentStepIndex);

                viewModel.BackCommand.Execute(null);
                Assert.AreEqual(FirstStartExperiencePageViewModel.StepNewOrReturningUser, viewModel.CurrentStepIndex);

                viewModel.ReturningUserCommand.Execute(null);
            }).ConfigureAwait(false);

            viewModel.SignInToRemoteStorageServiceCommand.Execute(
                new Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>(
                    () => new MockIRemoteStorageProvider(),
                    new RemoteStorageProviderMetadata
                    {
                        ProviderName = "Foo"
                    }));

            viewModel.SignInToRemoteStorageServiceCommand.WaitRunToCompletion();

            await Task.Delay(2000).ConfigureAwait(false);

            Assert.AreEqual(FirstStartExperiencePageViewModel.StepEnterSecretKey, viewModel.CurrentStepIndex);
            viewModel.BackCommand.Execute(null);

            Assert.AreEqual(FirstStartExperiencePageViewModel.StepSignInToCloudService, viewModel.CurrentStepIndex);

            viewModel.SignInToRemoteStorageServiceCommand.Execute(
                new Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>(
                    () => new MockIRemoteStorageProvider(),
                    new RemoteStorageProviderMetadata
                    {
                        ProviderName = "Foo"
                    }));

            viewModel.SignInToRemoteStorageServiceCommand.WaitRunToCompletion();

            await Task.Delay(2000).ConfigureAwait(false);

            // Simulate result of synchronization by creating fake data with the expected secret key.
            encryption.SetSecretKeys(secretKeys);
            await _app.ExportProvider.GetExport<IDataManager>().ClearLocalDataAsync(CancellationToken.None).ConfigureAwait(false);
            await _app.ExportProvider.GetExport<IDataManager>().LoadOrCreateLocalUserDataBundleAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(FirstStartExperiencePageViewModel.StepEnterSecretKey, viewModel.CurrentStepIndex);
            viewModel.RecoveryKey = "hello";
            viewModel.RecoveryKeyChangedCommand.Execute(null);
            viewModel.RecoveryKeyChangedCommand.WaitRunToCompletion();
            Assert.IsTrue(viewModel.InvalidRecoveryKey);
            Assert.AreEqual(FirstStartExperiencePageViewModel.StepEnterSecretKey, viewModel.CurrentStepIndex);

            viewModel.RecoveryKey = encryption.EncodeSecretKeysToBase64(secretKeys);
            viewModel.RecoveryKeyChangedCommand.Execute(null);
            viewModel.RecoveryKeyChangedCommand.WaitRunToCompletion();
            Assert.IsFalse(viewModel.InvalidRecoveryKey);

            Assert.AreEqual(FirstStartExperiencePageViewModel.StepWindowsHello, viewModel.CurrentStepIndex);
            Assert.IsTrue(viewModel.UseWindowsHello);

            viewModel.ContinueWindowsHelloCommand.Execute(null);

            await TaskHelper.RunOnUIThreadAsync(() =>
            {
                viewModel.Dispose();
            }).ConfigureAwait(false);
        }

        private async Task<FirstStartExperiencePageViewModel> GetViewModelAsync()
        {
            FirstStartExperiencePageViewModel result = null;

            await TaskHelper.RunOnUIThreadAsync(() =>
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                result = new FirstStartExperiencePageViewModel(
                    _app.ExportProvider.GetExport<ILogger>(),
                    _app.ExportProvider.GetExport<IEncryptionProvider>(),
                    _app.ExportProvider.GetExport<ISettingsProvider>(),
                    _app.ExportProvider.GetExport<IWindowsHelloAuthProvider>(),
                    new MockIRemoteSynchronizationService(),
                    new DataManager(
                        _app.ExportProvider.GetExport<ILogger>(),
                        _app.ExportProvider.GetExport<IEncryptionProvider>(),
                        _app.ExportProvider.GetExport<ISerializationProvider>(),
                        new MockIRemoteSynchronizationService()),
                    _app.ExportProvider.GetExport<IRecurrentTaskService>(),
                    new List<Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>>
                 {
                    new Lazy<IRemoteStorageProvider, RemoteStorageProviderMetadata>(
                        () => new MockIRemoteStorageProvider(),
                        new RemoteStorageProviderMetadata
                        {
                            ProviderName = "Foo"
                        })
                 });
#pragma warning restore CA2000 // Dispose objects before losing scope
            }).ConfigureAwait(false);

            return result;
        }
    }
}
