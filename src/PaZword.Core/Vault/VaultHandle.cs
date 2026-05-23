using PaZword.Kdbx.Models;
using System.Security.Cryptography;

namespace PaZword.Core.Vault;

/// <summary>
/// Opaque handle to an open vault. Carries the parsed <see cref="KdbxDocument"/>,
/// the originating file path, and the composite key cached for subsequent
/// <see cref="IVaultService.SaveAsync"/> calls. Disposing the handle zeros the
/// composite-key buffer.
/// </summary>
public sealed class VaultHandle : IDisposable
{
    private byte[]? _compositeKey;

    public string Path { get; }
    public KdbxDocument Document { get; }
    internal ReadOnlySpan<byte> CompositeKey =>
        _compositeKey ?? throw new ObjectDisposedException(nameof(VaultHandle));

    internal VaultHandle(string path, KdbxDocument document, byte[] compositeKey)
    {
        Path = path;
        Document = document;
        _compositeKey = compositeKey;
    }

    /// <summary>
    /// Create a fresh handle for a new (not-yet-saved) vault. Used by callers
    /// that build a document in memory and then immediately save it.
    /// </summary>
    public static VaultHandle CreateNew(string path, KdbxDocument document, VaultCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(credentials);
        var composite = PaZword.Kdbx.KeyDerivation.CompositeKey.Build(credentials.Password, credentials.KeyFile);
        return new VaultHandle(path, document, composite);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_compositeKey is not null)
        {
            CryptographicOperations.ZeroMemory(_compositeKey);
            _compositeKey = null;
        }
    }
}
