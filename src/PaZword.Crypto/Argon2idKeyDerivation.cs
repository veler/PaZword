using Konscious.Security.Cryptography;
using PaZword.Crypto.Abstractions;

namespace PaZword.Crypto;

/// <summary>
/// Argon2id key-derivation implementation backed by <c>Konscious.Security.Cryptography.Argon2</c>.
/// Per RFC 9106 §4: Argon2id is the recommended variant for password hashing.
/// </summary>
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

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var argon2 = new Argon2id(password)
            {
                Salt = salt,
                DegreeOfParallelism = degreeOfParallelism,
                MemorySize = memorySize,
                Iterations = iterations,
            };

            return argon2.GetBytes(outputLength);
        }, cancellationToken);
    }
}
