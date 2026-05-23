namespace PaZword.Kdbx.Models;

/// <summary>
/// A binary blob stored in the KDBX inner header binary pool.
/// Entries reference binaries by pool index.
/// </summary>
public sealed class KdbxBinary
{
    /// <summary>Zero-based index in the inner header binary pool.</summary>
    public int PoolIndex { get; init; }

    /// <summary>Whether this binary is protected by the inner random stream.</summary>
    public bool IsProtected { get; init; }

    /// <summary>Raw bytes (already decrypted from the inner stream if IsProtected).</summary>
    public byte[] Data { get; init; } = Array.Empty<byte>();
}
