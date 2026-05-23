namespace PaZword.App.Recents;

/// <summary>
/// One entry in the Welcome page's recent-vaults list.
/// </summary>
/// <remarks>
/// Persisted as JSON in <c>ApplicationData.LocalSettings["RecentVaults"]</c>.
/// Serialised via <see cref="RecentsJsonContext"/> (STJ source-gen, AOT-clean).
/// </remarks>
public sealed class RecentVaultEntry
{
    public RecentVaultEntry(string Path, string FileName, DateTimeOffset LastOpened)
    {
        this.Path = Path;
        this.FileName = FileName;
        this.LastOpened = LastOpened;
    }

    public string Path { get; init; }
    public string FileName { get; init; }
    public DateTimeOffset LastOpened { get; init; }
}
