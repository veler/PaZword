namespace PaZword.App.Recents;

/// <summary>
/// One entry in the Welcome page's recent-vaults list.
/// </summary>
/// <remarks>
/// Persisted as JSON in <c>ApplicationData.LocalSettings["RecentVaults"]</c>.
/// Serialised via <see cref="RecentsJsonContext"/> (STJ source-gen, AOT-clean).
/// </remarks>
public sealed record RecentVaultEntry(
    string Path,
    string FileName,
    DateTimeOffset LastOpened);
