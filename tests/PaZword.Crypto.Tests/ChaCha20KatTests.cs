using PaZword.Crypto;

namespace PaZword.Crypto.Tests;

/// <summary>
/// Known-Answer Tests for raw IETF ChaCha20 (RFC 8439 §2).
/// </summary>
public sealed class ChaCha20KatTests
{
    /// <summary>
    /// RFC 8439 §2.3.2 — single-block keystream vector.
    /// Key  = 00 01 02 .. 1f
    /// Nonce= 00 00 00 09 00 00 00 4a 00 00 00 00
    /// Counter = 1
    /// Expected first 64-byte keystream block (RFC 8439 §2.3.2, table).
    /// </summary>
    [Fact]
    public void Keystream_Rfc8439_Section2_3_2()
    {
        var key = Convert.FromHexString(
            "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
        var nonce = Convert.FromHexString("000000090000004a00000000");

        var zeros = new byte[64];
        var keystream = new byte[64];
        using var cipher = new ChaCha20(key, nonce, initialCounter: 1);
        cipher.Process(zeros, keystream);

        // Expected from RFC 8439 §2.3.2 (the "serialized block" table).
        var expected = Convert.FromHexString(
            "10f1e7e4d13b5915500fdd1fa32071c4" +
            "c7d1f4c733c068030422aa9ac3d46c4e" +
            "d2826446079faa0914c2d705d98b02a2" +
            "b5129cd1de164eb9cbd083e8a2503c4e");
        Assert.Equal(expected, keystream);
    }

    /// <summary>
    /// RFC 8439 §2.4.2 — full encryption vector ("Ladies and Gentlemen ...").
    /// </summary>
    [Fact]
    public void Encrypt_Rfc8439_Section2_4_2()
    {
        var key = Convert.FromHexString(
            "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
        var nonce = Convert.FromHexString("000000000000004a00000000");
        var plaintext = System.Text.Encoding.ASCII.GetBytes(
            "Ladies and Gentlemen of the class of '99: If I could offer you " +
            "only one tip for the future, sunscreen would be it.");

        var ciphertext = new byte[plaintext.Length];
        using (var cipher = new ChaCha20(key, nonce, initialCounter: 1))
            cipher.Process(plaintext, ciphertext);

        // Expected from RFC 8439 §2.4.2.
        var expected = Convert.FromHexString(
            "6e2e359a2568f98041ba0728dd0d6981" +
            "e97e7aec1d4360c20a27afccfd9fae0b" +
            "f91b65c5524733ab8f593dabcd62b357" +
            "1639d624e65152ab8f530c359f0861d8" +
            "07ca0dbf500d6a6156a38e088a22b65e" +
            "52bc514d16ccf806818ce91ab7793736" +
            "5af90bbf74a35be6b40b8eedf2785e42" +
            "874d");
        Assert.Equal(expected, ciphertext);

        // Round-trip: decrypt back to plaintext.
        var roundtrip = new byte[plaintext.Length];
        using (var cipher = new ChaCha20(key, nonce, initialCounter: 1))
            cipher.Process(ciphertext, roundtrip);
        Assert.Equal(plaintext, roundtrip);
    }

    /// <summary>Reset rewinds the block counter.</summary>
    [Fact]
    public void Reset_RewindsCounter()
    {
        var key = new byte[32];
        var nonce = new byte[12];
        var data = new byte[128];
        Random.Shared.NextBytes(data);

        var enc = new byte[data.Length];
        var dec = new byte[data.Length];
        using var c = new ChaCha20(key, nonce);
        c.Process(data, enc);
        c.Reset();
        c.Process(enc, dec);
        Assert.Equal(data, dec);
    }

    /// <summary>Span-aliased in-place encrypt/decrypt round-trips.</summary>
    [Fact]
    public void Process_InPlace_RoundTrips()
    {
        var key = new byte[32];
        Random.Shared.NextBytes(key);
        var nonce = new byte[12];
        Random.Shared.NextBytes(nonce);

        var plaintext = new byte[1000];
        Random.Shared.NextBytes(plaintext);
        var buffer = (byte[])plaintext.Clone();

        using (var c = new ChaCha20(key, nonce))
            c.Process(buffer);
        Assert.NotEqual(plaintext, buffer);

        using (var c = new ChaCha20(key, nonce))
            c.Process(buffer);
        Assert.Equal(plaintext, buffer);
    }
}
