namespace PaZword.Kdbx.Ciphers;

/// <summary>
/// Outer-payload cipher abstraction for KDBX 4. Implementations encrypt or
/// decrypt the concatenated HMAC-block payload using a 32-byte key derived
/// from the master key (see <c>kdbx4-spec-notes.md §6</c>).
/// </summary>
internal interface IKdbxCipher
{
    /// <summary>16-byte CipherID UUID written into the outer header.</summary>
    Guid CipherId { get; }

    /// <summary>Required IV length in bytes (AES-CBC: 16, ChaCha20: 12).</summary>
    int IvLength { get; }

    /// <summary>
    /// Wraps <paramref name="ciphertext"/> in a decrypting <see cref="Stream"/>.
    /// Reading from the returned stream returns plaintext bytes.
    /// </summary>
    Stream CreateDecryptingReadStream(Stream ciphertext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv);

    /// <summary>
    /// Wraps <paramref name="ciphertextSink"/> in an encrypting <see cref="Stream"/>.
    /// Writing plaintext to the returned stream emits ciphertext into the sink.
    /// </summary>
    Stream CreateEncryptingWriteStream(Stream ciphertextSink, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv);
}

/// <summary>Standard KDBX cipher UUIDs.</summary>
internal static class KdbxCipherIds
{
    /// <summary>AES-256-CBC + PKCS7.</summary>
    public static readonly Guid AesCbc = new("31C1F2E6-BF71-4350-BE58-05216AFC5AFF");

    /// <summary>ChaCha20 (raw, no Poly1305).</summary>
    public static readonly Guid ChaCha20 = new("D6038A2B-8B6F-4CB5-A524-339A31DBB59A");
}
