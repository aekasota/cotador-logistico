using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Tests.TestDoubles;

internal sealed class FakePresenceRepository : IPresenceRepository
{
    public List<PresenceRow> TeamRows { get; } = new();
    public List<PresenceRow> AllRows { get; } = new();
    public List<(Guid UserId, string Status)> Heartbeats { get; } = new();

    public Task HeartbeatAsync(Guid userId, string status, CancellationToken cancellationToken)
    {
        Heartbeats.Add((userId, status));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PresenceRow>> GetTeamPresenceAsync(Guid supervisorId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PresenceRow>>(TeamRows);

    public Task<IReadOnlyList<PresenceRow>> GetAllPresenceAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PresenceRow>>(AllRows);
}
