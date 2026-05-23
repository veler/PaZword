# PaZword — Project Brief

> **Single source of truth.** This file is maintained by Remy (Producer). It is the canonical handoff document across chats and agents. When in doubt, this wins.

---

## 1. Vision

A modern Windows 11 password manager that uses the **standard KeePass `.kdbx` v4** format, ships with a refined **Fluent UI** experience, and preserves PaZword's red brand identity. Fully native AOT. Solo project by Etienne Baudoux (veler).

The defining bet: **be a great Windows client for an open, portable vault format** — not another walled garden. Users own their `.kdbx` file and can open it in KeePass2, KeePassXC, KeePassium, Strongbox, or any other compliant client.

---

## 2. Non-Negotiables

- **WinUI 3** (WinAppSDK 1.7+) — not UWP, not MAUI, not WPF
- **NativeAOT** from day one (`PublishAot=true` on app project from sprint 0)
- **.NET 10**
- **Windows 11 only** (minimum)
- **MSIX, Microsoft Store** distribution
- **Standard `.kdbx` v4**, zero custom schema — must round-trip with KeePassXC
- **en-US only** (RESW) — FR/RU deferred
- **Solo developer workflow** — Remy + AI sub-agents do the work; one human reviewer (you)

---

## 3. Tech Stack

| Layer | Choice | Rationale |
|---|---|---|
| Runtime | .NET 10 + NativeAOT | Perf, startup, single-file deploy, no JIT footprint |
| UI | WinUI 3 (WinAppSDK 1.7+) | Native Win11 Fluent, Mica, modern controls |
| MVVM | `CommunityToolkit.Mvvm` | Source-generated, AOT-clean |
| DI | `Microsoft.Extensions.DependencyInjection` (explicit registration) | AOT-safe, no scanning |
| Logging | `Microsoft.Extensions.Logging` + file sink | Sentry added in v2 |
| JSON (settings) | `System.Text.Json` source-gen | AOT-clean |
| Crypto (KDF) | `Konscious.Security.Cryptography.Argon2` | Pure managed, AOT-OK |
| Crypto (ciphers/hash) | BCL `System.Security.Cryptography` | Built-in, AOT-OK |
| KDBX I/O | **Roll our own** (`PaZword.Kdbx`) | Crypto liability + AOT control; ~1.5–2.5k LOC scope |
| TOTP | `Otp.NET` | AOT-clean |
| Password strength | `zxcvbn-core` (audit) or roll subset | Pending AOT audit in sprint 0 |
| HTTP | `HttpClient` | HIBP k-anon, favicons |
| Tests | `xUnit` + `WinAppDriver`/Appium | Round-trip + UI smoke |
| Packaging | MSIX, single-project app model | Store submission |

---

## 4. Architecture (target)

```
src/
├── PaZword.App/              WinUI 3, MSIX, NativeAOT (entry point)
├── PaZword.Core/             Domain models, services, MVVM-agnostic
├── PaZword.Kdbx/             KDBX v4 read/write (Argon2id + ChaCha20/AES)
├── PaZword.Crypto/           Argon2 wrapper, DPAPI key wrap, secure buffers
├── PaZword.Migration/        Legacy PaZword vault → KDBX importer
├── PaZword.Sync/             Cloud-folder detection + conflict prompt
└── PaZword.Localization/     RESW (en-US)

tests/
├── PaZword.Kdbx.Tests/       Round-trip against KeePassXC fixtures
├── PaZword.Crypto.Tests/     Known-answer tests (KAT)
├── PaZword.Migration.Tests/  Fidelity tests vs real old vaults
└── PaZword.App.UITests/      WinAppDriver smoke
```

**AOT discipline rules:**
- `PublishAot=true` on `PaZword.App` from sprint 0
- `IsTrimmable=true` on every library
- No `Activator.CreateInstance`, no reflection-emit, no `BinaryFormatter`
- All DI registrations explicit
- All JSON via source-gen `JsonSerializerContext`
- CI fails on any trim or AOT warning

---

## 5. Feature Scope (v1)

### P0 — Must ship
- Open / create `.kdbx` vault (master password + optional key file)
- **Migration wizard** from old PaZword vault (one-shot, validated)
- Windows Hello unlock (DPAPI-wrapped composite key, password fallback)
- Entry CRUD (title, username, password, URL, notes, icon)
- Password generator + strength meter
- In-app search
- Auto-lock on idle / minimize
- Clipboard auto-clear (30 s default)
- Backup-on-save (rolling 5 copies)

### P1 — Targeted for v1, can slip
- TOTP (`otp` field, KeePassXC convention)
- Attachments (≤4 MB)
- Auto-icon detection (favicon fetch, opt-in)
- HIBP breach check (k-anonymity, opt-in)
- Cloud-folder picker (smart defaults for OneDrive/Dropbox/Google Drive/iCloud Drive)

### v2 — Planned, deferred
- API-level cloud sync with merge (OneDrive, Dropbox, Google Drive, iCloud)
- FIDO2 / hardware key unlock
- Sentry telemetry
- FR/RU localization
- Browser extension, autofill, passkeys, mobile companion (later still)

---

## 6. Design System

**Brand:** Red, refined. Confident, not loud. Mica backdrop, dark mode first-class.

**Red ramp (proposed, Milo):**

| Token | Light | Dark |
|---|---|---|
| BrandPrimary | `#C42B1C` | `#FF4D3D` |
| BrandPrimaryHover | `#B2261A` | `#FF6A5C` |
| BrandPrimaryPressed | `#9E2218` | `#E04335` |
| BrandSubtle | `#FDECEA` | `#3A1411` |
| BrandOnPrimary | `#FFFFFF` | `#FFFFFF` |

Verify WCAG AA on implementation (Sage + Milo).

**Logo:** Keep current red lock icon for v1. Refresh post-1.0.

**Typography:** Segoe UI Variable (Win11 default).
**Icons:** Segoe Fluent Icons for chrome; custom-icon-table from KDBX or fetched favicons for entries.
**Window:** `ExtendsContentIntoTitleBar = true`, search hosted in the title-bar grid (Edge/Settings style).
**Main vault:** 2-pane list/detail. Compact `NavigationView` on the left for top-level destinations (Vault, Generator, Settings).
**Motion:** Connected animations on entry open. Reveal on cards. Restrained.

---

## 7. Current State

**Date:** 2026-05-22

**Status:** Sprint 1 ✅ complete (Producer-merged, Ivy gate skipped — see carryover). Sprint 2 (App shell) open.

**Last sprint:** `docs/sprint-1/done.md` — `PaZword.Kdbx` reads/writes standard KDBX v4 round-trippably. ChaCha20 (managed RFC 8439), AES-CBC, HMAC block chain with tamper poisoning, Argon2id with independent RFC-9106 KAT against `phc-winner-argon2`, KDF floor enforced, password buffer zeroization. `IVaultService` wired in `PaZword.Core`. 49 tests green, 0 trim/AOT warnings. Two mid-sprint bugs caught and tested (XML cursor double-advance + ChaCha20 keystream misalignment).

**Open carryover (Ivy must gate during sprint 2):**
- Sprint-1 had no formal Ivy QA report. Producer-merged on Sage's self-attested DoD scorecard + 49 green tests + clean AOT publish. Ivy should retroactively review crypto pipeline in sprint 2 and file findings.
- `keepassxc-cli` third-party interop test (sprint-1 optional, deferred). File as sprint-9 polish task.
- Argon2d read support not implemented (currently throws `KdbxFormatException`). KeePassXC 2.7+ writes Argon2id by default. Reopen only if in-the-wild Argon2d vaults are reported.
- AES-KDF read path present but no dedicated test fixture. Add fixture in sprint 2 or sprint 5 (migration).

**Open carryover (later sprints):**
- Sprint 2: visual MSIX launch smoke; Milo dark-mode `BrandPrimary` shifted to `#E04335` (locked in `docs/brainstorm-app-shell-ux.md`); App-shell UX brainstorm output is sprint-2 input
- Sprint 5: migration wizard MUST scan cloud-sync folders (OneDrive / Dropbox / Google Drive / iCloud Drive) for legacy PaZword vaults — user may have their old vault stored there, not just `%LOCALAPPDATA%`
- Sprint 7: implement DPAPI key-wrap to pre-decisions D-1..D-5 in `docs/sprint-0/qa-report.md §4.2`

**No GitHub Issues open.**

---

## 8. Roadmap (proposed sprint sequence)

| Sprint | Theme | Outcome |
|---|---|---|
| 0 ✅ | Foundation & viability | AOT + WinUI 3 + .NET 10 spike green, CI pipeline, project skeleton, brand tokens locked, Argon2id KAT green, legacy format documented |
| 1 ✅ | KDBX core | Read/write KDBX v4, Argon2id (RFC-9106 KAT), ChaCha20+AES, HMAC block tamper-poison, KDF floor, secure zeroization, 49 tests green |
| 2 🚧 | App shell | Welcome / Locked / Unlocked pages, NavigationView, custom title bar, brand tokens v2, navigation service |
| 3 | Vault main view | List/detail, entry CRUD, basic search |
| 4 | Productivity | Password generator, strength meter, advanced search, settings UI |
| 5 | **Migration wizard** | Old PaZword vault → KDBX, fidelity tests, no-data-loss verification |
| 6 | TOTP + attachments + icons | `otp` field, binary attachments, custom icon table |
| 7 | Security hardening | Windows Hello, auto-lock, clipboard hygiene, backup-on-save |
| 8 | Network features | Favicon fetch, HIBP, cloud-folder picker |
| 9 | Polish | A11y pass, MSIX/Store pipeline, perf pass |
| 10 | Beta | Dogfood, bug-fix |
| 11 | 1.0 | Store submission |

---

## 9. Locked Decisions

| # | Decision | Date |
|---|---|---|
| D-001 | Windows 11 only, WinUI 3, MSIX/Store, .NET 10, NativeAOT | 2026-05-22 |
| D-002 | Standard KDBX v4 only — zero custom schema | 2026-05-22 |
| D-003 | Roll our own KDBX library (`PaZword.Kdbx`) | 2026-05-22 |
| D-004 | v1 cloud sync = folder picker w/ smart defaults; v2 = API-level | 2026-05-22 |
| D-005 | TOTP stored in standard `otp` string field | 2026-05-22 |
| D-006 | Windows Hello = DPAPI-wrapped composite key, password fallback mandatory | 2026-05-22 |
| D-007 | en-US only for v1; RESW infrastructure ready for v2 | 2026-05-22 |
| D-008 | Keep current red lock logo for v1 | 2026-05-22 |
| D-009 | FIDO2/hardware key deferred to v2 (architecture must allow) | 2026-05-22 |
| D-010 | Logging via `ILogger`; Sentry deferred to v2 | 2026-05-22 |
| D-011 | MVVM = CommunityToolkit.Mvvm; DI = MEDI explicit; JSON = STJ source-gen | 2026-05-22 |
| D-012 | Migration is one-shot wizard; legacy reader can be dropped post-migrate | 2026-05-22 |
| D-013 | Argon2d KDBX vaults rejected with typed exception (KeePassXC writes Argon2id by default; reopen only on demand) | 2026-05-22 |
| D-014 | Migration wizard (sprint 5) scans cloud-sync folders (OneDrive/Dropbox/Google Drive/iCloud Drive) in addition to `%LOCALAPPDATA%\PaZword` for legacy vaults | 2026-05-22 |
| D-015 | First-run experience: 3-screen onboarding (Welcome → "What's a KDBX" → "Create or Open" with migration option) | 2026-05-22 |
| D-016 | App-shell UX locked per `docs/brainstorm-app-shell-ux.md` §5 decision table (UX-1..7, ART-1..4, IMPL-1..2) | 2026-05-22 |

---

## 10. Open Questions

- None at brief-creation time. New items will be appended with a Q-### id.

---

## 11. Glossary

- **KDBX** — KeePass database format, currently v4 (Argon2 + AES/ChaCha20).
- **KAT** — Known-Answer Test (cryptographic test vectors).
- **DPAPI** — Windows Data Protection API; user-tied symmetric encryption.
- **HIBP** — Have I Been Pwned; breach-check API with k-anonymity model.
- **AOT** — Ahead-of-Time compilation. No JIT at runtime.

---

## 12. Team

| Agent | Role | Lives in |
|---|---|---|
| **Remy** | Producer (planning, brief, merge) | This chat |
| **Kira** | Product / UX | Brainstorm + per-sprint consult |
| **Milo** | Art direction / Fluent visuals | Brainstorm + UI sprints |
| **Nova** | Frontend / WinUI 3 | Dev chats |
| **Sage** | Backend / crypto / AOT | Dev chats |
| **Ivy** | QA / security review | Sprint-end gates |

You (Etienne) are the human reviewer. One PR at a time.
