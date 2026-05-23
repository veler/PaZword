# Sprint 1 — AOT notes

Append entries as new AOT-relevant findings surface during sprint 1.

## Day-1 spikes

### S1 — `System.Security.Cryptography.ChaCha20` is **not** in the .NET 10 BCL
Probed against `Microsoft.NETCore.App.Ref` 10.0.8. `ChaCha20Poly1305` exists (AEAD), but
KDBX 4 uses **raw ChaCha20** for the outer cipher and the inner protected-string stream
(no Poly1305 tag — integrity comes from the outer HMAC block chain). Decision: implement
**RFC 8439 §2** in pure managed C# inside `PaZword.Crypto` (~150 LOC). AOT-clean by
construction (no reflection, no dynamic, all `static`/`struct` math), tested against RFC
8439 §2.3.2 and §2.4.2 vectors.

### S2 — Konscious Argon2id matches `phc-winner-argon2` (C ref) byte-for-byte
Independently verified via `argon2-cffi` (Python binding to the canonical reference
implementation) for the RFC 9106 §5 parameters with `K=X=empty`. Three vectors baked
into `PaZword.Crypto.Tests/Argon2idIndependentKatTests.cs`. Konscious-vs-reference
divergence will be detected by these tests on any future package upgrade — sprint-0
carryover §4.1 T-1 closed.

## Standing rules (sprint 0 carry-over)

1. No `XmlSerializer`, no `DataContractSerializer`, no `BinaryFormatter`. XML I/O via
   `XmlReader`/`XmlWriter` only. KDBX body uses `XmlReader.Create(stream, settings)`
   with `DtdProcessing = Prohibit` and no resolver.
2. No reflection (`Type.GetType` over user input, `Activator.CreateInstance`, etc.).
   DI registrations stay explicit (sprint-0 rule).
3. No `Microsoft.Extensions.Configuration` (pulls reflection — not on the AOT path).
4. Every password / key / composite-key byte buffer is zeroed (`CryptographicOperations.ZeroMemory`
   or `Array.Clear`) on completion of the KDF or on disposal. Lazy decryption via
   `SecureField` (zeros on `Dispose`).
5. Use `Span<T>` / `ReadOnlySpan<T>` throughout the cipher and TLV code paths to avoid
   intermediate heap allocations that would force a public surface for trim analyzers
   to chase.
6. JSON: when JSON appears (settings, sprint 2+), it must use a source-generated
   `JsonSerializerContext` — none used this sprint.

## File-format choices for AOT-cleanliness

- All KDBX TLV reading uses primitive `BinaryReader`-style helpers in `IO/Endian.cs`.
  No `BinaryFormatter`, no `Marshal.PtrToStructure`.
- VariantMap encode/decode is hand-rolled by tag byte; no reflection on the value's
  CLR type. Public surface returns the raw `object?` boxed primitive; callers cast.
- ChaCha20 is implemented as a `struct` state machine + static `Block` method.
- AES-CBC routes to BCL `Aes` (AOT-clean).
