using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Tests.TestDoubles;

internal sealed class FakeProfileRepository : IProfileRepository
{
    private readonly Dictionary<Guid, Profile> _profiles = new();
    private readonly HashSet<string> _emails = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(Profile profile, string? email = null)
    {
        _profiles[profile.Id] = profile;
        if (email is not null) _emails.Add(email);
    }

    public Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.GetValueOrDefault(id));

    public Task<IReadOnlyList<Profile>> GetTeamAsync(Guid supervisorId, string? search, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Profile>>(
            Filter(_profiles.Values.Where(p => p.SupervisorId == supervisorId), search).ToList());

    public Task<IReadOnlyList<Profile>> GetAllAsync(Guid organizationId, string? search, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Profile>>(
            Filter(_profiles.Values.Where(p => p.OrganizationId == organizationId), search).ToList());

    private static IEnumerable<Profile> Filter(IEnumerable<Profile> profiles, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return profiles;
        return profiles.Where(p =>
            p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            (p.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (p.Position?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            p.Role.ToDbString().Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    public Task CreateAsync(Profile profile, CancellationToken cancellationToken)
    {
        _profiles[profile.Id] = profile;
        CreatedProfiles.Add(profile);
        return Task.CompletedTask;
    }

    public Task UpdateThemeAsync(Guid id, string theme, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task UpdateLanguageAsync(Guid id, string language, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task UpdateAdminFieldsAsync(
        Guid id, string? name, string? position, Guid? supervisorId, Role? role, CancellationToken cancellationToken)
    {
        if (!_profiles.TryGetValue(id, out var current)) return Task.CompletedTask;

        var supervisorProvided = supervisorId is not null || role is not null;
        _profiles[id] = Clone(current, name: name, position: position, positionProvided: position is not null,
            supervisorId: supervisorProvided ? supervisorId : current.SupervisorId, role: role ?? current.Role);
        return Task.CompletedTask;
    }

    public Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        if (_profiles.TryGetValue(id, out var current))
            _profiles[id] = Clone(current, supervisorId: current.SupervisorId, role: current.Role, isActive: isActive);
        return Task.CompletedTask;
    }

    public Task SetMustChangePasswordAsync(Guid id, bool value, CancellationToken cancellationToken)
    {
        if (_profiles.TryGetValue(id, out var current))
            _profiles[id] = Clone(current, supervisorId: current.SupervisorId, role: current.Role, mustChangePassword: value);
        return Task.CompletedTask;
    }

    public Task TouchLastSeenAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_emails.Contains(email));

    public List<Profile> CreatedProfiles { get; } = new();

    private static Profile Clone(
        Profile source, string? name = null, string? position = null, bool positionProvided = false,
        Guid? supervisorId = null, Role? role = null, bool? isActive = null, bool? mustChangePassword = null) => new()
    {
        Id = source.Id,
        OrganizationId = source.OrganizationId,
        Name = name ?? source.Name,
        Position = positionProvided ? position : source.Position,
        Role = role ?? source.Role,
        SupervisorId = supervisorId,
        Theme = source.Theme,
        Language = source.Language,
        IsActive = isActive ?? source.IsActive,
        MustChangePassword = mustChangePassword ?? source.MustChangePassword,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        LastSeenAt = source.LastSeenAt,
        PresenceStatus = source.PresenceStatus,
        Email = source.Email
    };
}
