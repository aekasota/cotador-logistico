using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CotadorLogistico.Api.Controllers;

[Route("api/presence")]
public sealed class PresenceController : CotadorControllerBase
{
    private readonly IPresenceRepository _presence;

    public PresenceController(ICurrentUserAccessor currentUser, IPresenceRepository presence) : base(currentUser)
    {
        _presence = presence;
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> HeartbeatAsync([FromBody] HeartbeatRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is not (PresenceStatuses.Online or PresenceStatuses.Quoting))
            return BadRequest(new { error = "Status de presença inválido." });

        await _presence.HeartbeatAsync(CurrentProfile.Id, request.Status, cancellationToken);
        return NoContent();
    }
}
