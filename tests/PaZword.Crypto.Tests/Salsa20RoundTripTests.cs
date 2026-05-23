using PaZword.Crypto;

namespace PaZword.Crypto.Tests;

/// <summary>
/// Round-trip tests for legacy Salsa20 (inner-stream read path).
/// </summary>
public sealed class Salsa20RoundTripTests
{
    [Fact]
    public void Encrypt_Then_Decrypt_Roundtrips()
    {
        var key = new byte[32];
        Random.Shared.NextBytes(key);
        var nonce = new byte[8];
        Random.Shared.NextBytes(nonce);

        var plaintext = new byte[1024];
        Random.Shared.NextBytes(plaintext);

        var ciphertext = new byte[plaintext.Length];
        using (var c = new Salsa20(key, nonce))
            c.Process(plaintext, ciphertext);
        Assert.NotEqual(plaintext, ciphertext);

        var decrypted = new byte[plaintext.Length];
        using (var c = new Salsa20(key, nonce))
            c.Process(ciphertext, decrypted);
        Assert.Equal(plaintext, decrypted);
    }
}
