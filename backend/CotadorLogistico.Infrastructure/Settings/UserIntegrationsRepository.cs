using CotadorLogistico.Core.Domain;
using CotadorLogistico.Core.Secrets;
using Dapper;
using Npgsql;

namespace CotadorLogistico.Infrastructure.Settings;

public sealed class UserIntegrationsRepository : IUserIntegrationsRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public UserIntegrationsRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task RecordUpdateAsync(Guid userId, string integrationKey, Guid updatedByUserId, CancellationToken cancellationToken)
    {
        var (updatedAtColumn, updatedByColumn) = ColumnsFor(integrationKey);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition($"""
            insert into public.user_integrations (user_id, {updatedAtColumn}, {updatedByColumn})
            values (@userId, now(), @updatedByUserId)
            on conflict (user_id) do update
                set {updatedAtColumn} = now(), {updatedByColumn} = @updatedByUserId
            """,
            new { userId, updatedByUserId }, cancellationToken: cancellationToken));
    }

    public async Task<IntegrationAudit?> GetAuditAsync(Guid userId, string integrationKey, CancellationToken cancellationToken)
    {
        var (updatedAtColumn, updatedByColumn) = ColumnsFor(integrationKey);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<Row>(new CommandDefinition($"""
            select s.{updatedAtColumn} as "UpdatedAt", s.{updatedByColumn} as "UpdatedByUserId", p.name as "UpdatedByName"
            from public.user_integrations s
            left join public.profiles p on p.id = s.{updatedByColumn}
            where s.user_id = @userId
            """,
            new { userId }, cancellationToken: cancellationToken));

        if (row?.UpdatedAt is null) return null;
        var updatedAt = new DateTimeOffset(DateTime.SpecifyKind(row.UpdatedAt.Value, DateTimeKind.Utc));
        return new IntegrationAudit(updatedAt, row.UpdatedByUserId, row.UpdatedByName);
    }

    private static (string UpdatedAtColumn, string UpdatedByColumn) ColumnsFor(string integrationKey) => integrationKey switch
    {
        SecretKeys.FrenetToken => ("frenet_updated_at", "frenet_updated_by"),
        SecretKeys.MelhorEnvioToken => ("melhor_envio_updated_at", "melhor_envio_updated_by"),
        SecretKeys.GeminiApiKey => ("gemini_updated_at", "gemini_updated_by"),
        _ => throw new ArgumentOutOfRangeException(nameof(integrationKey), integrationKey, "Integração desconhecida.")
    };

    private sealed class Row
    {
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public string? UpdatedByName { get; set; }
    }
}
