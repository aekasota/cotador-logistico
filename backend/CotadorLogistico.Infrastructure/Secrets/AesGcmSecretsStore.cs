using CotadorLogistico.Core.Secrets;
using CotadorLogistico.Infrastructure.Configuration;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CotadorLogistico.Infrastructure.Secrets;

public sealed class AesGcmSecretsStore : ISecretsStore
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly byte[] _masterKey;

    public AesGcmSecretsStore(NpgsqlDataSource dataSource, IOptions<SecretsOptions> options)
    {
        _dataSource = dataSource;

        var keyBase64 = options.Value.MasterKeyBase64;
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException(
                "Secrets:Mode está como \"Aes\" mas SECRETS_MASTER_KEY não foi configurada. " +
                "Gere uma com `openssl rand -base64 32` e configure a variável de ambiente.");
        }

        _masterKey = Convert.FromBase64String(keyBase64);
        if (_masterKey.Length != AesGcmCipher.KeySizeBytes)
        {
            throw new InvalidOperationException(
                $"SECRETS_MASTER_KEY precisa decodificar para {AesGcmCipher.KeySizeBytes} bytes (AES-256); recebeu {_masterKey.Length}.");
        }
    }

    public async Task<string?> GetAsync(Guid userId, string key, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<EncryptedRow>(new CommandDefinition(
            """select ciphertext as "Ciphertext", nonce as "Nonce", auth_tag as "AuthTag" from public.encrypted_secrets where key = @key""",
            new { key = ScopedKey(userId, key) }, cancellationToken: cancellationToken));

        if (row is null) return null;
        return AesGcmCipher.Decrypt(new EncryptedPayload(row.Ciphertext, row.Nonce, row.AuthTag), _masterKey);
    }

    public async Task SetAsync(Guid userId, string key, string value, CancellationToken cancellationToken)
    {
        var payload = AesGcmCipher.Encrypt(value, _masterKey);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            insert into public.encrypted_secrets (key, ciphertext, nonce, auth_tag, updated_at)
            values (@key, @ciphertext, @nonce, @tag, now())
            on conflict (key) do update
                set ciphertext = excluded.ciphertext,
                    nonce = excluded.nonce,
                    auth_tag = excluded.auth_tag,
                    updated_at = now()
            """,
            new { key = ScopedKey(userId, key), ciphertext = payload.Ciphertext, nonce = payload.Nonce, tag = payload.Tag },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> IsConfiguredAsync(Guid userId, string key, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "select count(*) from public.encrypted_secrets where key = @key",
            new { key = ScopedKey(userId, key) }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task RemoveAsync(Guid userId, string key, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "delete from public.encrypted_secrets where key = @key",
            new { key = ScopedKey(userId, key) }, cancellationToken: cancellationToken));
    }

    private static string ScopedKey(Guid userId, string key) => $"{userId}:{key}";

    private sealed record EncryptedRow(byte[] Ciphertext, byte[] Nonce, byte[] AuthTag);
}
