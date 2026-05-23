using PaZword.Crypto;
using PaZword.Kdbx;
using PaZword.Kdbx.KeyDerivation;
using PaZword.Kdbx.Models;
using PaZword.Kdbx.Writer;
using System.Security.Cryptography;

namespace PaZword.Kdbx.Tests;

/// <summary>
/// Sprint-1 DoD #4: KDBX writer enforces the production parameter floor.
/// Below-floor reads succeed with a warning; below-floor writes throw
/// <see cref="KdbxParameterFloorException"/>.
/// </summary>
public sealed class KdfFloorTests
{
    [Theory]
    [InlineData(1u, KdfFloor.MinMemoryKib, KdfFloor.MinParallelism)]      // t=1
    [InlineData(KdfFloor.MinIterations, 1024L, KdfFloor.MinParallelism)]   // m=1 MiB
    [InlineData(KdfFloor.MinIterations, KdfFloor.MinMemoryKib, 1u)]       // p=1
    public void ValidateForWrite_BelowFloor_Throws(uint t, long m, uint p)
    {
        var kdf = new KdbxKdfParameters
        {
            Uuid = KdbxKdfUuids.Argon2id,
            Salt = new byte[32],
            Iterations = t,
            MemoryKib = (ulong)m,
            Parallelism = p,
            Version = 0x13,
        };
        Assert.Throws<KdbxParameterFloorException>(() => kdf.ValidateForWrite());
    }

    [Fact]
    public void ValidateForWrite_AtFloor_Succeeds()
    {
        var kdf = KdbxKdfParameters.DefaultArgon2id(new byte[32]);
        kdf.ValidateForWrite(); // no throw
    }

    [Fact]
    public void ValidateForWrite_Argon2d_Refused()
    {
        var kdf = new KdbxKdfParameters
        {
            Uuid = KdbxKdfUuids.Argon2d,
            Salt = new byte[32],
            Iterations = (ulong)KdfFloor.MinIterations,
            MemoryKib = (ulong)KdfFloor.MinMemoryKib,
            Parallelism = (uint)KdfFloor.MinParallelism,
            Version = 0x13,
        };
        Assert.Throws<KdbxParameterFloorException>(() => kdf.ValidateForWrite());
    }

    [Fact]
    public async Task Writer_WithBelowFloor_Throws()
    {
        // Pre-seed the document header with below-floor params; the writer must reject.
        var src = TestVaults.BuildSimpleVault();
        // Forge a header containing weak Argon2id by constructing a new document with that header.
        var weakHeader = new KdbxHeader
        {
            MajorVersion = 4, MinorVersion = 1,
            CipherId = new Guid("D6038A2B-8B6F-4CB5-A524-339A31DBB59A").ToByteArray(),
            CompressionFlags = 1,
            MasterSeed = new byte[32],
            EncryptionIV = new byte[12],
            KdfParameters = new KdbxKdfParameters
            {
                Uuid = KdbxKdfUuids.Argon2id,
                Salt = new byte[32],
                Iterations = 1, MemoryKib = 8, Parallelism = 1, Version = 0x13,
            },
        };
        var weak = new KdbxDocument { Header = weakHeader, Root = src.Root, Meta = src.Meta };
        foreach (var b in src.Binaries) weak.Binaries.Add(b);

        var composite = CompositeKey.Build(TestVaults.MasterPassword);
        try
        {
            using var ms = new MemoryStream();
            await Assert.ThrowsAsync<KdbxParameterFloorException>(async () =>
                await new KdbxWriter().WriteAsync(weak, ms, composite));
        }
        finally { CryptographicOperations.ZeroMemory(composite); }
    }
}
