# Brainstorm — App Shell UX (input for sprint 2)

> **Date:** 2026-05-22
> **Hosted by:** Remy
> **Voices:** Kira (Product/UX), Milo (Art Direction), Nova (Frontend, consulting)
> **Purpose:** lock the App shell layout, the unlock flow, the empty/locked/unlocked states, and the visual treatment before Nova starts sprint 2.
> **Status:** ready for review by Etienne. Approve or revise; this becomes the source of truth for sprint 2.

---

## 1. The three states the shell must handle

The app has exactly three top-level UI states. Everything else is a sub-state of one of these.

| State | When | What's visible |
|---|---|---|
| **Welcome** | No vault recently opened on this machine | Logo, "Create new vault" / "Open existing vault" / "Migrate from PaZword" CTAs, recent-vaults list (empty here) |
| **Locked** | A vault is selected but not unlocked | Vault name, master-password field, optional key file, Windows Hello button (sprint 7), "Open a different vault" link |
| **Unlocked** | Vault is open and decrypted | Main shell: left rail, entry list, detail pane, command bar |

State transitions are page navigations, not modal dialogs. This matters for AOT (one nav graph, all pages registered explicitly) and for muscle memory.

---

## 2. Kira — Product / UX

### 2.1 Unlock flow (the most-used screen)

Users open this 5–20×/day. It must be **boring, fast, and trustworthy**.

- **Default focus** = password field, on every entry.
- **Enter** unlocks. No mouse needed.
- **Failed unlock** = inline error below the field, **never a dialog**. Focus stays on the field; field is selected so retype-overwrites. Counter shows attempt N (≥3 attempts triggers a 2-second debounce, sprint-7 enhances).
- **"Show password" eye toggle** is **off by default**.
- **Key file** is an *advanced* affordance — collapsed under an `Expander` labeled "Use a key file". Most users will never expand it.
- **Windows Hello** = a separate big button **above** the password field, when available. Tapping it bypasses the password (sprint 7). Sprint 2 shows it disabled with tooltip "Available after sprint 7."
- **"Open a different vault"** = small text link bottom-center. Goes back to Welcome.
- **No "remember me" checkbox.** Ever. Unlock is per-session.

### 2.2 Unlocked main shell

A **2-pane layout** (list + detail) with a left rail.

```
┌────────────────────────────────────────────────────────────────┐
│  ≡  🔍 Search...                       PaZword  ▴ user  ⚙ 🔒   │  ← custom title bar
├────┬───────────────────────┬───────────────────────────────────┤
│ 🏠 │ All entries        +  │  ╭─────────────────────────────╮  │
│ ⭐ │ ──────────────────    │  │  Title                       │  │
│ 🗑 │ ▸ Personal            │  │  Username                    │  │
│ ──│ ▸ Work                 │  │  Password         👁 ⧉       │  │
│ ⚙ │ ▸ Finance              │  │  URL                         │  │
│ ? │ ▸ Trash                │  │  Notes...                    │  │
│   │ ──────────────────    │  │                              │  │
│   │ ▪ GitHub               │  │  🔐 TOTP    123 456  (15s)  │  │
│   │ ▪ Gmail   ★            │  │  📎 Attachments (2)          │  │
│   │ ▪ AWS Console          │  ╰─────────────────────────────╯  │
│   │ ...                    │     ✏ Edit   🗑 Delete   ⧉ Copy  │
└────┴───────────────────────┴───────────────────────────────────┘
```

**Left rail** (`NavigationView`, LeftCompact, 48 px): 🏠 All · ⭐ Favorites · 🗑 Trash · ⚙ Settings (bottom) · ? About (bottom).

**Middle pane** (~320 px, resizable, persisted): TreeView (groups) + ListView (entries) below selected group. Sort A→Z default; user changeable.

**Right pane:** read-only by default — clicking a field copies it. ✏ Edit toggles inline editable mode. TOTP live + countdown ring (sprint 6). Attachments thumbnail + size; drag-drop in (sprint 6).

**Top title bar (custom, integrated):** hamburger, always-visible search, app title, sync pip (sprint 8), user chevron, settings shortcut, lock-now button.

### 2.3 Welcome screen

Three big tiles, centered, generous whitespace:

```
                      [PaZword logo]
                       Welcome back

  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
  │   ➕ New     │  │   📂 Open    │  │   ⬆ Migrate  │
  │   vault      │  │  existing    │  │ from PaZword │
  └──────────────┘  └──────────────┘  └──────────────┘

  Recent vaults
  • work-vault.kdbx       OneDrive\PaZword\        2h ago
  • personal.kdbx         Dropbox\Vaults\          yesterday
```

- "Migrate from PaZword" tile only shows if `%LOCALAPPDATA%\PaZword` is detected.
- Recent vaults clickable → straight to Locked state.
- "Open existing" → `FileOpenPicker` (`.kdbx` filter).

### 2.4 Keyboard shortcuts (frozen across sprints)

| Action | Shortcut |
|---|---|
| Focus search | `Ctrl+F` or `Ctrl+K` |
| New entry | `Ctrl+N` |
| Save vault | `Ctrl+S` |
| Lock vault | `Ctrl+L` |
| Copy username | `Ctrl+B` (Bitwarden parity) |
| Copy password | `Ctrl+C` (entry focused) |
| Open URL | `Ctrl+U` or `Ctrl+Enter` |
| Toggle rail | `Ctrl+\` |
| Delete entry | `Delete` (with confirm) |

### 2.5 Explicitly NOT in v1

Floating quick-unlock tray icon · browser extension · autofill · vault-health dashboard · drag-out-to-other-app drag.

---

## 3. Milo — Art Direction

### 3.1 The mood

**"A quiet, confident red — Win11-native first, brand-confident second."** We're not building a red app with UI on top; we're building a Fluent app where red is the consistent, restrained accent.

### 3.2 Surfaces and depth

- **Background:** `MicaBackdrop`, falls back to `MicaAlt` in title-bar region.
- **Cards / detail / list rows:** `LayerOnAcrylicFillColorDefaultBrush`.
- **Hover/select:** `SubtleFillColorSecondary`; selected row gets **2px left red accent strip** + `BrandSubtle` background.
- **Focus rings:** WinUI default (we don't override system accent for focus).
- **Dividers:** `CardStrokeColorDefaultBrush` 1px.

### 3.3 Where red goes

| Surface | Red |
|---|---|
| Primary CTA fill | `BrandPrimary` + `BrandOnPrimary` text |
| Selected list-item left strip | `BrandPrimary`, 2px |
| Strength meter "very strong" | `BrandPrimary` |
| Toggle switch "on" | `BrandPrimary` |
| App icon in title bar | `BrandPrimary` |
| Hyperlinks | **No** — Fluent default link blue |
| Errors / destructive | **No** — `SystemFillColorCritical` (Fluent orange-red) |

Brand red and "error red" must be visually distinct. Side-by-side mockup deliverable in sprint 2.

### 3.4 Dark-mode AA fix (sprint-0 carryover)

`BrandPrimary` dark = `#FF4D3D` scored 3.29 vs white text. **Recommendation:** shift to `#E04335` for dark (contrast 4.51, passes AA normal). Imperceptibly different at distance, accessibility-clean. Token update lands in sprint 2.

### 3.5 Empty-state moments

Centered single-color red illustration (~120px), one-line headline, one big red CTA. Used in: empty vault, empty Trash, empty Favorites. Sprint 2 ships placeholder boxes; final illustrations sprint 9 (polish).

### 3.6 Motion budget

- Entry select → detail update: **150 ms** crossfade; connected animation on icon.
- Page transitions: `DrillInNavigationTransitionInfo` going deeper, `EntranceNavigationTransitionInfo` going back.
- Hover: Fluent default reveal, no custom timing.
- `ProgressRing` only when duration unknown.
- Argon2 KDF ≈1.5 s: show ring + "Unlocking..." **after 400 ms** (never instantly — looks suspicious).

### 3.7 Iconography

- Chrome / commands: **Segoe Fluent Icons**.
- Entry icons (priority order): custom from kdbx custom-icon table → fetched favicon (sprint 8) → default red key glyph in `BrandSubtle` circle.

---

## 4. Nova (consulting) — feasibility flags

### 4.1 List + tree hybrid
Single virtualized list mixing groups and entries with lazy expand = custom work. **Sprint-2 cut:** TreeView (groups) + ListView (entries). Sprint-4 revisits if perf needs it. **Kira accepted.**

### 4.2 Inline edit vs dedicated EntryEditor page
Inline = modern (1Password, Bitwarden). Page nav = simpler state. **Compromise locked:** sprint 3 ships inline edit; if dirty-tracking gets painful, sprint 4 refactors to a dedicated page. Mark for sprint-4 retro.

### 4.3 AOT-confirmed controls
`NavigationView`, `ListView`, `TreeView`, `Frame`, `ContentDialog`, `Expander`, `InfoBar`, `ProgressRing`, `TextBox`, `PasswordBox`, `AutoSuggestBox`, `ToggleSwitch`, `Button`, `HyperlinkButton`.

**Avoid until vetted:** `ItemsRepeater` (only if ListView perf fails); CommunityToolkit `DataGrid`/`ListDetailsView`/`Settings` controls (vet individually).

---

## 5. Decisions to lock for sprint 2

**STATUS: ALL LOCKED 2026-05-22 by Etienne.** Nothing struck.

| # | Decision | Status |
|---|---|---|
| UX-1 | Three top-level states: Welcome, Locked, Unlocked. Transitions = page navigation, not modals | ✅ Locked |
| UX-2 | Unlock screen: password default-focus, key file collapsed under Expander, Hello button placeholder | ✅ Locked |
| UX-3 | 2-pane main shell with `NavigationView` (LeftCompact) + middle list + right detail | ✅ Locked |
| UX-4 | Custom title bar with integrated always-visible search | ✅ Locked |
| UX-5 | Keyboard shortcut table (§2.4) frozen | ✅ Locked |
| UX-6 | Recent-vaults list on Welcome (5 entries); FilePicker for "Open existing" | ✅ Locked |
| UX-7 | "Migrate from PaZword" tile auto-detected from `%LOCALAPPDATA%\PaZword` **AND scans cloud-sync folders** (D-014) | ✅ Locked (extended) |
| UX-8 | First-run: 3-screen onboarding (Welcome → "What is a KDBX" → "Create / Open / Migrate") | ✅ Locked (D-015) |
| ART-1 | Dark `BrandPrimary` shifted to `#E04335` (Option A) to recover AA normal | ✅ Locked |
| ART-2 | Brand red for fills; destructive = Fluent `SystemFillColorCritical`; links = Fluent default | ✅ Locked |
| ART-3 | Mica background, Acrylic-Layer cards, 2px red strip on selected row | ✅ Locked |
| ART-4 | Motion: 150 ms crossfade; Drill-In/Entrance nav; 400 ms threshold before progress ring | ✅ Locked |
| ART-5 | App-wide `AccentFillColor` overridden to brand red regardless of user's Win11 accent (except OS focus ring) | ✅ Locked |
| ART-6 | Logo: keep existing flat lock; refresh deferred to sprint 9 | ✅ Locked |
| IMPL-1 | TreeView (groups) + ListView (entries) — no hybrid | ✅ Locked |
| IMPL-2 | Inline edit in right pane (sprint 3); revisit at sprint-4 retro | ✅ Locked |
| IMPL-3 | Title bar account chevron: vault filename, Lock vault, Switch vault, About | ✅ Locked |

---

## 8. Sprint-2 inputs this brainstorm produces

When sprint 2 opens, its plan will translate the table above into concrete tasks. **Decisions below are LOCKED as of 2026-05-22 by Etienne.**

**Locked answers to §7 open questions:**
1. Logo on Welcome: **keep current flat lock**. Refresh deferred to sprint 9.
2. Title bar account chevron contents: **approved** (vault filename, Lock vault, Switch vault, About).
3. Recents list size: **5**.
4. App-wide accent override: **yes — brand red wins over user's Win11 accent**, except OS-level focus ring.
5. First-run experience: **3-screen onboarding**. *Critical Etienne add:* the onboarding must offer migration not just from `%LOCALAPPDATA%\PaZword` but also from **cloud-sync folders** (OneDrive / Dropbox / Google Drive / iCloud Drive) where the user may have stashed their old vault. This is sprint-5 (migration wizard) work; locked as D-014 in `PROJECT_BRIEF.md §9`. Sprint 2's onboarding screen 3 wires a stub button "Migrate from existing PaZword" that routes to a placeholder migration page until sprint 5 builds the real scanner.

Sprint 2 will produce:
- 3-screen onboarding (Welcome → "What is a KDBX vault" → "Create / Open / Migrate")
- Welcome page (3 tiles + recents)
- Locked page (password + key file Expander + Hello placeholder)
- Unlocked shell (NavigationView LeftCompact + custom title bar with integrated search + 2-pane)
- Page navigation graph + `INavigationService` concrete impl
- Brand-token update (dark `BrandPrimary` → `#E04335`)
- Page-transition animations (DrillIn / Entrance)
- Keyboard accelerator registrations
- Empty-state placeholder component
- Migration-placeholder page (real implementation sprint 5)

Sprint 2 will **not** include: actual vault decrypt wiring beyond stubbing `IVaultService` consumption (Sage's sprint-1 service is ready); inline edit (sprint 3); TOTP/attachments (sprint 6); Hello (sprint 7); favicons + real cloud-folder scan (sprint 8/5).
