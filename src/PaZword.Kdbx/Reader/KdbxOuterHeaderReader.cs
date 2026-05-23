using PaZword.Kdbx.IO;
using PaZword.Kdbx.Models;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace PaZword.Kdbx.Reader;

/// <summary>
/// Parses the KDBX 4 outer header: signature, version, TLV fields, header
/// SHA-256, header HMAC-SHA-256. Returns the parsed header and the raw header
/// bytes (needed for HMAC verification after the composite key is known).
/// </summary>
internal static class KdbxOuterHeaderReader
{
    public const uint Signature1 = 0x9AA2D903;
    public const uint Signature2 = 0xB54BFB67;

    public const byte FieldEnd               = 0;
    public const byte FieldCipherId          = 2;
    public const byte FieldCompressionFlags  = 3;
    public const byte FieldMasterSeed        = 4;
    public const byte FieldEncryptionIv      = 7;
    public const byte FieldKdfParameters     = 11;
    public const byte FieldPublicCustomData  = 12;

    public sealed class ParseResult
    {
        public required KdbxHeader Header { get; init; }
        public required byte[] RawHeaderBytes { get; init; }
        public required byte[] StoredHeaderSha256 { get; init; }
        public required byte[] StoredHeaderHmac { get; init; }
    }

    public static ParseResult Read(Stream source)
    {
        // Buffer the header into memory so we can hash it for integrity verification.
        using var headerBuffer = new MemoryStream();
        var tee = new TeeStream(source, headerBuffer);

        uint sig1 = Endian.ReadUInt32(tee);
        uint sig2 = Endian.ReadUInt32(tee);
        if (sig1 != Signature1 || sig2 != Signature2)
            throw new KdbxFormatException($"Not a KDBX file (signature 0x{sig1:X8}{sig2:X8}).");

        uint minor = Endian.ReadUInt16(tee);
        uint major = Endian.ReadUInt16(tee);
        if (major != 4)
            throw new KdbxUnsupportedVersionException(major, minor);

        byte[]? cipherId = null;
        int compressionFlags = 0;
        byte[]? masterSeed = null;
        byte[]? encryptionIv = null;
        KdbxKdfParameters? kdfParams = null;
        byte[]? publicCustomData = null;

        while (true)
        {
            byte fieldId = Endian.ReadByte(tee);
            uint fieldSize = Endian.ReadUInt32(tee);
            if (fieldSize > 1024 * 1024)
                throw new KdbxFormatException($"Outer header field {fieldId} too large: {fieldSize} bytes.");
            byte[] value = Endian.ReadBytes(tee, checked((int)fieldSize));

            switch (fieldId)
            {
                case FieldEnd:
                    break;
                case FieldCipherId:
                    if (value.Length != 16) throw new KdbxFormatException("CipherID must be 16 bytes.");
                    cipherId = value; break;
                case FieldCompressionFlags:
                    if (value.Length != 4) throw new KdbxFormatException("CompressionFlags must be 4 bytes.");
                    compressionFlags = BinaryPrimitives.ReadInt32LittleEndian(value); break;
                case FieldMasterSeed:
                    if (value.Length != 32) throw new KdbxFormatException("MasterSeed must be 32 bytes.");
                    masterSeed = value; break;
                case FieldEncryptionIv:
                    encryptionIv = value; break;
                case FieldKdfParameters:
                    kdfParams = DecodeKdfParameters(value); break;
                case FieldPublicCustomData:
                    publicCustomData = value; break;
                default:
                    // Unknown fields are ignored per the spec (forward-compat).
                    break;
            }

            if (fieldId == FieldEnd) break;
        }

        // After EndOfHeader, the stream contains:
        //   [32 bytes] SHA-256 of header
        //   [32 bytes] HMAC-SHA-256 of header
        // These two are NOT included in the header bytes used for hashing.
        byte[] storedSha = Endian.ReadBytes(source, 32);
        byte[] storedHmac = Endian.ReadBytes(source, 32);

        byte[] rawHeader = headerBuffer.ToArray();

        if (cipherId is null) throw new KdbxFormatException("Missing CipherID in outer header.");
        if (masterSeed is null) throw new KdbxFormatException("Missing MasterSeed in outer header.");
        if (encryptionIv is null) throw new KdbxFormatException("Missing EncryptionIV in outer header.");
        if (kdfParams is null) throw new KdbxFormatException("Missing KdfParameters in outer header.");

        var header = new KdbxHeader
        {
            MajorVersion = major,
            MinorVersion = minor,
            CipherId = cipherId,
            CompressionFlags = compressionFlags,
            MasterSeed = masterSeed,
            EncryptionIV = encryptionIv,
            KdfParameters = kdfParams,
            PublicCustomData = publicCustomData,
        };

        return new ParseResult
        {
            Header = header,
            RawHeaderBytes = rawHeader,
            StoredHeaderSha256 = storedSha,
            StoredHeaderHmac = storedHmac,
        };
    }

    public static KdbxKdfParameters DecodeKdfParameters(byte[] rawVariantMap)
    {
        var map = VariantMap.Decode(rawVariantMap);
        if (!map.TryGetValue("$UUID", out var uuidObj) || uuidObj is not byte[] uuidBytes || uuidBytes.Length != 16)
            throw new KdbxFormatException("KDF VariantMap missing or invalid $UUID.");
        var uuid = new Guid(uuidBytes);

        if (uuid == KdbxKdfUuids.Argon2id || uuid == KdbxKdfUuids.Argon2d)
        {
            return new KdbxKdfParameters
            {
                Uuid = uuid,
                Salt = map.TryGetValue("S", out var s) ? (byte[])s : null,
                Parallelism = map.TryGetValue("P", out var p) ? (uint)p : 0,
                MemoryKib = map.TryGetValue("M", out var m) ? (ulong)m : 0,
                Iterations = map.TryGetValue("I", out var i) ? (ulong)i : 0,
                Version = map.TryGetValue("V", out var v) ? (uint)v : 0x13,
            };
        }
        if (uuid == KdbxKdfUuids.AesKdf)
        {
            return new KdbxKdfParameters
            {
                Uuid = uuid,
                AesSeed = map.TryGetValue("S", out var s) ? (byte[])s : null,
                AesRounds = map.TryGetValue("R", out var r) ? (ulong)r : 0,
            };
        }
        throw new KdbxFormatException($"Unsupported KDF UUID {uuid}.");
    }

    /// <summary>Read-side tee — every byte read from <c>source</c> is also written to <c>sink</c>.</summary>
    private sealed class TeeStream : Stream
    {
        private readonly Stream _src; private readonly Stream _sink;
        public TeeStream(Stream src, Stream sink) { _src = src; _sink = sink; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = _src.Read(buffer, offset, count);
            if (n > 0) _sink.Write(buffer, offset, n);
            return n;
        }
        public override int Read(Span<byte> buffer)
        {
            int n = _src.Read(buffer);
            if (n > 0) _sink.Write(buffer[..n]);
            return n;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
