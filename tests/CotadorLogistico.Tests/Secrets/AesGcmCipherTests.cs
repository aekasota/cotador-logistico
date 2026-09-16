using System.Security.Cryptography;
using CotadorLogistico.Infrastructure.Secrets;

namespace CotadorLogistico.Tests.Secrets;

public sealed class AesGcmCipherTests
{
    private static byte[] RandomKey() => RandomNumberGenerator.GetBytes(AesGcmCipher.KeySizeBytes);

    [Fact]
    public void EncryptDecrypt_RoundTrip_DevolveOTextoOriginal()
    {
        var key = RandomKey();
        var payload = AesGcmCipher.Encrypt("token-super-secreto-da-frenet", key);

        var decrypted = AesGcmCipher.Decrypt(payload, key);

        Assert.Equal("token-super-secreto-da-frenet", decrypted);
    }

    [Fact]
    public void Encrypt_NuncaDevolveOTextoEmClaroNoCiphertext()
    {
        var key = RandomKey();
        const string secret = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        var payload = AesGcmCipher.Encrypt(secret, key);

        Assert.DoesNotContain(secret, System.Text.Encoding.UTF8.GetString(payload.Ciphertext));
    }

    [Fact]
    public void Decrypt_ComChaveErrada_LancaExcecaoEmVezDeDevolverLixoSilenciosamente()
    {
        var payload = AesGcmCipher.Encrypt("valor-original", RandomKey());

        Assert.ThrowsAny<CryptographicException>(() => AesGcmCipher.Decrypt(payload, RandomKey()));
    }

    [Fact]
    public void Decrypt_ComCiphertextAdulterado_LancaExcecao()
    {
        var key = RandomKey();
        var payload = AesGcmCipher.Encrypt("valor-original", key);
        payload.Ciphertext[0] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => AesGcmCipher.Decrypt(payload, key));
    }

    [Fact]
    public void Encrypt_DuasChamadasComOMesmoTexto_GeramNoncesDiferentes()
    {
        var key = RandomKey();
        var a = AesGcmCipher.Encrypt("mesmo-valor", key);
        var b = AesGcmCipher.Encrypt("mesmo-valor", key);

        Assert.False(a.Nonce.SequenceEqual(b.Nonce));
        Assert.False(a.Ciphertext.SequenceEqual(b.Ciphertext));
    }

    [Fact]
    public void Encrypt_ComChaveDeTamanhoErrado_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AesGcmCipher.Encrypt("x", new byte[16]));
    }
}
