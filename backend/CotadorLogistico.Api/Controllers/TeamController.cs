using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CotadorLogistico.Api.Controllers;

[Route("api/team")]
public sealed class TeamController : CotadorControllerBase
{
    private readonly IProfileRepository _profiles;
    private readonly IPresenceRepository _presence;
    private readonly IQuoteRepository _quotes;
    private readonly ISupabaseAuthAdminClient _authAdmin;
    private readonly IAuditLogRepository _auditLog;
    private readonly ILogger<TeamController> _logger;

    public TeamController(
        ICurrentUserAccessor currentUser, IProfileRepository profiles, IPresenceRepository presence,
        IQuoteRepository quotes, ISupabaseAuthAdminClient authAdmin, IAuditLogRepository auditLog, ILogger<TeamController> logger)
        : base(currentUser)
    {
        _profiles = profiles;
        _presence = presence;
        _quotes = quotes;
        _authAdmin = authAdmin;
        _auditLog = auditLog;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<TeamResponse>> GetTeamAsync([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var actor = CurrentProfile;
        RequireRole(Role.Supervisor);

        var isOwner = actor.Role == Role.Owner;

        var members = isOwner
            ? await _profiles.GetAllAsync(actor.OrganizationId, query, cancellationToken)
            : await _profiles.GetTeamAsync(actor.Id, query, cancellationToken);

        members = members.Where(m => m.Id != actor.Id).ToList();

        var presenceRows = isOwner
            ? await _presence.GetAllPresenceAsync(actor.OrganizationId, cancellationToken)
            : await _presence.GetTeamPresenceAsync(actor.Id, cancellationToken);
        var presenceByUser = presenceRows.ToDictionary(p => p.UserId, p => p.EffectiveStatus);

        var quoteCounts = isOwner
            ? await _quotes.CountByUserForAllAsync(actor.OrganizationId, cancellationToken)
            : await _quotes.CountByUserForTeamAsync(actor.Id, cancellationToken);

        var dtos = members
            .Select(m => new TeamMemberDto(
                m.Id, m.Name, isOwner ? m.Email : null, m.Position, m.Role.ToDbString(), m.SupervisorId, m.IsActive,
                presenceByUser.GetValueOrDefault(m.Id, PresenceStatuses.Offline),
                quoteCounts.GetValueOrDefault(m.Id, 0)))
            .OrderBy(m => m.Name)
            .ToList();

        return Ok(new TeamResponse(dtos, dtos.Count, dtos.Sum(m => m.QuoteCount)));
    }

    [HttpPost("users")]
    [EnableRateLimiting(RateLimitPolicies.Admin)]
    public async Task<ActionResult<CreateTeamUserResponse>> CreateUserAsync(
        [FromBody] CreateTeamUserRequest request, CancellationToken cancellationToken)
    {
        var actor = CurrentProfile;
        RequireRole(Role.Supervisor);

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Nome é obrigatório." });
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(new { error = "E-mail inválido." });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { error = "A senha precisa ter ao menos 8 caracteres." });

        var (targetRole, supervisorId, roleError) = await ResolveRoleAndSupervisorAsync(actor, request, cancellationToken);
        if (roleError is not null) return StatusCode(StatusCodes.Status403Forbidden, new { error = roleError });

        if (await _profiles.EmailExistsAsync(request.Email, cancellationToken))
            return Conflict(new { error = "Já existe uma conta cadastrada com este e-mail." });

        var userId = await _authAdmin.CreateUserAsync(request.Email.Trim(), request.Password, cancellationToken);

        await _profiles.CreateAsync(new Profile
        {
            Id = userId,
            OrganizationId = actor.OrganizationId,
            Name = request.Name.Trim(),
            Position = request.Position?.Trim(),
            Role = targetRole,
            SupervisorId = supervisorId,
            Theme = "light",
            Language = "pt-BR"
        }, cancellationToken);

        await _auditLog.RecordAsync(
            actor.OrganizationId, actor.Id, AuditActions.UserCreated, userId,
            new Dictionary<string, string> { ["role"] = targetRole.ToDbString() }, cancellationToken);

        _logger.LogInformation(
            "Usuário {NewUserId} criado como {Role} por {ActorId}.", userId, targetRole, actor.Id);

        return Ok(new CreateTeamUserResponse(userId, request.Name.Trim(), request.Email.Trim(), targetRole.ToDbString()));
    }

    private async Task<(Role TargetRole, Guid? SupervisorId, string? Error)> ResolveRoleAndSupervisorAsync(
        Profile actor, CreateTeamUserRequest request, CancellationToken cancellationToken)
    {
        var requestedRoleText = string.IsNullOrWhiteSpace(request.Role) ? "OPERATOR" : request.Role.Trim().ToUpperInvariant();

        if (requestedRoleText == "OWNER")
            return (Role.Operator, null, "Não é possível criar uma conta OWNER pela aplicação.");

        if (requestedRoleText == "SUPERVISOR")
        {
            if (actor.Role != Role.Owner)
                return (Role.Operator, null, "Somente o OWNER pode criar contas SUPERVISOR.");
            return (Role.Supervisor, null, null);
        }

        if (requestedRoleText != "OPERATOR")
            return (Role.Operator, null, "Role inválido.");

        if (actor.Role == Role.Supervisor)
            return (Role.Operator, actor.Id, null);

        if (request.SupervisorId is null)
            return (Role.Operator, actor.Id, null);

        var chosenSupervisor = await _profiles.GetByIdAsync(request.SupervisorId.Value, cancellationToken);
        if (chosenSupervisor is null || chosenSupervisor.Role < Role.Supervisor
            || chosenSupervisor.OrganizationId != actor.OrganizationId)
            return (Role.Operator, null, "supervisorId precisa apontar para um SUPERVISOR ou OWNER existente.");

        return (Role.Operator, chosenSupervisor.Id, null);
    }
}
