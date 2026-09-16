namespace CotadorLogistico.Api.Contracts;

public sealed record SettingsStatusResponse(
    bool FrenetConfigured,
    IntegrationAuditDto? FrenetAudit,
    bool MelhorEnvioConfigured,
    IntegrationAuditDto? MelhorEnvioAudit,
    bool GeminiConfigured,
    IntegrationAuditDto? GeminiAudit);

public sealed record IntegrationAuditDto(DateTimeOffset UpdatedAt, string? UpdatedByName);

public sealed record UpdateSettingsRequest(string? FrenetToken, string? MelhorEnvioToken, string? GeminiApiKey);
