using Microsoft.Extensions.Logging;
using PaZword.Kdbx.Interfaces;
using PaZword.Kdbx.KeyDerivation;
using PaZword.Kdbx.Reader;
using PaZword.Kdbx.Writer;
using System.Security.Cryptography;

namespace PaZword.Core.Vault;

/// <summary>
/// Default <see cref="IVaultService"/> implementation, backed by the
/// <c>PaZword.Kdbx</c> reader/writer. AOT-clean: no reflection, explicit
/// dependencies, all sensitive buffers zeroed in <c>finally</c>.
/// </summary>
public sealed class VaultService : IVaultService
{
    private readonly IKdbxReader _reader;
    private readonly IKdbxWriter _writer;
    private readonly ILogger<VaultService>? _logger;

    /// <summary>Default constructor wires the in-box reader / writer.</summary>
    public VaultService()
        : this(new KdbxReader(), new KdbxWriter(), logger: null) { }

    public VaultService(IKdbxReader reader, IKdbxWriter writer, ILogger<VaultService>? logger = null)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<VaultHandle> OpenAsync(string path, VaultCredentials credentials, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(credentials);

        byte[] composite = CompositeKey.Build(credentials.Password, credentials.KeyFile);
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await _reader.ReadAsync(stream, composite, cancellationToken).ConfigureAwait(false);
            // Transfer ownership of composite to the handle.
            var handle = new VaultHandle(path, doc, composite);
            composite = null!;
            return handle;
        }
        finally
        {
            if (composite is not null)
                CryptographicOperations.ZeroMemory(composite);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(VaultHandle handle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);

        // Defensive copy of the handle's composite so the underlying buffer
        // remains owned by the handle.
        var composite = handle.CompositeKey.ToArray();
        try
        {
            var tempPath = handle.Path + ".tmp";
            using (var stream = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await _writer.WriteAsync(handle.Document, stream, composite, cancellationToken).ConfigureAwait(false);
            }
            if (File.Exists(handle.Path)) File.Replace(tempPath, handle.Path, null);
            else File.Move(tempPath, handle.Path);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(composite);
        }
    }

    /// <inheritdoc/>
    public Task CloseAsync(VaultHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        handle.Dispose();
        return Task.CompletedTask;
    }
}
