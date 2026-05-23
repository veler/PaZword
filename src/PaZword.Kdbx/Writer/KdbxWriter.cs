using Microsoft.Extensions.Logging;
using PaZword.Crypto;
using PaZword.Kdbx.Ciphers;
using PaZword.Kdbx.Interfaces;
using PaZword.Kdbx.IO;
using PaZword.Kdbx.KeyDerivation;
using PaZword.Kdbx.Models;
using PaZword.Kdbx.Reader;
using System.IO.Compression;
using System.Security.Cryptography;

namespace PaZword.Kdbx.Writer;

/// <summary>
/// KDBX 4 writer. Implements <see cref="IKdbxWriter"/>. Default cipher:
/// ChaCha20. Default compression: GZip. Argon2id KDF parameters at the
/// production floor.
/// </summary>
public sealed class KdbxWriter : IKdbxWriter
{
    private readonly ILogger<KdbxWriter>? _logger;

    public KdbxWriter() { }
    public KdbxWriter(ILogger<KdbxWriter> logger) { _logger = logger; }

    /// <inheritdoc/>
    public Task WriteAsync(KdbxDocument document, Stream stream, byte[] compositeKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(compositeKey);
        return Task.Run(() => WriteCore(document, stream, compositeKey, cancellationToken), cancellationToken);
    }

    private void WriteCore(KdbxDocument doc, Stream stream, byte[] compositeKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Resolve / build header.
        var header = BuildHeader(doc);

        // Validate KDF for write — enforces production floor.
        header.KdfParameters!.ValidateForWrite();

        // Derive keys.
        var deriver = new KdbxKeyDeriver(_logger);
        using var keys = deriver.Derive(header.KdfParameters!, header.MasterSeed!, compositeKey, isReadingExistingVault: false);

        // Emit outer header + integrity tags.
        byte[] headerBytes = KdbxOuterHeaderWriter.BuildHeaderBytes(header);
        byte[] headerHmacKey = KdbxKeyDeriver.DeriveHeaderHmacKey(keys.HmacBaseKey);
        try
        {
            KdbxOuterHeaderWriter.WriteHeaderWithIntegrity(stream, headerBytes, headerHmacKey);
        }
        finally { CryptographicOperations.ZeroMemory(headerHmacKey); }

        // Encrypted payload: HmacBlocks → Cipher → GZip → Inner header + XML.
        ct.ThrowIfCancellationRequested();
        var cipher = KdbxCipherFactory.Resolve(new Guid(header.CipherId!));
        using var hmacBlocks = new HmacBlockWriteStream(stream, keys.HmacBaseKey, leaveOpen: true);
        using var encrypting = cipher.CreateEncryptingWriteStream(hmacBlocks, keys.MasterKey, header.EncryptionIV!);

        Stream payloadSink = encrypting;
        GZipStream? gzip = null;
        try
        {
            if (header.CompressionFlags == 1)
            {
                gzip = new GZipStream(encrypting, CompressionMode.Compress, leaveOpen: true);
                payloadSink = gzip;
            }

            // Inner header — ChaCha20 inner random stream by default.
            byte[] innerStreamKey = RandomNumberGenerator.GetBytes(64);
            try
            {
                KdbxInnerHeaderWriter.Write(payloadSink, KdbxInnerHeaderReader.StreamCipherId.ChaCha20, innerStreamKey, doc.Binaries);
                using var prot = InnerStreamFactory.Create(KdbxInnerHeaderReader.StreamCipherId.ChaCha20, innerStreamKey);
                var xml = new KdbxXmlWriter(prot);
                xml.Write(payloadSink, doc);
            }
            finally { CryptographicOperations.ZeroMemory(innerStreamKey); }

            payloadSink.Flush();
            gzip?.Flush();
        }
        finally
        {
            gzip?.Dispose();
        }
    }

    private static KdbxHeader BuildHeader(KdbxDocument doc)
    {
        // If the document already has a populated header (round-trip case),
        // preserve cipher/compression choices but always generate fresh
        // MasterSeed + EncryptionIV (every write must use fresh randomness).
        var inputHeader = doc.Header;
        byte[] cipherId = inputHeader.CipherId is { Length: 16 } ? inputHeader.CipherId : KdbxCipherIds.ChaCha20.ToByteArray();
        var cipher = KdbxCipherFactory.Resolve(new Guid(cipherId));
        int compression = inputHeader.CompressionFlags is 0 or 1 ? inputHeader.CompressionFlags : 1;

        byte[] masterSeed = RandomNumberGenerator.GetBytes(32);
        byte[] iv = RandomNumberGenerator.GetBytes(cipher.IvLength);

        var kdf = inputHeader.KdfParameters;
        if (kdf is null || !kdf.IsArgon2id)
        {
            // New / migrated vault: default Argon2id at the prod floor.
            kdf = KdbxKdfParameters.DefaultArgon2id(RandomNumberGenerator.GetBytes(32));
        }
        else
        {
            // Re-use existing parameters but fresh salt.
            kdf = new KdbxKdfParameters
            {
                Uuid = kdf.Uuid,
                Salt = RandomNumberGenerator.GetBytes(kdf.Salt is { Length: > 0 } ? kdf.Salt.Length : 32),
                Iterations = kdf.Iterations,
                MemoryKib = kdf.MemoryKib,
                Parallelism = kdf.Parallelism,
                Version = kdf.Version == 0 ? 0x13u : kdf.Version,
            };
        }

        return new KdbxHeader
        {
            MajorVersion = 4,
            MinorVersion = 1,
            CipherId = cipherId,
            CompressionFlags = compression,
            MasterSeed = masterSeed,
            EncryptionIV = iv,
            KdfParameters = kdf,
            PublicCustomData = inputHeader.PublicCustomData,
        };
    }
}
