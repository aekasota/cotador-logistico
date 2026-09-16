namespace CotadorLogistico.Api.Contracts;

public sealed record CreateTeamUserRequest(
    string Name, string? Position, string Email, string Password, string? Role, Guid? SupervisorId);

public sealed record CreateTeamUserResponse(Guid Id, string Name, string Email, string Role);

public sealed record TeamMemberDto(
    Guid Id, string Name, string? Email, string? Position, string Role, Guid? SupervisorId,
    bool IsActive, string PresenceStatus, int QuoteCount);

public sealed record TeamResponse(IReadOnlyList<TeamMemberDto> Members, int TotalMembers, int TotalQuotes);
