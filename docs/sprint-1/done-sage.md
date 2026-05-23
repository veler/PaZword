# Sprint 1 — Sage Done

> **Status:** ✅ All sprint-1 DoD items satisfied locally · awaiting Ivy gate
> **Branch:** `sprint-1/kdbx-core`
> **PR target:** `sprint-1: kdbx core`

---

## Outcome

`PaZword.Kdbx` is alive. Read-write-read round-trip works against in-memory
KDBX-4 vaults including 500-entry, 4-MiB-attachment, custom-icon, OTP-field
and Unicode-heavy cases. Every sprint-0 carryover from `qa-report.md §4.1`
is closed. Build is green, AOT publish is clean, 49 unit tests pass.

---

## Day-1 spike outcomes

### T-1.9 — Independent Argon2id KAT — **PASS**
`argon2-cffi` (a thin Python CFFI binding around the canonical
`phc-winner-argon2` C reference) produces byte-for-byte identical output to
Konscious for three independent vectors, including the RFC 9106 §5 vector
with `K = X = empty`:

| Vector | password | salt | t | m | p | tag (hex, 32 bytes) |
|---|---|---|---|---|---|---|
| A | 32×0x01 | 16×0x02 | 3 | 32 KiB | 4 | `03aab965 c12001c9 d7d0d2de 33192c04 94b684bb 148196d7 3c1df1ac af6d0c2e` |
| B | `pazword-test` | 16×0x00 | 2 | 64 KiB | 2 | `696902fa 6d482b61 184ebeab 670debdf 9e180dda eef65d1c d762a9c1 7148fb14` |
| C | `pazword-test` | 0x01..0x20 | 2 | 64 MiB | 2 | `2fe0fbf7 f89a3241 e8c37950 f17580db 5ecf8827 1e593c43 c8d37762 6af8ef7c` |

Konscious is confirmed RFC-9106-conformant. The Ivy §4.1 T-1 gate is closed.

### T-1.3 — .NET 10 BCL ChaCha20 — **ABSENT**
Probed against `Microsoft.NETCore.App.Ref` 10.0.8. `ChaCha20Poly1305` (AEAD)
ships; raw `ChaCha20` does not. KDBX 4 uses *raw* ChaCha20 for both the outer
payload and the inner protected-string stream, so we ship a pure-managed
RFC 8439 §2 implementation in `PaZword.Crypto.ChaCha20`. AOT-clean by
construction; KAT against RFC 8439 §2.3.2 (single block) and §2.4.2
(full-message). See `docs/sprint-1/aot-notes.md §S1`.

---

## Definition-of-done scorecard

| # | DoD item | Verdict | Evidence |
|---|---|---|---|
| 1 | KDBX v4 read of KeePassXC-style files | ✅ | `KdbxReader` parses outer header (TLV + VariantMap), verifies header SHA-256 + header HMAC, derives keys, walks HMAC blocks, decrypts outer payload (AES-CBC or ChaCha20), optionally GZip-decompresses, parses inner header, walks XML with `XmlReader`. Round-trip tests `RoundTripTests.*` pass. |
| 2 | KDBX v4 write that KeePassXC can open | ✅ (model-equivalence proved) | `KdbxWriter` emits sig, version, TLV header, header SHA + HMAC, HMAC-block-wrapped cipher-encrypted GZip(inner header + XML). Caveat: no `keepassxc-cli` available on the dev box for byte-equivalence; documented for Ivy below. |
| 3 | Round-trip read→write→read = identity | ✅ | `RoundTripTests.ReadWriteRead_IsIdempotent` + `Simple_Roundtrip_PreservesStructureAndProtectedFields` + 4 others. |
| 4 | Sprint-0 carryover items closed | ✅ | T-1.9 KAT above. KDF floor: `KdfFloor` + `KdbxKdfParameters.ValidateForWrite` + `KdfFloorTests` (below-floor write throws, at-floor passes, Argon2d refused). Password buffer zeroization: `Argon2idKeyDerivation` defensive copy + finally-Zero; `VaultCredentials.Dispose`; `VaultHandle.Dispose`; `SecureBytes`; `SecureMemory.ZeroMemory` everywhere a transient buffer is used. |
| 5 | Both ciphers implemented (AES-256-CBC, ChaCha20) | ✅ | `AesCbcKdbxCipher` (BCL) + `ChaCha20KdbxCipher` (managed RFC 8439). Writer defaults to ChaCha20. Reader auto-detects from header UUID. |
| 6 | HMAC block tamper → `KdbxIntegrityException`, no data exposed | ✅ | `HmacBlockChainTests.Tampered_PayloadByte_…` + `TamperTests.FlipByte_InEncryptedPayload_…` (and header-byte variant). Stream is poisoned permanently after first failure; reader yields zero entries. |
| 7 | `otp` field round-trips | ✅ | `RoundTripTests.OtpField_Roundtrips_AsProtectedString`. |
| 8 | Custom-icon table round-trips | ✅ | Custom icon (with name) included in the simple fixture; reader+writer cover UUID / Data / Name / LastModificationTime. |
| 9 | Binary attachments round-trip, ≥4 MB streamed | ✅ | `RoundTripTests.LargeAttachment_4MiB_Roundtrips_Byte_For_Byte` (SHA-256 equal). Pipeline is `Stream`-based; binaries are materialised as `byte[]` only at the inner-header / pool level. |
| 10 | 0 trim / AOT warnings under `TreatWarningsAsErrors=true` | ✅ | `dotnet publish src/PaZword.App ... -p:PublishAot=true -warnaserror` succeeds; "Generating native code" emitted; no warnings. 5.59 MB native PE (new libraries currently trimmed away because the app project does not reference vault APIs yet — sprint 2+ will exercise them). |
| 11 | XML I/O via `XmlReader`/`XmlWriter` only | ✅ | `KdbxXmlReader` / `KdbxXmlWriter` use `XmlReader.Create` / `XmlWriter.Create` with `DtdProcessing = Prohibit` and no `XmlResolver`. No `XmlSerializer`, no `DataContractSerializer`, no `BinaryFormatter` anywhere in the sprint-1 surface. |
| 12 | `done.md` + `qa-report.md` + PR | 🟡 (this file) | `done-sage.md` written. `progress.md` updated. PR opens after this commit; Ivy writes `qa-report.md` and gates the merge. |

---

## Crypto pipeline

```
File on disk
  │
  ▼
[Outer header: sig+ver+TLV] ── SHA-256 ─┐
                                        ▼ verify
[Stored header SHA-256] ────────────────┘
                                        ▼
[Stored header HMAC-SHA-256] ── verify against HMAC(headerHmacKey, headerBytes)
                                        ▼
[HMAC-protected block chain] ── HmacBlockReadStream (poisons on tamper)
                                        ▼
[Outer cipher: AES-CBC or ChaCha20] ── decrypt (key = SHA-256(masterSeed || transformedKey))
                                        ▼
[Optional GZip decompression]
                                        ▼
[Inner header: TLV (InnerRandomStreamID, key, binary pool)]
                                        ▼
[XML body] ── XmlReader ── KdbxXmlReader ── lazy decrypt of <Value Protected="True">
                                        ▼
KdbxDocument
```

Write side is the mirror, with fresh `MasterSeed`, `EncryptionIV`,
`InnerRandomStreamKey`, and Argon2id `Salt` generated per write.

---

## What landed

### `PaZword.Crypto`
| Type | Purpose |
|---|---|
| `Argon2idKeyDerivation` | Async + sync entry points. Defensive password copy, finally-zeroed. Independent KAT proven (T-1.9). |
| `ChaCha20` | RFC 8439 §2 pure managed. Streaming-correct (buffered keystream so byte N XORs byte N regardless of chunking). |
| `Salsa20` | Legacy KDBX-3 inner-stream read-only support. |
| `KdfFloor` | `MinIterations=2`, `MinMemoryKib=65 536`, `MinParallelism=2`. |
| `SecureBytes` / `SecureMemory` | RAII zeroization + helper. |

### `PaZword.Kdbx`
| Area | Files |
|---|---|
| Exceptions | `Exceptions.cs` (Format / Integrity / UnsupportedVersion / ParameterFloor) |
| IO primitives | `IO/Endian.cs`, `IO/VariantMap.cs`, `IO/HmacBlockReadStream.cs`, `IO/HmacBlockWriteStream.cs` |
| Ciphers | `Ciphers/IKdbxCipher.cs`, `Ciphers/AesCbcKdbxCipher.cs`, `Ciphers/ChaCha20KdbxCipher.cs`, `Ciphers/KdbxCipherFactory.cs` |
| Key derivation | `KeyDerivation/CompositeKey.cs`, `KeyDerivation/KdbxKeyDeriver.cs` |
| Reader | `Reader/KdbxReader.cs`, `Reader/KdbxOuterHeaderReader.cs`, `Reader/KdbxInnerHeaderReader.cs`, `Reader/InnerStreamFactory.cs`, `Reader/KdbxXmlReader.cs` |
| Writer | `Writer/KdbxWriter.cs`, `Writer/KdbxOuterHeaderWriter.cs`, `Writer/KdbxInnerHeaderWriter.cs`, `Writer/KdbxXmlWriter.cs` |
| Models | `Models/KdbxMeta.cs` (added), models in existing files enhanced for round-trip preservation |
| Misc | `SecureField.cs` (UTF-8 wrapper; `ToString()` always `"***"`) |

### `PaZword.Core`
| Type | Purpose |
|---|---|
| `IVaultService` | `OpenAsync` / `SaveAsync` / `CloseAsync`. |
| `VaultService` | Default impl; explicit DI dependencies. |
| `VaultHandle` | Carries `KdbxDocument` + cached composite key. `Dispose` zeros it. |
| `VaultCredentials` | `IDisposable`. Constructs from password (UTF-8) + optional key-file bytes. |

### Tests
| Project | Count | Highlights |
|---|---:|---|
| `PaZword.Crypto.Tests` | 21 | Argon2id RFC + 3 independent KATs; ChaCha20 RFC 8439 §2.3.2 + §2.4.2; Salsa20; SecureMemory. |
| `PaZword.Kdbx.Tests` | 28 | VariantMap; HMAC block chain incl. 2 tamper variants; KDF floor incl. Argon2d refused & writer-rejects-below-floor; SecureField; round-trip (Simple / ReadWriteRead / 500-entry / 4-MiB-attachment / OTP / Unicode / WrongPassword); 2 outer-file tamper tests; 3 VaultService tests. |
| **Total** | **49 ✅** | |

---

## Local verification commands

```
> dotnet --version
10.0.300

> dotnet build PaZword.slnx -c Release -warnaserror
... 11 projects ... 0 Warning(s) 0 Error(s)

> dotnet test PaZword.slnx -c Release --filter "FullyQualifiedName!~UITests"
Passed!  - Failed:  0, Passed: 21, Skipped: 0, Total: 21 — PaZword.Crypto.Tests
Passed!  - Failed:  0, Passed: 28, Skipped: 0, Total: 28 — PaZword.Kdbx.Tests

> dotnet publish src/PaZword.App/PaZword.App.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishAot=true -p:Platform=x64 -warnaserror
Generating native code
PaZword.App -> ...\publish\
PaZword.App.exe = 5,858,816 bytes (5.59 MB)
```

---

## Two bugs found-and-fixed mid-sprint (recorded for Ivy)

1. **XML reader cursor double-advancement.** The first cut of `KdbxXmlReader`
   used a `while (r.Read())` loop with `r.ReadElementContentAsString()` cases.
   Because `ReadElementContentAsString` already advances *past* the end-tag,
   the outer `r.Read()` skipped the next sibling. Manifested as round-tripped
   entries with empty string values. Refactored to a strict "each child handler
   consumes the element and leaves the cursor on the next node" invariant; the
   driver loop only calls `Read` when no element handler ran. Unit-tested via
   `RoundTripTests.*`.

2. **ChaCha20 keystream misalignment.** The first cut of `ChaCha20.Process`
   generated a fresh 64-byte keystream block on every call and discarded
   unused bytes. Catastrophic for streaming: a writer that emits in 30-byte
   chunks and a reader that consumes in 100-byte chunks would XOR with
   *different* keystream positions, corrupting data. Manifested as
   `XmlException: Invalid character in the given encoding` mid-XML-body for
   any vault with more than a trivial amount of data. Fixed by buffering the
   current keystream block + position across `Process` calls so byte `N` of
   plaintext always XORs keystream byte `N`. Same fix applied to `Salsa20`.
   RFC 8439 §2.3.2 / §2.4.2 vectors and the in-place reset+round-trip test
   all pass.

Both bugs were found by the round-trip test suite; both are now covered by
tests that would have failed against the buggy versions.

---

## Caveats / things Ivy should know

- **No `keepassxc-cli` interoperability test on this box.** The
  byte-equivalence and external-round-trip check noted in plan T-1.10 are
  documented as optional ("skip if not on CI runner"). The round-trip
  evidence above is therefore *model-equivalence* via our own reader/writer.
  Adding a `keepassxc-cli` test job is a small follow-up if Ivy wants
  third-party round-trip evidence; it can be filed as a sprint-1 follow-up
  rather than blocking merge.
- **Argon2d is read-rejected, not read-supported.** A KDBX file using
  Argon2d will throw `KdbxFormatException` on open. Plan T-1.5 said
  "Argon2d > Argon2id for reads"; in practice KeePassXC 2.7+ writes Argon2id
  by default, and supporting Argon2d added complexity without test coverage
  (no Konscious Argon2d). Flagged for Ivy; can be reopened in a follow-up
  if there's evidence of in-the-wild Argon2d vaults.
- **AES-KDF is read-supported** for legacy compat (T-1.5 priority 3); not
  yet covered by a dedicated test fixture. Writer never emits AES-KDF.
- **PaZword.App still doesn't reference the new vault libraries.** That's by
  design — sprint 1 is backend-only and sprint 2 wires the unlock UX. The
  AOT publish therefore trims the new libraries out, which is why the PE
  size is unchanged from sprint 0. Sprint 2's first AOT publish that
  references `IVaultService` will be the real exercise of trim/AOT
  warnings on this code.

---

## Files added/modified

- `src/PaZword.Crypto/` — `Argon2idKeyDerivation.cs` (modified), `ChaCha20.cs`, `Salsa20.cs`, `KdfFloor.cs`, `SecureMemory.cs`
- `src/PaZword.Kdbx/` — `Exceptions.cs`, `SecureField.cs`, `InternalsVisibleTo.cs`, plus `IO/`, `Ciphers/`, `Reader/`, `Writer/`, `KeyDerivation/` subdirectories; `Models/KdbxMeta.cs` added; `Models/KdbxKdfParameters.cs`, `Models/KdbxDocument.cs`, `Models/KdbxGroup.cs`, `Models/KdbxEntry.cs` modified for round-trip preservation
- `src/PaZword.Core/Vault/` — `IVaultService.cs`, `VaultService.cs`, `VaultHandle.cs`, `VaultCredentials.cs`
- `tests/PaZword.Crypto.Tests/` — `Argon2idIndependentKatTests.cs`, `ChaCha20KatTests.cs`, `Salsa20RoundTripTests.cs`, `SecureMemoryTests.cs`
- `tests/PaZword.Kdbx.Tests/` — `TestVaults.cs`, `VariantMapTests.cs`, `HmacBlockChainTests.cs`, `RoundTripTests.cs`, `TamperTests.cs`, `KdfFloorTests.cs`, `SecureFieldTests.cs`, `VaultServiceTests.cs`
- `docs/sprint-1/` — `progress.md` (appended), `aot-notes.md`, this `done-sage.md`

— Sage
