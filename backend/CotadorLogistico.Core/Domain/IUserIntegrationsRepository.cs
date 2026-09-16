namespace CotadorLogistico.Core.Domain;

public interface IUserIntegrationsRepository
{
    Task RecordUpdateAsync(Guid userId, string integrationKey, Guid updatedByUserId, CancellationToken cancellationToken);

    Task<IntegrationAudit?> GetAuditAsync(Guid userId, string integrationKey, CancellationToken cancellationToken);
}

public sealed record IntegrationAudit(DateTimeOffset UpdatedAt, Guid? UpdatedByUserId, string? UpdatedByName);
