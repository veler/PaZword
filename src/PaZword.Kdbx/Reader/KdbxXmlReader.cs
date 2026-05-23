using PaZword.Kdbx.Models;
using System.Buffers.Binary;
using System.Globalization;
using System.Xml;

namespace PaZword.Kdbx.Reader;

/// <summary>
/// Forward-only <see cref="XmlReader"/>-based parser for the KDBX 4 XML body.
/// Decrypts <c>&lt;Value Protected="True"&gt;</c> nodes against
/// <paramref name="protectedStream"/> in encounter order.
/// <para>
/// Iteration invariant: every child-handler returns with the cursor positioned
/// on the node <em>after</em> the element it consumed (i.e.,
/// <see cref="XmlReader.ReadElementContentAsString"/>-style). The driver loop
/// does not call <see cref="XmlReader.Read"/> after a dispatch.
/// </para>
/// </summary>
internal sealed class KdbxXmlReader
{
    private readonly IProtectedStream _protectedStream;

    public KdbxXmlReader(IProtectedStream protectedStream)
    {
        _protectedStream = protectedStream;
    }

    public KdbxDocument Read(Stream xmlBody, KdbxHeader header, IList<KdbxBinary> binaryPool)
    {
        var doc = new KdbxDocument { Header = header };
        foreach (var b in binaryPool) doc.Binaries.Add(b);

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreWhitespace = true,
            IgnoreProcessingInstructions = true,
            CloseInput = false,
        };
        using var reader = XmlReader.Create(xmlBody, settings);

        if (!reader.ReadToFollowing("KeePassFile"))
            throw new KdbxFormatException("Missing <KeePassFile> root element.");

        if (reader.IsEmptyElement) return doc;
        int rootDepth = reader.Depth;
        reader.Read();
        while (true)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == rootDepth) break;
            if (reader.NodeType != XmlNodeType.Element) { if (!reader.Read()) break; continue; }
            switch (reader.Name)
            {
                case "Meta": ReadMeta(reader, doc.Meta, doc.CustomIcons); break;
                case "Root": ReadRoot(reader, doc); break;
                default: reader.Skip(); break;
            }
        }
        return doc;
    }

    // ── Meta ────────────────────────────────────────────────────────────────
    private static void ReadMeta(XmlReader r, KdbxMeta meta, IList<KdbxCustomIcon> customIcons)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return; continue; }
            switch (r.Name)
            {
                case "Generator": meta.Generator = r.ReadElementContentAsString(); break;
                case "DatabaseName": meta.DatabaseName = r.ReadElementContentAsString(); break;
                case "DatabaseNameChanged": meta.DatabaseNameChanged = ReadTimestamp(r); break;
                case "DatabaseDescription": meta.DatabaseDescription = r.ReadElementContentAsString(); break;
                case "DatabaseDescriptionChanged": meta.DatabaseDescriptionChanged = ReadTimestamp(r); break;
                case "DefaultUserName": meta.DefaultUserName = r.ReadElementContentAsString(); break;
                case "DefaultUserNameChanged": meta.DefaultUserNameChanged = ReadTimestamp(r); break;
                case "MaintenanceHistoryDays": meta.MaintenanceHistoryDays = ReadInt(r, 365); break;
                case "Color": meta.Color = r.ReadElementContentAsString(); break;
                case "MasterKeyChanged": meta.MasterKeyChanged = ReadTimestamp(r); break;
                case "MasterKeyChangeRec": meta.MasterKeyChangeRec = ReadLong(r, -1); break;
                case "MasterKeyChangeForce": meta.MasterKeyChangeForce = ReadLong(r, -1); break;
                case "MemoryProtection": ReadMemoryProtection(r, meta.MemoryProtection); break;
                case "CustomIcons": ReadCustomIcons(r, customIcons); break;
                case "RecycleBinEnabled": meta.RecycleBinEnabled = ReadBool(r, true); break;
                case "RecycleBinUUID": meta.RecycleBinUuid = ReadGuid(r); break;
                case "RecycleBinChanged": meta.RecycleBinChanged = ReadTimestamp(r); break;
                case "EntryTemplatesGroup": meta.EntryTemplatesGroup = ReadGuid(r); break;
                case "EntryTemplatesGroupChanged": meta.EntryTemplatesGroupChanged = ReadTimestamp(r); break;
                case "HistoryMaxItems": meta.HistoryMaxItems = ReadInt(r, 10); break;
                case "HistoryMaxSize": meta.HistoryMaxSize = ReadLong(r, 6L * 1024 * 1024); break;
                case "LastSelectedGroup": meta.LastSelectedGroup = ReadGuid(r); break;
                case "LastTopVisibleGroup": meta.LastTopVisibleGroup = ReadGuid(r); break;
                default: r.Skip(); break;
            }
        }
    }

    private static void ReadMemoryProtection(XmlReader r, KdbxMemoryProtection mp)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return; continue; }
            switch (r.Name)
            {
                case "ProtectTitle":    mp.ProtectTitle    = ReadBool(r, false); break;
                case "ProtectUserName": mp.ProtectUserName = ReadBool(r, false); break;
                case "ProtectPassword": mp.ProtectPassword = ReadBool(r, true); break;
                case "ProtectURL":      mp.ProtectUrl      = ReadBool(r, false); break;
                case "ProtectNotes":    mp.ProtectNotes    = ReadBool(r, false); break;
                default: r.Skip(); break;
            }
        }
    }

    private static void ReadCustomIcons(XmlReader r, IList<KdbxCustomIcon> icons)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return; continue; }
            if (r.Name == "Icon")
            {
                if (r.IsEmptyElement) { r.Read(); continue; }
                int iconDepth = r.Depth;
                Guid uuid = Guid.Empty;
                byte[] data = Array.Empty<byte>();
                string? name = null;
                DateTime? lastMod = null;
                r.Read();
                while (true)
                {
                    if (r.NodeType == XmlNodeType.EndElement && r.Depth == iconDepth) { r.Read(); break; }
                    if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) break; continue; }
                    switch (r.Name)
                    {
                        case "UUID": uuid = ReadGuid(r); break;
                        case "Data":
                            var b64 = r.ReadElementContentAsString();
                            data = string.IsNullOrEmpty(b64) ? Array.Empty<byte>() : Convert.FromBase64String(b64);
                            break;
                        case "Name": name = r.ReadElementContentAsString(); break;
                        case "LastModificationTime": lastMod = ReadTimestamp(r); break;
                        default: r.Skip(); break;
                    }
                }
                icons.Add(new KdbxCustomIcon { Uuid = uuid, Data = data, Name = name, LastModificationTime = lastMod });
            }
            else
            {
                r.Skip();
            }
        }
    }

    // ── Root → Group → Entry ────────────────────────────────────────────────
    private void ReadRoot(XmlReader r, KdbxDocument doc)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return; continue; }
            if (r.Name == "Group") doc.Root = ReadGroup(r);
            else r.Skip();
        }
    }

    private KdbxGroup ReadGroup(XmlReader r)
    {
        var group = new KdbxGroup();
        if (r.IsEmptyElement) { r.Read(); return group; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return group; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return group; continue; }
            switch (r.Name)
            {
                case "UUID": group.Uuid = ReadGuid(r); break;
                case "Name": group.Name = r.ReadElementContentAsString(); break;
                case "Notes": group.Notes = r.ReadElementContentAsString(); break;
                case "IconID": group.IconId = ReadInt(r, 0); break;
                case "CustomIconUUID": group.CustomIconUuid = ReadGuid(r); break;
                case "Times": ReadGroupTimes(r, group); break;
                case "Group": group.Groups.Add(ReadGroup(r)); break;
                case "Entry": group.Entries.Add(ReadEntry(r)); break;
                default: r.Skip(); break;
            }
        }
    }

    private static void ReadGroupTimes(XmlReader r, KdbxGroup g)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return; continue; }
            switch (r.Name)
            {
                case "CreationTime": g.CreationTime = ReadTimestamp(r); break;
                case "LastAccessTime": g.LastAccessTime = ReadTimestamp(r); break;
                case "LastModificationTime": g.LastModificationTime = ReadTimestamp(r); break;
                case "ExpiryTime": g.ExpiryTime = ReadTimestamp(r); break;
                case "Expires": g.Expires = ReadBool(r, false); break;
                default: r.Skip(); break;
            }
        }
    }

    private KdbxEntry ReadEntry(XmlReader r)
    {
        var entry = new KdbxEntry();
        if (r.IsEmptyElement) { r.Read(); return entry; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return entry; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return entry; continue; }
            switch (r.Name)
            {
                case "UUID": entry.Uuid = ReadGuid(r); break;
                case "IconID": entry.IconId = ReadInt(r, 0); break;
                case "CustomIconUUID": entry.CustomIconUuid = ReadGuid(r); break;
                case "ForegroundColor": entry.ForegroundColor = r.ReadElementContentAsString(); break;
                case "BackgroundColor": entry.BackgroundColor = r.ReadElementContentAsString(); break;
                case "String": ReadEntryString(r, entry); break;
                case "Binary": ReadEntryBinary(r, entry); break;
                case "Times": ReadEntryTimes(r, entry); break;
                case "AutoType": ReadAutoType(r, entry); break;
                case "History": ReadEntryHistory(r, entry); break;
                default: r.Skip(); break;
            }
        }
    }

    private static void ReadAutoType(XmlReader r, KdbxEntry entry)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return; continue; }
            if (r.Name == "DefaultSequence") entry.AutoTypeSequence = r.ReadElementContentAsString();
            else r.Skip();
        }
    }

    private void ReadEntryString(XmlReader r, KdbxEntry entry)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        string? key = null;
        string value = string.Empty;
        bool isProtected = false;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); break; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) break; continue; }
            switch (r.Name)
            {
                case "Key": key = r.ReadElementContentAsString(); break;
                case "Value":
                    isProtected = r.GetAttribute("Protected") == "True";
                    if (r.IsEmptyElement)
                    {
                        value = string.Empty;
                        r.Read();
                    }
                    else
                    {
                        var rawText = r.ReadElementContentAsString();
                        if (isProtected && !string.IsNullOrEmpty(rawText))
                        {
                            var bytes = Convert.FromBase64String(rawText);
                            _protectedStream.Process(bytes);
                            value = System.Text.Encoding.UTF8.GetString(bytes);
                            System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
                        }
                        else value = rawText;
                    }
                    break;
                default: r.Skip(); break;
            }
        }
        if (key is not null)
            entry.Strings[key] = new KdbxProtectedString { Value = value, IsProtected = isProtected };
    }

    private static void ReadEntryBinary(XmlReader r, KdbxEntry entry)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        string? key = null;
        int? refIdx = null;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); break; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) break; continue; }
            switch (r.Name)
            {
                case "Key": key = r.ReadElementContentAsString(); break;
                case "Value":
                    var refAttr = r.GetAttribute("Ref");
                    if (refAttr is not null && int.TryParse(refAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
                        refIdx = idx;
                    if (r.IsEmptyElement) r.Read();
                    else r.Skip();
                    break;
                default: r.Skip(); break;
            }
        }
        if (key is not null && refIdx is not null)
            entry.Binaries[key] = new KdbxBinaryRef { PoolIndex = refIdx.Value };
    }

    private static void ReadEntryTimes(XmlReader r, KdbxEntry e)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return; continue; }
            switch (r.Name)
            {
                case "CreationTime": e.CreationTime = ReadTimestamp(r); break;
                case "LastAccessTime": e.LastAccessTime = ReadTimestamp(r); break;
                case "LastModificationTime": e.LastModificationTime = ReadTimestamp(r); break;
                case "ExpiryTime": e.ExpiryTime = ReadTimestamp(r); break;
                case "Expires": e.Expires = ReadBool(r, false); break;
                case "UsageCount": e.UsageCount = ReadInt(r, 0); break;
                case "LocationChanged": _ = ReadTimestamp(r); break;
                default: r.Skip(); break;
            }
        }
    }

    private void ReadEntryHistory(XmlReader r, KdbxEntry parent)
    {
        if (r.IsEmptyElement) { r.Read(); return; }
        int depth = r.Depth;
        r.Read();
        while (true)
        {
            if (r.NodeType == XmlNodeType.EndElement && r.Depth == depth) { r.Read(); return; }
            if (r.NodeType != XmlNodeType.Element) { if (!r.Read()) return; continue; }
            if (r.Name == "Entry") parent.History.Add(new KdbxEntryHistory { Entry = ReadEntry(r) });
            else r.Skip();
        }
    }

    // ── Primitives (each consumes the element and advances past the EndElement) ─────
    private static Guid ReadGuid(XmlReader r)
    {
        var s = r.ReadElementContentAsString();
        if (string.IsNullOrEmpty(s)) return Guid.Empty;
        return new Guid(Convert.FromBase64String(s));
    }

    private static int ReadInt(XmlReader r, int defaultValue)
    {
        var s = r.ReadElementContentAsString();
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
    }

    private static long ReadLong(XmlReader r, long defaultValue)
    {
        var s = r.ReadElementContentAsString();
        return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
    }

    private static bool ReadBool(XmlReader r, bool defaultValue)
    {
        var s = r.ReadElementContentAsString();
        if (string.IsNullOrEmpty(s)) return defaultValue;
        return s.Equals("True", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// KDBX 4 timestamp: base64-encoded little-endian uint64 = seconds since
    /// 0001-01-01T00:00:00Z. Falls back to ISO-8601 parsing for KDBX 3.1 carry-over.
    /// </summary>
    private static DateTime? ReadTimestamp(XmlReader r)
    {
        var s = r.ReadElementContentAsString();
        if (string.IsNullOrEmpty(s)) return null;
        if (TryParseBase64Timestamp(s, out var dt)) return dt;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var iso))
            return DateTime.SpecifyKind(iso, DateTimeKind.Utc);
        return null;
    }

    private static bool TryParseBase64Timestamp(string s, out DateTime result)
    {
        result = default;
        try
        {
            var bytes = Convert.FromBase64String(s);
            if (bytes.Length < 8) return false;
            ulong secs = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
            if (secs > (ulong)((DateTime.MaxValue - DateTime.MinValue).TotalSeconds))
                return false;
            result = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(secs);
            return true;
        }
        catch (FormatException) { return false; }
    }
}
