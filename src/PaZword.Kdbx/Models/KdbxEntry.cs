namespace PaZword.Kdbx.Models;

/// <summary>
/// Represents a KDBX entry (<c>&lt;Entry&gt;</c> element).
/// String values are stored as protected strings in KDBX; the reader decrypts them
/// from the inner header's protected stream before populating <see cref="Strings"/>.
/// </summary>
public sealed class KdbxEntry
{
    public Guid Uuid { get; init; }
    public int IconId { get; set; }
    public Guid? CustomIconUuid { get; set; }
    public string? ForegroundColor { get; set; }
    public string? BackgroundColor { get; set; }
    public DateTime? CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public DateTime? LastAccessTime { get; set; }
    public DateTime? ExpiryTime { get; set; }
    public bool Expires { get; set; }
    public int UsageCount { get; set; }

    /// <summary>
    /// String fields keyed by field name. Standard names: Title, UserName, Password, URL, Notes.
    /// TOTP is stored as "otp" (KeePassXC convention) per decision D-005.
    /// </summary>
    public IDictionary<string, KdbxProtectedString> Strings { get; } =
        new Dictionary<string, KdbxProtectedString>(StringComparer.Ordinal);

    /// <summary>Binary (file attachment) references keyed by attachment name.</summary>
    public IDictionary<string, KdbxBinaryRef> Binaries { get; } =
        new Dictionary<string, KdbxBinaryRef>(StringComparer.Ordinal);

    /// <summary>Auto-type configuration (sprint 6+).</summary>
    public string? AutoTypeSequence { get; set; }

    public IList<KdbxEntryHistory> History { get; } = new List<KdbxEntryHistory>();
}

/// <summary>A string field value, optionally protected (encrypted in the inner stream).</summary>
public sealed class KdbxProtectedString
{
    public string Value { get; set; } = string.Empty;
    public bool IsProtected { get; init; }
}

/// <summary>Reference from an entry to a binary in the inner header binary pool.</summary>
public sealed class KdbxBinaryRef
{
    public int PoolIndex { get; init; }
    public bool IsProtected { get; init; }
}

/// <summary>Historical snapshot of an entry.</summary>
public sealed class KdbxEntryHistory
{
    public KdbxEntry? Entry { get; init; }
}
