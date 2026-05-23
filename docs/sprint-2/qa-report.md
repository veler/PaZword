# Sprint 2 — QA report (Ivy gate)

> **Author:** Ivy
> **Date:** 2026-05-23
> **Branch under review:** `sprint-2/app-shell`
> **Verdict:** 🟢 **PASS — merge approved, conditional on one human-environment item (DoD #15).**

---

## Headline

All 17 DoD items in `docs/sprint-2/plan.md` are met or substantially met. Sixteen items I verified directly on the sprint-2 branch; one (DoD #15 — visual MSIX install + Mica + onboarding smoke on a clean Win11) requires a clean / Sandbox environment I do not have. The MSIX artifact builds cleanly and is well-formed; **final visual confirmation belongs to Etienne before push**, but it does not block merge of the source PR.

Carryover obligation (DoD #16 — Ivy retroactive sprint-1 crypto review) is **complete**: seven GitHub Issues filed (#35–#41), `docs/sprint-1/qa-report.md` written.

---

## Verification commands re-run

All commands from `docs/sprint-2/done-nova.md §Verification commands` re-executed on a fresh shell.

| Command | Result |
|---|---|
| `dotnet build src/PaZword.App/PaZword.App.csproj -p:Platform=x64 -c Debug` | ✅ 0 warnings, 0 errors, ~21 s |
| `dotnet publish src/PaZword.App/PaZword.App.csproj -c Release -r win-x64 -p:Platform=x64 -p:PublishAot=true -warnaserror` | ✅ 0 trim/AOT warnings, 0 errors |
| Native PE size on disk | ✅ **11.39 MB** (matches `aot-notes.md` claim) |
| MSIX publish (`-p:GenerateAppxPackageOnBuild=true`) | ✅ artifact produced at `src/PaZword.App/AppPackages/PaZword.App_1.0.0.0_x64_Test/PaZword.App_1.0.0.0_x64.msix` |
| MSIX size on disk | ✅ **45.39 MB** (matches `progress.md` claim) |
| `dotnet test tests/PaZword.Crypto.Tests/PaZword.Crypto.Tests.csproj` | ✅ 21/21 |
| `dotnet test tests/PaZword.Kdbx.Tests/PaZword.Kdbx.Tests.csproj` | ✅ 28/28 |
| **Total sprint-1 regression baseline** | ✅ **49/49 green** |

Only diagnostic emitted by the MSIX publish: `warning : Path to mspdbcmf.exe could not be found. A symbols package will not be generated.` — environmental (PDB symbol-package generator for Store upload). Does not affect the MSIX itself.

---

## DoD scorecard

| # | Item (`plan.md` §Sprint goals) | Method | Verdict |
|---|---|---|---|
| 1 | First launch shows 3-screen onboarding; "Don't show again" persists | Source review (`MainWindow.xaml.cs:44` decides first dest by `AppSettings.OnboardingCompleted`; `AppSettings.cs` writes to `LocalSettings`; `OnboardingPage.xaml.cs` sets the flag on Finish/Skip) | ✅ |
| 2 | Welcome page: 3 tiles + recents (≤5), STJ source-gen | Source review (`WelcomePage.xaml` 3 tiles; `RecentVaultsService` caps at 5 sorted by `LastOpenedUtc`; `RecentsJsonContext : JsonSerializerContext` declares both element + array — no reflection STJ) | ✅ |
| 3 | Locked page: PasswordBox default-focus, Enter unlocks, key file Expander, Hello placeholder | Source review (`LockedPage.xaml.cs:31` `Focus(Programmatic)`; `OnPasswordKeyDown` handles Enter; XAML hosts `Expander Header="Use a key file"`; Hello `Button IsEnabled="False"` with tooltip "Available after sprint 7.") | ✅ |
| 4 | Failed unlock → inline `InfoBar` + counter + 3-fail 2s debounce | Source review (`LockedViewModel.cs` constants `FailDebounceThreshold=3 / Ms=2000`; XAML `InfoBar` bound to `HasError` and `IsDebouncing`; counter via `ErrorMessageWithAttempt`) | ✅ |
| 5 | Successful unlock invokes `IVaultService.OpenAsync` | Source review (`LockedViewModel.UnlockAsync` line 213: `await Task.Run(() => _vault.OpenAsync(...))`) | ✅ |
| 6 | Unlocked shell: `NavigationView` LeftCompact + inner Frame; All / Fav / Trash / Settings / About | Source review (`VaultShellPage.xaml` `NavigationView PaneDisplayMode="LeftCompact"`; five `NavigationViewItem` entries with `Tag={x:Bind PageKeys.*}`; `ContentFrame` inside) | ✅ |
| 7 | Custom title bar: hamburger + search + title + user chevron + lock | Source review (`MainWindow.xaml` typed `TitleBar` with `IsPaneToggleButtonVisible` bound to `Chrome.IsFullMode`; `TitleBar.Content` = `AutoSuggestBox`; `RightHeader` = `DropDownButton` flyout (Lock vault / Switch vault / About) + standalone `🔒 Button`); `spike-titlebar-navview.md` documents drag-region rationale | ✅ |
| 8 | AllEntries: placeholder TreeView root + ListView + read-only detail pane; "Add" disabled | Source review (`AllEntriesPage.xaml` — TreeView root "All entries"; ListView bound to `EntryRow` projection; right pane read-only TextBlocks; "Add your first entry" CTA gated) | ✅ |
| 9 | State transitions via `Frame.Navigate` w/ `DrillIn` / `Entrance` | Source review (`NavigationService.NavigateTo` maps `NavigationTransition` enum to `DrillInNavigationTransitionInfo` / `EntranceNavigationTransitionInfo` / `SlideNavigationTransitionInfo`) | ✅ |
| 10 | Brand tokens v2: dark `BrandPrimary` → `#E04335`; `AccentFillColor*` overrides; selected-row 2px strip; destructive = `SystemFillColorCritical` | Source review (`Themes/Brand.Dark.xaml`); contrast re-run in `contrast-report.md` | ✅ — **see Finding F-1 below** for documented contrast caveat |
| 11 | Keyboard accelerators (Ctrl+L / Ctrl+F / Ctrl+K / Ctrl+\ / Ctrl+S) on VaultShell | Source review (`VaultShellPage.xaml.cs` `RegisterAccelerators` and `OnAcceleratorInvoked`); five accelerators registered with `ScopeOwner = this`; menu flyout uses a disabled-hint `KeyboardAccelerator` to show "Ctrl+L" visually | ✅ — **see Finding F-2 below** for `Ctrl+S` no-op |
| 12 | Empty-state placeholder used on AllEntries / Favorites / Trash | Source review (`Controls/EmptyState.xaml{,.cs}` user control with DP-backed Glyph/Title/Body/CtaText/CtaCommand/IsCtaEnabled; consumed by AllEntries, FavoritesPage, TrashPage) | ✅ |
| 13 | `INavigationService` concrete impl registered in DI | Source review (`AppServiceProvider.cs:49`: `services.AddSingleton<INavigationService, NavigationService>()`; MainWindow casts to concrete to call `Initialize(RootFrame)` + `RegisterPage<T>`) | ✅ |
| 14 | AOT publish clean with vault libs referenced | Re-ran `dotnet publish ... -p:PublishAot=true -warnaserror` | ✅ **0 trim/AOT warnings**; PE = 11.39 MB |
| 15 | Visual MSIX launch smoke (Mica visible on Welcome) | Artifact build ✅; **visual install + launch on clean Win11/Sandbox not performed by Ivy** (requires environment I do not have) | 🟡 **CONDITIONAL — see §"Block / unblock" below** |
| 16 | Ivy retroactive sprint-1 crypto review filed | This file + `docs/sprint-1/qa-report.md` + Issues #35–#41 | ✅ — see `docs/sprint-1/qa-report.md` for the full findings |
| 17 | `done.md` + per-agent docs + Ivy `qa-report.md` | `done-nova.md` ✅, `progress.md` ✅, `aot-notes.md` ✅, `contrast-report.md` ✅, `spike-titlebar-navview.md` ✅, this file ✅. **No `done-sage.md`** (Sage attested in `progress.md`: T-2.12 standby, no Sage-authored code). **No `done.md`** (Producer-authored on merge per house process) | 🟢 sufficient |

**16/17 green; 1/17 conditional (DoD #15).**

---

## Block / unblock

### Hard fails (block merge)

**None.** All source-side DoD items are met. AOT clean. Tests green. No regressions.

### Conditional (block **push**, not merge of source PR)

**DoD #15 — Visual MSIX install / launch / Mica smoke on a clean Win11.**

The `.msix` builds and is well-formed (45.39 MB, expected sideload structure). I cannot install + launch in this environment without modifying the active developer machine (which already has the legacy v1 PaZword installed — different package family name, would coexist, but installing an unsigned dev-build certificate to the active user trust store is out of scope for a QA pass).

Recommended human verification before Etienne pushes the PR / merges:

1. On a clean Win11 (≥ 22621) **or** Windows Sandbox:
   - Install the dev cert (`AppPackages/.../*_x64_Test/Install.ps1`).
   - `Add-AppxPackage .\PaZword.App_1.0.0.0_x64.msix`.
   - Launch from Start menu.
   - Confirm: Mica backdrop is visible; title bar shows brand icon + "PaZword"; first-launch lands on Onboarding (Welcome screen with the brand pip + Next / Skip).
2. Capture one screenshot (any drive); append to `docs/sprint-2/` as `msix-smoke-screenshot.png`.
3. Re-tick DoD #15 in `done-nova.md` and the producer-side `done.md` when written.

This is environmental, not architectural — the artifact does build and the AOT publish is clean, which is the part automation can verify. If a real smoke fails, **that** would block, and Nova would need to address.

### Recommended also-merge

Bundle into the sprint-2 PR (small, mechanical):

- **Issue #36** — Argon2d read-path regression test. Single test file; ~30 LOC; closes a policy lock-in gap.
- **Issue #38** — Four zeroization gaps. Single refactor PR; ~50 LOC across 4 files; tightens defense-in-depth.

Reason: both touch only `PaZword.Kdbx` / `PaZword.Crypto`, which Nova has not modified this sprint, so there is no merge conflict risk. Closing them now means sprint 3 starts on a cleaner crypto floor.

Optional (can also slip to sprint 3):

- **Issue #41** — `VaultCredentials.FromPassword(ReadOnlySpan<char>)` overload. The sprint-2 `LockedViewModel` is the first real consumer of `FromPassword(string)`. Adding the span overload before sprint 3 starts wiring entry-edit save flows is cheaper than after.

---

## Findings — flagged but not blocking

### F-1 — Dark `BrandPrimary` does not clear AA normal-text (documented + accepted)

`contrast-report.md` correctly recomputes white-on-`#E04335` at **4.18** (not the 4.51 quoted in `brainstorm-app-shell-ux.md §3.4`). The shift from `#FF4D3D` (3.29) still improves contrast +0.89 and clears AA Large / UI ≥ 3.0, which is all the brand surface needs per `brainstorm §3.3` — body text is never drawn on `BrandPrimary` in dark mode (CTAs use bold + 14 pt = "large"; selection strip is UI only).

Decision **ART-1** is honoured. The contrast report flags `#D43A2D` (~4.6) as the smallest brand-true shift to reach AA-normal if Etienne ever wants strict compliance; sprint 9 polish. Acceptable as-is.

### F-2 — `Ctrl+S` writes a `Debug.WriteLine` instead of an `InfoBar` (acknowledged drift from plan §T-2.11)

`plan.md T-2.11` says:

> `Ctrl+S` — Unlocked shell, no-op + `InfoBar` "No changes to save" (real save sprint 3).

`VaultShellPage.xaml.cs::ShowSaveNoOp` writes `Debug.WriteLine` because inner pages do not yet host a transient-InfoBar shelf. Acknowledged in `done-nova.md §Findings #4` as deliberate (real save + a transient-InfoBar shelf both arrive in sprint 3 together).

Reviewer-acceptable — the keyboard accelerator is registered and routed; the *visible* feedback is degraded by one sprint. Note for sprint 3: do not let this drift further.

### F-3 — `VaultService` is constructed without a logger in DI (closes the loop on Issue #40)

`AppServiceProvider.cs:41`:

```csharp
services.AddSingleton<IVaultService>(static _ => new VaultService());
```

This factory bypasses DI and instantiates `VaultService()` (parameterless ctor) → `new KdbxReader()` + `new KdbxWriter()` both with `null` loggers. Combined with the no-op `services.AddLogging(static _ => { })`, the entire `KdbxKeyDeriver` below-floor warning is dead in production until both are wired.

This is **exactly** Issue #40 manifesting. Not a sprint-2 blocker (the sprint-2 plan does not require functional logging — `LogWarning` is a "should-emit" not a "must-emit"). Sprint 3 / 4 should fix DI to resolve `ILogger<>` properly **before** any below-floor vault is opened in the wild.

### F-4 — `LockedViewModel._keyFileBytes` is never zeroized (sprint-2 scope; can slip)

`LockedViewModel.cs:189`: `_keyFileBytes = bytes;` (the file bytes from `File.ReadAllBytesAsync`).
`VaultCredentials.FromPassword(_password, _keyFileBytes)` (line 212) clones the array internally, so the credentials wrapper handles its own copy. The original `_keyFileBytes` on the viewmodel stays in heap unzeroized until `ClearKeyFile()` or `Initialize()` clears the *reference* (but not the bytes).

Add `CryptographicOperations.ZeroMemory(_keyFileBytes)` before clearing the reference. Two-line fix. Acceptable to do in the sprint-2 PR addendum or in sprint 3.

### F-5 — `Lock()` / `SwitchVault()` fire-and-forget `CloseAsync` (acceptable today)

`VaultShellPage.xaml.cs:138 / 146`:

```csharp
_ = handle is null ? Task.CompletedTask : _vault.CloseAsync(handle);
```

`VaultService.CloseAsync` is synchronous (calls `handle.Dispose()` and returns `Task.CompletedTask`). The fire-and-forget pattern is fine today. If a future `CloseAsync` becomes truly async (e.g. flushes a lock file), this should be `await`ed. Not a sprint-2 blocker; note for sprint 7 (security hardening pass).

---

## Cross-cutting checks — clean ✓

- **AOT discipline** (`PROJECT_BRIEF §4`): `{x:Bind}` only (no `{Binding}` anywhere); STJ source-gen via `RecentsJsonContext`; explicit DI registrations in `AppServiceProvider`; no `BinaryFormatter`, no `Activator.CreateInstance`, no reflection emit. ✓
- **Brand tokens** (`PROJECT_BRIEF §6`): every red surface in the new pages uses `BrandPrimaryBrush` / `BrandSubtleBackgroundBrush` / `SelectedRowAccentStripBrush` — no hardcoded hex in sprint-2 XAML. ✓
- **Destructive vs brand red separation** (decision ART-2): Trash empty state uses `SystemFillColorCritical` for the destructive CTA per the inline XAML comments in `Brand.Dark.xaml`. ✓
- **Title-bar drag region**: typed `TitleBar` slot model + `IsPaneToggleButtonVisible=False` on `NavigationView` matches the WinAppSDK 1.7+ recommended pattern (per `spike-titlebar-navview.md`). No manual `SetDragRectangles` calls — confirmed clean. ✓
- **Keyboard accelerators do not break Fluent UX**: each accelerator registered with `ScopeOwner = this` (page scope) — they fire only when the unlocked shell is focused, so they do not collide with PasswordBox's Enter handler on the Locked page, and `Ctrl+F`/`Ctrl+K` do not conflict with WinUI defaults (no built-in WinUI app-level shortcuts for these). The menu flyout's disabled `KeyboardAccelerator` element is the canonical Fluent pattern for showing "Ctrl+L" hint text without re-registering the actual handler. ✓
- **Sprint-1 regression baseline holds**: 49/49 tests pass on the sprint-2 branch with `PaZword.App` now actually referencing the vault libs. Trim/AOT analyzer has more reachable surface to chew on, still 0 warnings. ✓

---

## Gate verdict

🟢 **PASS** — merge `sprint-2/app-shell` once Etienne completes the visual MSIX smoke (DoD #15) on a clean Win11 or Sandbox. The source PR itself is approval-ready.

Recommend also folding Issues #36 + #38 into the merge PR (small, low-risk, closes sprint-1 carryover policy lock-in at the same time).

Sprint-1 retroactive obligation closed: `docs/sprint-1/qa-report.md` + Issues #35–#41.
