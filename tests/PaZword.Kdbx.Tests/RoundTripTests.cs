using PaZword.Kdbx.Models;
using PaZword.Kdbx.Reader;
using PaZword.Kdbx.Writer;
using PaZword.Kdbx.KeyDerivation;
using System.Security.Cryptography;
using System.Text;

namespace PaZword.Kdbx.Tests;

/// <summary>
/// End-to-end round-trip tests. Build a document → write to a stream → read
/// the stream back → assert document-model equality.
/// </summary>
public sealed class RoundTripTests
{
    private static async Task<KdbxDocument> RoundTrip(KdbxDocument source, string password = TestVaults.MasterPassword)
    {
        // Use a small KDF in the header for write so tests complete fast even
        // though the writer enforces the production floor by default. We
        // construct a header with floor-grade params (writer will validate).
        var writer = new KdbxWriter();
        var reader = new KdbxReader();

        var composite = CompositeKey.Build(password);
        try
        {
            using var ms = new MemoryStream();
            await writer.WriteAsync(source, ms, composite);
            ms.Position = 0;
            return await reader.ReadAsync(ms, composite);
        }
        finally { CryptographicOperations.ZeroMemory(composite); }
    }

    [Fact]
    public async Task Simple_Roundtrip_PreservesStructureAndProtectedFields()
    {
        var src = TestVaults.BuildSimpleVault();
        var round = await RoundTrip(src);

        // Meta
        Assert.Equal(src.Meta.DatabaseName, round.Meta.DatabaseName);
        Assert.Equal(src.Meta.DatabaseDescription, round.Meta.DatabaseDescription);
        Assert.Equal(src.Meta.DefaultUserName, round.Meta.DefaultUserName);

        AssertGroupEqual(src.Root!, round.Root!);

        // Custom icons
        Assert.Equal(src.CustomIcons.Count, round.CustomIcons.Count);
        for (int i = 0; i < src.CustomIcons.Count; i++)
        {
            Assert.Equal(src.CustomIcons[i].Uuid, round.CustomIcons[i].Uuid);
            Assert.Equal(src.CustomIcons[i].Data, round.CustomIcons[i].Data);
            Assert.Equal(src.CustomIcons[i].Name, round.CustomIcons[i].Name);
        }

        // Binaries
        Assert.Equal(src.Binaries.Count, round.Binaries.Count);
        for (int i = 0; i < src.Binaries.Count; i++)
            Assert.Equal(src.Binaries[i].Data, round.Binaries[i].Data);
    }

    [Fact]
    public async Task ReadWriteRead_IsIdempotent()
    {
        var src = TestVaults.BuildSimpleVault();
        var once = await RoundTrip(src);
        var twice = await RoundTrip(once);
        AssertGroupEqual(once.Root!, twice.Root!);
    }

    [Fact]
    public async Task Large_500Entries_Roundtrip()
    {
        var src = TestVaults.BuildLargeVault(500);
        var round = await RoundTrip(src);
        Assert.Equal(src.Root!.Entries.Count, round.Root!.Entries.Count);
        for (int i = 0; i < src.Root.Entries.Count; i++)
        {
            Assert.Equal(src.Root.Entries[i].Strings["Title"].Value, round.Root.Entries[i].Strings["Title"].Value);
            Assert.Equal(src.Root.Entries[i].Strings["Password"].Value, round.Root.Entries[i].Strings["Password"].Value);
            Assert.True(round.Root.Entries[i].Strings["Password"].IsProtected);
        }
    }

    [Fact]
    public async Task LargeAttachment_4MiB_Roundtrips_Byte_For_Byte()
    {
        var src = TestVaults.BuildLargeAttachmentVault();
        var round = await RoundTrip(src);
        Assert.Equal(src.Binaries[0].Data.Length, round.Binaries[0].Data.Length);
        Assert.Equal(SHA256.HashData(src.Binaries[0].Data), SHA256.HashData(round.Binaries[0].Data));
    }

    [Fact]
    public async Task OtpField_Roundtrips_AsProtectedString()
    {
        var src = TestVaults.BuildSimpleVault();
        var round = await RoundTrip(src);
        var bank = round.Root!.Groups[0]; // "Bank"
        var entry = bank.Entries[0];
        Assert.True(entry.Strings.ContainsKey("otp"));
        Assert.True(entry.Strings["otp"].IsProtected);
        Assert.StartsWith("otpauth://", entry.Strings["otp"].Value);
    }

    [Fact]
    public async Task UnicodeAndProtectedFields_Roundtrip()
    {
        var src = TestVaults.BuildSimpleVault();
        var round = await RoundTrip(src);
        var web = round.Root!.Groups[1];
        var edge = web.Entries[2];
        Assert.Equal("Edge Case — unicode 名字 🔐", edge.Strings["Title"].Value);
        Assert.Equal("naïve-π-α", edge.Strings["Password"].Value);
        Assert.True(edge.Strings["Password"].IsProtected);
    }

    [Fact]
    public async Task WrongPassword_Throws_Integrity()
    {
        var src = TestVaults.BuildSimpleVault();
        var writer = new KdbxWriter();
        var reader = new KdbxReader();
        var goodComposite = CompositeKey.Build(TestVaults.MasterPassword);
        try
        {
            using var ms = new MemoryStream();
            await writer.WriteAsync(src, ms, goodComposite);
            ms.Position = 0;
            var badComposite = CompositeKey.Build("wrong-password");
            try
            {
                await Assert.ThrowsAsync<KdbxIntegrityException>(async () =>
                    await reader.ReadAsync(ms, badComposite));
            }
            finally { CryptographicOperations.ZeroMemory(badComposite); }
        }
        finally { CryptographicOperations.ZeroMemory(goodComposite); }
    }

    private static void AssertGroupEqual(KdbxGroup a, KdbxGroup b)
    {
        Assert.Equal(a.Uuid, b.Uuid);
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Notes ?? string.Empty, b.Notes ?? string.Empty);
        Assert.Equal(a.IconId, b.IconId);
        Assert.Equal(a.Entries.Count, b.Entries.Count);
        Assert.Equal(a.Groups.Count, b.Groups.Count);
        for (int i = 0; i < a.Entries.Count; i++) AssertEntryEqual(a.Entries[i], b.Entries[i]);
        for (int i = 0; i < a.Groups.Count; i++) AssertGroupEqual(a.Groups[i], b.Groups[i]);
    }

    private static void AssertEntryEqual(KdbxEntry a, KdbxEntry b)
    {
        Assert.Equal(a.Uuid, b.Uuid);
        Assert.Equal(a.Strings.Count, b.Strings.Count);
        foreach (var kv in a.Strings)
        {
            Assert.True(b.Strings.ContainsKey(kv.Key), $"Missing key '{kv.Key}'.");
            Assert.Equal(kv.Value.Value, b.Strings[kv.Key].Value);
            Assert.Equal(kv.Value.IsProtected, b.Strings[kv.Key].IsProtected);
        }
        Assert.Equal(a.Binaries.Count, b.Binaries.Count);
        foreach (var kv in a.Binaries)
        {
            Assert.True(b.Binaries.ContainsKey(kv.Key));
            Assert.Equal(kv.Value.PoolIndex, b.Binaries[kv.Key].PoolIndex);
        }
    }
}
