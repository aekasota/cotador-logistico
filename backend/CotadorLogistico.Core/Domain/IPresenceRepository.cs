namespace CotadorLogistico.Core.Domain;

public interface IPresenceRepository
{
    Task HeartbeatAsync(Guid userId, string status, CancellationToken cancellationToken);

    Task<IReadOnlyList<PresenceRow>> GetTeamPresenceAsync(Guid supervisorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PresenceRow>> GetAllPresenceAsync(Guid organizationId, CancellationToken cancellationToken);
}

public sealed record PresenceRow(Guid UserId, string Name, string EffectiveStatus);
