namespace CotadorLogistico.Api.Contracts;

public sealed record ProfileResponse(
    Guid Id,
    string Name,
    string? Position,
    string? Email,
    string Role,
    string Theme,
    string Language,
    int TotalQuotes,
    int? TeamTotalQuotes,

    bool MustChangePassword);

public sealed record UpdatePreferencesRequest(string? Theme, string? Language);

public sealed record ChangePasswordRequest(string NewPassword);
