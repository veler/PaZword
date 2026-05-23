using PaZword.Crypto;

namespace PaZword.Kdbx.Models;

/// <summary>
/// KDF identifier constants as stored in the KDBX header VariantMap.
/// See docs/sprint-0/kdbx4-spec-notes.md §4.
/// </summary>
public static class KdbxKdfUuids
{
    /// <summary>AES-KDF (legacy, KDBX 3.1 compat).</summary>
    public static readonly Guid AesKdf = new("c9d9f39a-628a-4460-bf74-0d08c18a4fea");

    /// <summary>Argon2d (KDBX 4.0).</summary>
    public static readonly Guid Argon2d = new("ef636ddf-8c29-444b-91f7-a9a403e30a0c");

    /// <summary>Argon2id (KDBX 4.0, recommended).</summary>
    public static readonly Guid Argon2id = new("9e298b19-56db-4773-b23d-fc3ec6f0a1e6");
}

/// <summary>
/// KDF parameters extracted from the KDBX outer header VariantMap.
/// Populated by <see cref="Interfaces.IKdbxReader"/> after parsing the header.
/// </summary>
public sealed class KdbxKdfParameters
{
    /// <summary>UUID identifying the KDF algorithm.</summary>
    public Guid Uuid { get; init; }

    // Argon2 parameters ──────────────────────────────────────────────────────

    /// <summary>Salt (S parameter). 32 bytes for Argon2.</summary>
    public byte[]? Salt { get; init; }

    /// <summary>Parallelism (P). Number of lanes/threads.</summary>
    public uint Parallelism { get; init; }

    /// <summary>Memory cost (M) in kibibytes.</summary>
    public ulong MemoryKib { get; init; }

    /// <summary>Iteration count (T).</summary>
    public ulong Iterations { get; init; }

    /// <summary>Argon2 version (0x13 = 19 = v1.3).</summary>
    public uint Version { get; init; }

    // AES-KDF parameters ─────────────────────────────────────────────────────

    /// <summary>AES-KDF seed (32 bytes).</summary>
    public byte[]? AesSeed { get; init; }

    /// <summary>AES-KDF round count.</summary>
    public ulong AesRounds { get; init; }

    /// <summary>True if this is an Argon2-family KDF (id or d).</summary>
    public bool IsArgon2 => Uuid == KdbxKdfUuids.Argon2id || Uuid == KdbxKdfUuids.Argon2d;

    /// <summary>True if this is the recommended Argon2id KDF.</summary>
    public bool IsArgon2id => Uuid == KdbxKdfUuids.Argon2id;

    /// <summary>True if this is the AES-KDF (legacy).</summary>
    public bool IsAesKdf => Uuid == KdbxKdfUuids.AesKdf;

    /// <summary>
    /// Validate that this set of parameters satisfies the production write floor
    /// (Argon2id only — Argon2d / AES-KDF are read-only for compatibility).
    /// </summary>
    /// <exception cref="KdbxParameterFloorException">
    /// If the KDF is not Argon2id, or any of t / m / p is below the floor.
    /// </exception>
    public void ValidateForWrite()
    {
        if (!IsArgon2id)
            throw new KdbxParameterFloorException(
                "KDBX writer only emits Argon2id. Got UUID " + Uuid + ".");
        if (Salt is null || Salt.Length < 16)
            throw new KdbxParameterFloorException("Argon2id salt must be at least 16 bytes.");
        if (!KdfFloor.IsAtOrAboveFloor(Iterations, MemoryKib, Parallelism))
            throw new KdbxParameterFloorException(
                $"Argon2id parameters are below the production floor " +
                $"(t={Iterations}, m={MemoryKib} KiB, p={Parallelism}; " +
                $"floor: t>={KdfFloor.MinIterations}, " +
                $"m>={KdfFloor.MinMemoryKib} KiB, p>={KdfFloor.MinParallelism}).");
    }

    /// <summary>Default Argon2id parameters at the production floor.</summary>
    public static KdbxKdfParameters DefaultArgon2id(byte[] salt) => new()
    {
        Uuid = KdbxKdfUuids.Argon2id,
        Salt = salt,
        Iterations = (ulong)KdfFloor.MinIterations,
        MemoryKib = (ulong)KdfFloor.MinMemoryKib,
        Parallelism = (uint)KdfFloor.MinParallelism,
        Version = 0x13,
    };
}
