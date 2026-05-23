# Legacy PaZword Vault Format

> Author: Sage · Sprint 0 (2026-05-22)
> Source: `E:\source\veler\PaZword\Windows\Impl\PaZword.Core\Data\*` and `Security\EncryptionProvider.cs`
> Purpose: inform Sprint 5 migration wizard. No code written here.

---

## 1. Overview

The legacy PaZword vault is a **single AES-CBC-encrypted JSON file** stored in the Windows local app data folder. There is no open or standard format — it is entirely proprietary to PaZword v1.

| Property | Value |
|----------|-------|
| File name | `data.pzd` |
| Location | `%LOCALAPPDATA%\...\UserData\data.pzd` (via `Windows.Storage.ApplicationData.LocalFolder`) |
| Format | Versioned, encrypted JSON |
| Encryption | AES-256-CBC (Windows Runtime `CryptographicEngine`) |
| Key storage | Windows `PasswordVault` (Credential Manager) |
| Serializer | `Newtonsoft.Json` with `TypeNameHandling.Objects` |
| Schema version | Integer prefix; current = **3** |

---

## 2. On-disk Format

### Version detection

The file begins with an optional version prefix:

```
<version>:<encrypted-payload>
```

- If no colon is present → **version 1** (old, IV vulnerability).
- Otherwise, `version` is an ASCII integer before the first `:`.
- The last `:` position delimits `encrypted-payload` (see `UpgradeService.Load`).

### Payload encoding

The encrypted payload is a **Base64 string** (no padding stripped).

For version 1 (IV vulnerability):
- The entire file content is Base64 → decrypt with the global IV.

For version 2 and 3:
- Format is `[version]:[Base64IV];[Base64CipherText]` per field (string-level, see §4).
- The file itself is `[version]:[Base64CipherText-of-JSON]`.

---

## 3. Encryption Details

**Algorithm:** `SymmetricAlgorithmNames.AesCbcPkcs7` (AES-256-CBC with PKCS7 padding).

**Key material** is a `PasswordCredential` stored under resource name `"PaZwordSecretKeys"` in the Windows `PasswordVault`:

| `PasswordCredential` field | Content |
|---------------------------|---------|
| `UserName` | Base64-encoded AES key (block-length bytes, 16 for AES-128 or 32 for AES-256) |
| `Password` | Base64-encoded global IV (32 bytes — `IVLength = 32`) |

Key generation code: `CryptographicBuffer.GenerateRandom(blockLength)` for the key, `GenerateRandom(32)` for the IV.

**Per-field IV scheme (version 2+):**

Each encrypted string field stores its own IV prepended to the cipher text, separated by `';'`:

```
[Base64(IV)];[Base64(AesCbcEncrypt(plaintext, key, IV))]
```

If the separator is absent → use the global IV (retro-compat with version 1 fields; see `EncryptionProvider.DecryptString`).

**File-level encryption:**
The entire JSON body is encrypted as a single string with the global IV (re-use, no separator prefix in the file payload).

---

## 4. JSON Schema

After decryption, the payload is a Newtonsoft.Json `UserDataBundle` with `TypeNameHandling.Objects`, meaning the JSON embeds `$type` discriminators for polymorphic collections.

### Top-level structure

```json
{
  "$type": "PaZword.Api.Models.UserDataBundle, PaZword.Api",
  "Accounts": [ ... ],
  "Categories": [ ... ],
  "BrownBag": { "key": "value" }
}
```

### `Category`

```json
{
  "$type": "PaZword.Api.Models.Category, PaZword.Api",
  "Id": "00000000-0000-0000-0000-000000000000",
  "Name": "All",
  "Icon": 0,
  "LastModificationDate": "2024-01-01T00:00:00"
}
```

- `Id` for the built-in "All" category is always `{00000000-0000-0000-0000-000000000000}`.
- `Icon` is a `CategoryIcon` enum integer (0 = Default, 1 = Home, 2 = BankCard, 3 = Personal2, 4 = Professional, 5 = SocialMedia).

### `Account`

```json
{
  "$type": "PaZword.Api.Models.Account, PaZword.Api",
  "Id": "guid",
  "CategoryID": "guid",
  "IconMode": 0,
  "Base64Icon": null,
  "Title": "My Bank",
  "Url": "https://example.com",
  "IsFavorite": false,
  "CreationDate": "2024-01-01T00:00:00",
  "LastModificationDate": "2024-01-01T00:00:00",
  "AccountSubtitle": "user@example.com",
  "Data": [ ... ]
}
```

### `AccountData` (polymorphic)

Each element in `Data` has a `$type` field selecting the concrete class:

| `$type` suffix | Description |
|----------------|-------------|
| `CredentialData` | Login (username, email, password, security Q&A) |
| `BankAccountData` | Bank account (holder, routing, account, SWIFT, IBAN, PIN) |
| `PaymentCardData` | Payment card (bank, holder, number, expiry, CVV, type) |
| `WiFiCredentialData` | Wi-Fi SSID + password |
| `LicenseKeyData` | Software license (licensee, email, company, key) |
| `FileData` | File attachment reference (name, extension, thumbnail) |
| `OtherData` | Generic name/value pair |

All sensitive fields within `AccountData` subtypes are **double-encrypted**: the JSON stores the AES-CBC-encrypted Base64 value (which itself may contain a per-field IV prefix). The `SecureString` wrapping used at runtime is stripped during serialization by `SecureStringJsonConverter`.

### `PaymentCardType` enum

```
0 = Unknown, 1 = Visa, 2 = MasterCard, 3 = AmericanExpress, 4 = Discover, 5 = Other
```

---

## 5. File Attachments

Files are stored **separately** from `data.pzd`, one file per attachment, in the same `UserData` folder:

- File name = `Guid.ToString()` (the `FileData.Id` value), no extension.
- Content = AES-CBC encrypted Base64 of the raw file bytes (using global IV, `reuseGlobalIV: true`).
- Decode: `Base64Decode(AesDecrypt(fileContent))` → raw bytes.
- Max size: **4 MB** (`Constants.DataFileSizeLimit`).
- Max file count: **2 000** (`Constants.DataFileCountLimit`).
- `FileData` in JSON stores only the encrypted `FileName`, `FileExtension`, and an encrypted Base64 thumbnail PNG; the raw bytes are fetched separately by the `DataManager`.

---

## 6. Schema Version History

| Version | Breaking change | Migration |
|---------|-----------------|-----------|
| 1 | IV vulnerability: global IV reused for all fields | Decrypt and re-encrypt every protected field with a fresh per-field IV |
| 2 | Per-field IVs introduced | No data change; just re-encryption |
| 3 | Category icons added | Set default `CategoryIcon` enum values for known category names; unknown categories get `CategoryIcon.Default` |

---

## 7. BrownBag

`BrownBag` is a `ConcurrentDictionary<string, string>` (non-encrypted). It stores small cross-device settings, e.g. sync state tokens. No sensitive data should be here. Migration wizard can copy this verbatim.

---

## 8. Windows Hello Integration

Windows Hello is layered **on top** of the vault, not inside it:
- The AES key and IV are stored in the Windows `PasswordVault`.
- Windows Hello (`KeyCredentialManager`) protects access to the Credential Manager entry.
- If Windows Hello is enrolled, the app uses it to re-unlock the `PasswordVault` credentials on next launch.
- There is no Windows Hello material inside `data.pzd`.

In PaZword v2 (KeePass format), the master password takes the vault's role; DPAPI / Windows Hello wraps the composite key (D-006).

---

## 9. Migration Edge Cases for Sprint 5

1. **Missing `$type` discriminator**: `TypeNameHandling.Objects` can fail if the assembly name changes. The migration reader must use explicit type mappings, not rely on `$type`.

2. **Null / empty encrypted fields**: `EncryptionProvider.EncryptString` accepts null and returns encrypted empty string. The migrator must treat decrypted empty strings as missing fields (map to null / empty KDBX string).

3. **Double-encrypted sensitive fields**: Every `CredentialData.Password`, etc. is AES-encrypted inside the JSON value. Simply reading the JSON gives cipher text; the migrator must call `EncryptionProvider.DecryptString` on each field.

4. **File attachments separate from JSON**: For each `FileData` entry, the migrator must load the corresponding GUID-named file, decrypt it, and embed the raw bytes as a KDBX binary in the inner header pool.

5. **Thumbnail data loss**: `FileData.Base64Thumbnail` is a low-res preview PNG (96 px). KDBX has no thumbnail concept; discard during migration.

6. **Category → KDBX Group mapping**: Legacy categories are flat. Map each category to a top-level KDBX group under root. The "All" category (GUID `{00000000…}`) is virtual — accounts in "All" should be placed directly in the root group or a "Uncategorized" group.

7. **`BrownBag` has no KDBX equivalent**: Discard or store as a single KDBX custom data entry (meta `<CustomData>`) if retention is desired.

8. **Timestamps**: Legacy uses `DateTime` (local or UTC unclear). Convert to UTC before writing KDBX timestamps.

9. **`AccountSubtitle`**: This is a denormalized display hint — no KDBX equivalent. Discard.

10. **`IconMode` + `Base64Icon`**: Custom icon Base64 images can be placed in KDBX `<CustomIcons>` with a generated UUID and referenced from the entry/group.

11. **`SecurityQuestion` / `SecurityQuestionAnswer`** (CredentialData): No standard KDBX field. Store as custom string fields with keys `"SecurityQuestion"` and `"SecurityQuestionAnswer"`.

12. **`PaymentCardData`, `BankAccountData`, `WiFiCredentialData`, `LicenseKeyData`, `OtherData`**: No KDBX equivalents for structured card/bank data. Flatten to custom string fields on a single entry. Use a naming convention like `"Field:BankName"`, `"Field:CardNumber"`, etc.

13. **Corrupted vault detection**: If `AesDecrypt` throws or JSON parse fails, surface a clear error to the wizard — do **not** silently skip entries.

14. **Vault version < 3**: The migrator must run `UpgradeService`-equivalent logic before reading (handle version 1 IV vulnerability).
