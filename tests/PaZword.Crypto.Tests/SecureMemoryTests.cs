using PaZword.Crypto;
using System.Security.Cryptography;

namespace PaZword.Crypto.Tests;

/// <summary>
/// Tests for <see cref="SecureBytes"/> and <see cref="SecureMemory"/>.
/// </summary>
public sealed class SecureMemoryTests
{
    [Fact]
    public void SecureBytes_Dispose_ZeroesBuffer()
    {
        var b = new SecureBytes(64);
        Random.Shared.NextBytes(b.AsSpan());
        var underlying = b.Buffer;
        Assert.Contains(underlying, x => x != 0);

        b.Dispose();
        Assert.DoesNotContain(underlying, x => x != 0);
    }

    [Fact]
    public void SecureBytes_Dispose_IsIdempotent()
    {
        var b = new SecureBytes(8);
        b.Dispose();
        b.Dispose();
    }

    [Fact]
    public void SecureBytes_AfterDispose_BufferThrows()
    {
        var b = new SecureBytes(8);
        b.Dispose();
        Assert.Throws<ObjectDisposedException>(() => b.Buffer);
    }

    [Fact]
    public void Clear_NullOrEmpty_NoThrow()
    {
        SecureMemory.Clear(null);
        SecureMemory.Clear(Array.Empty<byte>());
    }

    [Fact]
    public void Clear_FillsZeros()
    {
        var b = new byte[16];
        Random.Shared.NextBytes(b);
        SecureMemory.Clear(b);
        Assert.DoesNotContain(b, x => x != 0);
    }
}
