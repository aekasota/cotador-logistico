using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Tests.TestDoubles;

internal sealed class FakeAuditLogRepository : IAuditLogRepository
{
    public sealed record Entry(Guid OrganizationId, Guid? ActorId, string Action, Guid? TargetUserId, IReadOnlyDictionary<string, string>? Metadata);

    public List<Entry> Entries { get; } = new();

    public Task RecordAsync(
        Guid organizationId, Guid? actorId, string action, Guid? targetUserId,
        IReadOnlyDictionary<string, string>? metadata, CancellationToken cancellationToken)
    {
        Entries.Add(new Entry(organizationId, actorId, action, targetUserId, metadata));
        return Task.CompletedTask;
    }
}
