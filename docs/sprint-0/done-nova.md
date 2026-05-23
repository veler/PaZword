# Sprint 0 — Done (Nova)

> Completed: 2026-05-22

## Tasks completed

| Task | Status | Notes |
|------|--------|-------|
| T-0.2 WinUI 3 + AOT spike | ✅ Done | `PaZword.App.exe` AOT-published, ~5.6 MB, **0 warnings / 0 errors** under `TreatWarningsAsErrors=true`. Mica backdrop + custom title bar + app icon all wired. |
| T-0.3 DI + MVVM bootstrap (Nova half) | ✅ Done | `AppServiceProvider.Build()` called from `App` ctor. `MainPageViewModel` proves DI + `CommunityToolkit.Mvvm` + `{x:Bind}` end-to-end under AOT. |
| T-0.4 Brand resource dictionary | ✅ Done | `Themes/Brand.{Light,Dark}.xaml` per brief §6 red ramp; `IThemeService` / `ThemeService` (LocalSettings) for Light/Dark/System; WCAG report in `docs/sprint-0/contrast-report.md`. |

## Definition-of-done items I touched

- ✅ DoD #1 — `dotnet publish -c Release -r win-x64 PaZword.App.csproj` produces a working AOT binary (verified locally on this workstation).
- ✅ DoD #6 — Brand resource dictionary (light + dark) committed; Mica backdrop visible.

## AOT status (verified end-to-end)

- `dotnet publish -c Release -r win-x64 src/PaZword.App` → 0 trim warnings, 0 AOT warnings, 0 errors. `TreatWarningsAsErrors=true` is active throughout the build.
- The published artifact is a single Win32 PE: `PaZword.App.exe` (~5.6 MB). No `System.Private.CoreLib.dll` next to it (confirmed by the new smoke test). The .pdb sits alongside for diagnostics.
- Three friction points hit and documented in `docs/sprint-0/aot-notes.md`:
  1. `MVVMTK0045` rules out field-based `[ObservableProperty]` under WinUI 3 + AOT. Until the partial-property generator works in this toolchain (or we change `LangVersion`), VMs use the manual `ObservableObject.SetProperty(ref ..)` pattern. `[RelayCommand]` is unaffected and is in use.
  2. `Microsoft.Extensions.Logging.Debug` is not in CPM. Removed the `AddDebug()` call; sprint 2 adds a file sink.
  3. WinUI XAML compiler requires `-p:Platform=x64` (or ARM64); without it Pass2 throws `WMC9999`. CI already passes this. Documented for future contributors.

## Files added/changed by Nova

- `src/PaZword.App/App.xaml` — merged brand `ThemeDictionaries` (Light/Default/HighContrast).
- `src/PaZword.App/App.xaml.cs` — builds DI on ctor, exposes `MainWindow` + `App.Services`, applies persisted theme on launch. Namespace `PaZword.App`.
- `src/PaZword.App/MainWindow.xaml.cs` — namespace fix (`PaZword.App`).
- `src/PaZword.App/MainPage.xaml{,.cs}` — resolves `MainPageViewModel` from DI; `{x:Bind}` to `ViewModel.Status` (OneWay) and `ViewModel.PingCommand`. Namespace fix.
- `src/PaZword.App/AppServiceProvider.cs` — added `MainPageViewModel` (transient) + `IThemeService → ThemeService` (singleton). Dropped `AddDebug` (see AOT note 2).
- `src/PaZword.App/ViewModels/MainPageViewModel.cs` — new.
- `src/PaZword.App/Services/ThemeService.cs` — new. Reads/writes `ApplicationData.LocalSettings`; applies `ElementTheme` to the main window root.
- `src/PaZword.App/Themes/Brand.Light.xaml` — new.
- `src/PaZword.App/Themes/Brand.Dark.xaml` — new.
- `src/PaZword.Core/Theming/IThemeService.cs` — new. `AppTheme { System, Light, Dark }`.
- `tests/PaZword.App.UITests/PaZword.App.UITests.csproj` — switched to `net10.0-windows10.0.22621.0`, added xUnit packages.
- `tests/PaZword.App.UITests/MainWindowSmokeTests.cs` — new. Asserts the AOT publish artifact's shape; full WinAppDriver scenarios scheduled for sprint 2.
- `docs/sprint-0/aot-notes.md` — new.
- `docs/sprint-0/contrast-report.md` — new. Computed WCAG ratios with a reproducible PowerShell snippet.
- `docs/sprint-0/progress.md` — appended Nova entries.

## Smoke test results

| Project | Tests | Result |
|---|---|---|
| `PaZword.Crypto.Tests` | 7 | ✅ all pass (Sage's Argon2id KAT) |
| `PaZword.App.UITests` | 1 | ✅ pass (`Aot_publish_produces_a_native_executable`) |

## Handoff notes

### To Ivy (sprint-end gate)

- DoD #1 verified locally; CI should re-verify on a fresh Windows runner. Expect the same `0 warning(s)` line in the publish log.
- DoD #6 verified visually requires running an MSIX-packaged build; sprint 0 only asserts the resource dictionary compiles and the AOT publish remains clean with theme XAML in place. Visual verification falls to manual QA when packaged builds are available.
- Threat-model-relevant decisions made this sprint by Nova:
  - Theme preference stored in `ApplicationData.LocalSettings`. Non-secret, low risk. Roams with the user account on a packaged build.
  - DI container is built **once** in the `App` constructor with `validateScopes: true`. No lifetime escape, no transient-into-singleton pitfalls.
  - `MainPage` resolves its VM from the static `App.Services` accessor in a parameterless ctor — necessary for `Frame.Navigate(typeof(MainPage))`. Acceptable because the page type is statically referenced.

### To Remy

- **Open question still open:** Kira+Milo UI/UX brainstorm has not happened. Not blocking sprint 0. Must happen before sprint 2 (App shell) and ideally also pre-sprint-3 (Vault list/detail).
- **Token tuning ask:** Milo, please review the dark-mode `BrandPrimary` (`#FF4D3D`) — 3.29 contrast with white text. Either we ship knowing accent fills are for icons/large UI only, or we shift the token to recover AA normal. Detail in `docs/sprint-0/contrast-report.md`.

### To Sage (sprint 1)

- `MainPageViewModel` is a throwaway demo. Sprint 2+ replaces it with real VMs.
- When the KDBX reader lands and exposes types we want to serialize (e.g. for diagnostics export), wire them through a `System.Text.Json` `JsonSerializerContext` — do not introduce reflection-based JSON.
- If you bump `CommunityToolkit.Mvvm` past 8.4.0, please retest the partial-property generator path so we can drop the manual `SetProperty` pattern.

## Things explicitly NOT in this PR

- No interactive UI flows (the empty `MainPage` deliberately ships only a status label + ping button as a DI/MVVM smoke surface).
- No `NavigationService` concrete implementation — only the `INavigationService` interface Sage stubbed in Core.
- No localization wiring (sprint 2+).
- No settings UI — `IThemeService` is a backing service only; sprint 2 builds the Settings page.
