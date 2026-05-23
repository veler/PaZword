# Sprint 0 — Foundation & Viability

> **Goal:** prove the stack works, lock the skeleton, eliminate risk before we write any product code.
> **Output:** a buildable, AOT-publishable, MSIX-packageable empty WinUI 3 shell with CI, plus an audited library decision and locked brand tokens.

---

## Sprint goals (definition of done)

1. ✅ `dotnet publish -c Release -r win-x64` on `PaZword.App` produces a working AOT binary that launches and shows an empty themed window.
2. ✅ `dotnet build` produces an MSIX package.
3. ✅ CI pipeline runs build + tests + AOT publish on every PR and fails on trim/AOT warnings.
4. ✅ KDBX library decision finalized: roll-our-own is confirmed feasible after reading the KeePass file-format spec; module skeleton + interfaces stubbed (no implementation yet).
5. ✅ Argon2id KAT runs green against published RFC 9106 vectors.
6. ✅ Brand resource dictionary (light + dark) committed; Mica backdrop visible.
7. ✅ Old PaZword vault format documented in `docs/sprint-0/legacy-vault-format.md` (read from `E:\source\veler\PaZword\Windows`).
8. ✅ `done.md` written, PR merged.

---

## Tasks

### T-0.1 — Repo skeleton (Sage)
- Create `src/PaZword.App`, `src/PaZword.Core`, `src/PaZword.Kdbx`, `src/PaZword.Crypto`, `src/PaZword.Migration`, `src/PaZword.Sync`, `src/PaZword.Localization`.
- Create `tests/PaZword.Kdbx.Tests`, `tests/PaZword.Crypto.Tests`, `tests/PaZword.Migration.Tests`, `tests/PaZword.App.UITests`.
- Add `Directory.Build.props` with `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<IsTrimmable>true</IsTrimmable>` (libs), AOT analyzers enabled.
- Add `Directory.Packages.props` (central package management).
- Add `.editorconfig` (C# style + naming).
- Add `PaZword.sln`.

### T-0.2 — WinUI 3 + AOT spike (Nova)
- `PaZword.App` as WinAppSDK single-project app, target `net10.0-windows10.0.22621.0`.
- `<PublishAot>true</PublishAot>`, `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`.
- App launches an empty `MainWindow` with custom title bar, Mica backdrop, app icon set.
- Verify `dotnet publish -c Release -r win-x64` succeeds with **zero AOT/trim warnings**. Document any required hints in `docs/sprint-0/aot-notes.md`.
- Add `MainWindow` smoke test (WinAppDriver) — launches and closes cleanly.

### T-0.3 — DI + MVVM bootstrap (Nova + Sage)
- `App.xaml.cs` builds `IServiceProvider` from MEDI with explicit registrations.
- One sample service registered, one VM consuming it via constructor injection.
- `CommunityToolkit.Mvvm` source generators confirmed working under AOT publish.
- `INavigationService` interface stubbed (no pages yet).

### T-0.4 — Brand resource dictionary (Milo + Nova)
- Create `src/PaZword.App/Themes/Brand.Light.xaml` and `Brand.Dark.xaml`.
- Define brushes for the red ramp (see `PROJECT_BRIEF.md` §6).
- Merge into `App.Resources`.
- Verify WCAG AA contrast in `docs/sprint-0/contrast-report.md` for `BrandPrimary` on light/dark surfaces and `BrandOnPrimary` on `BrandPrimary`.
- Theme switch (Light/Dark/System) wired to settings stub.

### T-0.5 — KDBX library spike (Sage)
- Read the KDBX 4 file-format spec and write `docs/sprint-0/kdbx4-spec-notes.md` summarizing: header layout, KDF parameters, inner header, XML body, binary references, HMAC verification.
- Stub interfaces in `PaZword.Kdbx`:
  - `IKdbxReader`, `IKdbxWriter`, `KdbxDocument`, `KdbxGroup`, `KdbxEntry`, `KdbxBinary`, `KdbxCustomIcon`, `KdbxKdfParameters`.
- Zero implementation in sprint 0. Just shape.
- Decision confirmed: roll-our-own is feasible within sprint 1's budget.

### T-0.6 — Crypto module + Argon2 KAT (Sage)
- `PaZword.Crypto` references `Konscious.Security.Cryptography.Argon2`.
- Implement `IKeyDerivation` with Argon2id only (RFC 9106 parameters).
- `tests/PaZword.Crypto.Tests` runs Argon2id KAT vectors green.
- Verify the dependency publishes cleanly under AOT.

### T-0.7 — Legacy vault format documentation (Sage)
- Read `E:\source\veler\PaZword\Windows\Impl\PaZword.Core\Data\*` and `Security\EncryptionProvider.cs`.
- Document the on-disk format, KDF, cipher, JSON schema, attachments storage, and any quirks in `docs/sprint-0/legacy-vault-format.md`.
- Identify edge cases the migration wizard must handle (corrupt entries, missing fields, attachment paths, etc.).
- **No code, just documentation.** Sprint 5 will implement the importer.

### T-0.8 — CI pipeline (Sage)
- `.github/workflows/ci.yml`:
  - Trigger: PR + push to `main`.
  - Job 1: `dotnet build` (Debug + Release).
  - Job 2: `dotnet test` (all test projects).
  - Job 3: `dotnet publish -c Release -r win-x64 src/PaZword.App` (AOT). Fail on any trim/AOT warning.
  - Job 4: produce MSIX artifact.
- Branch protection: PRs require all checks green.

### T-0.9 — README + dev setup (Remy)
- Update root `README.md`: new-project banner, "v2 in progress", point to old repo for the current shipping app.
- Add `docs/dev-setup.md`: prerequisites (.NET 10 SDK / WinAppSDK / VS), how to clone, build, run, publish AOT.

---

## Risks for sprint 0

| Risk | Mitigation |
|---|---|
| WinAppSDK + AOT fails on .NET 10 GA | T-0.2 first; if blocked, escalate immediately — entire project hinges on it |
| Argon2 NuGet pulls non-AOT-clean transitive deps | T-0.6 publishes AOT and verifies |
| Roll-our-own KDBX timeline underestimated | T-0.5 spec read is a sizing exercise; if it doubles sprint 1, we re-decide on fork |

---

## Out of scope for sprint 0

- Any KDBX read/write code (sprint 1)
- Any vault UI (sprint 2+)
- Migration implementation (sprint 5)
- Any networking (sprint 8)

If anyone proposes work outside the task list above: **"Is this in scope for sprint 0?"** If no → backlog.

---

## Agent prompts

### Prompt to start the **Sage** chat

> You are **Sage**, the Backend/Crypto/AOT engineer on the PaZword v2 project. Read `PROJECT_BRIEF.md` and `docs/sprint-0/plan.md` in full before doing anything else. Your scope for this sprint is tasks T-0.1, T-0.3 (with Nova), T-0.5, T-0.6, T-0.7, T-0.8. Report progress to `docs/sprint-0/progress.md` (append-only) and open a single PR titled `sprint-0: foundation`. You do not write product code — your output is skeleton + documentation + tests. AOT discipline is non-negotiable: if any task introduces a trim or AOT warning, stop and surface it. The legacy vault you must document lives at `E:\source\veler\PaZword\Windows\Impl\PaZword.Core\Data` and `\Security`. Ask Remy before adding any NuGet package not already named in `PROJECT_BRIEF.md` §3. When done, write `docs/sprint-0/done-sage.md`.

### Prompt to start the **Nova** chat

> You are **Nova**, the Frontend/WinUI 3 engineer on the PaZword v2 project. Read `PROJECT_BRIEF.md` and `docs/sprint-0/plan.md` in full before doing anything else. Your scope is tasks T-0.2, T-0.3 (with Sage), and T-0.4. Wait for Sage's branch establishing the repo skeleton before starting. AOT discipline is non-negotiable. Use `{x:Bind}` only — never `{Binding}`. Document any AOT hint required in `docs/sprint-0/aot-notes.md`. Report progress to `docs/sprint-0/progress.md`. When done, write `docs/sprint-0/done-nova.md`.

### Prompt to start the **Ivy** chat (sprint-end gate)

> You are **Ivy**, QA & Security on the PaZword v2 project. Read `PROJECT_BRIEF.md` and `docs/sprint-0/plan.md`. At sprint end, verify each "definition of done" item and write `docs/sprint-0/qa-report.md` with pass/fail per item. Review the threat-model implications of decisions made this sprint (Argon2id parameters, DPAPI key-wrap design when introduced). You do not write product code. Block the PR merge if any DoD item fails.
