# Sprint 0 — Done

> **Status:** ✅ Complete · merged to `main` · 2026-05-22
> **Branch:** `sprint-0/foundation`
> **Owners:** Sage (backend/infra), Nova (frontend/AOT), Ivy (QA gate), Remy (producer)
> See also: `done-sage.md`, `done-nova.md`, `qa-report.md`, `progress.md`.

---

## Outcome

A buildable, AOT-publishable, MSIX-packageable WinUI 3 shell on .NET 10 with CI, AOT discipline enforced from line 1, the KDBX library decision confirmed feasible, the Argon2id KDF live with green KAT, and the legacy PaZword vault format documented for the future migration wizard.

The stack is real. The build is green. The path forward is unblocked.

---

## Definition of done — final scorecard

| # | Item | Verdict |
|---|---|---|
| 1 | AOT publish of `PaZword.App` succeeds (5.6 MB native PE, 0 warnings) | ✅ |
| 2 | `dotnet build` produces an MSIX package | ✅ |
| 3 | CI fails on any trim/AOT warning | ✅ |
| 4 | KDBX skeleton + spec notes; roll-our-own decision confirmed | ✅ |
| 5 | Argon2id KAT green (7/7) | ✅ (with reservation — see §"Carryover") |
| 6 | Brand resource dictionary (light + dark) + Mica wired | ✅ (dark `BrandPrimary` AA flag — see §"Carryover") |
| 7 | Legacy vault format documented + 14 edge cases enumerated | ✅ |
| 8 | `done.md` written, PR merged | ✅ (this file) |

---

## What shipped

**Repo skeleton** — 7 src projects + 4 test projects, CPM, `.editorconfig`, `PaZword.slnx`.

**App project** — `PaZword.App` targets `net10.0-windows10.0.22621.0`, `PublishAot=true`, `WindowsAppSDKSelfContained=true`. Empty `MainWindow` with Mica backdrop, custom title bar, themed `MainPage` proving the DI + MVVM + `{x:Bind}` chain end-to-end under AOT.

**DI / MVVM** — MEDI explicit registration (`AppServiceProvider`, `validateScopes: true`), `CommunityToolkit.Mvvm` source generators verified under AOT, `INavigationService` interface stubbed.

**KDBX library shape** — `IKdbxReader`, `IKdbxWriter`, `KdbxDocument`, `KdbxGroup`, `KdbxEntry`, `KdbxBinary`, `KdbxCustomIcon`, `KdbxKdfParameters` interfaces + models. 233-line spec digest covering header, KDF VariantMap, HMAC chain, block format, inner header, XML body, TOTP convention.

**Crypto** — `PaZword.Crypto.Argon2idKeyDerivation` over `Konscious.Security.Cryptography.Argon2` 1.3.1. 7 KAT tests green. Dependency publishes AOT-clean.

**Brand tokens** — `Themes/Brand.Light.xaml` + `Brand.Dark.xaml` red ramp from brief §6, `ThemeService` persists Light/Dark/System to `ApplicationData.LocalSettings`. Fluent `AccentFillColor*` overridden so default controls inherit PaZword red.

**Legacy format docs** — AES-256-CBC + `PasswordVault` key scheme, versioned JSON format, full schema, attachments storage, schema version history, Windows Hello integration, 14 migration edge cases catalogued for sprint 5.

**CI pipeline** — 4 jobs: Build (Debug+Release), Test, AOT Publish (`-warnaserror`), MSIX artifact. Triggers on `push` + `pull_request` against `main`.

---

## Carryover into sprint 1 (Ivy gates)

These do not block sprint 0 merge but must land in sprint 1:

1. **Second Argon2id KAT** against a non-Konscious reference vector (proves RFC-9106 conformance independently of our chosen library).
2. **Production KDF parameter floor** for the KDBX writer (proposal: `t≥2`, `m≥64 MiB`, `p≥2`). Refuse to *write* below the floor; *reading* below it logs a warning.
3. **Password-buffer zeroization** at every KDF call site (`Array.Clear` on completion).

## Carryover into later sprints

- **Sprint 2 (App shell):** add visual smoke test "MSIX packaged app launches on clean Win11 23H2 VM and shows themed Mica window" — closes DoD #1 caveat.
- **Sprint 2 (App shell):** Milo reviews dark-mode `BrandPrimary` (`#FF4D3D` → 3.29 contrast against white) and decides keep-as-large-UI-only vs. token shift to recover AA normal.
- **Sprint 2/3:** Kira + Milo brainstorm for shell layout (left rail, list/detail, command bar, unlock screen). Pre-empted by sprint-0 work but never executed; must happen before App shell coding begins.
- **Sprint 7 (security):** DPAPI key-wrap design pre-decisions D-1 through D-5 in `qa-report.md §4.2` are accepted; sprint-7 plan must implement to spec.

## Known limitations (intentional, documented)

- `MainPageViewModel` uses manual `ObservableObject.SetProperty(ref ...)` instead of `[ObservableProperty]` field generator — `MVVMTK0045` blocks the field generator under WinUI 3 + AOT until the partial-property toolchain catches up. `[RelayCommand]` is unaffected and in use. Documented in `aot-notes.md §1`.
- `Microsoft.Extensions.Logging.Debug` is not in CPM. Sprint 2 wires a file sink.
- WinUI markup compiler requires `-p:Platform=x64` for AOT publish. CI sets it; documented for future contributors.
- The unpackaged `PaZword.App.exe` exits `0xC0000409` without WinAppSDK Bootstrap / MSIX registration. Expected; visual launch verification is sprint-2 work.

---

## Decisions added to the brief this sprint

None. All 12 locked decisions in `PROJECT_BRIEF.md §9` were respected. No new packages outside §3.

## Sprint metrics

- 11 projects compile clean (`TreatWarningsAsErrors=true`)
- 8/8 tests green (7 crypto KAT + 1 AOT smoke)
- 0 trim warnings, 0 AOT warnings
- 5.6 MB native PE
- ~34 s full release build
