namespace PaZword.App.Navigation;

/// <summary>
/// Stable page keys used by <see cref="NavigationService"/>. Centralised here so
/// view-models reference compile-time constants — no magic strings spread across
/// the codebase, and renames stay refactor-safe.
/// </summary>
public static class PageKeys
{
    // Root frame destinations
    public const string Onboarding = "Onboarding";
    public const string Welcome = "Welcome";
    public const string Locked = "Locked";
    public const string MigrationPlaceholder = "MigrationPlaceholder";
    public const string VaultShell = "VaultShell";

    // Vault-shell inner-frame destinations
    public const string AllEntries = "AllEntries";
    public const string Favorites = "Favorites";
    public const string Trash = "Trash";
    public const string Settings = "Settings";
    public const string About = "About";
}
