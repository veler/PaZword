using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using PaZword.App.Shell;
using PaZword.App.ViewModels;
using Windows.System;

namespace PaZword.App.Pages;

public sealed partial class LockedPage : Page
{
    public LockedViewModel ViewModel { get; }

    public LockedPage()
    {
        ViewModel = App.Services.GetRequiredService<LockedViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        App.Services.GetRequiredService<IShellChromeService>().SetMode(ShellChromeMode.Brand);

        if (e.Parameter is string path && !string.IsNullOrEmpty(path))
            ViewModel.Initialize(path);

        // PasswordBox default-focus per UX-2.
        DispatcherQueue.TryEnqueue(() => PasswordField.Focus(FocusState.Programmatic));
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.Password = PasswordField.Password;
    }

    private void OnPasswordKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel.UnlockCommand.CanExecute(null))
        {
            e.Handled = true;
            ViewModel.UnlockCommand.Execute(null);
        }
    }

    public PasswordRevealMode PasswordRevealMode(bool revealed) =>
        revealed ? Microsoft.UI.Xaml.Controls.PasswordRevealMode.Visible : Microsoft.UI.Xaml.Controls.PasswordRevealMode.Hidden;

    public Visibility RingVisibility(bool active) => active ? Visibility.Visible : Visibility.Collapsed;

    public string ErrorMessageWithAttempt(string? message, int count)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;
        return count > 1 ? $"{message} (Attempt {count})" : message;
    }

    public string KeyFileLabel(string? name, bool hasKey) =>
        hasKey ? (name ?? string.Empty) : "(none selected)";

    public Visibility KeyFileClearVisibility(bool hasKey) => hasKey ? Visibility.Visible : Visibility.Collapsed;
}
