using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Api.Controllers;
using CotadorLogistico.Api.Services;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Core.Secrets;
using CotadorLogistico.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CotadorLogistico.Tests.Controllers;

public sealed class AdminUsersControllerTests
{
    private static AdminUsersController MakeController(
        Profile actor, FakeProfileRepository? profiles = null, FakeSecretsStore? secrets = null,
        FakeUserIntegrationsRepository? audit = null, FakeAuditLogRepository? auditLog = null,
        FakeSupabaseAuthAdminClient? authAdmin = null)
    {
        profiles ??= new FakeProfileRepository();
        profiles.Seed(actor);
        secrets ??= new FakeSecretsStore();
        audit ??= new FakeUserIntegrationsRepository();
        auditLog ??= new FakeAuditLogRepository();
        authAdmin ??= new FakeSupabaseAuthAdminClient();

        return new AdminUsersController(
            new FakeCurrentUserAccessor(actor), profiles, secrets, audit, auditLog, authAdmin,
            new IntegrationStatusReader(secrets, audit), NullLogger<AdminUsersController>.Instance);
    }

    [Fact]
    public async Task ResetIntegrationAsync_Owner_InvalidaACredencialDoUsuario_SemNuncaDevolverOValor()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var operador = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: owner.OrganizationId);
        var secrets = new FakeSecretsStore();
        await secrets.SetAsync(operador.Id, SecretKeys.FrenetToken, "token-secreto-do-operador", CancellationToken.None);

        var profiles = new FakeProfileRepository();
        profiles.Seed(operador);
        var controller = MakeController(owner, profiles, secrets);

        var result = await controller.ResetIntegrationAsync(operador.Id, "frenet", CancellationToken.None);
        var json = System.Text.Json.JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value!);

        Assert.DoesNotContain("token-secreto-do-operador", json);
        Assert.Null(await secrets.GetAsync(operador.Id, SecretKeys.FrenetToken, CancellationToken.None));
        Assert.False(await secrets.IsConfiguredAsync(operador.Id, SecretKeys.FrenetToken, CancellationToken.None));
    }

    [Theory]
    [InlineData(Role.Operator)]
    [InlineData(Role.Supervisor)]
    public async Task ResetIntegrationAsync_NaoOwner_NaoTemPermissao(Role role)
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(role);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: actor.OrganizationId);
        var profiles = new FakeProfileRepository();
        profiles.Seed(alvo);
        var controller = MakeController(actor, profiles);

        await Assert.ThrowsAsync<InsufficientRoleException>(
            () => controller.ResetIntegrationAsync(alvo.Id, "frenet", CancellationToken.None));
    }

    [Fact]
    public async Task ResetIntegrationAsync_UsuarioDeOutraOrganizacao_RetornaNotFound()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var usuarioDeOutraEmpresa = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: Guid.NewGuid());
        var profiles = new FakeProfileRepository();
        profiles.Seed(usuarioDeOutraEmpresa);
        var controller = MakeController(owner, profiles);

        var result = await controller.ResetIntegrationAsync(usuarioDeOutraEmpresa.Id, "frenet", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ResetIntegrationAsync_ProviderDesconhecido_RetornaBadRequest()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: owner.OrganizationId);
        var profiles = new FakeProfileRepository();
        profiles.Seed(alvo);
        var controller = MakeController(owner, profiles);

        var result = await controller.ResetIntegrationAsync(alvo.Id, "correios", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetIntegrationsAsync_Owner_VeOStatusMasNuncaOValor()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: owner.OrganizationId);
        var secrets = new FakeSecretsStore();
        await secrets.SetAsync(alvo.Id, SecretKeys.GeminiApiKey, "chave-secreta", CancellationToken.None);
        var profiles = new FakeProfileRepository();
        profiles.Seed(alvo);
        var controller = MakeController(owner, profiles, secrets);

        var result = await controller.GetIntegrationsAsync(alvo.Id, CancellationToken.None);
        var json = System.Text.Json.JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result).Value!);

        Assert.Contains("\"geminiConfigured\":true", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chave-secreta", json);
    }

    [Fact]
    public async Task GetIntegrationsAsync_Supervisor_NaoTemPermissao()
    {
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: supervisor.OrganizationId);
        var profiles = new FakeProfileRepository();
        profiles.Seed(alvo);
        var controller = MakeController(supervisor, profiles);

        await Assert.ThrowsAsync<InsufficientRoleException>(
            () => controller.GetIntegrationsAsync(alvo.Id, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserAsync_Owner_AlteraCargoEMantemOResto()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: owner.OrganizationId);
        var profiles = new FakeProfileRepository();
        profiles.Seed(alvo);
        var controller = MakeController(owner, profiles);

        var result = await controller.UpdateUserAsync(alvo.Id, new UpdateUserAdminRequest(null, "Novo Cargo", null, null, null), CancellationToken.None);
        var dto = Assert.IsType<OkObjectResult>(result).Value as AdminUserDto;

        Assert.NotNull(dto);
        Assert.Equal("Novo Cargo", dto!.Position);
        Assert.Equal(alvo.Name, dto.Name);
    }

    [Fact]
    public async Task UpdateUserAsync_TrocaParaSupervisor_LimpaOSupervisorId()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var supervisorAtual = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor, organizationId: owner.OrganizationId);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, supervisorId: supervisorAtual.Id, organizationId: owner.OrganizationId);
        var profiles = new FakeProfileRepository();
        profiles.Seed(supervisorAtual);
        profiles.Seed(alvo);
        var controller = MakeController(owner, profiles);

        var result = await controller.UpdateUserAsync(alvo.Id, new UpdateUserAdminRequest(null, null, null, null, "SUPERVISOR"), CancellationToken.None);
        var dto = Assert.IsType<OkObjectResult>(result).Value as AdminUserDto;

        Assert.Equal("SUPERVISOR", dto!.Role);
        Assert.Null(dto.SupervisorId);
    }

    [Fact]
    public async Task UpdateUserAsync_NaoAceitaPromoverAOwner()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: owner.OrganizationId);
        var profiles = new FakeProfileRepository();
        profiles.Seed(alvo);
        var controller = MakeController(owner, profiles);

        var result = await controller.UpdateUserAsync(alvo.Id, new UpdateUserAdminRequest(null, null, null, null, "OWNER"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUserAsync_NuncaAlteraAPropriaConta()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var controller = MakeController(owner);

        var result = await controller.UpdateUserAsync(owner.Id, new UpdateUserAdminRequest("Outro Nome", null, null, null, null), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(Role.Operator)]
    [InlineData(Role.Supervisor)]
    public async Task UpdateUserAsync_NaoOwner_NaoTemPermissao(Role role)
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(role);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: actor.OrganizationId);
        var profiles = new FakeProfileRepository();
        profiles.Seed(alvo);
        var controller = MakeController(actor, profiles);

        await Assert.ThrowsAsync<InsufficientRoleException>(
            () => controller.UpdateUserAsync(alvo.Id, new UpdateUserAdminRequest("X", null, null, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task SetActiveAsync_Owner_DesativaOUsuario()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: owner.OrganizationId);
        var profiles = new FakeProfileRepository();
        profiles.Seed(alvo);
        var controller = MakeController(owner, profiles);

        var result = await controller.SetActiveAsync(alvo.Id, new SetActiveRequest(false), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var updated = await profiles.GetByIdAsync(alvo.Id, CancellationToken.None);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task SetActiveAsync_NuncaDesativaAPropriaConta()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var controller = MakeController(owner);

        var result = await controller.SetActiveAsync(owner.Id, new SetActiveRequest(false), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task IssueTemporaryPasswordAsync_Owner_MarcaMustChangePasswordEDevolveASenhaUmaVez()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var alvo = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: owner.OrganizationId);
        var profiles = new FakeProfileRepository();
        profiles.Seed(alvo);
        var authAdmin = new FakeSupabaseAuthAdminClient();
        var controller = MakeController(owner, profiles, authAdmin: authAdmin);

        var result = await controller.IssueTemporaryPasswordAsync(alvo.Id, CancellationToken.None);
        var dto = Assert.IsType<OkObjectResult>(result.Result).Value as TemporaryPasswordResponse;

        Assert.NotNull(dto);
        Assert.True(dto!.TemporaryPassword.Length >= 8);
        var updated = await profiles.GetByIdAsync(alvo.Id, CancellationToken.None);
        Assert.True(updated!.MustChangePassword);
        Assert.Equal((alvo.Id, (string?)null, dto.TemporaryPassword), authAdmin.UpdateCalls.Single());
    }
}
