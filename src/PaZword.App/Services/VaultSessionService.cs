using PaZword.Core.Vault;

namespace PaZword.App.Services;

/// <summary>
/// Holds the currently-open <see cref="VaultHandle"/> for the duration of the
/// unlocked-shell session. Set by the Locked page after a successful unlock;
/// cleared by Lock/Switch. Inner shell pages (AllEntries, Favorites, etc.) read
/// the handle from here instead of routing it through navigation parameters.
/// </summary>
public interface IVaultSessionService
{
    VaultHandle? Current { get; }
    void SetCurrent(VaultHandle handle);
    void Clear();
    event EventHandler? Changed;
}

/// <summary>Singleton DI implementation.</summary>
internal sealed class VaultSessionService : IVaultSessionService
{
    private VaultHandle? _current;

    public VaultHandle? Current => _current;

    public event EventHandler? Changed;

    public void SetCurrent(VaultHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        _current = handle;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        if (_current is null) return;
        _current.Dispose();
        _current = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
