using System.Security.Cryptography;
using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Api.Services;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Core.Secrets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CotadorLogistico.Api.Controllers;

[Route("api/admin/users")]
[EnableRateLimiting(RateLimitPolicies.Admin)]
public sealed class AdminUsersController : CotadorControllerBase
{
    private readonly IProfileRepository _profiles;
    private readonly ISecretsStore _secretsStore;
    private readonly IUserIntegrationsRepository _integrationsAudit;
    private readonly IAuditLogRepository _auditLog;
    private readonly ISupabaseAuthAdminClient _authAdmin;
    private readonly IntegrationStatusReader _statusReader;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        ICurrentUserAccessor currentUser, IProfileRepository profiles, ISecretsStore secretsStore,
        IUserIntegrationsRepository integrationsAudit, IAuditLogRepository auditLog, ISupabaseAuthAdminClient authAdmin,
        IntegrationStatusReader statusReader, ILogger<AdminUsersController> logger)
        : base(currentUser)
    {
        _profiles = profiles;
        _secretsStore = secretsStore;
        _integrationsAudit = integrationsAudit;
        _auditLog = auditLog;
        _authAdmin = authAdmin;
        _statusReader = statusReader;
        _logger = logger;
    }

    [HttpGet("{id:guid}/integrations")]
    public async Task<IActionResult> GetIntegrationsAsync(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(Role.Owner);
        var target = await ResolveSameOrganizationTargetAsync(id, cancellationToken);
        if (target is null) return NotFound();

        return Ok(await _statusReader.GetStatusAsync(target.Id, cancellationToken));
    }

    [HttpPost("{id:guid}/integrations/{provider}/reset")]
    public async Task<IActionResult> ResetIntegrationAsync(Guid id, string provider, CancellationToken cancellationToken)
    {
        RequireRole(Role.Owner);
        var actor = CurrentProfile;

        var key = ResolveProviderKey(provider);
        if (key is null) return BadRequest(new { error = "Integração desconhecida." });

        var target = await ResolveSameOrganizationTargetAsync(id, cancellationToken);
        if (target is null) return NotFound();

        await _secretsStore.RemoveAsync(target.Id, key, cancellationToken);

        await _integrationsAudit.RecordUpdateAsync(target.Id, key, actor.Id, cancellationToken);
        await _auditLog.RecordAsync(
            actor.OrganizationId, actor.Id, AuditActions.IntegrationReset, target.Id,
            new Dictionary<string, string> { ["provider"] = provider.ToLowerInvariant() }, cancellationToken);

        _logger.LogInformation(
            "OWNER {ActorId} resetou a integração {Provider} do usuário {TargetId}.", actor.Id, provider, target.Id);

        return Ok(await _statusReader.GetStatusAsync(target.Id, cancellationToken));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateUserAsync(
        Guid id, [FromBody] UpdateUserAdminRequest request, CancellationToken cancellationToken)
    {
        RequireRole(Role.Owner);
        var actor = CurrentProfile;

        if (id == actor.Id)
            return BadRequest(new { error = "Use a tela de Perfil para alterações na própria conta." });

        var target = await ResolveSameOrganizationTargetAsync(id, cancellationToken);
        if (target is null) return NotFound();

        var metadata = new Dictionary<string, string>();

        Role? newRole = null;
        Guid? resolvedSupervisorId = request.SupervisorId;

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var requestedRoleText = request.Role.Trim().ToUpperInvariant();
            if (requestedRoleText == "OWNER" || !RoleExtensions.TryParse(requestedRoleText, out var parsedRole) || parsedRole == Role.Owner)
                return BadRequest(new { error = "Role inválido — não é possível promover ninguém a OWNER pela aplicação (ver seção 23)." });

            newRole = parsedRole;
            if (newRole == Role.Supervisor)
            {
                resolvedSupervisorId = null;
            }
            else
            {
                resolvedSupervisorId ??= target.SupervisorId ?? actor.Id;
            }

            metadata["oldRole"] = target.Role.ToDbString();
            metadata["newRole"] = newRole.Value.ToDbString();
        }

        if (resolvedSupervisorId is not null)
        {
            var chosenSupervisor = await _profiles.GetByIdAsync(resolvedSupervisorId.Value, cancellationToken);
            if (chosenSupervisor is null || chosenSupervisor.Role < Role.Supervisor || chosenSupervisor.OrganizationId != actor.OrganizationId)
                return BadRequest(new { error = "supervisorId precisa apontar para um SUPERVISOR ou OWNER existente na mesma organização." });
        }

        string? newEmail = null;
        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(request.Email.Trim(), target.Email, StringComparison.OrdinalIgnoreCase))
        {
            var trimmedEmail = request.Email.Trim();
            if (!trimmedEmail.Contains('@')) return BadRequest(new { error = "E-mail inválido." });
            if (await _profiles.EmailExistsAsync(trimmedEmail, cancellationToken))
                return Conflict(new { error = "Já existe uma conta cadastrada com este e-mail." });
            newEmail = trimmedEmail;
            metadata["emailChanged"] = "true";
        }

        if (newEmail is not null)
        {
            try
            {
                await _authAdmin.UpdateUserAsync(target.Id, newEmail, newPassword: null, cancellationToken);
            }
            catch (SupabaseAdminException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
            }
        }

        var trimmedName = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
        await _profiles.UpdateAdminFieldsAsync(target.Id, trimmedName, request.Position?.Trim(), resolvedSupervisorId, newRole, cancellationToken);

        if (metadata.Count > 0 || trimmedName is not null || request.Position is not null)
        {
            await _auditLog.RecordAsync(
                actor.OrganizationId, actor.Id, AuditActions.UserUpdated, target.Id,
                metadata.Count > 0 ? metadata : null, cancellationToken);
        }

        _logger.LogInformation("OWNER {ActorId} atualizou o usuário {TargetId}.", actor.Id, target.Id);

        var updated = await _profiles.GetByIdAsync(target.Id, cancellationToken);
        return Ok(ToAdminUserDto(updated!));
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActiveAsync(Guid id, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        RequireRole(Role.Owner);
        var actor = CurrentProfile;

        if (id == actor.Id)
            return BadRequest(new { error = "Não é possível desativar a própria conta." });

        var target = await ResolveSameOrganizationTargetAsync(id, cancellationToken);
        if (target is null) return NotFound();

        await _profiles.SetActiveAsync(target.Id, request.IsActive, cancellationToken);
        await _auditLog.RecordAsync(
            actor.OrganizationId, actor.Id,
            request.IsActive ? AuditActions.UserActivated : AuditActions.UserDeactivated,
            target.Id, metadata: null, cancellationToken);

        _logger.LogInformation(
            "OWNER {ActorId} {Action} a conta do usuário {TargetId}.",
            actor.Id, request.IsActive ? "ativou" : "desativou", target.Id);

        return NoContent();
    }

    [HttpPost("{id:guid}/temporary-password")]
    public async Task<ActionResult<TemporaryPasswordResponse>> IssueTemporaryPasswordAsync(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(Role.Owner);
        var actor = CurrentProfile;

        var target = await ResolveSameOrganizationTargetAsync(id, cancellationToken);
        if (target is null) return NotFound();

        var temporaryPassword = GenerateTemporaryPassword();

        try
        {
            await _authAdmin.UpdateUserAsync(target.Id, newEmail: null, temporaryPassword, cancellationToken);
        }
        catch (SupabaseAdminException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }

        await _profiles.SetMustChangePasswordAsync(target.Id, true, cancellationToken);
        await _auditLog.RecordAsync(
            actor.OrganizationId, actor.Id, AuditActions.TemporaryPasswordIssued, target.Id, metadata: null, cancellationToken);

        _logger.LogInformation("OWNER {ActorId} gerou senha temporária para o usuário {TargetId}.", actor.Id, target.Id);

        return Ok(new TemporaryPasswordResponse(temporaryPassword));
    }

    private async Task<Profile?> ResolveSameOrganizationTargetAsync(Guid id, CancellationToken cancellationToken)
    {
        var target = await _profiles.GetByIdAsync(id, cancellationToken);

        if (target is null || target.OrganizationId != CurrentProfile.OrganizationId) return null;
        return target;
    }

    private static AdminUserDto ToAdminUserDto(Profile p) =>
        new(p.Id, p.Name, p.Email, p.Position, p.Role.ToDbString(), p.SupervisorId, p.IsActive);

    private static string? ResolveProviderKey(string provider) => provider.ToLowerInvariant() switch
    {
        "frenet" => SecretKeys.FrenetToken,
        "melhorenvio" or "melhor-envio" or "melhor_envio" => SecretKeys.MelhorEnvioToken,
        "gemini" => SecretKeys.GeminiApiKey,
        _ => null
    };

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var bytes = RandomNumberGenerator.GetBytes(16);
        var chars = new char[16];
        for (var i = 0; i < bytes.Length; i++) chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }
}
