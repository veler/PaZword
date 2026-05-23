using System.Buffers.Binary;
using System.Security.Cryptography;

namespace PaZword.Kdbx.IO;

/// <summary>
/// Read-side of the KDBX 4 HMAC-protected block stream
/// (<c>docs/sprint-0/kdbx4-spec-notes.md §7</c>).
///
/// <para>Each block is laid out as:</para>
/// <code>
///   [HMAC-SHA256 tag, 32 bytes]
///   [block size int32 LE]
///   [block data, size bytes]
/// </code>
///
/// <para>
/// A block of size 0 marks end-of-stream. Each block is authenticated with the
/// per-block key <c>SHA-512(block-index uint64 LE || HMAC base key)</c> over
/// the message <c>(block-index LE || block-size LE || block-data)</c>.
/// </para>
///
/// <para>
/// On HMAC mismatch the stream is permanently poisoned and a
/// <see cref="KdbxIntegrityException"/> is thrown. <strong>No partial data is
/// exposed to the caller</strong> — the integrity check runs before any byte
/// of the block is returned via <see cref="Read(System.Span{byte})"/>.
/// </para>
/// </summary>
internal sealed class HmacBlockReadStream : Stream
{
    private readonly Stream _source;
    private readonly byte[] _hmacBaseKey;
    private byte[]? _currentBlock;
    private int _bufferPos;
    private ulong _blockIndex;
    private bool _ended;
    private bool _poisoned;
    private readonly bool _leaveOpen;

    public HmacBlockReadStream(Stream source, byte[] hmacBaseKey, bool leaveOpen = false)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _hmacBaseKey = hmacBaseKey ?? throw new ArgumentNullException(nameof(hmacBaseKey));
        _leaveOpen = leaveOpen;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        if (_poisoned)
            throw new KdbxIntegrityException("HMAC block stream is poisoned.");
        if (_ended) return 0;

        int produced = 0;
        while (produced < buffer.Length)
        {
            if (_currentBlock is null || _bufferPos >= _currentBlock.Length)
            {
                if (!LoadNextBlock()) break;
            }
            int available = _currentBlock!.Length - _bufferPos;
            int take = Math.Min(available, buffer.Length - produced);
            _currentBlock.AsSpan(_bufferPos, take).CopyTo(buffer.Slice(produced, take));
            _bufferPos += take;
            produced += take;
        }
        return produced;
    }

    private bool LoadNextBlock()
    {
        Span<byte> tag = stackalloc byte[32];
        try
        {
            Endian.ReadExact(_source, tag);
        }
        catch (KdbxFormatException)
        {
            _poisoned = true;
            throw;
        }
        int size = Endian.ReadInt32(_source);
        if (size < 0)
        {
            _poisoned = true;
            throw new KdbxFormatException("Negative HMAC block size.");
        }
        var payload = size == 0 ? Array.Empty<byte>() : Endian.ReadBytes(_source, size);

        byte[] expectedTag = ComputeBlockHmac(_blockIndex, _hmacBaseKey, size, payload);

        if (!CryptographicOperations.FixedTimeEquals(expectedTag, tag))
        {
            _poisoned = true;
            _currentBlock = null;
            throw new KdbxIntegrityException(
                $"HMAC mismatch on block {_blockIndex}. Vault is corrupt or tampered.");
        }

        _blockIndex++;
        if (size == 0)
        {
            _ended = true;
            _currentBlock = null;
            return false;
        }
        _currentBlock = payload;
        _bufferPos = 0;
        return true;
    }

    internal static byte[] ComputeBlockHmac(ulong blockIndex, byte[] baseKey, int size, byte[] payload)
    {
        Span<byte> indexLe = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(indexLe, blockIndex);

        byte[] blockKey;
        using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA512))
        {
            sha.AppendData(indexLe);
            sha.AppendData(baseKey);
            blockKey = sha.GetHashAndReset();
        }

        byte[] tag;
        try
        {
            using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, blockKey);
            hmac.AppendData(indexLe);
            Span<byte> sizeLe = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(sizeLe, size);
            hmac.AppendData(sizeLe);
            hmac.AppendData(payload);
            tag = hmac.GetHashAndReset();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(blockKey);
        }
        return tag;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen) _source.Dispose();
        base.Dispose(disposing);
    }
}
