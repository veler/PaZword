using System.Security.Cryptography;
using System.Text;

namespace PaZword.Kdbx;

/// <summary>
/// A defensive wrapper around a sensitive UTF-8 byte buffer (typically a
/// decrypted protected field value). <see cref="ToString"/> never reveals the
/// underlying bytes; the buffer is zeroed on <see cref="Dispose"/>.
/// </summary>
/// <remarks>
/// AOT-clean: no reflection. <see cref="ToString"/> returns the constant
/// <c>"***"</c> so a <c>SecureField</c> can never accidentally land in a log
/// line via interpolation.
/// </remarks>
public sealed class SecureField : IDisposable
{
    private byte[]? _utf8;

    /// <summary>Wraps an existing UTF-8 buffer. Takes ownership.</summary>
    public SecureField(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        _utf8 = utf8Bytes;
    }

    /// <summary>Copies the UTF-8 bytes of <paramref name="value"/>.</summary>
    public static SecureField FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new SecureField(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>Length of the underlying buffer in bytes (0 once disposed).</summary>
    public int Length => _utf8?.Length ?? 0;

    /// <summary>
    /// Reveals the plaintext as a managed string. Callers should consume and
    /// discard the result as quickly as possible.
    /// </summary>
    /// <exception cref="ObjectDisposedException">If the field has been disposed.</exception>
    public string Reveal()
    {
        var bytes = _utf8 ?? throw new ObjectDisposedException(nameof(SecureField));
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>Copies the UTF-8 bytes into a fresh array (caller-owned).</summary>
    public byte[] RevealUtf8()
    {
        var bytes = _utf8 ?? throw new ObjectDisposedException(nameof(SecureField));
        var copy = new byte[bytes.Length];
        Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);
        return copy;
    }

    /// <summary>Always returns <c>"***"</c> to prevent accidental logging.</summary>
    public override string ToString() => "***";

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_utf8 is not null)
        {
            CryptographicOperations.ZeroMemory(_utf8);
            _utf8 = null;
        }
    }
}
