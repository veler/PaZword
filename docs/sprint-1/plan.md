# Sprint 1 — KDBX Core (read + write + round-trip)

> **Goal:** ship a working, AOT-clean `PaZword.Kdbx` library that reads and writes standard KDBX v4 files round-trippably against KeePassXC fixtures.
> **Output:** a fully-implemented KDBX v4 reader + writer with Argon2id KDF (production floor enforced), AES-256-CBC + ChaCha20 inner stream ciphers, HMAC-SHA-256 block integrity, custom-icon table, binary attachments, `otp` field convention. ~1500–2500 LOC + comprehensive tests.

---

## Sprint goals (definition of done)

1. ✅ `PaZword.Kdbx` reads a KDBX v4 file produced by KeePassXC 2.7+ and exposes the full document tree (groups, entries, fields, binaries, custom icons, metadata).
2. ✅ `PaZword.Kdbx` writes a KDBX v4 file that **KeePassXC opens without warning** and renders identically when round-tripped through KeePassXC.
3. ✅ Round-trip test: read → write → read again → assert identical document model. Green across the test fixture suite (≥5 fixtures: simple, large, with-binaries, with-custom-icons, with-otp).
4. ✅ **Sprint-0 carryover items closed:**
   - Second Argon2id KAT against a non-Konscious reference vector (RFC 9106 §5 with `K=X=empty`, or a `phc-winner-argon2` C-binary output captured offline).
   - KDBX writer enforces production parameter floor (`t≥2`, `m≥64 MiB`, `p≥2`). Below-floor reads succeed with `ILogger` warning; below-floor writes throw `KdbxParameterFloorException`.
   - Every KDF call site zeroes the password byte buffer on completion + targeted unit test asserting buffer is cleared.
5. ✅ Both ciphers implemented and tested: **AES-256-CBC** (legacy default) and **ChaCha20** (modern preferred). Reader auto-detects from header UUID; writer defaults to **ChaCha20** for new vaults.
6. ✅ HMAC-SHA-256 block chain validates on read; tampering anywhere in the file produces `KdbxIntegrityException` and **no entries are exposed to the caller**.
7. ✅ Standard `otp` string field round-trips (KeePassXC convention).
8. ✅ Custom icon table round-trips.
9. ✅ Binary attachments round-trip; ≥4 MB attachment in fixture suite, streamed not buffered.
10. ✅ All AOT discipline rules held: 0 trim warnings, 0 AOT warnings under `TreatWarningsAsErrors=true`.
11. ✅ XML I/O via `XmlReader`/`XmlWriter` only — no `XmlSerializer`, no reflection.
12. ✅ `done.md` + per-agent done docs + `qa-report.md` written. PR `sprint-1: kdbx core` merged.

---

## Tasks

### T-1.1 — KDBX v4 outer-file reader (Sage)
- Implement `KdbxOuterReader`: signature/version magic, outer header KVP parsing, KDF parameters (VariantMap), header HMAC verification.
- Header fields: `CipherID`, `CompressionFlags`, `MasterSeed`, `EncryptionIV`, `KdfParameters`, `PublicCustomData` (preserved opaque).
- Reject KDBX v3.x with `KdbxUnsupportedVersionException` (kdbx4-only).
- Typed errors: `KdbxFormatException`, `KdbxIntegrityException`, `KdbxUnsupportedVersionException`, `KdbxParameterFloorException`.

### T-1.2 — KDBX v4 outer-file writer (Sage)
- Implement `KdbxOuterWriter`: emit valid signature/version, write outer header KVPs, compute and emit header HMAC.
- Default cipher: **ChaCha20** (UUID `D6038A2B-8B6F-4CB5-A524-339A31DBB59A`).
- Default compression: GZip.
- Enforce production floor on KDF write (see DoD #4).

### T-1.3 — Cipher implementations (Sage)
- `ICipherStream` abstraction.
- `AesCbcCipher` using BCL `Aes` + PKCS7.
- `ChaCha20Cipher` — day-1 spike to confirm .NET 10 BCL availability. If absent, implement IETF ChaCha20 (RFC 8439 §2) in managed code (~150 LOC), KAT against RFC 8439 vectors.
- KAT for both: RFC 8439 (ChaCha20) and NIST SP 800-38A (AES-CBC).

### T-1.4 — HMAC block chain (Sage)
- KDBX v4 keyed-HMAC block stream (block-index ‖ block-size ‖ payload, HMAC = HMAC-SHA-256(blockKey, index ‖ size ‖ payload); per-block key = HMACKey || blockIndex).
- Read: verify each block before yielding bytes; mismatch → `KdbxIntegrityException`; stream poisoned (no partial reveal).
- Write: byte-equivalent to KeePassXC output for the same content.

### T-1.5 — Inner header + XML body parser (Sage)
- Inner header: `InnerRandomStreamID`, `InnerRandomStreamKey`, `Binary` entries (protected flag preserved).
- Inner random stream: ChaCha20 (modern, write); Salsa20 (legacy v3 carry-over, read only — never write).
- `XmlReader` (forward-only). Map to `KdbxDocument`.
- `<Value Protected="True">` decrypted **lazily** — `KdbxEntry.GetPassword()` returns a `SecureField` that zeros on dispose.

### T-1.6 — XML body writer (Sage)
- `XmlWriter` emits body matching KeePassXC canonical form (attribute order, whitespace).
- Re-encrypt protected fields with fresh inner random stream.
- Bonus: byte-equivalence test against KeePassXC re-save (advisory, not blocking).

### T-1.7 — Custom icons + attachments (Sage)
- Custom icon table (UUID + PNG bytes) round-trips.
- Binary attachments in inner header by index; entry `<Binary Key="..."><Value Ref="N"/>` references.
- 4 MB attachment fixture proves streaming (no `byte[]` for body).

### T-1.8 — KDF parameter floor + zeroization (Sage)
- `KdbxKdfParameters` validates on write: `t<2 || m<64 MiB || p<2` → `KdbxParameterFloorException`.
- Below-floor reads → `ILogger.LogWarning(...)`. Not an error.
- Every password buffer cleared after KDF (`Array.Clear` on the clone).
- Unit test asserts the clone is zeroed (`#if DEBUG` hook).

### T-1.9 — Second Argon2id KAT (Sage) — **DAY 1 BLOCKER**
- Reference vector from `phc-winner-argon2` C binary (or KeePassXC's bundled Argon2), `K=X=empty`, pinned as hex constant.
- Test: `Argon2idKeyDerivation` output matches. If fails → **stop sprint 1, ping Remy**.

### T-1.10 — Fixture corpus + round-trip suite (Sage)
- `tests/PaZword.Kdbx.Tests/Fixtures/` with KeePassXC-produced:
  - `simple.kdbx` (3 groups, 5 entries)
  - `large.kdbx` (500 entries)
  - `binaries.kdbx` (≥4 MB attachment)
  - `custom-icons.kdbx` (5 icons)
  - `otp.kdbx` (`otpauth://...`)
- Master password: `pazword-test`.
- Tests: `Read_KeePassXcFixture_ParsesAll`, `Roundtrip_ReadWriteRead_PreservesDocument`, optional `Roundtrip_KdbxToKeePassXcAndBack` via `keepassxc-cli` (skip if not on CI runner).

### T-1.11 — `PaZword.Core` glue (Sage)
- `IVaultService` in `PaZword.Core` — minimal:
  - `Task<VaultHandle> OpenAsync(string path, VaultCredentials creds, CancellationToken ct)`
  - `Task SaveAsync(VaultHandle handle, CancellationToken ct)`
  - `Task CloseAsync(VaultHandle handle)`
- `VaultService` wraps `PaZword.Kdbx`. No UI dependency.

### T-1.12 — Sprint-end docs (Sage)
- `docs/sprint-1/aot-notes.md` (new AOT hints if any).
- `docs/sprint-1/done-sage.md`.

---

## Risks for sprint 1

| Risk | Mitigation |
|---|---|
| Konscious Argon2 fails the independent KAT | T-1.9 day 1. If fails: stop, escalate, pivot to managed Argon2 or `libargon2` P/Invoke. |
| BCL `ChaCha20` not exposed in .NET 10 | T-1.3 day 1 spike. Fallback: RFC 8439 §2 managed (~150 LOC). |
| KeePassXC XML byte-canonical differs subtly | Round-trip test asserts **model** equality, not bytes. Byte equality is a bonus check, may skip. |
| 4 MB attachment buffered into RAM | `Stream`-based throughout; memory-profile test in fixture suite. |
| Protected field leaks through logs | Review checklist + `SecureField.ToString()` returns `"***"` + targeted test. |

---

## Out of scope for sprint 1

- Any UI (sprint 2+)
- Vault unlock UX (sprint 2)
- Windows Hello / DPAPI key-wrap (sprint 7)
- Cloud sync (sprint 8)
- Migration from old PaZword (sprint 5)
- KDBX v3.x read support (rejected with typed exception)

**"Is this in scope for sprint 1?"** If no → backlog.

---

## Agent prompts

### Prompt to start the **Sage** chat (sprint 1)

> You are **Sage**, the Backend/Crypto/AOT engineer on the PaZword v2 project. The repo is at `E:\source\veler\PaZword-New`. Branch off `main` to `sprint-1/kdbx-core`.
>
> Read **in order, in full** before any work:
> 1. `PROJECT_BRIEF.md` (esp. §3 stack, §7 current state, §9 locked decisions)
> 2. `docs/sprint-0/done.md`
> 3. `docs/sprint-0/qa-report.md` — **all sprint-1 entry conditions in §4.1 are gates on you**
> 4. `docs/sprint-0/kdbx4-spec-notes.md` — your own spec digest
> 5. `docs/sprint-0/legacy-vault-format.md` — context only; you are NOT implementing migration this sprint
> 6. `docs/sprint-1/plan.md` — this sprint
>
> Scope is **all of T-1.1 through T-1.12**. Single PR titled `sprint-1: kdbx core`.
>
> **Day-1 spikes before anything else:**
> - T-1.9 independent Argon2id KAT — if it fails, **stop and ping Remy**.
> - T-1.3 .NET 10 BCL ChaCha20 availability — if absent, plan RFC 8439 §2 managed implementation.
>
> **AOT discipline is non-negotiable.** No reflection, no `XmlSerializer`, no `BinaryFormatter`. `XmlReader`/`XmlWriter` only. 0 trim/AOT warnings under `TreatWarningsAsErrors=true`.
>
> **Crypto discipline is non-negotiable.** Every password/key buffer zeroed after use. Protected fields decrypted lazily via `SecureField`. No log statement ever contains a password, key, or protected field value.
>
> **Do not add a NuGet package** outside `PROJECT_BRIEF.md §3` without asking Remy first.
>
> Progress → `docs/sprint-1/progress.md` (append-only). When all DoD items pass locally, write `docs/sprint-1/done-sage.md` and open the PR. Ivy gates the merge.

### Prompt to start the **Ivy** chat (sprint-1 gate)

> You are **Ivy**, QA & Security on the PaZword v2 project. The repo is at `E:\source\veler\PaZword-New`.
>
> Read in full: `PROJECT_BRIEF.md`, `docs/sprint-0/qa-report.md` (you wrote it; §4.1 sprint-1 entry conditions are your gates), `docs/sprint-1/plan.md`, then Sage's `done-sage.md`.
>
> Write `docs/sprint-1/qa-report.md` with pass/fail per DoD item. Pay particular attention to:
> 1. **Sprint-0 carryover gates** — independent Argon2id KAT, KDF parameter floor, password-buffer zeroization.
> 2. **KDBX round-trip evidence** — read→write→read identity + optional KeePassXC-CLI round-trip.
> 3. **Integrity on tamper** — verify a test that flips a byte in the encrypted payload and asserts `KdbxIntegrityException` with **no entry data leaked**.
> 4. **No protected-field leakage** through logs (grep PR for `LogDebug`/`LogTrace` involving any field value).
> 5. **AOT publish clean** — re-run `dotnet publish -c Release -r win-x64 -p:PublishAot=true -warnaserror` locally; document result.
>
> Block the merge if any DoD item fails. Threat-model addendum welcome.

### Prompt for **Nova** (sprint 1)

> Sprint 1 is backend-only. Nothing for Nova this sprint. Sprint 2 is the App shell + unlock screen — wait for the Kira+Milo brainstorm output before starting.
