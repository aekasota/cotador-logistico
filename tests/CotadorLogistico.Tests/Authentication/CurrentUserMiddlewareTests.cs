using System.Security.Claims;
using System.Text.Json;
using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;

namespace CotadorLogistico.Tests.Authentication;

public sealed class CurrentUserMiddlewareTests
{
    private static async Task<(DefaultHttpContext Context, CurrentUserAccessor Accessor)> InvokeAsync(
        Profile profile, string path = "/api/team")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", profile.Id.ToString())], authenticationType: "Bearer"));

        var profiles = new FakeProfileRepository();
        profiles.Seed(profile);
        var accessor = new CurrentUserAccessor();
        var nextCalled = false;
        var middleware = new CurrentUserMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context, accessor, profiles);
        context.Items["NextCalled"] = nextCalled;
        return (context, accessor);
    }

    [Fact]
    public async Task InvokeAsync_ContaAtiva_DeixaPassar()
    {
        var profile = FakeCurrentUserAccessor.MakeProfile(Role.Operator, isActive: true, mustChangePassword: false);
        var (context, _) = await InvokeAsync(profile);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.True((bool)context.Items["NextCalled"]!);
    }

    [Fact]
    public async Task InvokeAsync_ContaDesativada_Bloqueia403SemChamarONext()
    {
        var profile = FakeCurrentUserAccessor.MakeProfile(Role.Operator, isActive: false);
        var (context, _) = await InvokeAsync(profile);

        Assert.Equal(403, context.Response.StatusCode);
        Assert.False((bool)context.Items["NextCalled"]!);
        Assert.Contains("ACCOUNT_DISABLED", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task InvokeAsync_MustChangePassword_BloqueiaRotaComumMasLiberaApiMe()
    {
        var profile = FakeCurrentUserAccessor.MakeProfile(Role.Operator, mustChangePassword: true);

        var (blocked, _) = await InvokeAsync(profile, path: "/api/team");
        Assert.Equal(403, blocked.Response.StatusCode);
        Assert.Contains("MUST_CHANGE_PASSWORD", await ReadBodyAsync(blocked));

        var (allowedMe, _) = await InvokeAsync(profile, path: "/api/me");
        Assert.Equal(200, allowedMe.Response.StatusCode);
        Assert.True((bool)allowedMe.Items["NextCalled"]!);

        var (allowedChange, _) = await InvokeAsync(profile, path: "/api/me/change-password");
        Assert.Equal(200, allowedChange.Response.StatusCode);
        Assert.True((bool)allowedChange.Items["NextCalled"]!);
    }

    [Fact]
    public async Task InvokeAsync_ContaDesativadaEComSenhaTemporariaPendente_DesativacaoGanha()
    {
        var profile = FakeCurrentUserAccessor.MakeProfile(Role.Operator, isActive: false, mustChangePassword: true);
        var (context, _) = await InvokeAsync(profile, path: "/api/me/change-password");

        Assert.Equal(403, context.Response.StatusCode);
        Assert.Contains("ACCOUNT_DISABLED", await ReadBodyAsync(context));
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
