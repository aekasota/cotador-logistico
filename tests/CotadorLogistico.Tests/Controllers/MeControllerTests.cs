using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Api.Controllers;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CotadorLogistico.Tests.Controllers;

public sealed class MeControllerTests
{
    private static MeController MakeController(
        Profile actor, FakeProfileRepository? profiles = null, FakeQuoteRepository? quotes = null,
        FakeSupabaseAuthAdminClient? authAdmin = null, FakeAuditLogRepository? auditLog = null)
    {
        profiles ??= new FakeProfileRepository();
        profiles.Seed(actor);

        return new MeController(
            new FakeCurrentUserAccessor(actor), profiles, quotes ?? new FakeQuoteRepository(),
            authAdmin ?? new FakeSupabaseAuthAdminClient(), auditLog ?? new FakeAuditLogRepository(),
            NullLogger<MeController>.Instance);
    }

    [Fact]
    public async Task ChangePasswordAsync_SenhaValida_AtualizaNoAuthEDesligaMustChangePassword()
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var profiles = new FakeProfileRepository();
        var authAdmin = new FakeSupabaseAuthAdminClient();
        var controller = MakeController(actor, profiles, authAdmin: authAdmin);

        await profiles.SetMustChangePasswordAsync(actor.Id, true, CancellationToken.None);

        var result = await controller.ChangePasswordAsync(new ChangePasswordRequest("nova-senha-123"), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal((actor.Id, (string?)null, "nova-senha-123"), authAdmin.UpdateCalls.Single());
        var updated = await profiles.GetByIdAsync(actor.Id, CancellationToken.None);
        Assert.False(updated!.MustChangePassword);
    }

    [Fact]
    public async Task ChangePasswordAsync_SenhaCurta_RetornaBadRequestSemChamarOAuth()
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var authAdmin = new FakeSupabaseAuthAdminClient();
        var controller = MakeController(actor, authAdmin: authAdmin);

        var result = await controller.ChangePasswordAsync(new ChangePasswordRequest("curta"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(authAdmin.UpdateCalls);
    }

    [Fact]
    public async Task GetMeAsync_ExpoeMustChangePassword()
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(Role.Operator, mustChangePassword: true);
        var controller = MakeController(actor);

        var result = await controller.GetMeAsync(CancellationToken.None);
        var dto = Assert.IsType<OkObjectResult>(result.Result).Value as ProfileResponse;

        Assert.NotNull(dto);
        Assert.True(dto!.MustChangePassword);
    }
}
