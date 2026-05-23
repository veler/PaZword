using System.Security.Cryptography;

namespace PaZword.Crypto;

/// <summary>
/// Helpers for handling sensitive byte buffers under AOT discipline.
/// Every password / key / composite-key buffer should be cleared with
/// <see cref="ZeroMemory"/> (or a using-scope wrapper) on completion.
/// </summary>
public static class SecureMemory
{
    /// <summary>
    /// Constant-time zeroization of a byte buffer.
    /// Delegates to <see cref="CryptographicOperations.ZeroMemory(System.Span{byte})"/>,
    /// which the JIT/AOT will not elide.
    /// </summary>
    public static void ZeroMemory(Span<byte> buffer) => CryptographicOperations.ZeroMemory(buffer);

    /// <summary>
    /// Zero a managed byte[] and return null. Convenience for caller patterns
    /// where the buffer reference is no longer needed.
    /// </summary>
    public static void Clear(byte[]? buffer)
    {
        if (buffer is { Length: > 0 })
            CryptographicOperations.ZeroMemory(buffer);
    }
}

/// <summary>
/// RAII-style wrapper around a sensitive byte buffer. Disposing the wrapper
/// zeroes the underlying buffer. Use with <c>using</c>.
/// </summary>
public sealed class SecureBytes : IDisposable
{
    private byte[]? _buffer;

    /// <summary>Wraps an existing buffer; takes ownership.</summary>
    public SecureBytes(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
    }

    /// <summary>Allocates a fresh zero-filled buffer of the given length.</summary>
    public SecureBytes(int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        _buffer = new byte[length];
    }

    /// <summary>The wrapped buffer. Throws if the wrapper has been disposed.</summary>
    public byte[] Buffer => _buffer ?? throw new ObjectDisposedException(nameof(SecureBytes));

    /// <summary>Length of the wrapped buffer (0 once disposed).</summary>
    public int Length => _buffer?.Length ?? 0;

    /// <summary>View of the buffer as a span. Throws if disposed.</summary>
    public Span<byte> AsSpan() => Buffer.AsSpan();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_buffer is not null)
        {
            CryptographicOperations.ZeroMemory(_buffer);
            _buffer = null;
        }
    }
}
