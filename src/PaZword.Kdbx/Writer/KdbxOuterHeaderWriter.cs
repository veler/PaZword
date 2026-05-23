using PaZword.Kdbx.IO;
using PaZword.Kdbx.Models;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace PaZword.Kdbx.Writer;

/// <summary>
/// Emits the KDBX 4 outer header (signature, version, TLV fields, header
/// SHA-256, header HMAC) to a destination stream.
/// </summary>
internal static class KdbxOuterHeaderWriter
{
    /// <summary>
    /// Serialises the outer header to a fresh byte array (signature through
    /// the EndOfHeader TLV, <em>not</em> including the header SHA-256 /
    /// header HMAC tags).
    /// </summary>
    public static byte[] BuildHeaderBytes(KdbxHeader header)
    {
        using var ms = new MemoryStream();
        Endian.WriteUInt32(ms, Reader.KdbxOuterHeaderReader.Signature1);
        Endian.WriteUInt32(ms, Reader.KdbxOuterHeaderReader.Signature2);
        Endian.WriteUInt16(ms, (ushort)header.MinorVersion);
        Endian.WriteUInt16(ms, (ushort)header.MajorVersion);

        WriteField(ms, Reader.KdbxOuterHeaderReader.FieldCipherId, header.CipherId!);

        Span<byte> compress = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(compress, header.CompressionFlags);
        WriteField(ms, Reader.KdbxOuterHeaderReader.FieldCompressionFlags, compress.ToArray());

        WriteField(ms, Reader.KdbxOuterHeaderReader.FieldMasterSeed, header.MasterSeed!);
        WriteField(ms, Reader.KdbxOuterHeaderReader.FieldEncryptionIv, header.EncryptionIV!);
        WriteField(ms, Reader.KdbxOuterHeaderReader.FieldKdfParameters, EncodeKdfParameters(header.KdfParameters!));

        if (header.PublicCustomData is { Length: > 0 })
            WriteField(ms, Reader.KdbxOuterHeaderReader.FieldPublicCustomData, header.PublicCustomData);

        // EndOfHeader. Spec uses "\r\n\r\n" as the value for compatibility.
        WriteField(ms, Reader.KdbxOuterHeaderReader.FieldEnd, new byte[] { 0x0D, 0x0A, 0x0D, 0x0A });
        return ms.ToArray();
    }

    public static void WriteHeaderWithIntegrity(Stream sink, byte[] headerBytes, byte[] headerHmacKey)
    {
        sink.Write(headerBytes);
        var sha = SHA256.HashData(headerBytes);
        sink.Write(sha);
        byte[] hmac;
        using (var h = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, headerHmacKey))
        {
            h.AppendData(headerBytes);
            hmac = h.GetHashAndReset();
        }
        sink.Write(hmac);
    }

    private static void WriteField(Stream s, byte id, byte[] value)
    {
        Endian.WriteByte(s, id);
        Endian.WriteUInt32(s, (uint)value.Length);
        if (value.Length > 0) s.Write(value);
    }

    public static byte[] EncodeKdfParameters(KdbxKdfParameters p)
    {
        var map = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["$UUID"] = p.Uuid.ToByteArray(),
        };
        if (p.IsArgon2)
        {
            if (p.Salt is null) throw new KdbxFormatException("Argon2 KDF requires a salt.");
            map["S"] = p.Salt;
            map["P"] = p.Parallelism;
            map["M"] = p.MemoryKib;
            map["I"] = p.Iterations;
            map["V"] = p.Version == 0 ? 0x13u : p.Version;
            map["$UUID"] = p.Uuid.ToByteArray();
        }
        else if (p.IsAesKdf)
        {
            if (p.AesSeed is null) throw new KdbxFormatException("AES-KDF requires a seed.");
            map["S"] = p.AesSeed;
            map["R"] = p.AesRounds;
        }
        else
        {
            throw new KdbxFormatException($"Cannot encode KDF UUID {p.Uuid}.");
        }
        return VariantMap.Encode(map);
    }
}
