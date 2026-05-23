using PaZword.Kdbx.Models;
using PaZword.Kdbx.Reader;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml;

namespace PaZword.Kdbx.Writer;

/// <summary>
/// Emits the KDBX 4 XML body via <see cref="XmlWriter"/>. Protected string
/// values are re-encrypted against the supplied <see cref="IProtectedStream"/>
/// in the same encounter order the reader expects.
/// </summary>
internal sealed class KdbxXmlWriter
{
    private readonly IProtectedStream _protectedStream;

    public KdbxXmlWriter(IProtectedStream protectedStream)
    {
        _protectedStream = protectedStream;
    }

    public void Write(Stream output, KdbxDocument doc)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            IndentChars = "\t",
            NewLineHandling = NewLineHandling.Replace,
            NewLineChars = "\n",
            CloseOutput = false,
            OmitXmlDeclaration = false,
        };
        using var w = XmlWriter.Create(output, settings);
        w.WriteStartDocument(standalone: false);
        w.WriteStartElement("KeePassFile");
        WriteMeta(w, doc.Meta, doc.CustomIcons);
        WriteRoot(w, doc.Root);
        w.WriteEndElement();
        w.WriteEndDocument();
        w.Flush();
    }

    private static void WriteMeta(XmlWriter w, KdbxMeta m, IList<KdbxCustomIcon> icons)
    {
        w.WriteStartElement("Meta");
        WriteString(w, "Generator", m.Generator);
        WriteString(w, "DatabaseName", m.DatabaseName ?? string.Empty);
        WriteTimestamp(w, "DatabaseNameChanged", m.DatabaseNameChanged);
        WriteString(w, "DatabaseDescription", m.DatabaseDescription ?? string.Empty);
        WriteTimestamp(w, "DatabaseDescriptionChanged", m.DatabaseDescriptionChanged);
        WriteString(w, "DefaultUserName", m.DefaultUserName ?? string.Empty);
        WriteTimestamp(w, "DefaultUserNameChanged", m.DefaultUserNameChanged);
        WriteInt(w, "MaintenanceHistoryDays", m.MaintenanceHistoryDays);
        WriteString(w, "Color", m.Color);
        WriteTimestamp(w, "MasterKeyChanged", m.MasterKeyChanged);
        WriteLong(w, "MasterKeyChangeRec", m.MasterKeyChangeRec);
        WriteLong(w, "MasterKeyChangeForce", m.MasterKeyChangeForce);
        // Memory protection
        w.WriteStartElement("MemoryProtection");
        WriteBool(w, "ProtectTitle",    m.MemoryProtection.ProtectTitle);
        WriteBool(w, "ProtectUserName", m.MemoryProtection.ProtectUserName);
        WriteBool(w, "ProtectPassword", m.MemoryProtection.ProtectPassword);
        WriteBool(w, "ProtectURL",      m.MemoryProtection.ProtectUrl);
        WriteBool(w, "ProtectNotes",    m.MemoryProtection.ProtectNotes);
        w.WriteEndElement();
        // Custom icons
        w.WriteStartElement("CustomIcons");
        foreach (var ic in icons)
        {
            w.WriteStartElement("Icon");
            WriteString(w, "UUID", Convert.ToBase64String(ic.Uuid.ToByteArray()));
            WriteString(w, "Data", Convert.ToBase64String(ic.Data));
            if (!string.IsNullOrEmpty(ic.Name)) WriteString(w, "Name", ic.Name);
            if (ic.LastModificationTime is not null) WriteTimestamp(w, "LastModificationTime", ic.LastModificationTime);
            w.WriteEndElement();
        }
        w.WriteEndElement();
        WriteBool(w, "RecycleBinEnabled", m.RecycleBinEnabled);
        WriteString(w, "RecycleBinUUID", Convert.ToBase64String(m.RecycleBinUuid.ToByteArray()));
        WriteTimestamp(w, "RecycleBinChanged", m.RecycleBinChanged);
        WriteString(w, "EntryTemplatesGroup", Convert.ToBase64String(m.EntryTemplatesGroup.ToByteArray()));
        WriteTimestamp(w, "EntryTemplatesGroupChanged", m.EntryTemplatesGroupChanged);
        WriteInt(w, "HistoryMaxItems", m.HistoryMaxItems);
        WriteLong(w, "HistoryMaxSize", m.HistoryMaxSize);
        WriteString(w, "LastSelectedGroup", Convert.ToBase64String(m.LastSelectedGroup.ToByteArray()));
        WriteString(w, "LastTopVisibleGroup", Convert.ToBase64String(m.LastTopVisibleGroup.ToByteArray()));
        w.WriteEndElement();
    }

    private void WriteRoot(XmlWriter w, KdbxGroup? root)
    {
        w.WriteStartElement("Root");
        if (root is not null) WriteGroup(w, root);
        w.WriteEndElement();
    }

    private void WriteGroup(XmlWriter w, KdbxGroup g)
    {
        w.WriteStartElement("Group");
        WriteString(w, "UUID", Convert.ToBase64String(g.Uuid.ToByteArray()));
        WriteString(w, "Name", g.Name);
        if (!string.IsNullOrEmpty(g.Notes)) WriteString(w, "Notes", g.Notes);
        WriteInt(w, "IconID", g.IconId);
        if (g.CustomIconUuid is { } cuuid && cuuid != Guid.Empty)
            WriteString(w, "CustomIconUUID", Convert.ToBase64String(cuuid.ToByteArray()));
        // Times
        w.WriteStartElement("Times");
        WriteTimestamp(w, "CreationTime", g.CreationTime);
        WriteTimestamp(w, "LastAccessTime", g.LastAccessTime);
        WriteTimestamp(w, "LastModificationTime", g.LastModificationTime);
        WriteTimestamp(w, "ExpiryTime", g.ExpiryTime);
        WriteBool(w, "Expires", g.Expires);
        w.WriteEndElement();
        foreach (var sub in g.Groups) WriteGroup(w, sub);
        foreach (var e in g.Entries) WriteEntry(w, e);
        w.WriteEndElement();
    }

    private void WriteEntry(XmlWriter w, KdbxEntry e)
    {
        w.WriteStartElement("Entry");
        WriteString(w, "UUID", Convert.ToBase64String(e.Uuid.ToByteArray()));
        WriteInt(w, "IconID", e.IconId);
        if (e.CustomIconUuid is { } cuuid && cuuid != Guid.Empty)
            WriteString(w, "CustomIconUUID", Convert.ToBase64String(cuuid.ToByteArray()));
        if (!string.IsNullOrEmpty(e.ForegroundColor)) WriteString(w, "ForegroundColor", e.ForegroundColor);
        if (!string.IsNullOrEmpty(e.BackgroundColor)) WriteString(w, "BackgroundColor", e.BackgroundColor);
        foreach (var kv in e.Strings)
        {
            w.WriteStartElement("String");
            WriteString(w, "Key", kv.Key);
            w.WriteStartElement("Value");
            if (kv.Value.IsProtected)
            {
                w.WriteAttributeString("Protected", "True");
                var plain = System.Text.Encoding.UTF8.GetBytes(kv.Value.Value);
                _protectedStream.Process(plain);
                if (plain.Length > 0) w.WriteString(Convert.ToBase64String(plain));
                CryptographicOperations.ZeroMemory(plain);
            }
            else
            {
                w.WriteString(kv.Value.Value);
            }
            w.WriteEndElement();
            w.WriteEndElement();
        }
        foreach (var kv in e.Binaries)
        {
            w.WriteStartElement("Binary");
            WriteString(w, "Key", kv.Key);
            w.WriteStartElement("Value");
            w.WriteAttributeString("Ref", kv.Value.PoolIndex.ToString(CultureInfo.InvariantCulture));
            w.WriteEndElement();
            w.WriteEndElement();
        }
        // Times
        w.WriteStartElement("Times");
        WriteTimestamp(w, "CreationTime", e.CreationTime);
        WriteTimestamp(w, "LastAccessTime", e.LastAccessTime);
        WriteTimestamp(w, "LastModificationTime", e.LastModificationTime);
        WriteTimestamp(w, "ExpiryTime", e.ExpiryTime);
        WriteBool(w, "Expires", e.Expires);
        WriteInt(w, "UsageCount", e.UsageCount);
        w.WriteEndElement();
        if (!string.IsNullOrEmpty(e.AutoTypeSequence))
        {
            w.WriteStartElement("AutoType");
            WriteString(w, "DefaultSequence", e.AutoTypeSequence);
            w.WriteEndElement();
        }
        if (e.History.Count > 0)
        {
            w.WriteStartElement("History");
            foreach (var h in e.History)
                if (h.Entry is not null) WriteEntry(w, h.Entry);
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    // ── Primitives ──────────────────────────────────────────────────────────
    private static void WriteString(XmlWriter w, string name, string value)
    {
        w.WriteStartElement(name);
        if (!string.IsNullOrEmpty(value)) w.WriteString(value);
        w.WriteEndElement();
    }

    private static void WriteInt(XmlWriter w, string name, int value)
        => w.WriteElementString(name, value.ToString(CultureInfo.InvariantCulture));

    private static void WriteLong(XmlWriter w, string name, long value)
        => w.WriteElementString(name, value.ToString(CultureInfo.InvariantCulture));

    private static void WriteBool(XmlWriter w, string name, bool value)
        => w.WriteElementString(name, value ? "True" : "False");

    private static void WriteTimestamp(XmlWriter w, string name, DateTime? value)
    {
        w.WriteStartElement(name);
        if (value is not null)
        {
            var utc = value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
            ulong seconds = (ulong)Math.Max(0, (utc - new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            Span<byte> bytes = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(bytes, seconds);
            w.WriteString(Convert.ToBase64String(bytes));
        }
        w.WriteEndElement();
    }
}
