using PaZword.Crypto;
using System.Security.Cryptography;
using System.Text;

namespace PaZword.Kdbx.KeyDerivation;

/// <summary>
/// KDBX composite key construction (<c>docs/sprint-0/kdbx4-spec-notes.md §6</c>):
/// <c>compositeKey = SHA256(SHA256(masterPassword) [+ SHA256(keyFileBytes)])</c>.
/// <para>
/// Caller passes raw password / key-file bytes; <see cref="Build"/> zeroes any
/// intermediate hashes. The returned composite key is the caller's
/// responsibility to clear after use (use <see cref="SecureBytes"/>).
/// </para>
/// </summary>
public static class CompositeKey
{
    /// <summary>Builds the 32-byte KDBX composite key.</summary>
    /// <param name="password">UTF-8 password bytes (may be null/empty for key-file-only vaults).</param>
    /// <param name="keyFile">Optional key-file contents.</param>
    public static byte[] Build(ReadOnlySpan<byte> password, ReadOnlySpan<byte> keyFile = default)
    {
        Span<byte> passwordHash = stackalloc byte[32];
        Span<byte> keyFileHash = stackalloc byte[32];
        bool havePassword = !password.IsEmpty;
        bool haveKeyFile = !keyFile.IsEmpty;

        if (havePassword)
            SHA256.HashData(password, passwordHash);
        if (haveKeyFile)
            SHA256.HashData(keyFile, keyFileHash);

        try
        {
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            if (havePassword) sha.AppendData(passwordHash);
            if (haveKeyFile) sha.AppendData(keyFileHash);
            if (!havePassword && !haveKeyFile)
                throw new ArgumentException("At least one of password or key file must be supplied.");
            return sha.GetHashAndReset();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordHash);
            CryptographicOperations.ZeroMemory(keyFileHash);
        }
    }

    /// <summary>Convenience overload taking a string password (UTF-8 encoded internally).</summary>
    public static byte[] Build(string? password, byte[]? keyFile = null)
    {
        byte[]? pwBytes = null;
        try
        {
            pwBytes = string.IsNullOrEmpty(password)
                ? Array.Empty<byte>()
                : Encoding.UTF8.GetBytes(password);
            return Build(
                pwBytes.AsSpan(),
                keyFile is null ? ReadOnlySpan<byte>.Empty : keyFile.AsSpan());
        }
        finally
        {
            if (pwBytes is { Length: > 0 })
                CryptographicOperations.ZeroMemory(pwBytes);
        }
    }
}
