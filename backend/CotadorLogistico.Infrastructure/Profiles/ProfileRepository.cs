using CotadorLogistico.Core.Domain;
using Dapper;
using Npgsql;

namespace CotadorLogistico.Infrastructure.Profiles;

public sealed class ProfileRepository : IProfileRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ProfileRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    private const string SelectColumns = """
        p.id as "Id", p.organization_id as "OrganizationId", p.name as "Name", p."position" as "Position",
        p.role as "RoleText", p.supervisor_id as "SupervisorId", p.theme as "Theme", p.language as "Language",
        p.is_active as "IsActive", p.must_change_password as "MustChangePassword",
        p.created_at as "CreatedAt", p.updated_at as "UpdatedAt", u.email as "Email"
        """;

    private const string FromClause = "from public.profiles p join auth.users u on u.id = p.id";

    public async Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<ProfileRow>(
            new CommandDefinition($"select {SelectColumns} {FromClause} where p.id = @id",
                new { id }, cancellationToken: cancellationToken));
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Profile>> GetTeamAsync(Guid supervisorId, string? search, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<ProfileRow>(
            new CommandDefinition($"""
                select {SelectColumns} {FromClause}
                where p.supervisor_id = @supervisorId
                  and (@search is null or p.name ilike @searchPattern or u.email ilike @searchPattern or p."position" ilike @searchPattern)
                order by p.name
                """,
                new { supervisorId, search, searchPattern = ToSearchPattern(search) }, cancellationToken: cancellationToken));
        return rows.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Profile>> GetAllAsync(Guid organizationId, string? search, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<ProfileRow>(
            new CommandDefinition($"""
                select {SelectColumns} {FromClause}
                where p.organization_id = @organizationId
                  and (@search is null or p.name ilike @searchPattern or u.email ilike @searchPattern
                       or p."position" ilike @searchPattern or p.role ilike @searchPattern)
                order by p.name
                """,
                new { organizationId, search, searchPattern = ToSearchPattern(search) }, cancellationToken: cancellationToken));
        return rows.Select(r => r.ToDomain()).ToList();
    }

    private static string? ToSearchPattern(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";

    public async Task CreateAsync(Profile profile, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            insert into public.profiles (id, organization_id, name, "position", role, supervisor_id, theme, language)
            values (@Id, @OrganizationId, @Name, @Position, @Role, @SupervisorId, @Theme, @Language)
            """,
            new
            {
                profile.Id,
                profile.OrganizationId,
                profile.Name,
                profile.Position,
                Role = profile.Role.ToDbString(),
                profile.SupervisorId,
                profile.Theme,
                profile.Language
            },
            cancellationToken: cancellationToken));
    }

    public async Task UpdateThemeAsync(Guid id, string theme, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "update public.profiles set theme = @theme where id = @id",
            new { id, theme }, cancellationToken: cancellationToken));
    }

    public async Task UpdateLanguageAsync(Guid id, string language, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "update public.profiles set language = @language where id = @id",
            new { id, language }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAdminFieldsAsync(
        Guid id, string? name, string? position, Guid? supervisorId, Role? role, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            update public.profiles
            set name = coalesce(@name, name),
                "position" = case when @positionProvided then @position else "position" end,
                supervisor_id = case when @supervisorProvided then @supervisorId else supervisor_id end,
                role = coalesce(@roleText, role)
            where id = @id
            """,
            new
            {
                id, name, position,
                positionProvided = position is not null,
                supervisorId,
                supervisorProvided = supervisorId is not null || role is not null,
                roleText = role?.ToDbString()
            },
            cancellationToken: cancellationToken));
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "update public.profiles set is_active = @isActive where id = @id",
            new { id, isActive }, cancellationToken: cancellationToken));
    }

    public async Task SetMustChangePasswordAsync(Guid id, bool value, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            "update public.profiles set must_change_password = @value where id = @id",
            new { id, value }, cancellationToken: cancellationToken));
    }

    public Task TouchLastSeenAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "select count(*) from auth.users where lower(email) = lower(@email)",
            new { email }, cancellationToken: cancellationToken));
        return count > 0;
    }

    private sealed class ProfileRow
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = "";
        public string? Position { get; set; }
        public string RoleText { get; set; } = "";
        public Guid? SupervisorId { get; set; }
        public string Theme { get; set; } = "";
        public string Language { get; set; } = "";
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Email { get; set; }

        public Profile ToDomain() => new()
        {
            Id = Id,
            OrganizationId = OrganizationId,
            Name = Name,
            Position = Position,
            Role = Enum.TryParse<Role>(RoleText, ignoreCase: true, out var role) ? role : Role.Operator,
            SupervisorId = SupervisorId,
            Theme = Theme,
            Language = Language,
            IsActive = IsActive,
            MustChangePassword = MustChangePassword,
            CreatedAt = new DateTimeOffset(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)),
            UpdatedAt = new DateTimeOffset(DateTime.SpecifyKind(UpdatedAt, DateTimeKind.Utc)),
            Email = Email
        };
    }
}
