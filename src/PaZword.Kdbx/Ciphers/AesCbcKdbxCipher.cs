using System.Security.Cryptography;

namespace PaZword.Kdbx.Ciphers;

/// <summary>AES-256-CBC + PKCS7 padding, routed to the BCL.</summary>
internal sealed class AesCbcKdbxCipher : IKdbxCipher
{
    public Guid CipherId => KdbxCipherIds.AesCbc;
    public int IvLength => 16;

    public Stream CreateDecryptingReadStream(Stream ciphertext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key.ToArray();
        aes.IV = iv.ToArray();
        var decryptor = aes.CreateDecryptor();
        return new CryptoStream(new DisposeMany(ciphertext, aes), decryptor, CryptoStreamMode.Read, leaveOpen: false);
    }

    public Stream CreateEncryptingWriteStream(Stream ciphertextSink, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key.ToArray();
        aes.IV = iv.ToArray();
        var encryptor = aes.CreateEncryptor();
        return new CryptoStream(new DisposeMany(ciphertextSink, aes), encryptor, CryptoStreamMode.Write, leaveOpen: false);
    }

    /// <summary>
    /// Stream wrapper that disposes both the wrapped stream and an extra disposable
    /// (e.g. the <see cref="Aes"/> instance) when itself disposed.
    /// </summary>
    private sealed class DisposeMany : Stream
    {
        private readonly Stream _inner;
        private readonly IDisposable _extra;

        public DisposeMany(Stream inner, IDisposable extra) { _inner = inner; _extra = extra; }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _inner.Dispose(); _extra.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
