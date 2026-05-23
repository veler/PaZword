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

**Status:** Sprint 0 ✅ complete and merged. Sprint 1 (KDBX core) open.

**Last sprint:** `docs/sprint-0/done.md` — buildable, AOT-publishable, MSIX-packageable WinUI 3 shell on .NET 10. CI green. Argon2id KDF live (7/7 KAT). KDBX library skeleton + spec digest. Legacy vault format documented (14 migration edge cases). 5.6 MB native PE, 0 trim/AOT warnings.

**Open carryover (Ivy gates into sprint 1):**
- Second Argon2id KAT against a non-Konscious reference vector
- KDBX-write parameter floor (`t≥2`, `m≥64 MiB`, `p≥2`); reads below floor warn only
- Password-buffer zeroization at every KDF call site

**Open carryover (later sprints):**
- Sprint 2: visual MSIX launch smoke (closes sprint-0 DoD #1 caveat); Milo dark-mode `BrandPrimary` AA review; Kira + Milo App-shell brainstorm
- Sprint 7: implement DPAPI key-wrap to pre-decisions D-1..D-5 in `docs/sprint-0/qa-report.md §4.2`

**No GitHub Issues open.**

---

## 8. Roadmap (proposed sprint sequence)

| Sprint | Theme | Outcome |
|---|---|---|
| 0 ✅ | Foundation & viability | AOT + WinUI 3 + .NET 10 spike green, CI pipeline, project skeleton, brand tokens locked, Argon2id KAT green, legacy format documented |
| 1 🚧 | KDBX core | Read/write KDBX v4, Argon2id KDF prod-floor, ChaCha20/AES cipher, round-trips with KeePassXC fixtures |
| 2 | App shell | Theming, navigation, unlock screen, settings scaffold |
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
