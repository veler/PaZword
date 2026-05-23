using PaZword.Core.Vault;
using PaZword.Kdbx;

namespace PaZword.Kdbx.Tests;

/// <summary>
/// Glue tests for <see cref="IVaultService"/> (PaZword.Core).
/// </summary>
public sealed class VaultServiceTests
{
    [Fact]
    public async Task SaveAndOpen_RoundTripsViaDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pazword-test-{Guid.NewGuid():N}.kdbx");
        try
        {
            var svc = new VaultService();
            var doc = TestVaults.BuildSimpleVault();
            using (var creds = VaultCredentials.FromPassword(TestVaults.MasterPassword))
            using (var handle = VaultHandle.CreateNew(path, doc, creds))
            {
                await svc.SaveAsync(handle);
            }

            using (var creds = VaultCredentials.FromPassword(TestVaults.MasterPassword))
            using (var opened = await svc.OpenAsync(path, creds))
            {
                Assert.Equal(doc.Meta.DatabaseName, opened.Document.Meta.DatabaseName);
                Assert.Equal(doc.Root!.Entries.Count, opened.Document.Root!.Entries.Count);
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Open_WithWrongPassword_ThrowsIntegrity()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pazword-bad-{Guid.NewGuid():N}.kdbx");
        try
        {
            var svc = new VaultService();
            var doc = TestVaults.BuildSimpleVault();
            using (var creds = VaultCredentials.FromPassword("right"))
            using (var handle = VaultHandle.CreateNew(path, doc, creds))
            {
                await svc.SaveAsync(handle);
            }

            using var bad = VaultCredentials.FromPassword("wrong");
            await Assert.ThrowsAnyAsync<KdbxIntegrityException>(() => svc.OpenAsync(path, bad));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void VaultHandle_Dispose_IsIdempotent()
    {
        var doc = TestVaults.BuildSimpleVault();
        using var creds = VaultCredentials.FromPassword("hello");
        var handle = VaultHandle.CreateNew("/tmp/x.kdbx", doc, creds);
        handle.Dispose();
        handle.Dispose(); // second dispose is a no-op
    }
}
