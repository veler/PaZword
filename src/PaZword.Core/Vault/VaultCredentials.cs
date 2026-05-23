using System.Security.Cryptography;
using System.Text;

namespace PaZword.Core.Vault;

/// <summary>
/// Credentials supplied to <see cref="IVaultService.OpenAsync"/> and
/// <see cref="IVaultService.SaveAsync"/>. Owns its sensitive buffers and
/// zeroes them on <see cref="Dispose"/>.
/// </summary>
public sealed class VaultCredentials : IDisposable
{
    private byte[]? _password;
    private byte[]? _keyFile;

    /// <summary>UTF-8 encoded password bytes (defensive copy).</summary>
    public ReadOnlySpan<byte> Password => _password ?? ReadOnlySpan<byte>.Empty;

    /// <summary>Optional key-file bytes.</summary>
    public ReadOnlySpan<byte> KeyFile => _keyFile ?? ReadOnlySpan<byte>.Empty;

    /// <summary>
    /// Builds a credentials object from a string password and optional key-file
    /// bytes. Both inputs are defensively copied.
    /// </summary>
    public static VaultCredentials FromPassword(string password, byte[]? keyFile = null)
    {
        ArgumentNullException.ThrowIfNull(password);
        var pwBytes = Encoding.UTF8.GetBytes(password);
        var keyCopy = keyFile is null ? null : (byte[])keyFile.Clone();
        return new VaultCredentials(pwBytes, keyCopy);
    }

    /// <summary>Builds credentials directly from raw bytes (takes ownership).</summary>
    public VaultCredentials(byte[]? password, byte[]? keyFile)
    {
        _password = password;
        _keyFile = keyFile;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_password is { Length: > 0 }) CryptographicOperations.ZeroMemory(_password);
        if (_keyFile is { Length: > 0 }) CryptographicOperations.ZeroMemory(_keyFile);
        _password = null;
        _keyFile = null;
    }
}
