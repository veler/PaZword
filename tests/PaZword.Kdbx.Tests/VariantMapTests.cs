using PaZword.Kdbx;
using PaZword.Kdbx.IO;

namespace PaZword.Kdbx.Tests;

public sealed class VariantMapTests
{
    [Fact]
    public void RoundTrip_AllSupportedTypes()
    {
        var map = new Dictionary<string, object>
        {
            ["u32"] = 42u,
            ["u64"] = 0xDEADBEEFCAFEBABEUL,
            ["i32"] = -7,
            ["i64"] = (long)-9_000_000_000,
            ["b"]   = true,
            ["s"]   = "héllo",
            ["bs"]  = new byte[] { 1, 2, 3, 4, 5 },
        };

        var encoded = VariantMap.Encode(map);
        var decoded = VariantMap.Decode(encoded);

        Assert.Equal(42u, decoded["u32"]);
        Assert.Equal(0xDEADBEEFCAFEBABEUL, decoded["u64"]);
        Assert.Equal(-7, decoded["i32"]);
        Assert.Equal((long)-9_000_000_000, decoded["i64"]);
        Assert.Equal(true, decoded["b"]);
        Assert.Equal("héllo", decoded["s"]);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, (byte[])decoded["bs"]);
    }

    [Fact]
    public void Decode_RejectsTruncatedInput()
    {
        var encoded = VariantMap.Encode(new Dictionary<string, object> { ["k"] = (uint)1 });
        Assert.Throws<KdbxFormatException>(() => VariantMap.Decode(encoded.AsSpan(0, encoded.Length - 1)));
    }

    [Fact]
    public void Decode_UnknownTypeThrows()
    {
        // 0x01 0x00 — version; 0x99 — unknown type; ...
        var bytes = new byte[] { 0x00, 0x01, 0x99, 0x01, 0x00, 0x00, 0x00, (byte)'k', 0x00, 0x00, 0x00, 0x00, 0x00 };
        Assert.Throws<KdbxFormatException>(() => VariantMap.Decode(bytes));
    }
}
