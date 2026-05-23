namespace PaZword.Kdbx.Models;

/// <summary>
/// A custom icon stored in the KDBX XML meta section.
/// Referenced by <see cref="KdbxEntry.CustomIconUuid"/> or <see cref="KdbxGroup.CustomIconUuid"/>.
/// </summary>
public sealed class KdbxCustomIcon
{
    public Guid Uuid { get; init; }

    /// <summary>PNG data for the icon.</summary>
    public byte[] Data { get; init; } = Array.Empty<byte>();

    /// <summary>Optional display name (KDBX 4.1+).</summary>
    public string? Name { get; init; }

    /// <summary>Optional last-modification timestamp (KDBX 4.1+).</summary>
    public DateTime? LastModificationTime { get; init; }
}
