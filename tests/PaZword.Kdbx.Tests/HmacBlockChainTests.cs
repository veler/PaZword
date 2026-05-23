using PaZword.Kdbx;
using PaZword.Kdbx.IO;
using System.Security.Cryptography;

namespace PaZword.Kdbx.Tests;

public sealed class HmacBlockChainTests
{
    [Fact]
    public void Write_Then_Read_RoundTrips()
    {
        var key = new byte[64];
        RandomNumberGenerator.Fill(key);
        var payload = new byte[3 * HmacBlockWriteStream.BlockSize + 7];
        RandomNumberGenerator.Fill(payload);

        using var ms = new MemoryStream();
        using (var w = new HmacBlockWriteStream(ms, key, leaveOpen: true))
            w.Write(payload, 0, payload.Length);

        ms.Position = 0;
        using var r = new HmacBlockReadStream(ms, key, leaveOpen: true);
        using var dest = new MemoryStream();
        r.CopyTo(dest);
        Assert.Equal(payload, dest.ToArray());
    }

    [Fact]
    public void Tampered_PayloadByte_ThrowsIntegrityException_And_NoData_Yielded()
    {
        var key = new byte[64];
        RandomNumberGenerator.Fill(key);
        var payload = new byte[1024];
        RandomNumberGenerator.Fill(payload);

        using var ms = new MemoryStream();
        using (var w = new HmacBlockWriteStream(ms, key, leaveOpen: true))
            w.Write(payload, 0, payload.Length);

        // Flip a payload byte inside the first block. Block layout:
        //   [32 HMAC][4 size][payload]
        var raw = ms.ToArray();
        raw[64] ^= 0xFF;
        using var tampered = new MemoryStream(raw);
        using var r = new HmacBlockReadStream(tampered, key, leaveOpen: true);
        var buf = new byte[256];
        var ex = Assert.Throws<KdbxIntegrityException>(() => r.Read(buf, 0, buf.Length));
        // After the throw, no bytes should have been yielded.
        Assert.DoesNotContain(buf, b => b != 0);
        // Subsequent reads also throw (poisoned stream).
        Assert.Throws<KdbxIntegrityException>(() => r.Read(buf, 0, buf.Length));
    }

    [Fact]
    public void Tampered_HmacTag_ThrowsIntegrityException()
    {
        var key = new byte[64];
        RandomNumberGenerator.Fill(key);
        var payload = new byte[256];
        RandomNumberGenerator.Fill(payload);

        using var ms = new MemoryStream();
        using (var w = new HmacBlockWriteStream(ms, key, leaveOpen: true))
            w.Write(payload, 0, payload.Length);

        var raw = ms.ToArray();
        raw[0] ^= 0x01;
        using var tampered = new MemoryStream(raw);
        using var r = new HmacBlockReadStream(tampered, key, leaveOpen: true);
        var buf = new byte[256];
        Assert.Throws<KdbxIntegrityException>(() => r.Read(buf, 0, buf.Length));
    }
}
