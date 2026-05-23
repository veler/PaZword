namespace PaZword.Crypto;

/// <summary>
/// Production parameter floor for the Argon2id KDF used by the KDBX writer.
/// Sprint-0 carryover (qa-report §4.1 T-2): refuse to <em>write</em> a vault below this
/// floor; <em>reading</em> below the floor is allowed but should emit an
/// <c>ILogger.LogWarning</c>.
/// </summary>
/// <remarks>
/// Values match the KeePassXC 2.7 default + RFC 9106 §4 second-recommended option:
/// <list type="bullet">
///   <item><description><see cref="MinIterations"/> = 2 (t)</description></item>
///   <item><description><see cref="MinMemoryKib"/> = 65_536 KiB = 64 MiB (m)</description></item>
///   <item><description><see cref="MinParallelism"/> = 2 (p)</description></item>
/// </list>
/// </remarks>
public static class KdfFloor
{
    /// <summary>Minimum Argon2id iteration count (t) for new vaults.</summary>
    public const int MinIterations = 2;

    /// <summary>Minimum Argon2id memory cost (m) in kibibytes for new vaults — 64 MiB.</summary>
    public const long MinMemoryKib = 64L * 1024;

    /// <summary>Minimum Argon2id parallelism (p) for new vaults.</summary>
    public const int MinParallelism = 2;

    /// <summary>
    /// Returns true if the given parameters meet the production floor.
    /// </summary>
    public static bool IsAtOrAboveFloor(ulong iterations, ulong memoryKib, uint parallelism) =>
        iterations >= MinIterations &&
        memoryKib >= (ulong)MinMemoryKib &&
        parallelism >= MinParallelism;
}
