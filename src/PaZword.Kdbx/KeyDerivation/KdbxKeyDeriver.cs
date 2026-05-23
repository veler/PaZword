using Microsoft.Extensions.Logging;
using PaZword.Crypto;
using PaZword.Kdbx.Models;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace PaZword.Kdbx.KeyDerivation;

/// <summary>
/// Runs the KDBX 4 key-derivation chain for a given header + composite key:
/// <list type="number">
///   <item><description>Run the header's KDF over the composite key → 32-byte transformed key.</description></item>
///   <item><description>masterKey = SHA-256(masterSeed || transformedKey).</description></item>
///   <item><description>hmacBaseKey = SHA-512(masterSeed || transformedKey || 0x01).</description></item>
/// </list>
/// <para>
/// The KDF call site clears every internal copy of the composite / transformed key.
/// On below-floor read parameters an <c>ILogger.LogWarning</c> is emitted (writer
/// refuses below-floor — see <see cref="KdbxKdfParameters.ValidateForWrite"/>).
/// </para>
/// </summary>
internal sealed class KdbxKeyDeriver
{
    private readonly Argon2idKeyDerivation _argon2id = new();
    private readonly ILogger? _logger;

    public KdbxKeyDeriver(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Result of the key-derivation chain. Caller owns the buffers and must
    /// clear them with <see cref="Dispose"/> (or
    /// <see cref="CryptographicOperations.ZeroMemory(System.Span{byte})"/>).
    /// </summary>
    public sealed class DerivedKeys : IDisposable
    {
        public byte[] MasterKey { get; }       // 32 bytes
        public byte[] HmacBaseKey { get; }     // 64 bytes
        public byte[] TransformedKey { get; }  // 32 bytes (kept for cipher-key derivation)

        internal DerivedKeys(byte[] masterKey, byte[] hmacBaseKey, byte[] transformedKey)
        {
            MasterKey = masterKey;
            HmacBaseKey = hmacBaseKey;
            TransformedKey = transformedKey;
        }

        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(MasterKey);
            CryptographicOperations.ZeroMemory(HmacBaseKey);
            CryptographicOperations.ZeroMemory(TransformedKey);
        }
    }

    /// <summary>
    /// Runs the full chain. Throws on unsupported KDF; below-floor parameters
    /// are accepted for reads but log a warning.
    /// </summary>
    /// <param name="kdfParams">KDF parameters from the outer header.</param>
    /// <param name="masterSeed">32-byte master seed from the outer header.</param>
    /// <param name="compositeKey">Caller-owned composite key (will not be cleared by this method).</param>
    public DerivedKeys Derive(KdbxKdfParameters kdfParams, byte[] masterSeed, byte[] compositeKey, bool isReadingExistingVault)
    {
        ArgumentNullException.ThrowIfNull(kdfParams);
        ArgumentNullException.ThrowIfNull(masterSeed);
        ArgumentNullException.ThrowIfNull(compositeKey);
        if (masterSeed.Length != 32)
            throw new KdbxFormatException($"Master seed must be 32 bytes (got {masterSeed.Length}).");

        if (isReadingExistingVault &&
            kdfParams.IsArgon2id &&
            !KdfFloor.IsAtOrAboveFloor(kdfParams.Iterations, kdfParams.MemoryKib, kdfParams.Parallelism))
        {
            _logger?.LogWarning(
                "KDBX vault uses Argon2id parameters below the production floor " +
                "(t={Iterations}, m={MemoryKib} KiB, p={Parallelism}). The vault opens; " +
                "encourage the user to re-save to upgrade KDF strength.",
                kdfParams.Iterations, kdfParams.MemoryKib, kdfParams.Parallelism);
        }

        byte[] transformedKey = kdfParams.Uuid switch
        {
            var u when u == KdbxKdfUuids.Argon2id => RunArgon2id(kdfParams, compositeKey),
            var u when u == KdbxKdfUuids.Argon2d  => throw new KdbxFormatException(
                "Argon2d-keyed KDBX files are not supported by PaZword v2 (Argon2id only). " +
                "Re-save the vault in KeePassXC with Argon2id."),
            var u when u == KdbxKdfUuids.AesKdf   => RunAesKdf(kdfParams, compositeKey),
            _ => throw new KdbxFormatException($"Unsupported KDF UUID {kdfParams.Uuid}."),
        };

        byte[] masterKey;
        byte[] hmacBase;
        try
        {
            // masterKey = SHA-256(masterSeed || transformedKey)
            using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                sha.AppendData(masterSeed);
                sha.AppendData(transformedKey);
                masterKey = sha.GetHashAndReset();
            }
            // hmacBaseKey = SHA-512(masterSeed || transformedKey || 0x01)
            using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA512))
            {
                sha.AppendData(masterSeed);
                sha.AppendData(transformedKey);
                sha.AppendData(new byte[] { 0x01 });
                hmacBase = sha.GetHashAndReset();
            }
        }
        catch
        {
            CryptographicOperations.ZeroMemory(transformedKey);
            throw;
        }

        return new DerivedKeys(masterKey, hmacBase, transformedKey);
    }

    /// <summary>
    /// Computes the header-HMAC key as
    /// <c>SHA-512(0xFFFFFFFFFFFFFFFF || hmacBaseKey)</c> per KDBX spec.
    /// </summary>
    public static byte[] DeriveHeaderHmacKey(byte[] hmacBaseKey)
    {
        Span<byte> headerIndex = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(headerIndex, ulong.MaxValue);
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        sha.AppendData(headerIndex);
        sha.AppendData(hmacBaseKey);
        return sha.GetHashAndReset();
    }

    private byte[] RunArgon2id(KdbxKdfParameters p, byte[] compositeKey)
    {
        if (p.Salt is null || p.Salt.Length == 0)
            throw new KdbxFormatException("Argon2id KDF requires a salt.");
        if (p.MemoryKib == 0 || p.MemoryKib > int.MaxValue)
            throw new KdbxFormatException($"Argon2id memory cost {p.MemoryKib} KiB out of range.");
        if (p.Iterations == 0 || p.Iterations > int.MaxValue)
            throw new KdbxFormatException($"Argon2id iterations {p.Iterations} out of range.");
        if (p.Parallelism == 0 || p.Parallelism > 64)
            throw new KdbxFormatException($"Argon2id parallelism {p.Parallelism} out of range.");

        return _argon2id.DeriveKey(
            compositeKey,
            p.Salt,
            (int)p.Parallelism,
            (int)p.MemoryKib,
            (int)p.Iterations,
            outputLength: 32);
    }

    private static byte[] RunAesKdf(KdbxKdfParameters p, byte[] compositeKey)
    {
        // Legacy AES-KDF: AES-ECB-encrypt compositeKey with `seed` for `rounds` iterations,
        // then SHA-256 the result. Read-only path for KeePassXC compat.
        if (p.AesSeed is null || p.AesSeed.Length != 32)
            throw new KdbxFormatException("AES-KDF seed must be 32 bytes.");
        if (compositeKey.Length != 32)
            throw new KdbxFormatException("AES-KDF composite key must be 32 bytes.");
        if (p.AesRounds == 0 || p.AesRounds > int.MaxValue)
            throw new KdbxFormatException($"AES-KDF rounds {p.AesRounds} out of range.");

        byte[] state = new byte[32];
        Buffer.BlockCopy(compositeKey, 0, state, 0, 32);
        try
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = p.AesSeed;
            using var encryptor = aes.CreateEncryptor();
            Span<byte> half = stackalloc byte[16];
            byte[] inBuf = new byte[16];
            byte[] outBuf = new byte[16];
            for (ulong i = 0; i < p.AesRounds; i++)
            {
                Buffer.BlockCopy(state, 0, inBuf, 0, 16);
                encryptor.TransformBlock(inBuf, 0, 16, outBuf, 0);
                Buffer.BlockCopy(outBuf, 0, state, 0, 16);
                Buffer.BlockCopy(state, 16, inBuf, 0, 16);
                encryptor.TransformBlock(inBuf, 0, 16, outBuf, 0);
                Buffer.BlockCopy(outBuf, 0, state, 16, 16);
            }
            return SHA256.HashData(state);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(state);
        }
    }
}
