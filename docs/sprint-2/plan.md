# Sprint 2 — App Shell

> **Goal:** ship the three top-level UI states (Welcome / Locked / Unlocked) with navigation, custom title bar, brand-token v2, and a stubbed unlock flow that exercises `IVaultService` for the first time under AOT.
> **Input:** `docs/brainstorm-app-shell-ux.md` (decisions UX-1..8, ART-1..6, IMPL-1..3 are all locked).
> **Output:** a runnable app where you can: see onboarding on first launch, see Welcome with 3 tiles + recents, click Open Existing → pick a `.kdbx` → land on Locked page → type password → Sage's `IVaultService` opens the vault → land on Unlocked shell with NavigationView and a placeholder list/detail.

---

## Sprint goals (definition of done)

1. ✅ First launch shows 3-screen onboarding; "Don't show again" persists.
2. ✅ Welcome page: 3 tiles (New / Open / Migrate stub), recent-vaults list (max 5), persisted to `LocalSettings`.
3. ✅ Locked page: master password field default-focus, Enter unlocks, key file Expander, Hello button placeholder (disabled, tooltip).
4. ✅ Failed unlock = inline `InfoBar` error below field, attempt counter, no dialog. After 3 failures, 2-second debounce.
5. ✅ Successful unlock invokes `IVaultService.OpenAsync` (Sage's sprint-1 service); on success navigates to Unlocked shell.
6. ✅ Unlocked shell: `NavigationView` LeftCompact (🏠 All / ⭐ Favorites / 🗑 Trash / ⚙ Settings / ? About). Right-of-rail = `Frame` hosting destinations.
7. ✅ Custom title bar: hamburger + always-visible `AutoSuggestBox` (search, no-op this sprint) + app title + user chevron (vault name, Lock, Switch vault, About) + 🔒 Lock button.
8. ✅ "All entries" destination: TreeView (groups placeholder, root only) + ListView (entries from `VaultHandle`). Right pane: read-only field display for selected entry. **No editing this sprint.**
9. ✅ Empty / locked / unlocked state transitions are `Frame.Navigate` page navigations with `DrillInNavigationTransitionInfo` / `EntranceNavigationTransitionInfo`.
10. ✅ Brand tokens v2: dark `BrandPrimary` updated to `#E04335`; `AccentFillColor*` overridden to brand red; selected-row 2px left strip + `BrandSubtle` background; destructive uses `SystemFillColorCritical`.
11. ✅ Keyboard accelerators registered: `Ctrl+L` (lock), `Ctrl+F`/`Ctrl+K` (focus search), `Ctrl+\` (toggle rail), `Ctrl+S` (save — no-op stub).
12. ✅ All pages decorated with empty-state placeholder component (used in: empty vault, empty Trash, empty Favorites).
13. ✅ `INavigationService` concrete implementation lives in `PaZword.App.Navigation`, registered in DI.
14. ✅ AOT publish stays clean: `dotnet publish -c Release -r win-x64 -p:PublishAot=true -warnaserror` succeeds with 0 trim/AOT warnings — and this time `PaZword.App` actually references `IVaultService` so the vault libs are NOT trimmed away.
15. ✅ Visual MSIX launch smoke (sprint-0 carryover): packaged MSIX installs on a clean Win11, app launches, shows Welcome with Mica.
16. ✅ Ivy retroactive sprint-1 crypto review filed (carryover obligation).
17. ✅ `done.md` + per-agent done docs + Ivy `qa-report.md`. PR `sprint-2: app shell` merged through proper Ivy gate.

---

## Tasks

### T-2.1 — Brand tokens v2 (Nova)
- Update `Themes/Brand.Dark.xaml`: `BrandPrimary` → `#E04335` (ART-1).
- Override `AccentFillColorDefault`, `AccentFillColorSecondary`, `AccentFillColorTertiary` → brand ramp (ART-5).
- Define `SelectedRowAccentStripBrush`, `BrandSubtleBackgroundBrush` for list selection.
- Document destructive vs brand red distinction with side-by-side comment in XAML.
- Re-run `docs/sprint-2/contrast-report.md` for the new dark `BrandPrimary` and AccentFill overrides.

### T-2.2 — Navigation service + page registry (Nova)
- `INavigationService` concrete implementation in `PaZword.App.Navigation.NavigationService`.
- Explicit page registry — every destination type registered in DI (AOT requires no scanning).
- Methods: `NavigateTo<TPage>(object? parameter, NavigationTransitionInfo?)`, `GoBack()`, `Reset()`.
- Persists nav stack so back arrow works.

### T-2.3 — Onboarding pages (Nova)
- `OnboardingPage` host + 3 sub-views: WelcomeIntroView, KdbxExplainerView, ChooseActionView.
- Pip navigation indicator + Next / Skip buttons.
- "Don't show again" persisted in `LocalSettings` (key `OnboardingCompleted`).
- ChooseActionView routes to: Welcome (create-or-open) OR MigrationPlaceholderPage.

### T-2.4 — Welcome page (Nova)
- 3 tiles: `+ New vault`, `📂 Open existing`, `⬆ Migrate from PaZword`.
- Migrate tile **always visible** (per D-015) — clicking routes to MigrationPlaceholderPage.
- Recent vaults list (max 5, sorted by last-opened), each row = filename + folder path + relative time. Click → Locked page with that path.
- Recents persisted in `LocalSettings` as STJ source-gen (`JsonSerializerContext`, AOT clean).
- "Open existing" → `FileOpenPicker` (`.kdbx` filter).

### T-2.5 — Locked / unlock page (Nova)
- Vault filename + breadcrumb path at top.
- `PasswordBox` default-focus, eye toggle off-by-default.
- `Expander` "Use a key file" → `FileOpenPicker`.
- Hello button **disabled** placeholder with tooltip "Available after sprint 7."
- "Open a different vault" `HyperlinkButton` bottom → back to Welcome.
- Enter / Unlock button → `IVaultService.OpenAsync` on background task.
- During unlock: after 400 ms, show `ProgressRing` + "Unlocking..." (ART-4).
- Failure: `InfoBar` below field with attempt count. After 3 failures, 2-second `Task.Delay` debounce (UI disabled).
- Success: `NavigationService.NavigateTo<VaultShellPage>()` with `VaultHandle` parameter.

### T-2.6 — Migration placeholder page (Nova)
- Single page with: "Migration wizard arrives in a future update." short copy, link back to Welcome.
- Sprint 5 replaces with the real wizard. UX-7 / D-014 tile routes here.

### T-2.7 — Vault shell (Unlocked) (Nova)
- `VaultShellPage` hosts custom title bar + `NavigationView` LeftCompact + `Frame`.
- Custom title bar: `ExtendsContentIntoTitleBar = true`, grid with hamburger / search / title / user chevron / lock button.
- Destinations: AllEntriesPage, FavoritesPage, TrashPage, SettingsPage, AboutPage.
- User chevron flyout: vault filename (header), Lock vault, Switch vault, About.

### T-2.8 — AllEntriesPage (Nova)
- TreeView left (single root "All" node placeholder — group tree wiring is sprint 3 work; this sprint shows a flat list under the root).
- ListView right of tree showing entries from `VaultHandle.Document` (read-only).
- Selected entry → detail pane (right of split) shows fields as read-only TextBlocks.
- Selected list-item left strip (2px brand red) per ART-3.
- Empty state: placeholder component "Your vault is empty" + disabled "Add your first entry" CTA (real action in sprint 3).

### T-2.9 — Settings + About placeholder pages (Nova)
- Settings: theme selector (System / Light / Dark) wired to existing `IThemeService`. Other settings placeholder text "Coming soon".
- About: version, license, links.

### T-2.10 — Empty-state component (Milo + Nova)
- `UserControl` `EmptyState`: properties `Glyph` (Segoe Fluent Icon), `Title`, `Body`, `CtaText`, `CtaCommand`.
- Used by: AllEntries / Favorites / Trash / Welcome-recents-empty.
- Sprint 9 swaps Glyph for SVG illustration; structure stays.

### T-2.11 — Keyboard accelerators (Nova)
- Register the locked shortcut table at the appropriate scope:
  - `Ctrl+L` — Unlocked shell, calls `IVaultService.CloseAsync` and navigates to Welcome.
  - `Ctrl+F` / `Ctrl+K` — Unlocked shell, focuses title-bar search.
  - `Ctrl+\` — Unlocked shell, toggles NavigationView `IsPaneOpen`.
  - `Ctrl+S` — Unlocked shell, no-op + `InfoBar` "No changes to save" (real save sprint 3).

### T-2.12 — AOT verification under vault references (Sage + Nova)
- Sprint 1 left `PaZword.App` *not* referencing vault libraries; this sprint adds the reference.
- Run AOT publish; investigate and resolve any new trim/AOT warnings.
- Add `JsonSerializerContext` for the recent-vaults persistence (and any other STJ usage).
- Update `docs/sprint-2/aot-notes.md` with new hints.

### T-2.13 — MSIX visual smoke (Nova + Ivy)
- Build MSIX, install on a clean Win11 (or Sandbox), launch, screenshot Welcome page with Mica visible.
- Closes sprint-0 DoD #1 caveat.

### T-2.14 — Ivy retroactive sprint-1 crypto review (Ivy)
- Audit `PaZword.Kdbx` + `PaZword.Crypto` for:
  - Log statements that could leak protected fields / passwords / keys (grep `LogDebug` / `LogTrace` / `LogInformation`).
  - Transient buffers escaping zeroization.
  - AES-KDF read path coverage gap.
  - Argon2d rejection policy verification (regression test).
- File findings as GitHub Issues (`severity:major/minor`) — do NOT block sprint 2 unless a critical leak is found.
- Append summary to `docs/sprint-1/qa-report.md` (retroactive — create the file).

### T-2.15 — Sprint-end docs (Nova + Sage + Ivy)
- `docs/sprint-2/aot-notes.md`, `done-nova.md`, `done-sage.md` (if needed), `qa-report.md`.

---

## Risks for sprint 2

| Risk | Mitigation |
|---|---|
| First AOT publish referencing vault libraries surfaces new trim warnings | T-2.12 explicitly budgeted; Sage on standby to add `[DynamicallyAccessedMembers]` or refactor reflection-y patterns |
| Custom title bar + NavigationView interaction has gotchas on Win11 24H2+ | Nova allocates day 1 for a title-bar spike before the page work |
| `IVaultService.OpenAsync` blocks UI thread despite being async | KDF runs on `Task.Run` (Sage confirmed sprint-1); verify in UI smoke |
| Keyboard accelerators conflict with WinUI defaults | Test matrix per the locked shortcut table; Ctrl+S/Ctrl+C clashes most likely |
| Onboarding "Don't show again" persistence races first-launch theme load | Settings load synchronously at App.OnLaunched before navigation |

---

## Out of scope for sprint 2

- Entry CRUD / edit / save (sprint 3)
- Real migration wizard (sprint 5)
- TOTP + attachments (sprint 6)
- Windows Hello (sprint 7)
- Real cloud-folder scan (sprint 5 + 8)
- Favicon fetch (sprint 8)
- Password generator UI (sprint 4)
- Search implementation (sprint 4)

**"Is this in scope for sprint 2?"** If no → backlog.

---

## Agent prompts

### Prompt to start the **Nova** chat (sprint 2)

> You are **Nova**, the Frontend/WinUI 3 engineer on the PaZword v2 project. The repo is at `E:\source\veler\PaZword-New`. Branch off `main` to `sprint-2/app-shell`.
>
> Read **in order, in full** before any work:
> 1. `PROJECT_BRIEF.md` (esp. §6 design system, §7 current state, §9 locked decisions — note D-014, D-015, D-016)
> 2. `docs/sprint-1/done.md` — what Sage shipped
> 3. `docs/brainstorm-app-shell-ux.md` — **all decisions in §5 are LOCKED; §8 is your spec**
> 4. `docs/sprint-0/aot-notes.md` — AOT discipline rules carry over
> 5. `docs/sprint-2/plan.md` — this sprint
>
> Scope is **all of T-2.1 through T-2.13 and T-2.15**. Single PR titled `sprint-2: app shell`. Sage is on standby for T-2.12 (AOT investigation if new warnings appear). Ivy runs T-2.14 (sprint-1 retro) in parallel and gates the merge per proper process this time.
>
> **Day-1 spike:** custom title bar + `NavigationView` LeftCompact + `ExtendsContentIntoTitleBar` interaction on Win11 24H2+. Confirm there are no layout / drag-region gotchas before building dependent pages.
>
> **AOT discipline:** `{x:Bind}` only, no `{Binding}`. STJ source-gen for any JSON (recent-vaults persistence). All page navigation via the new `INavigationService`. No CommunityToolkit controls beyond what's in `PROJECT_BRIEF.md §3` without asking Remy.
>
> **Brand discipline:** every red surface uses tokens (`BrandPrimaryBrush` etc.), never hardcoded hex. Destructive uses `SystemFillColorCritical` (Fluent), not brand red. Links use Fluent default link color.
>
> Progress → `docs/sprint-2/progress.md` (append-only). When all your DoD items pass locally, write `docs/sprint-2/done-nova.md`. Wait for Ivy gate before merge.

### Prompt for **Sage** chat (sprint 2 — standby)

> You are **Sage** on standby for sprint 2 (`E:\source\veler\PaZword-New`). Read `PROJECT_BRIEF.md`, `docs/sprint-1/done.md`, `docs/sprint-2/plan.md`. Your only proactive task is T-2.12: when Nova first runs `dotnet publish -c Release -r win-x64 -p:PublishAot=true -warnaserror` with `PaZword.App` referencing `PaZword.Core.Vault.IVaultService` and `PaZword.Kdbx`, expect trim warnings. Investigate, add minimal `[DynamicallyAccessedMembers]` or refactor reflection-y patterns; **never** add a runtime fallback. Help Nova add a `JsonSerializerContext` for recent-vaults persistence. Append progress to `docs/sprint-2/progress.md`. Write `docs/sprint-2/done-sage.md` only if you contributed code.

### Prompt for **Ivy** chat (sprint 2 — retro + gate)

> You are **Ivy** on the PaZword v2 project (`E:\source\veler\PaZword-New`). Read `PROJECT_BRIEF.md`, `docs/sprint-1/done.md` (process deviation noted — sprint 1 was Producer-merged without your gate), `docs/sprint-2/plan.md`.
>
> **T-2.14 retroactive sprint-1 audit (parallel to Nova's work):**
> - Audit `src/PaZword.Kdbx/` + `src/PaZword.Crypto/` for: log statements that could leak protected fields / passwords / keys (grep `LogDebug` / `LogTrace` / `LogInformation`); transient buffers escaping zeroization; AES-KDF read coverage; Argon2d rejection regression test.
> - Findings → GitHub Issues with `severity:critical/major/minor`. Do NOT reopen sprint 1; carryover items only.
> - Append retroactive summary to `docs/sprint-1/qa-report.md` (create the file).
>
> **Sprint-2 gate:**
> - Verify every DoD item in `docs/sprint-2/plan.md`.
> - Special attention: visual MSIX smoke (T-2.13), AOT publish with new vault references (T-2.12), keyboard accelerators don't break Fluent UX.
> - Write `docs/sprint-2/qa-report.md`. Block merge if any DoD item fails.
