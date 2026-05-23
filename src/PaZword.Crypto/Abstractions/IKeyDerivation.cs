namespace PaZword.Crypto.Abstractions;

/// <summary>
/// Key-derivation function abstraction.
/// AOT discipline: implementations must not use reflection or dynamic code generation.
/// </summary>
public interface IKeyDerivation
{
    /// <summary>
    /// Derives a key from the supplied password and salt using Argon2id.
    /// </summary>
    /// <param name="password">UTF-8 password bytes. Caller must zero after use.</param>
    /// <param name="salt">Random salt (≥ 16 bytes; KDBX uses 32).</param>
    /// <param name="degreeOfParallelism">Argon2 parallelism (P parameter).</param>
    /// <param name="memorySize">Memory cost in kibibytes (M parameter).</param>
    /// <param name="iterations">Iteration count (T parameter).</param>
    /// <param name="outputLength">Length of the derived key in bytes (default 32).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Derived key bytes of length <paramref name="outputLength"/>. Caller must zero after use.</returns>
    Task<byte[]> DeriveKeyAsync(
        byte[] password,
        byte[] salt,
        int degreeOfParallelism,
        int memorySize,
        int iterations,
        int outputLength = 32,
        CancellationToken cancellationToken = default);
}
