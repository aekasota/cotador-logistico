using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Api.Services;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Core.Secrets;
using Microsoft.AspNetCore.Mvc;

namespace CotadorLogistico.Api.Controllers;

[Route("api/settings")]
public sealed class SettingsController : CotadorControllerBase
{
    private readonly ISecretsStore _secretsStore;
    private readonly IUserIntegrationsRepository _audit;
    private readonly IntegrationStatusReader _statusReader;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ICurrentUserAccessor currentUser, ISecretsStore secretsStore, IUserIntegrationsRepository audit,
        IntegrationStatusReader statusReader, ILogger<SettingsController> logger)
        : base(currentUser)
    {
        _secretsStore = secretsStore;
        _audit = audit;
        _statusReader = statusReader;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<SettingsStatusResponse>> GetStatusAsync(CancellationToken cancellationToken) =>
        Ok(await _statusReader.GetStatusAsync(CurrentProfile.Id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SettingsStatusResponse>> UpdateAsync(
        [FromBody] UpdateSettingsRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentProfile.Id;

        if (!string.IsNullOrWhiteSpace(request.FrenetToken))
        {
            await _secretsStore.SetAsync(userId, SecretKeys.FrenetToken, request.FrenetToken.Trim(), cancellationToken);
            await _audit.RecordUpdateAsync(userId, SecretKeys.FrenetToken, userId, cancellationToken);
            _logger.LogInformation("Token da Frenet atualizado por {UserId}.", userId);
        }

        if (!string.IsNullOrWhiteSpace(request.MelhorEnvioToken))
        {
            await _secretsStore.SetAsync(userId, SecretKeys.MelhorEnvioToken, request.MelhorEnvioToken.Trim(), cancellationToken);
            await _audit.RecordUpdateAsync(userId, SecretKeys.MelhorEnvioToken, userId, cancellationToken);
            _logger.LogInformation("Token do Melhor Envio atualizado por {UserId}.", userId);
        }

        if (!string.IsNullOrWhiteSpace(request.GeminiApiKey))
        {
            await _secretsStore.SetAsync(userId, SecretKeys.GeminiApiKey, request.GeminiApiKey.Trim(), cancellationToken);
            await _audit.RecordUpdateAsync(userId, SecretKeys.GeminiApiKey, userId, cancellationToken);
            _logger.LogInformation("Chave do Gemini atualizada por {UserId}.", userId);
        }

        return Ok(await _statusReader.GetStatusAsync(userId, cancellationToken));
    }
}
