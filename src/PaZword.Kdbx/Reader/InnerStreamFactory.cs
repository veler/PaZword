using PaZword.Crypto;
using System.Security.Cryptography;

namespace PaZword.Kdbx.Reader;

/// <summary>
/// XOR keystream supplier for the KDBX inner protected-string stream.
/// Bytes are consumed in entry-XML-encounter order.
/// </summary>
internal interface IProtectedStream : IDisposable
{
    /// <summary>XOR the keystream into <paramref name="buffer"/> in place.</summary>
    void Process(Span<byte> buffer);
}

/// <summary>
/// Inner-stream factory. KDBX 4 modern: ChaCha20 keyed with the first 32 bytes
/// of <c>SHA-512(InnerRandomStreamKey)</c> and nonce = next 12 bytes. Legacy
/// (KDBX 3.1 / v4 with Salsa20 selected): Salsa20 keyed with
/// <c>SHA-256(InnerRandomStreamKey)</c> + fixed 8-byte nonce
/// <c>E830094B97205D2A</c>.
/// </summary>
internal static class InnerStreamFactory
{
    private static readonly byte[] Salsa20InitialIv =
    {
        0xE8, 0x30, 0x09, 0x4B, 0x97, 0x20, 0x5D, 0x2A,
    };

    public static IProtectedStream Create(KdbxInnerHeaderReader.StreamCipherId cipherId, byte[] streamKey)
    {
        switch (cipherId)
        {
            case KdbxInnerHeaderReader.StreamCipherId.ChaCha20:
            {
                Span<byte> hash = stackalloc byte[64];
                SHA512.HashData(streamKey, hash);
                var key = hash[..32].ToArray();
                var nonce = hash.Slice(32, 12).ToArray();
                CryptographicOperations.ZeroMemory(hash);
                return new ChaChaProtectedStream(key, nonce);
            }
            case KdbxInnerHeaderReader.StreamCipherId.Salsa20:
            {
                Span<byte> hash = stackalloc byte[32];
                SHA256.HashData(streamKey, hash);
                var key = hash.ToArray();
                CryptographicOperations.ZeroMemory(hash);
                return new SalsaProtectedStream(key, Salsa20InitialIv);
            }
            case KdbxInnerHeaderReader.StreamCipherId.None:
                return new NullProtectedStream();
            default:
                throw new KdbxFormatException($"Unsupported InnerRandomStreamID {(int)cipherId}.");
        }
    }

    private sealed class ChaChaProtectedStream : IProtectedStream
    {
        private readonly ChaCha20 _cipher;
        public ChaChaProtectedStream(byte[] key, byte[] nonce) { _cipher = new ChaCha20(key, nonce); }
        public void Process(Span<byte> buffer) => _cipher.Process(buffer);
        public void Dispose() => _cipher.Dispose();
    }

    private sealed class SalsaProtectedStream : IProtectedStream
    {
        private readonly Salsa20 _cipher;
        public SalsaProtectedStream(byte[] key, byte[] nonce) { _cipher = new Salsa20(key, nonce); }
        public void Process(Span<byte> buffer) => _cipher.Process(buffer);
        public void Dispose() => _cipher.Dispose();
    }

    private sealed class NullProtectedStream : IProtectedStream
    {
        public void Process(Span<byte> buffer) { }
        public void Dispose() { }
    }
}
