using System.Buffers.Binary;
using System.Security.Cryptography;

namespace PaZword.Crypto;

/// <summary>
/// Pure-managed Salsa20 stream cipher (Bernstein, 2008) — 20 rounds, 256-bit
/// key, 64-bit nonce, 64-bit block counter. Used by KDBX 4 only for the inner
/// protected-string stream when <c>InnerRandomStreamID == 2</c> (legacy
/// KDBX-3 carry-over). <strong>Read-only:</strong> PaZword never writes
/// Salsa20.
/// </summary>
/// <remarks>
/// Streaming-correct: unused keystream bytes are buffered across
/// <see cref="Process(ReadOnlySpan{byte}, Span{byte})"/> calls.
/// </remarks>
public sealed class Salsa20 : IDisposable
{
    private static readonly uint[] Constants =
    {
        0x61707865u, 0x3320646eu, 0x79622d32u, 0x6b206574u,
    };

    private readonly uint[] _state = new uint[16];
    private readonly byte[] _keystreamBuffer = new byte[64];
    private int _keystreamPos = 64;
    private bool _disposed;

    public Salsa20(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        if (key.Length != 32)
            throw new ArgumentException("Salsa20 key must be exactly 32 bytes.", nameof(key));
        if (nonce.Length != 8)
            throw new ArgumentException("Salsa20 nonce must be exactly 8 bytes.", nameof(nonce));

        _state[0]  = Constants[0];
        _state[5]  = Constants[1];
        _state[10] = Constants[2];
        _state[15] = Constants[3];

        _state[1]  = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(0,  4));
        _state[2]  = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(4,  4));
        _state[3]  = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8,  4));
        _state[4]  = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(12, 4));
        _state[11] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(16, 4));
        _state[12] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(20, 4));
        _state[13] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(24, 4));
        _state[14] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(28, 4));

        _state[6] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
        _state[7] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));
        _state[8] = 0;
        _state[9] = 0;
    }

    public void Process(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (output.Length < input.Length)
            throw new ArgumentException("Output span is shorter than input.", nameof(output));

        int processed = 0;
        while (processed < input.Length)
        {
            if (_keystreamPos >= 64)
            {
                Block(_state, _keystreamBuffer);
                unchecked
                {
                    _state[8]++;
                    if (_state[8] == 0) _state[9]++;
                }
                _keystreamPos = 0;
            }
            int available = 64 - _keystreamPos;
            int n = Math.Min(available, input.Length - processed);
            for (int i = 0; i < n; i++)
                output[processed + i] = (byte)(input[processed + i] ^ _keystreamBuffer[_keystreamPos + i]);
            _keystreamPos += n;
            processed += n;
        }
    }

    public void Process(Span<byte> buffer) => Process(buffer, buffer);

    private static void Block(ReadOnlySpan<uint> state, Span<byte> output)
    {
        Span<uint> x = stackalloc uint[16];
        state.CopyTo(x);
        for (int i = 0; i < 10; i++)
        {
            QR(ref x[4],  ref x[0],  ref x[12], ref x[8]);
            QR(ref x[9],  ref x[5],  ref x[1],  ref x[13]);
            QR(ref x[14], ref x[10], ref x[6],  ref x[2]);
            QR(ref x[3],  ref x[15], ref x[11], ref x[7]);

            QR(ref x[1],  ref x[0],  ref x[3],  ref x[2]);
            QR(ref x[6],  ref x[5],  ref x[4],  ref x[7]);
            QR(ref x[11], ref x[10], ref x[9],  ref x[8]);
            QR(ref x[12], ref x[15], ref x[14], ref x[13]);
        }
        for (int i = 0; i < 16; i++)
        {
            uint v = unchecked(x[i] + state[i]);
            BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(i * 4, 4), v);
        }
    }

    private static void QR(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        unchecked
        {
            a ^= System.Numerics.BitOperations.RotateLeft(b + d,  7);
            c ^= System.Numerics.BitOperations.RotateLeft(a + b,  9);
            d ^= System.Numerics.BitOperations.RotateLeft(c + a, 13);
            b ^= System.Numerics.BitOperations.RotateLeft(d + c, 18);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        CryptographicOperations.ZeroMemory(System.Runtime.InteropServices.MemoryMarshal.AsBytes(_state.AsSpan()));
        CryptographicOperations.ZeroMemory(_keystreamBuffer);
        _disposed = true;
    }
}
