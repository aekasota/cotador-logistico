using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Tests.TestDoubles;

internal sealed class FakeUserIntegrationsRepository : IUserIntegrationsRepository
{
    private readonly Dictionary<(Guid UserId, string Key), IntegrationAudit> _audits = new();

    public Task RecordUpdateAsync(Guid userId, string integrationKey, Guid updatedByUserId, CancellationToken cancellationToken)
    {
        _audits[(userId, integrationKey)] = new IntegrationAudit(DateTimeOffset.UtcNow, updatedByUserId, $"Usuário {updatedByUserId}");
        return Task.CompletedTask;
    }

    public Task<IntegrationAudit?> GetAuditAsync(Guid userId, string integrationKey, CancellationToken cancellationToken) =>
        Task.FromResult(_audits.GetValueOrDefault((userId, integrationKey)));
}
