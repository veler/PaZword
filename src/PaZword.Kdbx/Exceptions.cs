namespace PaZword.Kdbx;

/// <summary>
/// Thrown when a KDBX file is structurally malformed (bad signature, truncated
/// TLV, invalid VariantMap, etc.). Never thrown for integrity failures
/// (see <see cref="KdbxIntegrityException"/>) or unsupported versions
/// (see <see cref="KdbxUnsupportedVersionException"/>).
/// </summary>
public class KdbxFormatException : Exception
{
    public KdbxFormatException() { }
    public KdbxFormatException(string message) : base(message) { }
    public KdbxFormatException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a KDBX file fails a cryptographic integrity check (header HMAC,
/// block HMAC, or composite-key mismatch). On this exception <strong>no entry
/// data has been exposed</strong> to the caller — the reader is poisoned.
/// </summary>
public sealed class KdbxIntegrityException : KdbxFormatException
{
    public KdbxIntegrityException() { }
    public KdbxIntegrityException(string message) : base(message) { }
    public KdbxIntegrityException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a KDBX file declares an unsupported major version (anything
/// other than 4). KDBX 3.x is intentionally not supported by PaZword v2.
/// </summary>
public sealed class KdbxUnsupportedVersionException : KdbxFormatException
{
    public uint MajorVersion { get; }
    public uint MinorVersion { get; }
    public KdbxUnsupportedVersionException(uint major, uint minor)
        : base($"KDBX version {major}.{minor} is not supported. Only KDBX 4.x is supported.")
    {
        MajorVersion = major;
        MinorVersion = minor;
    }
}

/// <summary>
/// Thrown by the KDBX writer when the supplied KDF parameters fall below the
/// production floor (see <see cref="PaZword.Crypto.KdfFloor"/>). Reading
/// below-floor vaults is allowed; <em>writing</em> them is refused.
/// </summary>
public sealed class KdbxParameterFloorException : KdbxFormatException
{
    public KdbxParameterFloorException(string message) : base(message) { }
}
