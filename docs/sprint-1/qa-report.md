# Sprint 1 — QA report (retroactive)

> **Author:** Ivy
> **Date:** 2026-05-22 (filed during sprint 2)
> **Status:** 🟡 retroactive (sprint 1 was Producer-merged on 2026-05-22 without Ivy gate; this file closes the process gap).
> **Scope:** crypto pipeline audit of `src/PaZword.Kdbx/` + `src/PaZword.Crypto/`.
> **Action:** carryover items filed as GitHub Issues (#35–#41). Sprint 1 is **not** reopened (per sprint-2 plan §T-2.14 instruction).

---

## Context

`docs/sprint-1/done.md §QA gate note (process deviation)` records the deviation:

> House rule: critical sprints require Ivy sign-off before merge. **This was not done for sprint 1.** Producer-merged on Sage's self-attested DoD scorecard + 49 green tests + clean AOT publish.

The brief assigned me a retroactive audit during sprint 2 (T-2.14) focused on:

- Log statements that could leak protected fields / passwords / keys (grep `LogDebug` / `LogTrace` / `LogInformation`).
- Transient buffers escaping zeroization.
- AES-KDF read coverage.
- Argon2d rejection regression test.

---

## Audit method

1. Static review of every file under `src/PaZword.Kdbx/` and `src/PaZword.Crypto/`.
2. `grep -rn "Log(Debug|Trace|Information|Warning|Error|Critical)\|Console\.\|Debug\.\|Trace\."` over both directories.
3. Cross-checked sprint-1 test inventory (`tests/PaZword.Kdbx.Tests/`, `tests/PaZword.Crypto.Tests/`) for coverage gaps.
4. Re-ran the full test suite to confirm 49/49 baseline before filing carryovers:
   - `dotnet test -c Release tests/PaZword.Crypto.Tests/...` → 21 passed.
   - `dotnet test -c Release tests/PaZword.Kdbx.Tests/...` → 28 passed.
5. Re-ran `dotnet publish -c Release -r win-x64 -p:PublishAot=true -warnaserror` against `PaZword.App` (sprint-2 branch) to confirm 0 trim/AOT warnings still holds with vault libs now referenced.

---

## Logging surface — clean ✓

Findings (entire `src/PaZword.Kdbx/` + `src/PaZword.Crypto/`):

| Hit | File | Severity |
|---|---|---|
| `_logger?.LogWarning(...)` | `PaZword.Kdbx/KeyDerivation/KdbxKeyDeriver.cs:77` | clean |

That single call site logs **only KDF cost parameters** (`{Iterations}`, `{MemoryKib}`, `{Parallelism}`) when an existing vault is opened with below-floor Argon2id parameters. No protected fields, no password bytes, no key material referenced.

Zero `LogDebug` / `LogTrace` / `LogInformation` calls in either project. Zero `Console.*`, `Debug.*`, or `Trace.*` calls. `SecureField.ToString()` returns the constant `"***"` so an accidental `$"...{secureField}..."` cannot leak.

**Verdict: no logging-based leak risk.** ✓

---

## Carryover findings (filed as GitHub Issues — sprint 1 NOT reopened)

| # | Severity | Issue | Summary |
|---|---|---|---|
| #35 | major | AES-KDF read path has no test fixture | `KdbxKeyDeriver.RunAesKdf` ships untested. A silent regression on the legacy-read path would not fail any current test. Already known carryover from `done.md`; elevated because the sprint-5 migration wizard will hit it. |
| #36 | major | Argon2d read-path rejection has no regression test | D-013 policy (Argon2d refused on read) is implemented in `KdbxKeyDeriver.Derive` but only the **write-path** validator is tested (`KdfFloorTests.ValidateForWrite_Argon2d_Refused`). A contributor could weaken the read-path policy without any test failing. |
| #38 | major | Four zeroization gaps in the crypto pipeline | (a) `KdbxKeyDeriver.RunAesKdf` `inBuf`/`outBuf` not zeroized (and dead `Span<byte> half`); (b) `KdbxKeyDeriver.Derive` `masterKey` leaks on partial-failure path; (c) `AesCbcKdbxCipher` / `ChaCha20KdbxCipher` `ToArray()` cipher-key heap copies not zeroized; (d) `KdbxReader` does not zero `InnerStreamKey` (writer-side does — asymmetric). |
| #37 | minor | `SecureField` implemented but not wired into `KdbxProtectedString` | Reader materialises protected plaintext into a managed `string`. `SecureField` exists, is tested, but is not on the hot path. Decision A/B/C needed (wire it through, document the gap, or remove). |
| #39 | minor | `InnerStreamFactory` leaks heap copies of inner stream key + nonce | `hash.Slice(...).ToArray()` allocations not zeroized; trivial fix to use `ReadOnlySpan<byte>` ctors. |
| #40 | minor | Default `KdbxReader()` / `KdbxWriter()` / `VaultService()` ctors silently drop below-floor warnings | Production default in `AppServiceProvider` constructs `new VaultService()` → both inner reader / writer have `null` logger → below-floor warning never reaches a sink. Sprint-2 DI thread-through or `[Obsolete]` markers recommended. |
| #41 | minor | `VaultCredentials.FromPassword(string)` cannot zeroize the source string | Sprint-2 LockedPage hands a managed `string` from `PasswordBox.Password` to the credentials factory. Add a `ReadOnlySpan<char>` overload before the UI binds to it; mark the `string` overload `[Obsolete]`. |

**Three Major, four Minor, zero Critical.** None of the Major findings is an exploitable leak today — they are gaps in defense-in-depth + missing regression tests for policy decisions. The crypto pipeline as shipped does, at a functional level, what the sprint-1 scorecard claims.

---

## What the audit confirmed is correct ✓

- **HMAC block tamper poisoning** (`HmacBlockReadStream.LoadNextBlock`) sets `_poisoned = true` *before* throwing and `CryptographicOperations.FixedTimeEquals` is used for the tag comparison. ✓
- **Outer header SHA-256 + HMAC verification** (`KdbxReader.ReadCore` lines 40–61) runs **before** any payload byte is decrypted; on integrity failure the caller receives a typed `KdbxIntegrityException` and zero plaintext. ✓
- **KDF floor write enforcement** (`KdbxKdfParameters.ValidateForWrite`, `KdfFloorTests`). ✓
- **Argon2id RFC-9106 KAT** independently verified against `phc-winner-argon2` reference (`Argon2idIndependentKatTests`). ✓
- **ChaCha20 RFC 8439 §2.3.2 / §2.4.2** test vectors green; streaming-correct buffered keystream (mid-sprint bug fix retained). ✓
- **`CompositeKey.Build`** stackallocs the password / key-file SHA-256 intermediates and zeroes them in `finally`. ✓
- **`SecureBytes` / `SecureMemory.ZeroMemory`** delegate to `CryptographicOperations.ZeroMemory` (BCL-elision-safe). ✓
- **`SecureField.ToString()`** returns `"***"` constant — accidental interpolation cannot leak. ✓
- **No `BinaryFormatter`, no `Activator.CreateInstance`, no reflection-emit** anywhere in either project (carried from sprint-0 discipline). ✓
- **XML reading**: `XmlReaderSettings.DtdProcessing = Prohibit`, `XmlResolver = null` — no XXE surface. ✓
- **Async API boundary**: `KdbxReader.ReadAsync` / `KdbxWriter.WriteAsync` / `Argon2idKeyDerivation.DeriveKeyAsync` push CPU-bound work onto `Task.Run` (confirmed sprint-1; UI thread protection holds for sprint-2 unlock flow). ✓

---

## What `done.md` overstates (documentation drift)

`docs/sprint-1/done.md §What shipped → PaZword.Kdbx → Reader` states:

> Reader: outer header, inner header, XML body via `XmlReader`. Lazy decrypt of protected fields via `SecureField`.

In practice the reader **eagerly** decrypts protected fields into a managed `string` (`KdbxXmlReader.ReadEntryString`). `SecureField` is built and tested but not used on the document model. Filed as Issue #37 — decision needed (wire it through, document the gap, or delete the unused type).

This is not a security regression: any password manager hands plaintext to the UI eventually. It is a documentation drift that overstates the protection the model provides.

---

## Sprint-1 scorecard re-verdict (retroactive)

| # | Item (`done.md`) | Sprint-1 verdict | Ivy retroactive verdict |
|---|---|---|---|
| 1 | KDBX v4 read | ✅ | ✅ |
| 2 | KDBX v4 write (KeePassXC-openable) | ✅ model-equivalence; byte-equiv vs `keepassxc-cli` deferred | ✅ as stated; `keepassxc-cli` interop remains sprint-9 |
| 3 | Read→write→read identity round-trip | ✅ | ✅ |
| 4 | Sprint-0 carryover (2nd Argon2 KAT, KDF floor, zeroization) | ✅ all three closed | 🟡 zeroization audit reveals 4 gaps (Issue #38); not regression — gaps in defense-in-depth |
| 5 | AES-CBC + ChaCha20 ciphers | ✅ | ✅ functional; cipher-key heap copies unzeroized (rolled into #38) |
| 6 | HMAC block chain tamper → exception + no data leak | ✅ | ✅ verified — `_poisoned` set before throw, `FixedTimeEquals` for tag |
| 7 | `otp` field round-trips | ✅ | ✅ |
| 8 | Custom-icon table round-trips | ✅ | ✅ |
| 9 | 4 MB attachment streamed, round-trips | ✅ | ✅ |
| 10 | 0 trim/AOT warnings | ✅ | ✅ confirmed (re-run on sprint-2 branch with vault refs) |
| 11 | XML I/O via `XmlReader`/`XmlWriter` only | ✅ | ✅ + DTDs prohibited, XmlResolver null |
| 12 | done.md + QA report + PR | 🟡 done.md ✅; Ivy gate skipped; PR Producer-merged | 🟢 Ivy gate retroactively closed by this file + Issues #35–#41 |

---

## Recommendation

Sprint 1 stays merged. The seven Issues filed are carryover scope for sprint 2 (#36, #38, #39, #40) or later sprints (#35 sprint-5, #37 sprint-3, #41 sprint-2/7). None blocks the sprint-2 merge.

For the sprint-2 PR specifically: I recommend bundling #36 (Argon2d regression test) and #38 (zeroization gaps) into the sprint-2 audit PR so the policy lock-in lands at the same time as the app shell. This is small, mechanical, and gives sprint 3 a cleaner crypto floor to build on.

Going forward: every "critical" sprint (per the house rule) goes through Ivy gate **before** Producer-merge — this retroactive gap is closed by this file but should not recur.
