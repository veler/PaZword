using PaZword.Kdbx.IO;
using PaZword.Kdbx.Models;
using System.Security.Cryptography;

namespace PaZword.Kdbx.Reader;

/// <summary>
/// Parses the KDBX 4 inner header (lives at the start of the decrypted +
/// decompressed payload). Yields the inner random stream parameters and the
/// binary attachment pool, then leaves <paramref name="stream"/> positioned at
/// the start of the XML body.
/// </summary>
internal static class KdbxInnerHeaderReader
{
    public const byte FieldEnd                  = 0;
    public const byte FieldInnerRandomStreamId  = 1;
    public const byte FieldInnerRandomStreamKey = 2;
    public const byte FieldBinary               = 3;

    public enum StreamCipherId
    {
        None = 0,
        ArcFourVariant = 1, // Legacy, never used.
        Salsa20 = 2,
        ChaCha20 = 3,
    }

    public sealed class ParseResult
    {
        public StreamCipherId InnerStreamId { get; set; }
        public byte[] InnerStreamKey { get; set; } = Array.Empty<byte>();
        public List<KdbxBinary> Binaries { get; } = new();
    }

    public static ParseResult Read(Stream stream)
    {
        var result = new ParseResult();

        while (true)
        {
            byte fieldId = Endian.ReadByte(stream);
            uint fieldSize = Endian.ReadUInt32(stream);
            if (fieldId == FieldEnd)
            {
                _ = Endian.ReadBytes(stream, checked((int)fieldSize)); // drain (usually empty)
                break;
            }
            if (fieldSize > int.MaxValue)
                throw new KdbxFormatException($"Inner header field {fieldId} too large.");

            switch (fieldId)
            {
                case FieldInnerRandomStreamId:
                    if (fieldSize != 4) throw new KdbxFormatException("InnerRandomStreamID must be 4 bytes.");
                    var idBytes = Endian.ReadBytes(stream, 4);
                    result.InnerStreamId = (StreamCipherId)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(idBytes);
                    break;
                case FieldInnerRandomStreamKey:
                    result.InnerStreamKey = Endian.ReadBytes(stream, (int)fieldSize);
                    break;
                case FieldBinary:
                    if (fieldSize < 1) throw new KdbxFormatException("Binary entry has no flags byte.");
                    byte flags = Endian.ReadByte(stream);
                    var data = Endian.ReadBytes(stream, (int)fieldSize - 1);
                    result.Binaries.Add(new KdbxBinary
                    {
                        PoolIndex = result.Binaries.Count,
                        IsProtected = (flags & 0x01) != 0,
                        Data = data,
                    });
                    break;
                default:
                    _ = Endian.ReadBytes(stream, (int)fieldSize); // skip unknown
                    break;
            }
        }
        return result;
    }
}
