using System.Text.Json;
using CotadorLogistico.Core.Domain;
using Dapper;
using Npgsql;

namespace CotadorLogistico.Infrastructure.Settings;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public AuditLogRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task RecordAsync(
        Guid organizationId, Guid? actorId, string action, Guid? targetUserId,
        IReadOnlyDictionary<string, string>? metadata, CancellationToken cancellationToken)
    {
        var metadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            insert into public.audit_logs (organization_id, actor_id, action, target_user_id, metadata)
            values (@organizationId, @actorId, @action, @targetUserId, @metadataJson::jsonb)
            """,
            new { organizationId, actorId, action, targetUserId, metadataJson },
            cancellationToken: cancellationToken));
    }
}
