namespace CotadorLogistico.Api.Contracts;

public sealed record UpdateUserAdminRequest(string? Name, string? Position, string? Email, Guid? SupervisorId, string? Role);

public sealed record AdminUserDto(
    Guid Id, string Name, string? Email, string? Position, string Role, Guid? SupervisorId, bool IsActive);

public sealed record SetActiveRequest(bool IsActive);

public sealed record TemporaryPasswordResponse(string TemporaryPassword);
