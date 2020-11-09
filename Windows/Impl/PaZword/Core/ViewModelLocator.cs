using PaZword.Api;
using PaZword.ViewModels;
using PaZword.ViewModels.Dialog;
using PaZword.ViewModels.Other;
using System.Composition.Hosting;

namespace PaZword.Core
{
    public sealed class ViewModelLocator
    {
        private CompositionHost ExportProvider => ((IApp)Windows.UI.Xaml.Application.Current).ExportProvider;

        public TitleBarViewModel TitleBar => ExportProvider?.GetExport<TitleBarViewModel>();

        public MainPageViewModel MainPage => ExportProvider?.GetExport<MainPageViewModel>();

        public SettingsPageViewModel SettingsPage => ExportProvider?.GetExport<SettingsPageViewModel>();

        public CategoryPageViewModel CategoryPage => ExportProvider?.GetExport<CategoryPageViewModel>();

        // Getting a new instance everytime this property is called.
        public AccountPageViewModel AccountPage => ExportProvider?.GetExport<AccountPageViewModel>();

        // Getting a new instance everytime this property is called.
        public FirstStartExperiencePageViewModel FirstStartExperiencePage => ExportProvider?.GetExport<FirstStartExperiencePageViewModel>();

        // Getting a new instance everytime this property is called.
        public AuthenticationPageViewModel AuthenticationPage => ExportProvider?.GetExport<AuthenticationPageViewModel>();

        // Getting a new instance everytime this property is called.
        public SetupTwoFactorAuthenticationDialogViewModel SetupTwoFactorAuthenticationDialog => ExportProvider?.GetExport<SetupTwoFactorAuthenticationDialogViewModel>();

        // Getting a new instance everytime this property is called.
        public BreachDiscoveredDialogViewModel BreachDiscoveredDialog => ExportProvider?.GetExport<BreachDiscoveredDialogViewModel>();

        // Getting a new instance everytime this property is called.
        public PasswordGeneratorDialogViewModel PasswordGeneratorDialog => ExportProvider?.GetExport<PasswordGeneratorDialogViewModel>();

        // Getting a new instance everytime this property is called.
        public CategoryNameDialogViewModel CategoryNameDialog => ExportProvider?.GetExport<CategoryNameDialogViewModel>();
    }
}
