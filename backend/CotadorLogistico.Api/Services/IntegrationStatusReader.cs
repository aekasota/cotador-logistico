using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Core.Secrets;

namespace CotadorLogistico.Api.Services;

public sealed class IntegrationStatusReader
{
    private readonly ISecretsStore _secretsStore;
    private readonly IUserIntegrationsRepository _audit;

    public IntegrationStatusReader(ISecretsStore secretsStore, IUserIntegrationsRepository audit)
    {
        _secretsStore = secretsStore;
        _audit = audit;
    }

    public async Task<SettingsStatusResponse> GetStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var frenetConfigured = await _secretsStore.IsConfiguredAsync(userId, SecretKeys.FrenetToken, cancellationToken);
        var meConfigured = await _secretsStore.IsConfiguredAsync(userId, SecretKeys.MelhorEnvioToken, cancellationToken);
        var geminiConfigured = await _secretsStore.IsConfiguredAsync(userId, SecretKeys.GeminiApiKey, cancellationToken);

        var frenetAudit = await _audit.GetAuditAsync(userId, SecretKeys.FrenetToken, cancellationToken);
        var meAudit = await _audit.GetAuditAsync(userId, SecretKeys.MelhorEnvioToken, cancellationToken);
        var geminiAudit = await _audit.GetAuditAsync(userId, SecretKeys.GeminiApiKey, cancellationToken);

        return new SettingsStatusResponse(
            frenetConfigured, ToDto(frenetAudit),
            meConfigured, ToDto(meAudit),
            geminiConfigured, ToDto(geminiAudit));
    }

    private static IntegrationAuditDto? ToDto(IntegrationAudit? audit) =>
        audit is null ? null : new IntegrationAuditDto(audit.UpdatedAt, audit.UpdatedByName);
}
