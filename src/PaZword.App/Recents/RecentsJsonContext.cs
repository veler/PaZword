using System.Text.Json.Serialization;

namespace PaZword.App.Recents;

/// <summary>
/// System.Text.Json source-gen context for the recent-vaults persistence
/// format. AOT discipline: every JSON type the app reads or writes is reachable
/// through a typed <see cref="JsonSerializerContext"/> — no runtime reflection.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(RecentVaultEntry))]
[JsonSerializable(typeof(RecentVaultEntry[]))]
public sealed partial class RecentsJsonContext : JsonSerializerContext
{
}
