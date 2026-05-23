using PaZword.Kdbx.IO;
using PaZword.Kdbx.Models;
using PaZword.Kdbx.Reader; // for field-ID constants

namespace PaZword.Kdbx.Writer;

/// <summary>Emits the KDBX 4 inner header to the payload stream.</summary>
internal static class KdbxInnerHeaderWriter
{
    public static void Write(Stream payload, KdbxInnerHeaderReader.StreamCipherId innerStreamId, byte[] innerStreamKey, IList<KdbxBinary> binaries)
    {
        // InnerRandomStreamID (uint32)
        WriteField(payload, KdbxInnerHeaderReader.FieldInnerRandomStreamId, BitConverter.GetBytes((uint)innerStreamId));
        // InnerRandomStreamKey
        WriteField(payload, KdbxInnerHeaderReader.FieldInnerRandomStreamKey, innerStreamKey);
        // Binaries
        foreach (var b in binaries)
        {
            var buf = new byte[b.Data.Length + 1];
            buf[0] = b.IsProtected ? (byte)0x01 : (byte)0x00;
            Buffer.BlockCopy(b.Data, 0, buf, 1, b.Data.Length);
            WriteField(payload, KdbxInnerHeaderReader.FieldBinary, buf);
        }
        // EndOfHeader
        WriteField(payload, KdbxInnerHeaderReader.FieldEnd, Array.Empty<byte>());
    }

    private static void WriteField(Stream s, byte id, byte[] value)
    {
        Endian.WriteByte(s, id);
        Endian.WriteUInt32(s, (uint)value.Length);
        if (value.Length > 0) s.Write(value);
    }
}
