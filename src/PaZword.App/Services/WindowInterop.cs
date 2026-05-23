using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace PaZword.App.Services;

/// <summary>
/// Bridges WinUI windows to native HWNDs for APIs that don't yet have a
/// WindowId-aware overload (e.g. <c>FileOpenPicker</c>). Resolves the active
/// window via the running <see cref="App"/> instance — no statics-by-reflection,
/// AOT-clean.
/// </summary>
internal static class WindowInterop
{
    /// <summary>Returns the HWND for the app's main window. Throws if none is active.</summary>
    public static IntPtr MainWindowHandle()
    {
        var window = (Application.Current as App)?.MainWindow
            ?? throw new InvalidOperationException("Main window not available.");
        return WindowNative.GetWindowHandle(window);
    }

    /// <summary>
    /// Initializes a WinRT object that needs the window HWND (file pickers,
    /// folder pickers, certain dialogs).
    /// </summary>
    public static void InitializeWithMainWindow(object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        InitializeWithWindow.Initialize(target, MainWindowHandle());
    }
}
