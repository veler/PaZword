using PaZword.Kdbx.Models;

namespace PaZword.Kdbx.Interfaces;

/// <summary>
/// Reads a KDBX 4 database file.
/// Implementation lives in sprint 1; this interface defines the contract only.
/// </summary>
public interface IKdbxReader
{
    /// <summary>
    /// Reads and decrypts a KDBX 4 stream using the supplied composite key.
    /// </summary>
    /// <param name="stream">The raw KDBX file stream (positioned at byte 0).</param>
    /// <param name="compositeKey">
    ///   The composite key bytes (derived from master password + optional key file).
    ///   Caller is responsible for zeroing this array after the call.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The fully parsed <see cref="KdbxDocument"/>.</returns>
    Task<KdbxDocument> ReadAsync(
        Stream stream,
        byte[] compositeKey,
        CancellationToken cancellationToken = default);
}
