namespace PaZword.Kdbx.Models;

/// <summary>
/// Represents a KDBX database document (top-level container).
/// </summary>
public sealed class KdbxDocument
{
    /// <summary>Parsed from the KDBX outer header.</summary>
    public KdbxHeader Header { get; init; } = new();

    /// <summary>Database metadata (parsed from <c>&lt;Meta&gt;</c>).</summary>
    public KdbxMeta Meta { get; set; } = new();

    /// <summary>Root group. Every entry lives inside a group tree rooted here.</summary>
    public KdbxGroup? Root { get; set; }

    /// <summary>Custom icons referenced by entries/groups.</summary>
    public IList<KdbxCustomIcon> CustomIcons { get; } = new List<KdbxCustomIcon>();

    /// <summary>Binary attachments (inner header pool).</summary>
    public IList<KdbxBinary> Binaries { get; } = new List<KdbxBinary>();
}

/// <summary>Outer header fields parsed from a KDBX 4 file.</summary>
public sealed class KdbxHeader
{
    public uint MajorVersion { get; init; }
    public uint MinorVersion { get; init; }
    public byte[]? CipherId { get; init; }        // 16-byte UUID
    public int CompressionFlags { get; init; }    // 0=none, 1=GZip
    public byte[]? MasterSeed { get; init; }      // 32 bytes
    public byte[]? EncryptionIV { get; init; }
    public KdbxKdfParameters? KdfParameters { get; init; }
    public byte[]? PublicCustomData { get; init; } // optional VariantMap, preserved opaque
}
