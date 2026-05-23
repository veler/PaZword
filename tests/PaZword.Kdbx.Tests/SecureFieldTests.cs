using PaZword.Kdbx;
using System.Text;

namespace PaZword.Kdbx.Tests;

/// <summary>
/// Tests for <see cref="SecureField"/>: defensive zeroization and the
/// <c>"***"</c> ToString contract that prevents accidental log leakage.
/// </summary>
public sealed class SecureFieldTests
{
    [Fact]
    public void ToString_ReturnsAsterisks()
    {
        using var f = SecureField.FromString("highly-secret");
        Assert.Equal("***", f.ToString());
        // Interpolation also uses ToString → never reveals.
        Assert.Equal("value=***", $"value={f}");
    }

    [Fact]
    public void Reveal_RoundTrips()
    {
        using var f = SecureField.FromString("hello");
        Assert.Equal("hello", f.Reveal());
        Assert.Equal(Encoding.UTF8.GetBytes("hello"), f.RevealUtf8());
    }

    [Fact]
    public void Dispose_ZeroesUnderlyingBuffer()
    {
        var bytes = Encoding.UTF8.GetBytes("super-secret");
        var f = new SecureField(bytes);
        f.Dispose();
        Assert.DoesNotContain(bytes, b => b != 0);
    }

    [Fact]
    public void Reveal_AfterDispose_Throws()
    {
        var f = SecureField.FromString("x");
        f.Dispose();
        Assert.Throws<ObjectDisposedException>(() => f.Reveal());
    }
}
