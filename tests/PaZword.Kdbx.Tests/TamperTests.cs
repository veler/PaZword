using PaZword.Kdbx;
using PaZword.Kdbx.KeyDerivation;
using PaZword.Kdbx.Reader;
using PaZword.Kdbx.Writer;
using System.Security.Cryptography;

namespace PaZword.Kdbx.Tests;

/// <summary>
/// Tamper tests — Ivy sprint-1 gate #3. A single byte flipped anywhere in the
/// encrypted payload must cause the reader to throw <see cref="KdbxIntegrityException"/>
/// <strong>without yielding any entry data</strong>.
/// </summary>
public sealed class TamperTests
{
    private static async Task<byte[]> ProduceVault()
    {
        var src = TestVaults.BuildSimpleVault();
        var composite = CompositeKey.Build(TestVaults.MasterPassword);
        try
        {
            using var ms = new MemoryStream();
            await new KdbxWriter().WriteAsync(src, ms, composite);
            return ms.ToArray();
        }
        finally { CryptographicOperations.ZeroMemory(composite); }
    }

    [Fact]
    public async Task FlipByte_InEncryptedPayload_Throws_Integrity_And_NoEntries_Exposed()
    {
        var bytes = await ProduceVault();

        // Find a byte well past the outer header + integrity tags (skip first 512 bytes
        // which mostly contain the header + KDF params + SHAs). We aim at the HMAC blocks /
        // ciphertext area.
        int tamperOffset = bytes.Length / 2;
        bytes[tamperOffset] ^= 0xAA;

        var composite = CompositeKey.Build(TestVaults.MasterPassword);
        try
        {
            using var ms = new MemoryStream(bytes);
            var reader = new KdbxReader();
            await Assert.ThrowsAnyAsync<KdbxIntegrityException>(async () => await reader.ReadAsync(ms, composite));
        }
        finally { CryptographicOperations.ZeroMemory(composite); }
    }

    [Fact]
    public async Task FlipByte_InOuterHeader_Throws_Integrity_Before_Decryption()
    {
        var bytes = await ProduceVault();

        // Outer header begins at byte 12 (signatures). Flip something inside the TLV
        // that's part of the SHA-256 + HMAC hashed region — e.g. byte 50.
        bytes[50] ^= 0x01;

        var composite = CompositeKey.Build(TestVaults.MasterPassword);
        try
        {
            using var ms = new MemoryStream(bytes);
            var reader = new KdbxReader();
            await Assert.ThrowsAnyAsync<KdbxIntegrityException>(async () => await reader.ReadAsync(ms, composite));
        }
        finally { CryptographicOperations.ZeroMemory(composite); }
    }
}
