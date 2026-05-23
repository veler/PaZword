using PaZword.Kdbx.Models;

namespace PaZword.Kdbx.Interfaces;

/// <summary>
/// Writes a KDBX 4 database file.
/// Implementation lives in sprint 1; this interface defines the contract only.
/// </summary>
public interface IKdbxWriter
{
    /// <summary>
    /// Encrypts and writes a <see cref="KdbxDocument"/> to the supplied stream.
    /// </summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="stream">Writable destination stream (must support seeking for HMAC blocks).</param>
    /// <param name="compositeKey">
    ///   The composite key bytes. Caller is responsible for zeroing after the call.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(
        KdbxDocument document,
        Stream stream,
        byte[] compositeKey,
        CancellationToken cancellationToken = default);
}
