using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CotadorLogistico.Api.Controllers;

[ApiController]
[Authorize]
public abstract class CotadorControllerBase : ControllerBase
{
    protected readonly ICurrentUserAccessor CurrentUser;

    protected CotadorControllerBase(ICurrentUserAccessor currentUser) => CurrentUser = currentUser;

    protected Profile CurrentProfile =>
        CurrentUser.Profile ?? throw new NoProfileException();

    protected void RequireRole(Role minimum)
    {
        if (!CurrentProfile.Role.IsAtLeast(minimum))
            throw new InsufficientRoleException(minimum);
    }
}

public sealed class NoProfileException : Exception
{
    public NoProfileException() : base("Conta sem perfil associado. Contate o administrador.") { }
}

public sealed class InsufficientRoleException(Role minimum) : Exception($"Esta ação exige o papel {minimum} ou superior.");
