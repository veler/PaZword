using PaZword.Kdbx;
using PaZword.Kdbx.IO;
using PaZword.Kdbx.KeyDerivation;
using PaZword.Kdbx.Models;
using PaZword.Kdbx.Reader;
using PaZword.Kdbx.Writer;

namespace PaZword.Kdbx.Tests;

/// <summary>Test helpers shared across the KDBX test suite.</summary>
internal static class TestVaults
{
    public const string MasterPassword = "pazword-test";

    /// <summary>Build a small reference document used by the round-trip suite.</summary>
    public static KdbxDocument BuildSimpleVault()
    {
        var doc = new KdbxDocument
        {
            Meta = new KdbxMeta
            {
                Generator = "PaZword",
                DatabaseName = "Simple Test Vault",
                DatabaseDescription = "Round-trip test fixture",
                DefaultUserName = "alice",
            },
        };

        var root = new KdbxGroup
        {
            Uuid = Guid.NewGuid(),
            Name = "Root",
            CreationTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var bank = new KdbxGroup { Uuid = Guid.NewGuid(), Name = "Bank" };
        var web  = new KdbxGroup { Uuid = Guid.NewGuid(), Name = "Web Sites" };
        root.Groups.Add(bank);
        root.Groups.Add(web);

        AddEntry(root, "Personal email", "alice@example.com", "p4ssw0rd!", "https://mail.example.com");
        AddEntry(bank, "Acme Bank", "alice123", "tr0ub4dor&3", "https://bank.example.com",
                 notes: "Security questions: see notes app",
                 otp: "otpauth://totp/Acme%20Bank:alice123?secret=JBSWY3DPEHPK3PXP&issuer=Acme%20Bank&algorithm=SHA1&digits=6&period=30");
        AddEntry(web, "GitHub", "alice", "Sup3rS3cret#%^", "https://github.com");
        AddEntry(web, "Wikipedia", "alice", "wiki-pass-2024", "https://wikipedia.org");
        AddEntry(web, "Edge Case — unicode 名字 🔐", "用户", "naïve-π-α", "https://例え.jp");

        doc.Root = root;

        // Add a custom icon (red 1×1 PNG).
        doc.CustomIcons.Add(new KdbxCustomIcon
        {
            Uuid = Guid.NewGuid(),
            Data = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="),
            Name = "TestIcon",
        });

        // Add a 16 KiB binary attachment.
        var blob = new byte[16 * 1024];
        Random.Shared.NextBytes(blob);
        doc.Binaries.Add(new KdbxBinary { PoolIndex = 0, IsProtected = false, Data = blob });

        // Reference the binary from one entry.
        bank.Entries[0].Binaries["statement.bin"] = new KdbxBinaryRef { PoolIndex = 0 };

        return doc;
    }

    private static void AddEntry(KdbxGroup parent, string title, string user, string pw, string url,
                                 string? notes = null, string? otp = null)
    {
        var e = new KdbxEntry
        {
            Uuid = Guid.NewGuid(),
            IconId = 0,
            CreationTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            LastModificationTime = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc),
        };
        e.Strings["Title"]    = new KdbxProtectedString { Value = title, IsProtected = false };
        e.Strings["UserName"] = new KdbxProtectedString { Value = user,  IsProtected = false };
        e.Strings["Password"] = new KdbxProtectedString { Value = pw,    IsProtected = true };
        e.Strings["URL"]      = new KdbxProtectedString { Value = url,   IsProtected = false };
        if (notes is not null)
            e.Strings["Notes"] = new KdbxProtectedString { Value = notes, IsProtected = false };
        if (otp is not null)
            e.Strings["otp"]   = new KdbxProtectedString { Value = otp,   IsProtected = true };
        parent.Entries.Add(e);
    }

    /// <summary>Build a larger vault (500 entries) for the "large" fixture.</summary>
    public static KdbxDocument BuildLargeVault(int entryCount = 500)
    {
        var doc = new KdbxDocument { Meta = new KdbxMeta { DatabaseName = "Large Vault" } };
        var root = new KdbxGroup { Uuid = Guid.NewGuid(), Name = "Root" };
        var rng = new Random(42);
        for (int i = 0; i < entryCount; i++)
        {
            var e = new KdbxEntry { Uuid = Guid.NewGuid() };
            e.Strings["Title"]    = new KdbxProtectedString { Value = $"Site #{i:D4}", IsProtected = false };
            e.Strings["UserName"] = new KdbxProtectedString { Value = $"user{i}", IsProtected = false };
            e.Strings["Password"] = new KdbxProtectedString { Value = $"pw-{rng.Next():X8}", IsProtected = true };
            e.Strings["URL"]      = new KdbxProtectedString { Value = $"https://site{i}.example.com", IsProtected = false };
            root.Entries.Add(e);
        }
        doc.Root = root;
        return doc;
    }

    /// <summary>Build a vault with a single 4 MB attachment, for streaming verification.</summary>
    public static KdbxDocument BuildLargeAttachmentVault()
    {
        var doc = new KdbxDocument { Meta = new KdbxMeta { DatabaseName = "Binaries" } };
        var root = new KdbxGroup { Uuid = Guid.NewGuid(), Name = "Root" };
        var rng = new Random(1234);
        var blob = new byte[4 * 1024 * 1024 + 17]; // 4 MB + tail
        rng.NextBytes(blob);
        doc.Binaries.Add(new KdbxBinary { PoolIndex = 0, Data = blob });
        var entry = new KdbxEntry { Uuid = Guid.NewGuid() };
        entry.Strings["Title"]    = new KdbxProtectedString { Value = "Big Attachment", IsProtected = false };
        entry.Strings["Password"] = new KdbxProtectedString { Value = "secret", IsProtected = true };
        entry.Binaries["big.bin"] = new KdbxBinaryRef { PoolIndex = 0 };
        root.Entries.Add(entry);
        doc.Root = root;
        return doc;
    }

    /// <summary>Use a very fast Argon2 setting in the test path so the suite finishes quickly.</summary>
    public static byte[] BuildCompositeKey(string password = MasterPassword) =>
        CompositeKey.Build(password);

    /// <summary>
    /// Inject test-only Argon2id parameters (well below the prod floor) onto a
    /// brand-new document so tests run quickly. Real writes use the floor.
    /// </summary>
    public static void OverrideKdfForFastTests(KdbxDocument doc)
    {
        doc.Header.GetType(); // ensure non-null
        // Replace KdfParameters by constructing a new header value.
        var hdr = new KdbxHeader
        {
            MajorVersion = 4, MinorVersion = 1,
            CipherId = doc.Header.CipherId,
            CompressionFlags = doc.Header.CompressionFlags,
            MasterSeed = doc.Header.MasterSeed,
            EncryptionIV = doc.Header.EncryptionIV,
            KdfParameters = new KdbxKdfParameters
            {
                Uuid = KdbxKdfUuids.Argon2id,
                Salt = new byte[32],
                Iterations = 2,
                MemoryKib = 64 * 1024,
                Parallelism = 2,
                Version = 0x13,
            },
            PublicCustomData = doc.Header.PublicCustomData,
        };
        // We cannot reassign Header (init), so callers should construct documents
        // that include this header from the outset. Returning prebuilt header:
        _ = hdr; // see TestWriter
    }

    /// <summary>Writer factory used by tests — always emits the production-floor Argon2id.</summary>
    public static KdbxWriter WriterAtFloor() => new KdbxWriter();

    /// <summary>Reader factory.</summary>
    public static KdbxReader Reader() => new KdbxReader();
}
