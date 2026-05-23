using System.Buffers.Binary;

namespace PaZword.Kdbx.IO;

/// <summary>
/// Forward-only little-endian binary primitives over a <see cref="Stream"/>.
/// AOT-clean: no <see cref="System.IO.BinaryReader"/> (which keeps a decoder
/// allocator and can drift on non-Latin character sets).
/// </summary>
internal static class Endian
{
    public static byte ReadByte(Stream s)
    {
        int b = s.ReadByte();
        if (b < 0) throw new KdbxFormatException("Unexpected end of stream while reading byte.");
        return (byte)b;
    }

    public static ushort ReadUInt16(Stream s)
    {
        Span<byte> buf = stackalloc byte[2];
        ReadExact(s, buf);
        return BinaryPrimitives.ReadUInt16LittleEndian(buf);
    }

    public static uint ReadUInt32(Stream s)
    {
        Span<byte> buf = stackalloc byte[4];
        ReadExact(s, buf);
        return BinaryPrimitives.ReadUInt32LittleEndian(buf);
    }

    public static int ReadInt32(Stream s)
    {
        Span<byte> buf = stackalloc byte[4];
        ReadExact(s, buf);
        return BinaryPrimitives.ReadInt32LittleEndian(buf);
    }

    public static ulong ReadUInt64(Stream s)
    {
        Span<byte> buf = stackalloc byte[8];
        ReadExact(s, buf);
        return BinaryPrimitives.ReadUInt64LittleEndian(buf);
    }

    public static long ReadInt64(Stream s)
    {
        Span<byte> buf = stackalloc byte[8];
        ReadExact(s, buf);
        return BinaryPrimitives.ReadInt64LittleEndian(buf);
    }

    public static byte[] ReadBytes(Stream s, int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0) return Array.Empty<byte>();
        var buffer = new byte[count];
        ReadExact(s, buffer);
        return buffer;
    }

    public static void ReadExact(Stream s, Span<byte> buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = s.Read(buffer[total..]);
            if (n <= 0)
                throw new KdbxFormatException("Unexpected end of stream.");
            total += n;
        }
    }

    public static void WriteByte(Stream s, byte v) => s.WriteByte(v);

    public static void WriteUInt16(Stream s, ushort v)
    {
        Span<byte> buf = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buf, v);
        s.Write(buf);
    }

    public static void WriteUInt32(Stream s, uint v)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buf, v);
        s.Write(buf);
    }

    public static void WriteInt32(Stream s, int v)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buf, v);
        s.Write(buf);
    }

    public static void WriteUInt64(Stream s, ulong v)
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buf, v);
        s.Write(buf);
    }

    public static void WriteInt64(Stream s, long v)
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(buf, v);
        s.Write(buf);
    }

    public static void WriteBytes(Stream s, ReadOnlySpan<byte> bytes) => s.Write(bytes);
}
