using System.Reflection;
using Microsoft.UI.Xaml.Controls;

namespace PaZword.App.Pages.VaultShell;

public sealed partial class AboutPage : Page
{
    public AboutPage() => InitializeComponent();

    /// <summary>Version line shown under the brand mark.</summary>
    public string VersionLine =>
        $"Version {GetVersion()} · Sprint 2 — App shell";

    private static string GetVersion()
    {
        var asm = typeof(AboutPage).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return info?.InformationalVersion ?? asm.GetName().Version?.ToString() ?? "dev";
    }
}
