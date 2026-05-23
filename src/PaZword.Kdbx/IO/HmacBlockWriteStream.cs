namespace PaZword.Kdbx.IO;

/// <summary>
/// Write-side of the KDBX 4 HMAC-protected block stream. Buffers up to
/// <see cref="BlockSize"/> bytes, then emits an authenticated block on
/// buffer-full. <see cref="Finish"/> flushes the tail block (if any) and
/// writes a zero-size terminator block.
/// </summary>
internal sealed class HmacBlockWriteStream : Stream
{
    public const int BlockSize = 1024 * 1024; // 1 MiB blocks (KeePassXC default).

    private readonly Stream _sink;
    private readonly byte[] _hmacBaseKey;
    private readonly byte[] _buffer = new byte[BlockSize];
    private int _bufferLen;
    private ulong _blockIndex;
    private bool _finished;
    private readonly bool _leaveOpen;

    public HmacBlockWriteStream(Stream sink, byte[] hmacBaseKey, bool leaveOpen = false)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _hmacBaseKey = hmacBaseKey ?? throw new ArgumentNullException(nameof(hmacBaseKey));
        _leaveOpen = leaveOpen;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush() { }

    public override void Write(byte[] buffer, int offset, int count) =>
        Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (_finished) throw new InvalidOperationException("Block stream already finished.");
        int written = 0;
        while (written < buffer.Length)
        {
            int free = _buffer.Length - _bufferLen;
            int take = Math.Min(free, buffer.Length - written);
            buffer.Slice(written, take).CopyTo(_buffer.AsSpan(_bufferLen, take));
            _bufferLen += take;
            written += take;
            if (_bufferLen == _buffer.Length)
                EmitBlock(_buffer.AsSpan(0, _bufferLen));
        }
    }

    public void Finish()
    {
        if (_finished) return;
        if (_bufferLen > 0)
            EmitBlock(_buffer.AsSpan(0, _bufferLen));
        EmitBlock(ReadOnlySpan<byte>.Empty);
        _finished = true;
    }

    private void EmitBlock(ReadOnlySpan<byte> payload)
    {
        var payloadCopy = payload.IsEmpty ? Array.Empty<byte>() : payload.ToArray();
        byte[] tag = HmacBlockReadStream.ComputeBlockHmac(_blockIndex, _hmacBaseKey, payloadCopy.Length, payloadCopy);

        _sink.Write(tag);
        Endian.WriteInt32(_sink, payloadCopy.Length);
        if (payloadCopy.Length > 0)
            _sink.Write(payloadCopy);

        _bufferLen = 0;
        _blockIndex++;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!_finished) Finish();
            if (!_leaveOpen) _sink.Dispose();
        }
        base.Dispose(disposing);
    }
}
