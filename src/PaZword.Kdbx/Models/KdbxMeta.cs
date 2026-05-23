namespace PaZword.Kdbx.Models;

/// <summary>
/// KDBX <c>&lt;Meta&gt;</c> contents. Round-trip-relevant fields only — fields not
/// listed here (e.g. RecycleBin* state, HistoryMaxItems) are preserved opaque
/// via <see cref="UnknownXml"/> by the reader/writer.
/// </summary>
public sealed class KdbxMeta
{
    public string Generator { get; set; } = "PaZword";
    public string? DatabaseName { get; set; }
    public DateTime? DatabaseNameChanged { get; set; }
    public string? DatabaseDescription { get; set; }
    public DateTime? DatabaseDescriptionChanged { get; set; }
    public string? DefaultUserName { get; set; }
    public DateTime? DefaultUserNameChanged { get; set; }
    public int MaintenanceHistoryDays { get; set; } = 365;
    public string Color { get; set; } = string.Empty;
    public DateTime? MasterKeyChanged { get; set; }
    public long MasterKeyChangeRec { get; set; } = -1;
    public long MasterKeyChangeForce { get; set; } = -1;
    public bool RecycleBinEnabled { get; set; } = true;
    public Guid RecycleBinUuid { get; set; }
    public DateTime? RecycleBinChanged { get; set; }
    public Guid EntryTemplatesGroup { get; set; }
    public DateTime? EntryTemplatesGroupChanged { get; set; }
    public int HistoryMaxItems { get; set; } = 10;
    public long HistoryMaxSize { get; set; } = 6 * 1024 * 1024;
    public Guid LastSelectedGroup { get; set; }
    public Guid LastTopVisibleGroup { get; set; }

    public KdbxMemoryProtection MemoryProtection { get; set; } = new();

    /// <summary>Custom plug-in data (preserved as VariantMap bytes for round-trip).</summary>
    public IDictionary<string, object> CustomData { get; } = new Dictionary<string, object>(StringComparer.Ordinal);
}

/// <summary>
/// Per-field "protect in memory" hints emitted into the KDBX XML body.
/// Only <c>Password</c> defaults to protected; PaZword writes the standard
/// KeePassXC settings here.
/// </summary>
public sealed class KdbxMemoryProtection
{
    public bool ProtectTitle { get; set; }
    public bool ProtectUserName { get; set; }
    public bool ProtectPassword { get; set; } = true;
    public bool ProtectUrl { get; set; }
    public bool ProtectNotes { get; set; }
}
