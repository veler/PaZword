namespace PaZword.Kdbx.Ciphers;

/// <summary>Static lookup from a CipherID UUID to the corresponding implementation.</summary>
internal static class KdbxCipherFactory
{
    public static IKdbxCipher Resolve(Guid cipherId)
    {
        if (cipherId == KdbxCipherIds.ChaCha20) return new ChaCha20KdbxCipher();
        if (cipherId == KdbxCipherIds.AesCbc)   return new AesCbcKdbxCipher();
        throw new KdbxFormatException($"Unsupported outer cipher UUID {cipherId}.");
    }
}
