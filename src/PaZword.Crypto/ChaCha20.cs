using System.Buffers.Binary;
using System.Security.Cryptography;

namespace PaZword.Crypto;

/// <summary>
/// Pure-managed IETF ChaCha20 stream cipher per RFC 8439 §2.
/// <para>
/// .NET 10 BCL ships <see cref="ChaCha20Poly1305"/> (AEAD) but not the raw
/// stream cipher. KDBX 4 uses raw ChaCha20 for both the outer payload and
/// the inner protected-string stream (integrity is provided by the outer
/// HMAC block chain, not Poly1305), so we ship our own implementation.
/// </para>
/// <para>
/// AOT-clean: no reflection, no dynamic code generation, no unmanaged pointer
/// math. Pure managed math with <see cref="Span{T}"/> / <see cref="ReadOnlySpan{T}"/>.
/// </para>
/// <para>
/// <b>Streaming-correct:</b> unused keystream bytes are buffered across
/// <see cref="Process(ReadOnlySpan{byte}, Span{byte})"/> calls so the
/// keystream byte at position <em>N</em> always XORs plaintext byte <em>N</em>
/// regardless of how the caller chunks input.
/// </para>
/// </summary>
public sealed class ChaCha20 : IDisposable
{
    private static readonly uint[] Constants =
    {
        0x61707865u, 0x3320646eu, 0x79622d32u, 0x6b206574u,
    };

    private readonly uint[] _state = new uint[16];
    private readonly byte[] _keystreamBuffer = new byte[64];
    private int _keystreamPos = 64; // 64 == buffer empty / need to refill
    private uint _initialCounter;
    private bool _disposed;

    /// <summary>
    /// Initialises a new ChaCha20 cipher with a 256-bit key, 96-bit nonce, and
    /// 32-bit initial counter (RFC 8439 §2.3 layout).
    /// </summary>
    /// <param name="key">32 bytes.</param>
    /// <param name="nonce">12 bytes.</param>
    /// <param name="initialCounter">Initial block counter (usually 0 or 1).</param>
    public ChaCha20(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, uint initialCounter = 0)
    {
        if (key.Length != 32)
            throw new ArgumentException("ChaCha20 key must be exactly 32 bytes.", nameof(key));
        if (nonce.Length != 12)
            throw new ArgumentException("ChaCha20 nonce must be exactly 12 bytes.", nameof(nonce));

        _state[0] = Constants[0]; _state[1] = Constants[1];
        _state[2] = Constants[2]; _state[3] = Constants[3];

        for (int i = 0; i < 8; i++)
            _state[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));

        _initialCounter = initialCounter;
        _state[12] = initialCounter;
        _state[13] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
        _state[14] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));
        _state[15] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(8, 4));
    }

    /// <summary>
    /// Resets the block counter to the initial value supplied at construction
    /// and discards any buffered keystream.
    /// </summary>
    public void Reset()
    {
        _state[12] = _initialCounter;
        _keystreamPos = 64;
        CryptographicOperations.ZeroMemory(_keystreamBuffer);
    }

    /// <summary>
    /// Encrypts (or decrypts — ChaCha20 is symmetric) <paramref name="input"/>
    /// into <paramref name="output"/>. Buffers may alias.
    /// </summary>
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
                _state[12]++;
                if (_state[12] == 0)
                    throw new CryptographicException("ChaCha20 block counter wrapped — message too long.");
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

    /// <summary>In-place processing convenience.</summary>
    public void Process(Span<byte> buffer) => Process(buffer, buffer);

    /// <summary>RFC 8439 §2.3.1 — produces the 64-byte keystream block.</summary>
    private static void Block(ReadOnlySpan<uint> state, Span<byte> output)
    {
        Span<uint> w = stackalloc uint[16];
        state.CopyTo(w);

        for (int i = 0; i < 10; i++)
        {
            QuarterRound(ref w[0], ref w[4], ref w[8],  ref w[12]);
            QuarterRound(ref w[1], ref w[5], ref w[9],  ref w[13]);
            QuarterRound(ref w[2], ref w[6], ref w[10], ref w[14]);
            QuarterRound(ref w[3], ref w[7], ref w[11], ref w[15]);

            QuarterRound(ref w[0], ref w[5], ref w[10], ref w[15]);
            QuarterRound(ref w[1], ref w[6], ref w[11], ref w[12]);
            QuarterRound(ref w[2], ref w[7], ref w[8],  ref w[13]);
            QuarterRound(ref w[3], ref w[4], ref w[9],  ref w[14]);
        }

        for (int i = 0; i < 16; i++)
        {
            uint v = unchecked(w[i] + state[i]);
            BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(i * 4, 4), v);
        }
    }

    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        unchecked
        {
            a += b; d ^= a; d = System.Numerics.BitOperations.RotateLeft(d, 16);
            c += d; b ^= c; b = System.Numerics.BitOperations.RotateLeft(b, 12);
            a += b; d ^= a; d = System.Numerics.BitOperations.RotateLeft(d, 8);
            c += d; b ^= c; b = System.Numerics.BitOperations.RotateLeft(b, 7);
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
