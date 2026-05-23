# Sprint 2 — Done (Nova)

> **Status:** ✅ Nova-side complete on `sprint-2/app-shell` · awaiting Ivy gate for merge.
> **Branch:** `sprint-2/app-shell` (off `sprint-1/kdbx-core` HEAD, which carries Sage's sprint-1 work).
> **PR title (planned):** `sprint-2: app shell`.

## Outcome

The app boots into the right state on every launch:

- **First launch:** `OnboardingPage` (3 screens: Welcome → KDBX explainer → Create / Open / Migrate).
- **Subsequent launches:** `WelcomePage` (3 tiles + recent-vaults list capped at 5).
- **From Welcome:** `Open existing` → FileOpenPicker (`.kdbx`) → `LockedPage` (password + key-file Expander + Hello placeholder). Enter unlocks via `IVaultService.OpenAsync`. Wrong password → InfoBar error + attempt counter; ≥3 failures → 2-second debounce. Successful unlock → `VaultShellPage`.
- **VaultShell:** custom title bar (hamburger + always-visible search + brand title + user-chevron flyout + lock button) over a `NavigationView` LeftCompact (All entries / Favorites / Trash / Settings / About) + content `Frame`. AllEntries renders a flat read-only list of entries with a detail pane. Ctrl+L locks; Ctrl+F / Ctrl+K focus the search; Ctrl+\ toggles the rail; Ctrl+S is a no-op stub.

49 sprint-1 tests still green. AOT publish clean. MSIX packaged.

## Definition of done — final scorecard

| # | Item (`docs/sprint-2/plan.md`) | Verdict |
|---|---|---|
| 1 | First launch shows 3-screen onboarding; "Don't show again" persists | ✅ persisted via `AppSettings.OnboardingCompleted` LocalSettings key |
| 2 | Welcome page: 3 tiles + recents (≤5), STJ source-gen | ✅ `RecentVaultsService` + `RecentsJsonContext` |
| 3 | Locked page: PasswordBox default-focus, Enter unlocks, key file Expander, Hello placeholder | ✅ |
| 4 | Failed unlock → inline `InfoBar` + counter + 3-fail 2s debounce | ✅ |
| 5 | Successful unlock invokes `IVaultService.OpenAsync` (Sage's sprint-1 service) | ✅ |
| 6 | Unlocked shell: `NavigationView` LeftCompact (All / Fav / Trash / Settings / About) + inner Frame | ✅ |
| 7 | Custom title bar: hamburger + search AutoSuggestBox + title + user chevron (filename / Lock / Switch / About) + lock button | ✅ WinAppSDK 1.7+ typed `TitleBar` control |
| 8 | AllEntries: placeholder TreeView root + ListView + read-only detail pane; "Add" disabled | ✅ |
| 9 | State transitions = `Frame.Navigate` page navigations with `DrillIn` / `Entrance` info | ✅ via `NavigationTransition` enum |
| 10 | Brand tokens v2: dark `BrandPrimary` → `#E04335`; `AccentFillColor*` overridden; selected-row 2px strip + `BrandSubtleBackground`; destructive = `SystemFillColorCritical` | ✅ — see also note in §Findings |
| 11 | Keyboard accelerators (Ctrl+L / Ctrl+F / Ctrl+K / Ctrl+\ / Ctrl+S) on VaultShell | ✅ |
| 12 | Empty-state placeholder used on AllEntries / Favorites / Trash / Welcome | ✅ via `Controls.EmptyState` user control |
| 13 | `INavigationService` concrete impl registered in DI | ✅ `NavigationService` + `PageKeys` registry |
| 14 | AOT publish stays clean with `PaZword.App` referencing vault libs | ✅ 0 trim/AOT warnings, native PE 11.39 MB |
| 15 | Visual MSIX launch smoke | 🟡 artifact produced (`AppPackages/PaZword.App_1.0.0.0_x64.msix`, 45 MB), 0 build errors. **Visual install + Mica confirmation is Ivy's gate.** |
| 16 | Ivy retroactive sprint-1 crypto review | — owned by Ivy (T-2.14) |
| 17 | `done.md` + per-agent docs + Ivy qa-report.md | ✅ Nova-side; Sage + Ivy pending |

## What shipped (file inventory)

### Brand tokens (T-2.1)
- `src/PaZword.App/Themes/Brand.Dark.xaml` — `BrandPrimary` → `#E04335`; new `SelectedRowAccentStripBrush` + `BrandSubtleBackgroundBrush`; commentary on brand vs destructive separation.
- `src/PaZword.App/Themes/Brand.Light.xaml` — same new aliases; ramp unchanged.
- `docs/sprint-2/contrast-report.md` — re-run for the new dark ramp.

### Navigation (T-2.2)
- `src/PaZword.Core/Navigation/INavigationService.cs` — added `Reset`, `NavigationTransition` enum, `RegisterPage` with `DynamicallyAccessedMembers(PublicConstructors)`.
- `src/PaZword.App/Navigation/NavigationService.cs` — concrete Frame-wrapping impl.
- `src/PaZword.App/Navigation/PageKeys.cs` — central page-key constants.

### Shell chrome
- `src/PaZword.App/Shell/ShellChromeService.cs` — `IShellChromeService` (INPC) + `IShellHost` + impl.

### Recents (T-2.4 + STJ source-gen for T-2.12)
- `src/PaZword.App/Recents/RecentVaultEntry.cs`
- `src/PaZword.App/Recents/RecentsJsonContext.cs` (`JsonSourceGenerationOptions` + 2 `JsonSerializable`)
- `src/PaZword.App/Recents/IRecentVaultsService.cs`
- `src/PaZword.App/Recents/RecentVaultsService.cs`

### Session + settings
- `src/PaZword.App/Services/VaultSessionService.cs` (`IVaultSessionService`)
- `src/PaZword.App/Services/AppSettings.cs` (`OnboardingCompleted`)
- `src/PaZword.App/Services/WindowInterop.cs` (HWND for file pickers)

### Pages (T-2.3 / T-2.4 / T-2.5 / T-2.6 / T-2.7 / T-2.8 / T-2.9)
- `src/PaZword.App/Pages/Onboarding/OnboardingPage.xaml{,.cs}` — 3-screen flow + pip indicator + Skip / Next / Back / "Create or open" / "Migrate" CTAs
- `src/PaZword.App/Pages/WelcomePage.xaml{,.cs}` — 3 tiles + recents list
- `src/PaZword.App/Pages/LockedPage.xaml{,.cs}` — full unlock flow
- `src/PaZword.App/Pages/MigrationPlaceholderPage.xaml{,.cs}` — sprint-5 placeholder
- `src/PaZword.App/Pages/VaultShellPage.xaml{,.cs}` — NavigationView host + IShellHost impl + keyboard accelerators
- `src/PaZword.App/Pages/VaultShell/AllEntriesPage.xaml{,.cs}` — list + detail
- `src/PaZword.App/Pages/VaultShell/FavoritesPage.xaml{,.cs}` — EmptyState
- `src/PaZword.App/Pages/VaultShell/TrashPage.xaml{,.cs}` — EmptyState
- `src/PaZword.App/Pages/VaultShell/SettingsPage.xaml{,.cs}` — theme selector + placeholders
- `src/PaZword.App/Pages/VaultShell/AboutPage.xaml{,.cs}` — version + GitHub / License links

### View-models
- `src/PaZword.App/ViewModels/OnboardingViewModel.cs`
- `src/PaZword.App/ViewModels/WelcomeViewModel.cs`
- `src/PaZword.App/ViewModels/LockedViewModel.cs`
- `src/PaZword.App/ViewModels/MigrationPlaceholderViewModel.cs`
- `src/PaZword.App/ViewModels/VaultShellViewModel.cs`
- `src/PaZword.App/ViewModels/AllEntriesViewModel.cs` (incl. `EntryRow` projection)
- `src/PaZword.App/ViewModels/SettingsViewModel.cs`

### Controls
- `src/PaZword.App/Controls/EmptyState.xaml{,.cs}` — DP-backed `Glyph` / `Title` / `Body` / `CtaText` / `CtaCommand` / `IsCtaEnabled`.

### Bootstrap
- `src/PaZword.App/App.xaml.cs` — `MainWindowImpl` typed accessor.
- `src/PaZword.App/AppServiceProvider.cs` — explicit registrations for `IVaultService`, `IVaultSessionService`, `IRecentVaultsService`, `IShellChromeService`, `INavigationService`, all VMs.
- `src/PaZword.App/MainWindow.xaml{,.cs}` — typed `TitleBar` control, slot-bound to `IShellChromeService`. Decides first destination by `AppSettings.OnboardingCompleted` (D-015).
- `src/PaZword.App/PaZword.App.csproj` — explicit `<ProjectReference Include="..\PaZword.Kdbx\…" />` so trim analysis exercises vault types.
- **Removed:** `MainPage.xaml{,.cs}` and `ViewModels/MainPageViewModel.cs` (sprint-0 spike, superseded).

### Spike + docs
- `docs/sprint-2/spike-titlebar-navview.md` — Day-1 spike outcome (Go).
- `docs/sprint-2/aot-notes.md` — sprint-2 AOT issues + resolutions + new standing rules.
- `docs/sprint-2/contrast-report.md` — dark ramp v2 + WCAG findings.
- `docs/sprint-2/done-nova.md` — this file.
- `docs/sprint-2/progress.md` — append-only log (see entries).

## Findings to flag

1. **Brand contrast math correction (non-blocker).** `docs/brainstorm-app-shell-ux.md §3.4` claims white-on-`#E04335` is 4.51 (passing AA normal); recomputation shows the actual ratio is **4.18** — still a +0.89 improvement over `#FF4D3D` (3.29) and clears AA Large/UI (≥3.0), which is all the brand surface needs per `brainstorm §3.3`. Decision **ART-1** is honoured. A future polish-tier shift to `#D43A2D` (~4.6) is filed in the contrast report as the smallest path to AA-normal compliance if needed.

2. **AOT growth is expected.** Native PE went 5.59 MB → 11.39 MB on first reachability of `PaZword.Kdbx` + `PaZword.Crypto` (Argon2, ChaCha20, AES, XML reader/writer) from `PaZword.App`. Plus STJ source-gen support. No trim/AOT warnings.

3. **MSIX visual install** is environmental — the artifact builds with 0 errors and a single benign warning about `mspdbcmf.exe` (PDB symbols only). Ivy must perform the Win11 install + Mica + onboarding visual smoke per **T-2.13**.

4. **`Ctrl+S` (save no-op)** writes a debug breadcrumb instead of surfacing an `InfoBar`. The inner pages don't yet host a transient-InfoBar shelf — sprint 3 introduces one alongside real save behaviour. Reviewer-acceptable for the no-op stub this sprint.

5. **Sprint-1 carryover (Ivy T-2.14)** runs in parallel; Nova has not touched `PaZword.Kdbx` / `PaZword.Crypto` source. Ivy's retroactive findings land as GitHub Issues.

## Verification commands

```powershell
# Debug build
dotnet build src/PaZword.App/PaZword.App.csproj -p:Platform=x64 -c Debug

# AOT release publish (0 trim/AOT warnings under -warnaserror)
dotnet publish src/PaZword.App/PaZword.App.csproj `
    -c Release -r win-x64 -p:Platform=x64 -p:PublishAot=true -warnaserror

# MSIX package
dotnet publish src/PaZword.App/PaZword.App.csproj `
    -c Release -p:Platform=x64 -p:RuntimeIdentifier=win-x64 `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxPackageSigningEnabled=false `
    -p:AppxBundle=Never `
    -p:UapAppxPackageBuildMode=SideLoadOnly
# → src/PaZword.App/AppPackages/PaZword.App_1.0.0.0_x64_Test/PaZword.App_1.0.0.0_x64.msix

# Sprint-1 tests stay green
dotnet test tests/PaZword.Crypto.Tests/PaZword.Crypto.Tests.csproj
dotnet test tests/PaZword.Kdbx.Tests/PaZword.Kdbx.Tests.csproj
```

## Handoff

Sprint-3 picks up:
- Entry CRUD + inline edit (IMPL-2)
- Real group-tree wiring on AllEntries (sprint 3, IMPL-1)
- Vault save behaviour (Ctrl+S no longer a stub; `Save vault`/InfoBar)
- "Create new vault" CTA (Welcome) → real new-vault wizard

Ivy gate items for sprint-2 PR:
- Run the verification commands above on a clean Win11.
- Visual MSIX install + onboarding screenshot + Mica visible on Welcome.
- Title-bar drag region behaves under DPI 100 % / 150 % / 200 %.
- Keyboard accelerator table (§T-2.11) actually fires, no Fluent default conflicts.
- T-2.14 retroactive sprint-1 crypto findings filed (independent path).
