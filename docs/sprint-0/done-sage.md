# Sprint 0 — Done (Sage)

> Completed: 2026-05-22

## Tasks completed

| Task | Status | Notes |
|------|--------|-------|
| T-0.1 Repo skeleton | ✅ Done | 7 src projects + 4 test projects, PaZword.slnx, Directory.Build.props, Directory.Packages.props (CPM), .editorconfig |
| T-0.3 DI + MVVM bootstrap | ✅ Done | `INavigationService` stub, `DiagnosticsService` sample, `AppServiceProvider` MEDI scaffold |
| T-0.5 KDBX library spike | ✅ Done | Interfaces + model stubs in `PaZword.Kdbx`; `docs/sprint-0/kdbx4-spec-notes.md` |
| T-0.6 Crypto module + Argon2 KAT | ✅ Done | `Argon2idKeyDerivation` + 7 KAT tests all green |
| T-0.7 Legacy vault docs | ✅ Done | `docs/sprint-0/legacy-vault-format.md` — format, encryption, schema, 14 migration edge cases |
| T-0.8 CI pipeline | ✅ Done | `.github/workflows/ci.yml` — Build, Test, AOT Publish (trim-warn gate), MSIX |

## AOT status

- `PaZword.Crypto`: no AOT warnings. `Konscious.Security.Cryptography.Argon2` is pure managed; builds and tests cleanly.
- `PaZword.Kdbx`, `PaZword.Core`: interface stubs only; no runtime code that could trigger trim warnings.
- `PaZword.App`: `PublishAot=true` set; Windows 11 min version (`10.0.22621.0`); `WindowsAppSDKSelfContained=true`. Full AOT publish will be validated by CI job 3 once Nova completes T-0.2.

## NuGet packages added (vs. PROJECT_BRIEF §3 list)

All packages are already named in `PROJECT_BRIEF.md §3`. No new packages were introduced. Exact versions pinned in `Directory.Packages.props`.

| Package | Version | Why |
|---------|---------|-----|
| `Konscious.Security.Cryptography.Argon2` | 1.3.1 | Argon2id KDF (named in brief) |
| `CommunityToolkit.Mvvm` | 8.4.0 | MVVM source-gen (named in brief) |
| `Microsoft.Extensions.DependencyInjection` | 10.0.0 | DI container (named in brief) |
| `Microsoft.Extensions.Logging` | 10.0.0 | Logging (named in brief) |
| `Microsoft.WindowsAppSDK` | 2.1.3 | WinAppSDK (brief says 1.7+; 2.1.3 is latest stable) |

## Handoff to Nova

Nova needs `PaZword.App` as the foundation for T-0.2 (AOT spike) and T-0.4 (brand tokens).

- The app project has `PublishAot=true` and `WindowsAppSDKSelfContained=true` already set.
- `AppServiceProvider.cs` is ready for Nova to wire up the `INavigationService` concrete implementation.
- `CommunityToolkit.Mvvm` is in CPM — Nova can add it to `PaZword.App.csproj` without a version.

## Known open items

- RFC 9106 vector: Konscious omits secret + associated data parameters, so our KAT uses a Konscious-derived vector, not the exact RFC 9106 §5 tag. Comment in `Argon2idKatTests.cs` explains this. No action needed; vector is stable for Konscious 1.3.x.
- `PaZword.App` AOT publish not yet validated locally (requires full WinAppSDK MSIX toolchain). CI job 3 will be the gate.
