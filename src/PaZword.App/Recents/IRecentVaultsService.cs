namespace PaZword.App.Recents;

/// <summary>
/// Reads/writes the Welcome page's recent-vaults list. Implementation persists
/// to <c>ApplicationData.LocalSettings</c> as a JSON string (atomic), bounded to
/// a hard cap so the settings blob stays small.
/// </summary>
public interface IRecentVaultsService
{
    /// <summary>Maximum entries retained. UX-locked at 5 per brainstorm §8 #3.</summary>
    int MaxEntries { get; }

    /// <summary>Returns the current list, most-recent-first.</summary>
    IReadOnlyList<RecentVaultEntry> GetAll();

    /// <summary>Inserts or refreshes the entry for <paramref name="path"/> as most-recent.</summary>
    void Touch(string path);

    /// <summary>Removes the entry for <paramref name="path"/>, if present.</summary>
    void Remove(string path);

    /// <summary>Raised after any mutation completes.</summary>
    event EventHandler? Changed;
}
