using System.Text;

namespace PaZword.Kdbx.IO;

/// <summary>
/// VariantMap encoder/decoder per the KeePass KDBX 4 spec
/// (<c>docs/sprint-0/kdbx4-spec-notes.md §4</c>).
///
/// <para>
/// A VariantMap is a 2-byte version header + sequence of typed key/value
/// entries terminated by a zero-type byte. AOT-clean: each value type is
/// decoded by a fixed branch — no reflection on the boxed value.
/// </para>
/// </summary>
internal static class VariantMap
{
    /// <summary>VariantMap format version (we accept &lt;= 0x0100 critical, write 0x0100).</summary>
    public const ushort Version = 0x0100;
    public const ushort VersionMask = 0xFF00;

    public const byte TypeNone   = 0x00;
    public const byte TypeUInt32 = 0x04;
    public const byte TypeUInt64 = 0x05;
    public const byte TypeBool   = 0x08;
    public const byte TypeInt32  = 0x0C;
    public const byte TypeInt64  = 0x0D;
    public const byte TypeString = 0x18;
    public const byte TypeBytes  = 0x42;

    /// <summary>
    /// Decodes a VariantMap. Keys are case-sensitive (Ordinal). Values are boxed
    /// primitives, <c>byte[]</c>, or <c>string</c>; callers cast.
    /// </summary>
    public static Dictionary<string, object> Decode(ReadOnlySpan<byte> source)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        if (source.Length < 2)
            throw new KdbxFormatException("VariantMap too short.");
        ushort version = (ushort)(source[0] | (source[1] << 8));
        if ((version & VersionMask) > (Version & VersionMask))
            throw new KdbxFormatException($"VariantMap critical version 0x{version:X4} is not supported.");
        int pos = 2;

        while (true)
        {
            if (pos >= source.Length)
                throw new KdbxFormatException("VariantMap truncated (missing terminator).");
            byte type = source[pos++];
            if (type == TypeNone)
                return result;

            if (pos + 4 > source.Length)
                throw new KdbxFormatException("VariantMap truncated reading key length.");
            int keyLen = source[pos] | (source[pos + 1] << 8) | (source[pos + 2] << 16) | (source[pos + 3] << 24);
            pos += 4;
            if (keyLen < 0 || pos + keyLen > source.Length)
                throw new KdbxFormatException("VariantMap key length is invalid.");
            string key = Encoding.UTF8.GetString(source.Slice(pos, keyLen));
            pos += keyLen;

            if (pos + 4 > source.Length)
                throw new KdbxFormatException("VariantMap truncated reading value length.");
            int valLen = source[pos] | (source[pos + 1] << 8) | (source[pos + 2] << 16) | (source[pos + 3] << 24);
            pos += 4;
            if (valLen < 0 || pos + valLen > source.Length)
                throw new KdbxFormatException("VariantMap value length is invalid.");
            var valBytes = source.Slice(pos, valLen);
            pos += valLen;

            object value = type switch
            {
                TypeUInt32 => ReadFixedUInt32(valBytes),
                TypeUInt64 => ReadFixedUInt64(valBytes),
                TypeBool   => ReadFixedBool(valBytes),
                TypeInt32  => ReadFixedInt32(valBytes),
                TypeInt64  => ReadFixedInt64(valBytes),
                TypeString => Encoding.UTF8.GetString(valBytes),
                TypeBytes  => valBytes.ToArray(),
                _ => throw new KdbxFormatException($"VariantMap type 0x{type:X2} is not supported."),
            };
            result[key] = value;
        }
    }

    private static uint  ReadFixedUInt32(ReadOnlySpan<byte> s) { RequireLen(s.Length, 4, "uint32"); return System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(s); }
    private static ulong ReadFixedUInt64(ReadOnlySpan<byte> s) { RequireLen(s.Length, 8, "uint64"); return System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(s); }
    private static int   ReadFixedInt32 (ReadOnlySpan<byte> s) { RequireLen(s.Length, 4, "int32");  return System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(s); }
    private static long  ReadFixedInt64 (ReadOnlySpan<byte> s) { RequireLen(s.Length, 8, "int64");  return System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(s); }
    private static bool  ReadFixedBool  (ReadOnlySpan<byte> s) { RequireLen(s.Length, 1, "bool");   return s[0] != 0; }

    private static void RequireLen(int actual, int expected, string label)
    {
        if (actual != expected)
            throw new KdbxFormatException($"VariantMap value '{label}' has wrong length ({actual}, expected {expected}).");
    }

    /// <summary>Encodes the VariantMap to a fresh byte array.</summary>
    public static byte[] Encode(IReadOnlyDictionary<string, object> map)
    {
        using var ms = new MemoryStream();
        Endian.WriteUInt16(ms, Version);
        foreach (var kv in map)
        {
            byte type;
            byte[] valueBytes;
            switch (kv.Value)
            {
                case uint u:    type = TypeUInt32; valueBytes = new byte[4]; System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(valueBytes, u); break;
                case ulong ul:  type = TypeUInt64; valueBytes = new byte[8]; System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(valueBytes, ul); break;
                case bool b:    type = TypeBool;   valueBytes = new[] { b ? (byte)1 : (byte)0 }; break;
                case int i:     type = TypeInt32;  valueBytes = new byte[4]; System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(valueBytes, i); break;
                case long l:    type = TypeInt64;  valueBytes = new byte[8]; System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(valueBytes, l); break;
                case string s:  type = TypeString; valueBytes = Encoding.UTF8.GetBytes(s); break;
                case byte[] bs: type = TypeBytes;  valueBytes = bs; break;
                default:
                    throw new ArgumentException(
                        $"VariantMap value for key '{kv.Key}' has unsupported type {kv.Value?.GetType()}.");
            }
            Endian.WriteByte(ms, type);
            byte[] keyBytes = Encoding.UTF8.GetBytes(kv.Key);
            Endian.WriteInt32(ms, keyBytes.Length);
            ms.Write(keyBytes);
            Endian.WriteInt32(ms, valueBytes.Length);
            ms.Write(valueBytes);
        }
        Endian.WriteByte(ms, TypeNone);
        return ms.ToArray();
    }
}
