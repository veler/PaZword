using Konscious.Security.Cryptography;
using PaZword.Crypto.Abstractions;
using System.Security.Cryptography;

namespace PaZword.Crypto;

/// <summary>
/// Argon2id key-derivation implementation backed by <c>Konscious.Security.Cryptography.Argon2</c>.
/// Per RFC 9106 §4: Argon2id is the recommended variant for password hashing.
/// </summary>
/// <remarks>
/// Conformance: see <c>tests/PaZword.Crypto.Tests/Argon2idIndependentKatTests.cs</c> —
/// outputs verified byte-for-byte against the canonical <c>phc-winner-argon2</c> C
/// reference implementation (via <c>argon2-cffi</c>) for three independent vectors.
/// <para>
/// <b>Zeroization:</b> the wrapper passes the caller-supplied <paramref>password</paramref>
/// buffer to a <em>defensive copy</em> for Konscious, then zeros that copy before
/// returning. The caller's original buffer is the caller's responsibility (use
/// <see cref="SecureBytes"/> or <see cref="SecureMemory.Clear(byte[]?)"/>).
/// </para>
/// </remarks>
public sealed class Argon2idKeyDerivation : IKeyDerivation
{
    /// <inheritdoc/>
    public Task<byte[]> DeriveKeyAsync(
        byte[] password,
        byte[] salt,
        int degreeOfParallelism,
        int memorySize,
        int iterations,
        int outputLength = 32,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(salt);
        if (salt.Length < 8)
            throw new ArgumentException("Salt must be at least 8 bytes.", nameof(salt));
        if (degreeOfParallelism < 1)
            throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism));
        if (memorySize < 8)
            throw new ArgumentOutOfRangeException(nameof(memorySize));
        if (iterations < 1)
            throw new ArgumentOutOfRangeException(nameof(iterations));
        if (outputLength < 4)
            throw new ArgumentOutOfRangeException(nameof(outputLength));

        // Defensive copy. We zero this copy after Argon2 completes so that
        // we hold no plaintext password longer than necessary (the original
        // buffer is the caller's responsibility — see SecureBytes).
        var passwordCopy = new byte[password.Length];
        Buffer.BlockCopy(password, 0, passwordCopy, 0, password.Length);

        return Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var argon2 = new Argon2id(passwordCopy)
                {
                    Salt = salt,
                    DegreeOfParallelism = degreeOfParallelism,
                    MemorySize = memorySize,
                    Iterations = iterations,
                };

                return argon2.GetBytes(outputLength);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordCopy);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Synchronous variant for callers that already run off the UI thread (e.g. the
    /// KDBX reader executing on a worker). Same zeroization contract.
    /// </summary>
    public byte[] DeriveKey(
        byte[] password,
        byte[] salt,
        int degreeOfParallelism,
        int memorySize,
        int iterations,
        int outputLength = 32)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(salt);
        if (salt.Length < 8)
            throw new ArgumentException("Salt must be at least 8 bytes.", nameof(salt));
        if (degreeOfParallelism < 1)
            throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism));
        if (memorySize < 8)
            throw new ArgumentOutOfRangeException(nameof(memorySize));
        if (iterations < 1)
            throw new ArgumentOutOfRangeException(nameof(iterations));
        if (outputLength < 4)
            throw new ArgumentOutOfRangeException(nameof(outputLength));

        var passwordCopy = new byte[password.Length];
        Buffer.BlockCopy(password, 0, passwordCopy, 0, password.Length);
        try
        {
            using var argon2 = new Argon2id(passwordCopy)
            {
                Salt = salt,
                DegreeOfParallelism = degreeOfParallelism,
                MemorySize = memorySize,
                Iterations = iterations,
            };
            return argon2.GetBytes(outputLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordCopy);
        }
    }
}
