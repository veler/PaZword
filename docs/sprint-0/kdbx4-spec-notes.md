# KDBX 4 File-Format Notes

> Author: Sage · Sprint 0 (2026-05-22)
> Primary source: [KeePass File Format Specification](https://keepass.info/help/kb/kdbx_4.html) + KeePassXC source cross-reference.
> This document summarises what matters for `PaZword.Kdbx` (sprint 1 implementation).

---

## 1. File Signature

| Bytes | Value | Meaning |
|-------|-------|---------|
| 0–3   | `0x03 0xD9 0xA2 0x9A` | KeePass primary signature |
| 4–7   | `0x67 0xFB 0x4B 0xB5` | KeePass secondary signature |
| 8–9   | `0x00 0x04` | Minor version (0 for KDBX 4.0, 1 for 4.1) |
| 10–11 | `0x04 0x00` | Major version = 4 |

Total: 12 bytes.

---

## 2. Outer Header (TLV stream)

After the 12-byte signature, a sequence of **Type–Length–Value** fields follows.

- **Type**: 1 byte (unsigned).
- **Length**: 4 bytes (little-endian uint32) in KDBX 4; 2 bytes in KDBX 3.1.
- **Value**: `Length` bytes.

Field ends when `Type == 0` (EndOfHeader). All fields must be read even if unknown.

| Type | Name | Notes |
|------|------|-------|
| 0 | EndOfHeader | Value = `\r\n\r\n` |
| 2 | CipherID | 16-byte UUID. AES-256-CBC = `31C1F2E6BF714350BE5805216AFC5AFF`; ChaCha20 = `D6038A2B8B6F4CB5A524339A31DBB59A` |
| 3 | CompressionFlags | uint32. 0 = none, 1 = GZip |
| 4 | MasterSeed | 32 random bytes. Mixed into the composite key before cipher key derivation |
| 7 | EncryptionIV | IV for the outer cipher block |
| 11 | KdfParameters | VariantMap (see §4) |
| 12 | PublicCustomData | VariantMap; optional plugin data |

---

## 3. Header HMAC

After the TLV stream, the file contains:

1. **Header SHA-256 hash** (32 bytes) — integrity check of the raw header bytes.
2. **Header HMAC-SHA-256** (32 bytes) — authenticates the header with a key derived from the master key.

The HMAC key is `HMAC-SHA-256(key=SHA512(masterSeed + compositeKey + 0x01), data=header_bytes)`.

If either check fails → **abort; do not decrypt**.

---

## 4. KDF Parameters (VariantMap)

A VariantMap is a typed key-value dictionary:

- 2-byte version header (`0x01 0x00`).
- Repeated entries: `[1-byte type][2-byte key-length][key UTF-8][4-byte value-length][value]`.
- Terminated by a zero-type byte.

| VariantMap type | Meaning |
|-----------------|---------|
| `0x04` | uint32 |
| `0x05` | uint64 |
| `0x08` | bool |
| `0x0C` | int32 |
| `0x0D` | int64 |
| `0x18` | string (UTF-8) |
| `0x42` | byte array |

**Argon2 VariantMap keys:**

| Key | Type | Description |
|-----|------|-------------|
| `$UUID` | bytes (16) | Algorithm UUID (Argon2d or Argon2id — see §5) |
| `S` | bytes (32) | Salt |
| `P` | uint32 | Parallelism |
| `M` | uint64 | Memory in kibibytes |
| `I` | uint64 | Iterations |
| `V` | uint32 | Argon2 version (19 = 0x13) |

---

## 5. KDF UUIDs

| UUID | Algorithm |
|------|-----------|
| `C9D9F39A-628A-4460-BF74-0D08C18A4FEA` | AES-KDF (KDBX 3.1 legacy) |
| `EF636DDF-8C29-444B-91F7-A9A403E30A0C` | Argon2d |
| `9E298B19-56DB-4773-B23D-FC3EC6F0A1E6` | Argon2id **(recommended for PaZword)** |

PaZword v2 writes Argon2id. When reading, we must also handle Argon2d and AES-KDF for interoperability with KeePassXC vaults that use the older settings.

---

## 6. Composite Key Derivation

```
compositeKey = SHA256(SHA256(masterPassword) [+ SHA256(keyFileBytes)])

transformedKey = KDF(compositeKey, kdfParams)   // Argon2id or AES-KDF

masterKey = SHA256(masterSeed || transformedKey)
```

The master key is then used to derive:
- **Cipher key** (first 32 bytes of `SHA512(masterKey)` for ChaCha20/AES-256).
- **HMAC key** (see §3).

---

## 7. Encrypted Payload (KDBX 4 block format)

The data after the header + HMAC is split into **content blocks**:

```
[HMAC-SHA256 of block (32 bytes)]
[block size (int32, little-endian)]
[block data (block size bytes)]
```

- Block size 0 = end of data.
- Block HMAC key: `SHA512(blockIndex uint64 LE || masterKey)`.
- Blocks are authenticated before decryption.
- After concatenation and cipher decryption (ChaCha20/AES-256-CBC), the payload is optionally GZip-compressed.

---

## 8. Inner Header

Inside the decrypted+decompressed stream, **before** the XML, there is an inner header using the same TLV format (but with 4-byte lengths):

| Type | Name | Notes |
|------|------|-------|
| 0 | EndOfHeader | |
| 1 | InnerRandomStreamID | uint32: 2=Salsa20, 3=ChaCha20 (recommended) |
| 2 | InnerRandomStreamKey | 64 bytes for ChaCha20 |
| 3 | Binary | `[1-byte flags][data]` — flags bit 0 = isProtected |

Binaries accumulate into a zero-indexed pool; entries reference them by index.

---

## 9. XML Body

The plaintext XML document root is `<KeePassFile>`:

```xml
<KeePassFile>
  <Meta>
    <Generator>PaZword</Generator>
    <DatabaseName>...</DatabaseName>
    <DatabaseDescription>...</DatabaseDescription>
    <MemoryProtection>
      <ProtectUserName>False</ProtectUserName>
      <ProtectPassword>True</ProtectPassword>
      ...
    </MemoryProtection>
    <CustomIcons>
      <Icon><UUID>base64</UUID><Data>base64-PNG</Data></Icon>
    </CustomIcons>
    <Binaries><!-- Only present in KDBX 3.1; in KDBX 4 binaries live in inner header --></Binaries>
    <CustomData><!-- VariantMap as KV pairs --></CustomData>
  </Meta>
  <Root>
    <Group>
      <UUID>base64</UUID>
      <Name>Root</Name>
      <Group>...</Group>  <!-- nested groups -->
      <Entry>
        <UUID>base64</UUID>
        <IconID>0</IconID>
        <String>
          <Key>Title</Key>
          <Value>...</Value>
        </String>
        <String>
          <Key>Password</Key>
          <Value Protected="True">base64-ciphertext</Value>
        </String>
        <Binary>
          <Key>attachment.pdf</Key>
          <Value Ref="0"/>  <!-- index into inner header binary pool -->
        </Binary>
        <AutoType>...</AutoType>
        <History>...</History>
        <Times>
          <CreationTime>2024-01-01T00:00:00Z</CreationTime>
          <LastModificationTime>...</LastModificationTime>
          <LastAccessTime>...</LastAccessTime>
          <ExpiryTime>...</ExpiryTime>
          <Expires>False</Expires>
          <UsageCount>0</UsageCount>
        </Times>
      </Entry>
    </Group>
  </Root>
</KeePassFile>
```

**Protected values** are decrypted in order of appearance using the inner random stream. Each `Value Protected="True"` node consumes the next N bytes from the stream (N = base64-decoded length).

**Timestamps** are stored as base64-encoded **little-endian uint64** (seconds since `0001-01-01T00:00:00Z`) in KDBX 4. In KDBX 3.1 they are ISO-8601 strings.

---

## 10. TOTP Field (KeePassXC convention — D-005)

TOTP is stored as a plain string field with key `"otp"`:

```
otp = otpauth://totp/label?secret=BASE32SECRET&issuer=Issuer&algorithm=SHA1&digits=6&period=30
```

PaZword reads/writes this format unchanged; `Otp.NET` parses the URI in sprint 6.

---

## 11. Implementation Notes for Sprint 1

1. **Read path first**: implement `IKdbxReader` before `IKdbxWriter`. Round-trip tests drive correctness.
2. **Cipher support priority**: ChaCha20 (default in KeePassXC 2.x) > AES-256-CBC.
3. **KDF support priority**: Argon2id > Argon2d > AES-KDF (legacy read only).
4. **Timestamp parsing**: detect format (base64 uint64 vs ISO-8601) by trying base64 first.
5. **Protected string stream**: ChaCha20 inner stream must be seekable per entry scan order (not parallel).
6. **AOT**: use `System.Security.Cryptography.ChaCha20Poly1305` (BCL, AOT-clean). Do **not** use BouncyCastle.
7. **Memory**: zero sensitive byte arrays (`Array.Clear`) before they are GC'd.
8. **Block HMAC failure**: throw `CryptographicException`; do not return partial data.
