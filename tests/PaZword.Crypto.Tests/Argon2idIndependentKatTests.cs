using PaZword.Crypto;

namespace PaZword.Crypto.Tests;

/// <summary>
/// <strong>Sprint-0 carryover §4.1 T-1 (independent KAT) — Ivy gate.</strong>
///
/// <para>
/// These vectors were computed with <c>argon2-cffi</c> 25.x, which is a thin CFFI
/// binding around the canonical <c>phc-winner-argon2</c> C reference implementation.
/// Konscious-vs-reference divergence on any future package upgrade will be caught
/// here; conformance is independent of Konscious.
/// </para>
///
/// <para>
/// All three vectors use <c>K = X = empty</c> (RFC 9106 simplified form), since
/// Konscious does not expose the optional secret / associated-data inputs.
/// </para>
/// </summary>
public sealed class Argon2idIndependentKatTests
{
    private readonly Argon2idKeyDerivation _kdf = new();

    /// <summary>
    /// RFC 9106 §5 inputs with K=X=empty:
    /// password=32×0x01, salt=16×0x02, t=3, m=32 KiB, p=4, tagLen=32.
    /// Reference output (phc-winner-argon2 via argon2-cffi):
    /// <c>03aab965c12001c9d7d0d2de33192c0494b684bb148196d73c1df1acaf6d0c2e</c>.
    /// </summary>
    [Fact]
    public async Task Vector_RfcSimplified_MatchesReference()
    {
        var password = new byte[32];
        Array.Fill(password, (byte)0x01);
        var salt = new byte[16];
        Array.Fill(salt, (byte)0x02);

        var actual = await _kdf.DeriveKeyAsync(password, salt, 4, 32, 3);

        var expected = Convert.FromHexString(
            "03aab965c12001c9d7d0d2de33192c0494b684bb148196d73c1df1acaf6d0c2e");
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// password="pazword-test", salt=16×0x00, t=2, m=64 KiB, p=2.
    /// Reference output:
    /// <c>696902fa6d482b61184ebeab670debdf9e180ddaeef65d1cd762a9c17148fb14</c>.
    /// </summary>
    [Fact]
    public async Task Vector_SmallParams_MatchesReference()
    {
        var password = System.Text.Encoding.UTF8.GetBytes("pazword-test");
        var salt = new byte[16];

        var actual = await _kdf.DeriveKeyAsync(password, salt, 2, 64, 2);

        var expected = Convert.FromHexString(
            "696902fa6d482b61184ebeab670debdf9e180ddaeef65d1cd762a9c17148fb14");
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Production-floor parameters: t=2, m=65 536 KiB (64 MiB), p=2.
    /// Salt = 0x01..0x20.
    /// Reference output:
    /// <c>2fe0fbf7f89a3241e8c37950f17580db5ecf88271e593c43c8d377626af8ef7c</c>.
    /// </summary>
    [Fact]
    public async Task Vector_ProductionFloor_MatchesReference()
    {
        var password = System.Text.Encoding.UTF8.GetBytes("pazword-test");
        var salt = new byte[32];
        for (int i = 0; i < 32; i++) salt[i] = (byte)(i + 1);

        var actual = await _kdf.DeriveKeyAsync(password, salt, 2, 65_536, 2);

        var expected = Convert.FromHexString(
            "2fe0fbf7f89a3241e8c37950f17580db5ecf88271e593c43c8d377626af8ef7c");
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// The KDF wrapper makes a defensive password copy and zeroes it before
    /// returning. The caller's original buffer is untouched (caller's
    /// responsibility) — verify both halves of that contract.
    /// </summary>
    [Fact]
    public async Task DeriveKey_LeavesCallerPassword_Untouched()
    {
        var password = System.Text.Encoding.UTF8.GetBytes("untouched");
        var copy = (byte[])password.Clone();
        var salt = new byte[16];

        _ = await _kdf.DeriveKeyAsync(password, salt, 1, 8, 1);

        Assert.Equal(copy, password); // caller's buffer is unchanged
    }
}
