using CotadorLogistico.Core.Secrets;
using Dapper;
using Npgsql;

namespace CotadorLogistico.Infrastructure.Secrets;

public sealed class VaultSecretsStore : ISecretsStore
{
    private readonly NpgsqlDataSource _dataSource;

    public VaultSecretsStore(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<string?> GetAsync(Guid userId, string key, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "select app_secrets.get_secret(@key)",
            new { key = ScopedKey(userId, key) }, cancellationToken: cancellationToken));
    }

    public async Task SetAsync(Guid userId, string key, string value, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "select app_secrets.set_secret(@key, @value)",
            new { key = ScopedKey(userId, key), value }, cancellationToken: cancellationToken));
    }

    public async Task<bool> IsConfiguredAsync(Guid userId, string key, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "select app_secrets.is_configured(@key)",
            new { key = ScopedKey(userId, key) }, cancellationToken: cancellationToken));
    }

    public async Task RemoveAsync(Guid userId, string key, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "select app_secrets.delete_secret(@key)",
            new { key = ScopedKey(userId, key) }, cancellationToken: cancellationToken));
    }

    private static string ScopedKey(Guid userId, string key) => $"{userId}:{key}";
}
