using Microsoft.Extensions.Logging;
using PaZword.Crypto;
using PaZword.Kdbx.Ciphers;
using PaZword.Kdbx.Interfaces;
using PaZword.Kdbx.IO;
using PaZword.Kdbx.KeyDerivation;
using PaZword.Kdbx.Models;
using System.IO.Compression;
using System.Security.Cryptography;

namespace PaZword.Kdbx.Reader;

/// <summary>
/// KDBX 4 reader. Implements <see cref="IKdbxReader"/>.
/// </summary>
public sealed class KdbxReader : IKdbxReader
{
    private readonly ILogger<KdbxReader>? _logger;

    public KdbxReader() { }
    public KdbxReader(ILogger<KdbxReader> logger) { _logger = logger; }

    /// <inheritdoc/>
    public Task<KdbxDocument> ReadAsync(Stream stream, byte[] compositeKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(compositeKey);
        return Task.Run(() => ReadCore(stream, compositeKey, cancellationToken), cancellationToken);
    }

    private KdbxDocument ReadCore(Stream stream, byte[] compositeKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // 1. Outer header.
        var parsed = KdbxOuterHeaderReader.Read(stream);
        ct.ThrowIfCancellationRequested();

        // 2. Verify header SHA-256.
        var actualSha = SHA256.HashData(parsed.RawHeaderBytes);
        if (!CryptographicOperations.FixedTimeEquals(actualSha, parsed.StoredHeaderSha256))
            throw new KdbxIntegrityException("Outer header SHA-256 mismatch — file is corrupt.");

        // 3. Derive keys.
        var deriver = new KdbxKeyDeriver(_logger);
        using var keys = deriver.Derive(parsed.Header.KdfParameters!, parsed.Header.MasterSeed!, compositeKey, isReadingExistingVault: true);

        // 4. Verify header HMAC.
        byte[] headerHmacKey = KdbxKeyDeriver.DeriveHeaderHmacKey(keys.HmacBaseKey);
        try
        {
            byte[] expectedHeaderHmac;
            using (var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, headerHmacKey))
            {
                hmac.AppendData(parsed.RawHeaderBytes);
                expectedHeaderHmac = hmac.GetHashAndReset();
            }
            if (!CryptographicOperations.FixedTimeEquals(expectedHeaderHmac, parsed.StoredHeaderHmac))
                throw new KdbxIntegrityException("Outer header HMAC mismatch — wrong password or tampered file.");
        }
        finally { CryptographicOperations.ZeroMemory(headerHmacKey); }

        // 5. Wrap remaining stream: HmacBlocks → Cipher → optional GZip.
        var hmacBlocks = new HmacBlockReadStream(stream, keys.HmacBaseKey, leaveOpen: true);
        var cipher = KdbxCipherFactory.Resolve(new Guid(parsed.Header.CipherId!));
        var decrypting = cipher.CreateDecryptingReadStream(hmacBlocks, keys.MasterKey, parsed.Header.EncryptionIV!);
        Stream payload = decrypting;
        if (parsed.Header.CompressionFlags == 1)
            payload = new GZipStream(decrypting, CompressionMode.Decompress);

        using (payload)
        {
            // 6. Inner header.
            ct.ThrowIfCancellationRequested();
            var inner = KdbxInnerHeaderReader.Read(payload);

            using var protectedStream = InnerStreamFactory.Create(inner.InnerStreamId, inner.InnerStreamKey);

            // 7. XML body.
            var xmlReader = new KdbxXmlReader(protectedStream);
            var doc = xmlReader.Read(payload, parsed.Header, inner.Binaries);
            return doc;
        }
    }
}
