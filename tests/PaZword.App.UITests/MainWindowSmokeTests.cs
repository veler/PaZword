using System.Runtime.InteropServices;
using Xunit;

namespace PaZword.App.UITests;

/// <summary>
/// Sprint-0 smoke tests for the NativeAOT-published <c>PaZword.App.exe</c>.
///
/// The goal of T-0.2 is to prove the toolchain produces a working AOT binary,
/// not to drive end-user UI flows yet — there is no interactive UI surface to
/// drive in sprint 0 beyond an empty themed window. Full WinAppDriver / Appium
/// scenarios land in sprint 2 when the app shell exists and is MSIX-registered
/// for unpackaged-or-packaged identity.
///
/// Note on unpackaged launch: a WinAppSDK app's bare <c>.exe</c> cannot be
/// executed directly from a test harness without either (a) MSIX registration
/// or (b) the WindowsAppSDK Bootstrap API initialised in <c>Main</c>. Doing so
/// in sprint 0 would expand scope. Instead we assert on the artifact's shape,
/// which is the contract T-0.2 actually owns.
/// </summary>
public sealed class MainWindowSmokeTests
{
    [Fact]
    public void Aot_publish_produces_a_native_executable()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var exe = LocatePublishedExecutable();
        Assert.NotNull(exe);
        Assert.True(File.Exists(exe), $"Expected AOT exe at: {exe}");

        var info = new FileInfo(exe);

        // The AOT-compiled WinUI 3 app is a self-contained PE binary. It must
        // be substantially larger than a managed stub (~150 KB) because the
        // .NET runtime + framework is linked in. 1 MB is a generous lower
        // bound; current builds are ~5–7 MB.
        Assert.True(info.Length > 1_000_000,
            $"AOT exe is suspiciously small ({info.Length} bytes) — managed stub?");

        // It is a valid Win32 PE.
        Assert.True(IsPortableExecutable(exe), $"{exe} is not a valid PE binary.");

        // A NativeAOT build does NOT ship a managed System.Private.CoreLib.dll
        // alongside the .exe — that is the whole point. If it appears, the
        // publish silently fell back to non-AOT mode.
        var publishDir = Path.GetDirectoryName(exe)!;
        var coreLib = Path.Combine(publishDir, "System.Private.CoreLib.dll");
        Assert.False(File.Exists(coreLib),
            $"AOT publish leaked a managed runtime dll: {coreLib}.");
    }

    private static bool IsPortableExecutable(string path)
    {
        Span<byte> header = stackalloc byte[2];
        using var fs = File.OpenRead(path);
        return fs.Read(header) == 2 && header[0] == (byte)'M' && header[1] == (byte)'Z';
    }

    private static string? LocatePublishedExecutable()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "PaZword.slnx")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            return null;
        }

        var publishRoot = Path.Combine(
            current.FullName,
            "src", "PaZword.App", "bin", "Release");

        if (!Directory.Exists(publishRoot))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(publishRoot, "PaZword.App.exe", SearchOption.AllDirectories)
            .FirstOrDefault(p => p.Contains(Path.Combine("publish"), StringComparison.OrdinalIgnoreCase));
    }
}
