using System.Security.Claims;
using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Api.Authentication;

public sealed class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, CurrentUserAccessor accessor, IProfileRepository profileRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var subClaim = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(subClaim, out var userId))
            {
                accessor.UserId = userId;
                accessor.Profile = await profileRepository.GetByIdAsync(userId, context.RequestAborted);
                accessor.IsAuthenticatedWithoutProfile = accessor.Profile is null;

                if (accessor.Profile is { IsActive: false })
                {
                    await WriteForbiddenAsync(context, "ACCOUNT_DISABLED", "Esta conta foi desativada. Contate o administrador.");
                    return;
                }

                if (accessor.Profile is { MustChangePassword: true } && !IsExemptFromPasswordChangeGate(context.Request.Path))
                {
                    await WriteForbiddenAsync(context, "MUST_CHANGE_PASSWORD", "É necessário definir uma nova senha antes de continuar.");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool IsExemptFromPasswordChangeGate(PathString path) =>
        path.Equals("/api/me", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/api/me/change-password", StringComparison.OrdinalIgnoreCase);

    private static async Task WriteForbiddenAsync(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = code, message });
    }
}
