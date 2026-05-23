# Sprint 2 — AOT notes

> Maintained by Nova during T-2.12. Continues `docs/sprint-0/aot-notes.md` and applies
> exclusively to delta introduced by sprint 2 (app shell, vault libs referenced from
> `PaZword.App` for the first time).

## Headline status

- `dotnet publish -c Release -r win-x64 -p:Platform=x64 -p:PublishAot=true -warnaserror`
  on `src/PaZword.App/PaZword.App.csproj` succeeds **with 0 trim/AOT warnings and 0 errors**.
- Native PE size: **11.39 MB** (sprint-1: 5.59 MB). Growth attributed to:
  - `PaZword.Kdbx` / `PaZword.Crypto` now reachable from `PaZword.App` (T-2.12 explicit reference).
  - 9 new pages + 7 new view-models + 4 new services + `EmptyState` user control.
  - `System.Text.Json` source-gen support code for `RecentVaultEntry[]`.
- All 49 sprint-1 tests still pass after the App-project refactor.

## New hints in effect

### 1. `RegisterPage` page types annotated `DynamicallyAccessedMembers(PublicConstructors)`

`PaZword.App.Navigation.NavigationService.RegisterPage(string, [DAM] Type)` and
the strongly-typed overload `RegisterPage<TPage>` both flow
`DynamicallyAccessedMembers(PublicConstructors)` to the page type. This:

- Keeps each registered page's parameterless constructor reachable for `Frame.Navigate(Type)`.
- Matches the interface contract on `PaZword.Core.Navigation.INavigationService.RegisterPage`
  (the trim analyzer emits `IL2092` if the override drops the annotation — fixed during T-2.12).

No `RD.xml`, no `UnconditionalSuppressMessage`, no `DynamicDependency` calls were needed.

### 2. STJ source-gen for the recent-vaults persistence

`PaZword.App.Recents.RecentsJsonContext : JsonSerializerContext` declares
`[JsonSerializable(typeof(RecentVaultEntry))]` and
`[JsonSerializable(typeof(RecentVaultEntry[]))]`. All serialise/deserialise call sites
go through `RecentsJsonContext.Default.RecentVaultEntryArray`. No reflection-based JSON
calls remain in the app.

### 3. Trim/AOT-clean controls used in sprint 2

- `TitleBar` (WinAppSDK 1.7+ typed control)
- `NavigationView`, `NavigationViewItem`
- `Frame`, `Page`
- `ListView`, `TreeView`, `TreeViewNode`
- `PasswordBox`, `TextBox`, `AutoSuggestBox`
- `Expander`, `InfoBar`, `ProgressRing`, `DropDownButton`, `MenuFlyout`, `MenuFlyoutItem`
- `FontIcon`, `ImageIconSource`
- `Button`, `HyperlinkButton`, `ToggleSwitch` (not yet used in sprint 2 but present in palette)

All bind via `{x:Bind}`. The classic `{Binding}` does not appear anywhere in the app project.

## Issues encountered (and resolutions)

### 1. `WMC9999` on `MarkupCompilePass2` — surface error masks a real bind-path parser exception

Symptom (build output):
> Xaml Internal Error WMC9999: Could not find any resources appropriate for the specified culture …
> "Microsoft.UI.Xaml.Markup.Compiler.ErrorMessages.resources" was correctly embedded …

Real root cause (only visible via `-flp:Verbosity=diagnostic` log):

```
at Microsoft.UI.Xaml.Markup.Compiler.Parsing.ParseErrorListener.SyntaxError(...)
at Microsoft.UI.Xaml.Markup.Compiler.BindPathStep.Parse(String bindPath, ...)
at Microsoft.UI.Xaml.Markup.Compiler.BindAssignment.ParsePath()
```

The bind-path parser threw on `{x:Bind Strings[Title].Value, …}` — `{x:Bind}` does **not**
support dictionary indexer syntax with literal keys.

**Resolution:** introduced `PaZword.App.ViewModels.EntryRow` — a thin DTO that pre-projects
`Title` and `UserName` strings — and the list-item `DataTemplate` binds to `EntryRow`
properties directly. AOT-clean and easier to reason about.

**Standing rule (added):** no `{x:Bind}` dictionary-indexer paths; always go through a
typed view-model property.

### 2. `IL2092` — DAM annotations must match across `INavigationService` override

The first cut put `[DynamicallyAccessedMembers(PublicConstructors)]` only on the impl
parameter, not the interface parameter. The IL trim analyzer (`-warnaserror`) rejected
the mismatch.

**Resolution:** mirrored the annotation onto `INavigationService.RegisterPage`. The
attribute reference required no new `using` (already in `System.Diagnostics.CodeAnalysis`).

### 3. `WMC0015` — `InfoBar.ActionButton` must be `ButtonBase`

First cut placed a `<TextBlock>` inside `InfoBar.ActionButton`. The control expects
`ButtonBase`-derived content.

**Resolution:** removed the action-button slot, baked the attempt counter into the message
text via a code-behind helper (`ErrorMessageWithAttempt(message, count)`).

### 4. `WMC1506` — `OneWay` binding chain through an interface with no INPC marker

`{x:Bind Chrome.IsFullMode, Mode=OneWay}` on `MainWindow.xaml` raised the warning because
`IShellChromeService` did not declare INPC. The implementation does (inherits
`ObservableObject`) but the compiler only sees the interface's static type.

**Resolution:** declared `interface IShellChromeService : INotifyPropertyChanged`. The
warning vanishes; the implementation already raises notifications.

## Standing rules (enforced by reviewers)

1. `{x:Bind}` only — never `{Binding}`. (Carry-over from sprint 0.)
2. No `{x:Bind}` dictionary-indexer paths (sprint-2 lesson #1). Use a row VM.
3. Every `INavigationService.RegisterPage` overload / impl carries
   `[DynamicallyAccessedMembers(PublicConstructors)]` on the `Type` parameter.
4. All JSON I/O routes through a `JsonSerializerContext` (`RecentsJsonContext` today;
   add another context per new payload).
5. Any new interface that participates in WinUI `{x:Bind … OneWay/TwoWay}` must declare
   `: INotifyPropertyChanged`. (Sprint-2 lesson #4.)
6. `CommunityToolkit.Mvvm` partial-property generator status (sprint-0 carry-over): still
   not adopted; observable properties use manual `SetProperty`. `[RelayCommand]` is in
   active use across sprint 2 VMs and remains AOT-clean.

## Inventory (sprint 2 delta)

| Project | `IsTrimmable` | AOT analyzer | Notes |
|---|---|---|---|
| `PaZword.Core` | true | on | New: `INavigationService` (Reset, NavigationTransition enum). |
| `PaZword.App` | false (AOT root) | on (via `PublishAot=true`) | New: 9 pages, 7 VMs, 4 services, 1 user control, 2 STJ JsonSerializable types. Verified clean. |
| `PaZword.Kdbx` | true | on | Now actually reachable from `PaZword.App` (T-2.12 verified clean). |
| `PaZword.Crypto` | true | on | Now actually reachable from `PaZword.App` (T-2.12 verified clean). |

## Open carryover

- The `MVVMTK0045` partial-property generator workaround remains in place. Reopen when
  CommunityToolkit.Mvvm ships a release whose generator emits the C# 13 partial-property
  implementation in our toolchain.
- Visual MSIX install / launch confirmation is Ivy's call (T-2.13). The artifact builds
  cleanly via the AOT publish above; identity-launch with package identity is the runtime
  verification step.
