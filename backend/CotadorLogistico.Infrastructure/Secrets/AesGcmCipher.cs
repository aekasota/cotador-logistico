using System.Security.Cryptography;
using System.Text;

namespace CotadorLogistico.Infrastructure.Secrets;

public static class AesGcmCipher
{
    public const int NonceSizeBytes = 12;
    public const int TagSizeBytes = 16;
    public const int KeySizeBytes = 32;

    public static EncryptedPayload Encrypt(string plaintext, byte[] key)
    {
        ValidateKey(key);

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return new EncryptedPayload(ciphertext, nonce, tag);
    }

    public static string Decrypt(EncryptedPayload payload, byte[] key)
    {
        ValidateKey(key);

        var plaintextBytes = new byte[payload.Ciphertext.Length];
        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, plaintextBytes);
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static void ValidateKey(byte[] key)
    {
        if (key.Length != KeySizeBytes)
            throw new ArgumentException($"A chave precisa ter {KeySizeBytes} bytes (AES-256); recebeu {key.Length}.", nameof(key));
    }
}

public sealed record EncryptedPayload(byte[] Ciphertext, byte[] Nonce, byte[] Tag);
