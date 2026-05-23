namespace PaZword.Core.Vault;

/// <summary>
/// High-level vault service contract used by the rest of the application.
/// Implementation lives in <see cref="VaultService"/> and wraps the
/// <c>PaZword.Kdbx</c> reader/writer.
/// </summary>
public interface IVaultService
{
    /// <summary>Opens a vault from disk. The handle caches the composite key.</summary>
    Task<VaultHandle> OpenAsync(string path, VaultCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a vault back to disk using the composite key cached in
    /// <paramref name="handle"/>. To re-key a vault, create a fresh handle with
    /// <see cref="VaultHandle.CreateNew"/>.
    /// </summary>
    Task SaveAsync(VaultHandle handle, CancellationToken cancellationToken = default);

    /// <summary>Closes a vault handle, releasing any cached sensitive material.</summary>
    Task CloseAsync(VaultHandle handle);
}
