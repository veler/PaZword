# Day-1 spike — Custom title bar + NavigationView LeftCompact

> **Owner:** Nova
> **Date:** 2026-05-22
> **Goal:** confirm the layout for sprint 2 (`MainWindow` with `ExtendsContentIntoTitleBar=true` + WinAppSDK 1.7+ `TitleBar` typed control + `NavigationView` `PaneDisplayMode=LeftCompact`) has no drag-region or layout gotchas on Win11 22H2+ (we target ≥ 22621.0 per `PaZword.App.csproj`).

## Plan in one paragraph

The WinAppSDK 1.7+ ships a typed `Microsoft.UI.Xaml.Controls.TitleBar` control that takes care of drag-region computation **automatically**: interactive children placed in the `LeftHeader`, `Content` (centered slot, perfect for our always-visible search), and `RightHeader` slots pass-through input correctly; the rest of the bar area is auto-marked as draggable. We pair it with the framework `IsPaneToggleButtonVisible` + `PaneToggleRequested` event for the hamburger so we don't have to draw our own. The NavigationView lives in row 1 of the window grid, *below* the title bar, in `LeftCompact` display mode (48 px rail). This is exactly the Edge/Settings pattern Kira and Milo asked for in `brainstorm-app-shell-ux.md §2.2 / §3`.

## Confirmed APIs (from MS Learn, current docs)

| Slot | API | Use |
|---|---|---|
| Hamburger | `TitleBar.IsPaneToggleButtonVisible` + `PaneToggleRequested` | Drives `NavigationView.IsPaneOpen` toggle |
| Brand icon | `TitleBar.IconSource` (`ImageIconSource`) | App icon — Mica-friendly PNG already in `Assets/AppIcon.ico` |
| Title | `TitleBar.Title` | "PaZword" |
| Search box | `TitleBar.Content` | Centered `AutoSuggestBox`; passthrough auto |
| User chevron + lock | `TitleBar.RightHeader` | `DropDownButton` + `Button` (Segoe Fluent Icons) |
| Caption buttons | OS-drawn via `AppWindowTitleBar` | Untouched — we keep system min/max/close |

Notes:
- `ExtendsContentIntoTitleBar = true` **must** be set in code (XAML throws). Done in `MainWindow.xaml.cs`.
- `SetTitleBar(AppTitleBar)` registers our control as the drag-region source — done once at window construction.
- We deliberately do **not** call `AppWindow.TitleBar.SetDragRectangles` manually. The `TitleBar` typed control handles it; the older manual recipe (with `XamlRoot.RasterizationScale`, `SizeChanged`-recompute, etc., from the "Title bar customization → Full customization" doc) is the legacy path we are *not* on.
- `TitleBar.PreferredHeightOption` — the typed control matches `Tall` automatically when `Content`/`LeftHeader`/`RightHeader` are populated. No need to set explicitly.

## Layout decision

```
MainWindow (ExtendsContentIntoTitleBar=true, MicaBackdrop)
└── Grid
    ├── [Row 0, Auto]   TitleBar  x:Name=AppTitleBar
    │                    ├── (slots populated by IShellChromeService per page)
    │                    └── IsPaneToggleButtonVisible bound to ShellChrome.IsPaneToggleVisible
    └── [Row 1, *]      Frame     x:Name=RootFrame
                         └── Page (Onboarding / Welcome / Locked / VaultShell / Migration)
                              └── (VaultShell) Grid
                                   ├── [Col 0]  NavigationView PaneDisplayMode=LeftCompact
                                   └── [Col 1]  Frame ContentFrame (AllEntries / Favorites / Trash / Settings / About)
```

Why a single window-level `TitleBar` (not per-page):
1. `SetTitleBar()` is window-scoped. Having one element + swapping its content is *cheaper* than re-registering on every nav.
2. The drag-region is recomputed only when the title bar element resizes; swapping slot contents through the chrome service doesn't perturb it.
3. AOT: one control, one explicit XAML graph. No reflection, no element-finding tricks.

## Slot population matrix (driven by `IShellChromeService`)

| Page | LeftHeader | Icon+Title | Content | RightHeader | IsPaneToggleButtonVisible |
|---|---|---|---|---|---|
| OnboardingPage | — | brand icon + "PaZword" | — | — | false |
| WelcomePage | — | brand icon + "PaZword" | — | — | false |
| LockedPage | — | brand icon + "PaZword" | — | — | false |
| MigrationPlaceholderPage | — | brand icon + "PaZword" | — | — | false |
| VaultShellPage | — | brand icon + "PaZword" | `AutoSuggestBox` (search, no-op) | user chevron `DropDownButton` + 🔒 lock `Button` | **true** |

The chrome service exposes:
- `void SetMode(ShellChromeMode)` → `Brand` / `Full` / `None`
- `void FocusSearch()` — called by Ctrl+F / Ctrl+K accelerator
- `IRelayCommand TogglePaneCommand` — bound from MainWindow to `TitleBar.PaneToggleRequested`

`MainWindow` binds its `TitleBar` slot visibility to the service via `{x:Bind}` `Mode=OneWay`.

## Drag-region gotchas considered (and the answers)

| Concern | Resolution |
|---|---|
| AutoSuggestBox in title bar swallows clicks meant for dragging | The typed `TitleBar.Content` slot is automatically a passthrough region — verified in MS Learn anatomy docs. No manual `SetDragRectangles` call needed. |
| Hamburger and chevron flyouts pop *under* the OS caption buttons | `TitleBar.LeftHeader` / `RightHeader` are inside the calc — they stay clear of caption-button reserved area (system reserves the right-most ~138 px). |
| `Mica` + extends-into-title-bar shows neutral fallback when window inactive | Expected Fluent behavior. No special handling. |
| `NavigationView` insists on hosting its own pane toggle button | We set `IsPaneToggleButtonVisible="False"` on `NavigationView` and drive the pane from the `TitleBar` button instead (the recommended pattern when extending into the title bar). |
| Window-min size accidentally crops Content slot | We don't set a custom min size; default is fine for Welcome/Locked. For VaultShell we'll set `Window.AppWindow.Resize` baseline only if we hit a problem; sprint 2 ships with default. |
| `PreferredHeightOption=Tall` toggling between pages causes layout flicker | We do not toggle the height — the typed control selects it based on slot content, and our slots are present on VaultShell only; brand-only pages get Standard height. Acceptable per Milo's motion budget (no flicker noted in WinUI Gallery reference impl). |
| Right-to-left flow direction | en-US only per D-007; not exercised in v1. |

## What this spike does **not** validate

- **Visual** verification of Mica + brand-tinted title bar under the OS caption-button area requires running on a real Win11 box. That validation is folded into **T-2.13 (MSIX visual smoke)** and **T-2.14 (Ivy gate)**. Build artifacts in `bin/x64/Debug` are produced fine by sprint-0's build pipeline, which we re-ran clean against our updated `MainWindow.xaml`.
- DPI-scale edge cases at 175% / 200% require visual confirmation. Documented as a follow-up on the Ivy gate checklist for T-2.13.

## Go / no-go

✅ **GO.** Layout is supported, drag regions are auto-managed by the typed `TitleBar`, and the `NavigationView` LeftCompact + `IsPaneToggleButtonVisible=False` + window-level toggle pattern is documented as the WinAppSDK 1.7+ recommended approach.

Sprint 2 pages can be built against this layout without revisiting drag regions.
