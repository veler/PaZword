using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using PaZword.App.Navigation;
using PaZword.App.Pages.VaultShell;
using PaZword.App.Services;
using PaZword.App.Shell;
using PaZword.App.ViewModels;
using PaZword.Core.Navigation;
using PaZword.Core.Vault;
using Windows.System;

namespace PaZword.App.Pages;

/// <summary>
/// The unlocked vault shell. Hosts a LeftCompact <see cref="NavigationView"/> with
/// inner <see cref="Frame"/>, registers itself as the <see cref="IShellHost"/>, and
/// drives the window title bar into <see cref="ShellChromeMode.Full"/>.
/// </summary>
public sealed partial class VaultShellPage : Page, IShellHost
{
    private readonly NavigationService _innerNav = new();
    private readonly IShellChromeService _chrome;
    private readonly IVaultSessionService _session;
    private readonly IVaultService _vault;
    private readonly INavigationService _rootNav;

    public VaultShellViewModel ViewModel { get; }

    public VaultShellPage()
    {
        ViewModel = App.Services.GetRequiredService<VaultShellViewModel>();
        _chrome = App.Services.GetRequiredService<IShellChromeService>();
        _session = App.Services.GetRequiredService<IVaultSessionService>();
        _vault = App.Services.GetRequiredService<IVaultService>();
        _rootNav = App.Services.GetRequiredService<INavigationService>();
        InitializeComponent();
        RegisterInnerPages();
        RegisterAccelerators();
    }

    private void RegisterInnerPages()
    {
        _innerNav.Initialize(ContentFrame);
        _innerNav.RegisterPage<AllEntriesPage>(PageKeys.AllEntries);
        _innerNav.RegisterPage<FavoritesPage>(PageKeys.Favorites);
        _innerNav.RegisterPage<TrashPage>(PageKeys.Trash);
        _innerNav.RegisterPage<SettingsPage>(PageKeys.Settings);
        _innerNav.RegisterPage<AboutPage>(PageKeys.About);
    }

    private void RegisterAccelerators()
    {
        // Ctrl+L — Lock
        KeyboardAccelerators.Add(new KeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.L, ScopeOwner = this });
        // Ctrl+F — Focus search
        KeyboardAccelerators.Add(new KeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.F, ScopeOwner = this });
        // Ctrl+K — Focus search (alternative)
        KeyboardAccelerators.Add(new KeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.K, ScopeOwner = this });
        // Ctrl+\ — Toggle pane
        KeyboardAccelerators.Add(new KeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control, Key = (VirtualKey)0xDC, ScopeOwner = this });
        // Ctrl+S — Save (no-op)
        KeyboardAccelerators.Add(new KeyboardAccelerator { Modifiers = VirtualKeyModifiers.Control, Key = VirtualKey.S, ScopeOwner = this });

        foreach (var acc in KeyboardAccelerators)
            acc.Invoked += OnAcceleratorInvoked;
    }

    private void OnAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        switch (sender.Key)
        {
            case VirtualKey.L: Lock(); break;
            case VirtualKey.F:
            case VirtualKey.K: FocusSearch(); break;
            case VirtualKey.S: ShowSaveNoOp(); break;
            case (VirtualKey)0xDC: TogglePane(); break;
            default: args.Handled = false; break;
        }
    }

    private void ShowSaveNoOp()
    {
        // T-2.11: Ctrl+S is a no-op for sprint 2; sprint 3 wires real save.
        // We surface a transient infobar via the chrome service host pattern, but for now
        // just write a debug breadcrumb — the inner pages don't yet host an InfoBar shelf.
        System.Diagnostics.Debug.WriteLine("[VaultShell] Ctrl+S — save no-op (sprint 3 wires real save).");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var session = _session.Current;
        if (session is null)
        {
            // Defensive: shouldn't happen — Locked page sets the session before navigating here.
            _rootNav.Reset(PageKeys.Welcome);
            return;
        }

        ViewModel.VaultFileName = System.IO.Path.GetFileName(session.Path);

        _chrome.SetVaultFileName(ViewModel.VaultFileName);
        _chrome.SetMode(ShellChromeMode.Full);
        _chrome.SetHost(this);

        // Default destination
        _innerNav.Reset(PageKeys.AllEntries);
        if (ShellNav.MenuItems[0] is NavigationViewItem first)
            ShellNav.SelectedItem = first;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _chrome.SetHost(null);
        _chrome.SetVaultFileName(null);
        _chrome.SetMode(ShellChromeMode.Brand);
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string key })
            _innerNav.NavigateTo(key, parameter: null, NavigationTransition.Entrance);
    }

    // ---- IShellHost ----

    public void TogglePane() => ShellNav.IsPaneOpen = !ShellNav.IsPaneOpen;

    public void Lock()
    {
        var handle = _session.Current;
        _ = handle is null ? Task.CompletedTask : _vault.CloseAsync(handle);
        _session.Clear();
        _rootNav.Reset(PageKeys.Welcome);
    }

    public void SwitchVault()
    {
        var handle = _session.Current;
        _ = handle is null ? Task.CompletedTask : _vault.CloseAsync(handle);
        _session.Clear();
        _rootNav.Reset(PageKeys.Welcome);
    }

    public void NavigateAbout()
    {
        // Programmatically reflect the selection in the rail so the user sees where they landed.
        foreach (var item in ShellNav.FooterMenuItems)
        {
            if (item is NavigationViewItem nvi && Equals(nvi.Tag, PageKeys.About))
            {
                ShellNav.SelectedItem = nvi;
                return;
            }
        }
        _innerNav.NavigateTo(PageKeys.About, parameter: null, NavigationTransition.Entrance);
    }

    public void FocusSearch()
    {
        // The search box lives in the window title bar. Have MainWindow handle this.
        // Routed via App through a quick service callback — see MainWindow.
        (Application.Current as App)?.MainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            (Application.Current as App)?.MainWindowImpl?.FocusTitleBarSearch();
        });
    }
}
