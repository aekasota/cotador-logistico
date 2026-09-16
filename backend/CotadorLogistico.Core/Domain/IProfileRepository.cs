namespace CotadorLogistico.Core.Domain;

public interface IProfileRepository
{
    Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Profile>> GetTeamAsync(Guid supervisorId, string? search, CancellationToken cancellationToken);

    Task<IReadOnlyList<Profile>> GetAllAsync(Guid organizationId, string? search, CancellationToken cancellationToken);

    Task CreateAsync(Profile profile, CancellationToken cancellationToken);

    Task UpdateThemeAsync(Guid id, string theme, CancellationToken cancellationToken);

    Task UpdateLanguageAsync(Guid id, string language, CancellationToken cancellationToken);

    Task UpdateAdminFieldsAsync(
        Guid id, string? name, string? position, Guid? supervisorId, Role? role, CancellationToken cancellationToken);

    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken);

    Task SetMustChangePasswordAsync(Guid id, bool value, CancellationToken cancellationToken);

    Task TouchLastSeenAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
}
