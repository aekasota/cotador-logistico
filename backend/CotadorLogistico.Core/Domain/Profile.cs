namespace CotadorLogistico.Core.Domain;

public sealed class Profile
{
    public required Guid Id { get; init; }

    public required Guid OrganizationId { get; init; }

    public required string Name { get; init; }
    public string? Position { get; init; }
    public required Role Role { get; init; }
    public Guid? SupervisorId { get; init; }
    public required string Theme { get; init; }
    public required string Language { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? LastSeenAt { get; init; }
    public string PresenceStatus { get; init; } = "OFFLINE";

    public bool IsActive { get; init; } = true;

    public bool MustChangePassword { get; init; }

    public string? Email { get; init; }
}

public static class PresenceStatuses
{
    public const string Online = "ONLINE";
    public const string Quoting = "QUOTING";
    public const string Offline = "OFFLINE";

    public static readonly TimeSpan OfflineThreshold = TimeSpan.FromSeconds(90);
}
