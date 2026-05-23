using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PaZword.App.Services;
using PaZword.Kdbx.Models;

namespace PaZword.App.ViewModels;

/// <summary>
/// Row-level VM for the entry list. Pre-projects the Title / UserName strings
/// so the list-item <c>{x:Bind}</c> paths stay AOT-friendly (no dictionary-indexer
/// expressions, which the WinUI XAML compiler does not parse).
/// </summary>
public sealed class EntryRow
{
    public KdbxEntry Entry { get; }
    public string Title { get; }
    public string UserName { get; }

    public EntryRow(KdbxEntry entry)
    {
        Entry = entry;
        Title = ReadString(entry, "Title", fallback: "(no title)");
        UserName = ReadString(entry, "UserName", fallback: string.Empty);
    }

    private static string ReadString(KdbxEntry entry, string key, string fallback)
    {
        if (entry.Strings.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v.Value))
            return v.Value;
        return fallback;
    }
}

/// <summary>
/// Backs <see cref="Pages.VaultShell.AllEntriesPage"/>.
/// Sprint 2: flat read-only list of all entries from the current vault, with a
/// read-only detail pane for the selection. Group tree, edit, and search arrive
/// in sprint 3.
/// </summary>
public sealed partial class AllEntriesViewModel : ObservableObject
{
    private readonly IVaultSessionService _session;
    private EntryRow? _selected;

    public ObservableCollection<EntryRow> Entries { get; } = new();

    public EntryRow? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(SelectedTitle));
                OnPropertyChanged(nameof(SelectedUserName));
                OnPropertyChanged(nameof(SelectedPasswordMasked));
                OnPropertyChanged(nameof(SelectedUrl));
                OnPropertyChanged(nameof(SelectedNotes));
            }
        }
    }

    public bool HasSelection => _selected is not null;

    public string SelectedTitle => _selected?.Title ?? string.Empty;
    public string SelectedUserName => _selected?.UserName ?? string.Empty;
    public string SelectedPasswordMasked => HasSelection ? "••••••••••" : string.Empty;
    public string SelectedUrl => GetField("URL");
    public string SelectedNotes => GetField("Notes");

    public bool HasEntries => Entries.Count > 0;

    public AllEntriesViewModel(IVaultSessionService session)
    {
        _session = session;
        Refresh();
        _session.Changed += (_, _) => Refresh();
    }

    public void Refresh()
    {
        Entries.Clear();
        var root = _session.Current?.Document.Root;
        if (root is not null)
        {
            foreach (var entry in FlattenEntries(root))
                Entries.Add(new EntryRow(entry));
        }
        // Sort A→Z by Title.
        var sorted = Entries.OrderBy(static r => r.Title, StringComparer.OrdinalIgnoreCase).ToArray();
        Entries.Clear();
        foreach (var r in sorted) Entries.Add(r);

        Selected = Entries.Count > 0 ? Entries[0] : null;
        OnPropertyChanged(nameof(HasEntries));
    }

    private static IEnumerable<KdbxEntry> FlattenEntries(KdbxGroup group)
    {
        foreach (var e in group.Entries) yield return e;
        foreach (var child in group.Groups)
            foreach (var e in FlattenEntries(child)) yield return e;
    }

    private string GetField(string key)
    {
        if (_selected?.Entry is null) return string.Empty;
        if (_selected.Entry.Strings.TryGetValue(key, out var value))
            return value.Value;
        return string.Empty;
    }
}
