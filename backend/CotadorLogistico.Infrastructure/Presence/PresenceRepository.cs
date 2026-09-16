using CotadorLogistico.Core.Domain;
using Dapper;
using Npgsql;

namespace CotadorLogistico.Infrastructure.Presence;

public sealed class PresenceRepository : IPresenceRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PresenceRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task HeartbeatAsync(Guid userId, string status, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            insert into public.presence (user_id, reported_status, last_seen_at)
            values (@userId, @status, now())
            on conflict (user_id) do update
                set reported_status = excluded.reported_status,
                    last_seen_at = excluded.last_seen_at
            """,
            new { userId, status }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PresenceRow>> GetTeamPresenceAsync(Guid supervisorId, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<PresenceRow>(new CommandDefinition("""
            select p.id as "UserId", p.name as "Name",
                   public.presence_effective_status(pr.reported_status, pr.last_seen_at) as "EffectiveStatus"
            from public.profiles p
            left join public.presence pr on pr.user_id = p.id
            where p.supervisor_id = @supervisorId
            order by p.name
            """,
            new { supervisorId }, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<PresenceRow>> GetAllPresenceAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<PresenceRow>(new CommandDefinition("""
            select p.id as "UserId", p.name as "Name",
                   public.presence_effective_status(pr.reported_status, pr.last_seen_at) as "EffectiveStatus"
            from public.profiles p
            left join public.presence pr on pr.user_id = p.id
            where p.organization_id = @organizationId
            order by p.name
            """,
            new { organizationId }, cancellationToken: cancellationToken));
        return rows.ToList();
    }
}
