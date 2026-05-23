using System.Text.Json;
using Windows.Storage;

namespace PaZword.App.Recents;

/// <summary>
/// LocalSettings-backed <see cref="IRecentVaultsService"/>. JSON serialisation
/// goes through <see cref="RecentsJsonContext"/> (STJ source-gen → AOT-clean).
/// </summary>
internal sealed class RecentVaultsService : IRecentVaultsService
{
    private const string SettingKey = "RecentVaults";

    public int MaxEntries => 5;

    public event EventHandler? Changed;

    public IReadOnlyList<RecentVaultEntry> GetAll()
    {
        var json = ApplicationData.Current.LocalSettings.Values[SettingKey] as string;
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<RecentVaultEntry>();

        try
        {
            var parsed = JsonSerializer.Deserialize(json, RecentsJsonContext.Default.RecentVaultEntryArray);
            if (parsed is null) return Array.Empty<RecentVaultEntry>();

            // Defensive: drop malformed rows, then sort by LastOpened desc, cap to MaxEntries.
            return parsed
                .Where(static e => !string.IsNullOrEmpty(e.Path) && !string.IsNullOrEmpty(e.FileName))
                .OrderByDescending(static e => e.LastOpened)
                .Take(MaxEntries)
                .ToArray();
        }
        catch (JsonException)
        {
            // Corrupt blob → reset.
            ApplicationData.Current.LocalSettings.Values.Remove(SettingKey);
            return Array.Empty<RecentVaultEntry>();
        }
    }

    public void Touch(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var fileName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(fileName)) return;

        var existing = GetAll();
        var next = new List<RecentVaultEntry>(existing.Count + 1)
        {
            new(path, fileName, DateTimeOffset.UtcNow),
        };
        // Append previous entries that aren't this path, oldest of the surviving entries dropping off naturally.
        next.AddRange(existing.Where(e => !string.Equals(e.Path, path, StringComparison.OrdinalIgnoreCase)));
        Persist(next.Take(MaxEntries));
    }

    public void Remove(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var existing = GetAll();
        var next = existing.Where(e => !string.Equals(e.Path, path, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (next.Length == existing.Count) return; // no-op
        Persist(next);
    }

    private void Persist(IEnumerable<RecentVaultEntry> entries)
    {
        var array = entries.ToArray();
        var json = JsonSerializer.Serialize(array, RecentsJsonContext.Default.RecentVaultEntryArray);
        ApplicationData.Current.LocalSettings.Values[SettingKey] = json;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
