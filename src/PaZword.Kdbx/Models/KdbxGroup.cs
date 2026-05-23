namespace PaZword.Kdbx.Models;

/// <summary>
/// Represents a group node in the KDBX XML tree (<c>&lt;Group&gt;</c> element).
/// </summary>
public sealed class KdbxGroup
{
    public Guid Uuid { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int IconId { get; set; }
    public Guid? CustomIconUuid { get; set; }
    public DateTime? CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public DateTime? LastAccessTime { get; set; }
    public DateTime? ExpiryTime { get; set; }
    public bool Expires { get; set; }

    public IList<KdbxGroup> Groups { get; } = new List<KdbxGroup>();
    public IList<KdbxEntry> Entries { get; } = new List<KdbxEntry>();
}
