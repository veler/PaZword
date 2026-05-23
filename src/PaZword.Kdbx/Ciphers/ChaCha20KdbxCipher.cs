using PaZword.Crypto;

namespace PaZword.Kdbx.Ciphers;

/// <summary>
/// Raw ChaCha20 (RFC 8439) outer cipher. KDBX 4 uses raw ChaCha20 (no
/// Poly1305); integrity is provided by the outer HMAC block chain.
/// </summary>
internal sealed class ChaCha20KdbxCipher : IKdbxCipher
{
    public Guid CipherId => KdbxCipherIds.ChaCha20;
    public int IvLength => 12;

    public Stream CreateDecryptingReadStream(Stream ciphertext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
        => new ChaCha20Stream(ciphertext, key.ToArray(), iv.ToArray(), forWrite: false);

    public Stream CreateEncryptingWriteStream(Stream ciphertextSink, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv)
        => new ChaCha20Stream(ciphertextSink, key.ToArray(), iv.ToArray(), forWrite: true);

    /// <summary>
    /// Stream that XORs every passing byte with a ChaCha20 keystream.
    /// </summary>
    private sealed class ChaCha20Stream : Stream
    {
        private readonly Stream _inner;
        private readonly ChaCha20 _cipher;
        private readonly bool _forWrite;
        private bool _disposed;

        public ChaCha20Stream(Stream inner, byte[] key, byte[] iv, bool forWrite)
        {
            _inner = inner;
            _cipher = new ChaCha20(key, iv);
            _forWrite = forWrite;
        }

        public override bool CanRead => !_forWrite;
        public override bool CanSeek => false;
        public override bool CanWrite => _forWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (!CanRead) throw new NotSupportedException();
            int n = _inner.Read(buffer);
            if (n > 0)
                _cipher.Process(buffer[..n]);
            return n;
        }

        public override void Write(byte[] buffer, int offset, int count) =>
            Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!CanWrite) throw new NotSupportedException();
            // Rent a scratch buffer to keep caller buffer untouched.
            var scratch = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                _cipher.Process(buffer, scratch.AsSpan(0, buffer.Length));
                _inner.Write(scratch.AsSpan(0, buffer.Length));
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(scratch, clearArray: true);
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) { _cipher.Dispose(); _inner.Dispose(); }
            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
