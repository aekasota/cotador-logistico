using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CotadorLogistico.Api.Controllers;

[Route("api/me")]
public sealed class MeController : CotadorControllerBase
{
    private readonly IProfileRepository _profiles;
    private readonly IQuoteRepository _quotes;
    private readonly ISupabaseAuthAdminClient _authAdmin;
    private readonly IAuditLogRepository _auditLog;
    private readonly ILogger<MeController> _logger;

    public MeController(
        ICurrentUserAccessor currentUser, IProfileRepository profiles, IQuoteRepository quotes,
        ISupabaseAuthAdminClient authAdmin, IAuditLogRepository auditLog, ILogger<MeController> logger)
        : base(currentUser)
    {
        _profiles = profiles;
        _quotes = quotes;
        _authAdmin = authAdmin;
        _auditLog = auditLog;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> GetMeAsync(CancellationToken cancellationToken)
    {
        var profile = CurrentProfile;
        var totalQuotes = await _quotes.CountForUserAsync(profile.Id, cancellationToken);

        int? teamTotal = null;
        if (profile.Role.IsAtLeast(Role.Supervisor))
        {
            var teamCounts = await _quotes.CountByUserForTeamAsync(profile.Id, cancellationToken);
            teamTotal = teamCounts.Values.Sum();
        }

        return Ok(new ProfileResponse(
            profile.Id, profile.Name, profile.Position, profile.Email,
            profile.Role.ToDbString(), profile.Theme, profile.Language,
            totalQuotes, teamTotal, profile.MustChangePassword));
    }

    [HttpPatch("preferences")]
    public async Task<IActionResult> UpdatePreferencesAsync(
        [FromBody] UpdatePreferencesRequest request, CancellationToken cancellationToken)
    {
        var profile = CurrentProfile;

        if (!string.IsNullOrWhiteSpace(request.Theme))
        {
            if (request.Theme is not ("light" or "dark"))
                return BadRequest(new { error = "Tema inválido." });
            await _profiles.UpdateThemeAsync(profile.Id, request.Theme, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            if (request.Language is not ("pt-BR" or "es-MX" or "en-US"))
                return BadRequest(new { error = "Idioma inválido." });
            await _profiles.UpdateLanguageAsync(profile.Id, request.Language, cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var profile = CurrentProfile;

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest(new { error = "A nova senha precisa ter ao menos 8 caracteres." });

        try
        {
            await _authAdmin.UpdateUserAsync(profile.Id, newEmail: null, request.NewPassword, cancellationToken);
        }
        catch (SupabaseAdminException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }

        await _profiles.SetMustChangePasswordAsync(profile.Id, false, cancellationToken);
        await _auditLog.RecordAsync(
            profile.OrganizationId, profile.Id, AuditActions.PasswordChangedBySelf, profile.Id, metadata: null, cancellationToken);

        _logger.LogInformation("Usuário {UserId} trocou a própria senha.", profile.Id);

        return NoContent();
    }
}
