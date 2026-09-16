using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Api.Controllers;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CotadorLogistico.Tests.Controllers;

public sealed class TeamControllerTests
{
    private static TeamController MakeController(
        Profile actor, FakeProfileRepository? profiles = null, FakePresenceRepository? presence = null,
        FakeQuoteRepository? quotes = null, FakeSupabaseAuthAdminClient? authAdmin = null)
    {
        profiles ??= new FakeProfileRepository();
        profiles.Seed(actor);

        return new TeamController(
            new FakeCurrentUserAccessor(actor),
            profiles,
            presence ?? new FakePresenceRepository(),
            quotes ?? new FakeQuoteRepository(),
            authAdmin ?? new FakeSupabaseAuthAdminClient(),
            new FakeAuditLogRepository(),
            NullLogger<TeamController>.Instance);
    }

    [Fact]
    public async Task GetTeamAsync_Operator_NaoTemPermissao()
    {
        var operator_ = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var controller = MakeController(operator_);

        await Assert.ThrowsAsync<InsufficientRoleException>(() => controller.GetTeamAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task GetTeamAsync_Supervisor_VeSoOProprioTime()
    {
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var meuOperador = FakeCurrentUserAccessor.MakeProfile(Role.Operator, supervisorId: supervisor.Id);
        var operadorDeOutroTime = FakeCurrentUserAccessor.MakeProfile(Role.Operator, supervisorId: Guid.NewGuid());

        var profiles = new FakeProfileRepository();
        profiles.Seed(meuOperador);
        profiles.Seed(operadorDeOutroTime);

        var controller = MakeController(supervisor, profiles);

        var result = await controller.GetTeamAsync(null, CancellationToken.None);
        var response = Assert.IsType<OkObjectResult>(result.Result).Value as TeamResponse;

        Assert.NotNull(response);
        Assert.Single(response!.Members);
        Assert.Equal(meuOperador.Id, response.Members[0].Id);
    }

    [Fact]
    public async Task GetTeamAsync_Owner_VeTodosMenosEleMesmo()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var operador = FakeCurrentUserAccessor.MakeProfile(Role.Operator, supervisorId: supervisor.Id);

        var profiles = new FakeProfileRepository();
        profiles.Seed(supervisor);
        profiles.Seed(operador);

        var controller = MakeController(owner, profiles);

        var result = await controller.GetTeamAsync(null, CancellationToken.None);
        var response = Assert.IsType<OkObjectResult>(result.Result).Value as TeamResponse;

        Assert.NotNull(response);
        Assert.Equal(2, response!.Members.Count);
        Assert.DoesNotContain(response.Members, m => m.Id == owner.Id);
    }

    [Fact]
    public async Task GetTeamAsync_Owner_NuncaVeMembrosDeOutraOrganizacao()
    {
        var organizacaoA = Guid.NewGuid();
        var organizacaoB = Guid.NewGuid();
        var ownerA = FakeCurrentUserAccessor.MakeProfile(Role.Owner, organizationId: organizacaoA);
        var funcionarioDaEmpresaA = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: organizacaoA);
        var funcionarioDaEmpresaB = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: organizacaoB);

        var profiles = new FakeProfileRepository();
        profiles.Seed(funcionarioDaEmpresaA);
        profiles.Seed(funcionarioDaEmpresaB);

        var controller = MakeController(ownerA, profiles);

        var result = await controller.GetTeamAsync(null, CancellationToken.None);
        var response = Assert.IsType<OkObjectResult>(result.Result).Value as TeamResponse;

        Assert.NotNull(response);
        Assert.Single(response!.Members);
        Assert.Equal(funcionarioDaEmpresaA.Id, response.Members[0].Id);
    }

    [Fact]
    public async Task CreateUserAsync_Supervisor_CriaOperatorSobSiMesmo_IgnorandoSupervisorIdDoRequest()
    {
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var profiles = new FakeProfileRepository();
        var controller = MakeController(supervisor, profiles);

        var request = new CreateTeamUserRequest(
            "Novo Operador", "Vendedor", "novo@empresa.com", "senha12345", Role: null,
            SupervisorId: Guid.NewGuid());

        var result = await controller.CreateUserAsync(request, CancellationToken.None);
        var response = Assert.IsType<OkObjectResult>(result.Result).Value as CreateTeamUserResponse;

        Assert.NotNull(response);
        var created = profiles.CreatedProfiles.Single();
        Assert.Equal(Role.Operator, created.Role);
        Assert.Equal(supervisor.Id, created.SupervisorId);
    }

    [Fact]
    public async Task CreateUserAsync_Supervisor_NaoConsegueCriarSupervisor()
    {
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var controller = MakeController(supervisor);

        var request = new CreateTeamUserRequest("Fulano", null, "fulano@empresa.com", "senha12345", "SUPERVISOR", null);

        var result = await controller.CreateUserAsync(request, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task CreateUserAsync_NinguemConsegueCriarOwner_NemOOwner()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var controller = MakeController(owner);

        var request = new CreateTeamUserRequest("Fulano", null, "fulano@empresa.com", "senha12345", "OWNER", null);

        var result = await controller.CreateUserAsync(request, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task CreateUserAsync_Owner_ConsegueCriarSupervisorSemSupervisorId()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var profiles = new FakeProfileRepository();
        var controller = MakeController(owner, profiles);

        var request = new CreateTeamUserRequest("Nova Supervisora", "Gerente", "supervisora@empresa.com", "senha12345", "SUPERVISOR", null);

        await controller.CreateUserAsync(request, CancellationToken.None);

        var created = profiles.CreatedProfiles.Single();
        Assert.Equal(Role.Supervisor, created.Role);
        Assert.Null(created.SupervisorId);
    }

    [Fact]
    public async Task CreateUserAsync_Owner_EscolhendoSupervisorExistente_AssociaCorretamente()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var outroSupervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var profiles = new FakeProfileRepository();
        profiles.Seed(outroSupervisor);

        var controller = MakeController(owner, profiles);
        var request = new CreateTeamUserRequest(
            "Operador Novo", null, "operador@empresa.com", "senha12345", "OPERATOR", outroSupervisor.Id);

        await controller.CreateUserAsync(request, CancellationToken.None);

        var created = profiles.CreatedProfiles.Single();
        Assert.Equal(outroSupervisor.Id, created.SupervisorId);
    }

    [Fact]
    public async Task CreateUserAsync_NovoUsuario_HerdaAOrganizacaoDeQuemCriou()
    {
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var profiles = new FakeProfileRepository();
        var controller = MakeController(supervisor, profiles);

        var request = new CreateTeamUserRequest("Novo Operador", null, "novo2@empresa.com", "senha12345", null, null);
        await controller.CreateUserAsync(request, CancellationToken.None);

        var created = profiles.CreatedProfiles.Single();
        Assert.Equal(supervisor.OrganizationId, created.OrganizationId);
    }

    [Fact]
    public async Task CreateUserAsync_Owner_NaoConsegueAssociarASupervisorDeOutraOrganizacao()
    {
        var organizacaoB = Guid.NewGuid();
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var supervisorDeOutraEmpresa = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor, organizationId: organizacaoB);
        var profiles = new FakeProfileRepository();
        profiles.Seed(supervisorDeOutraEmpresa);

        var controller = MakeController(owner, profiles);
        var request = new CreateTeamUserRequest(
            "Operador Novo", null, "operador2@empresa.com", "senha12345", "OPERATOR", supervisorDeOutraEmpresa.Id);

        var result = await controller.CreateUserAsync(request, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
        Assert.Empty(profiles.CreatedProfiles);
    }

    [Fact]
    public async Task CreateUserAsync_EmailJaExistente_RetornaConflict()
    {
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var profiles = new FakeProfileRepository();
        profiles.Seed(FakeCurrentUserAccessor.MakeProfile(Role.Operator), email: "ja-existe@empresa.com");

        var controller = MakeController(supervisor, profiles);
        var request = new CreateTeamUserRequest("Fulano", null, "ja-existe@empresa.com", "senha12345", null, null);

        var result = await controller.CreateUserAsync(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUserAsync_SenhaCurta_RetornaBadRequest()
    {
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var controller = MakeController(supervisor);
        var request = new CreateTeamUserRequest("Fulano", null, "fulano@empresa.com", "123", null, null);

        var result = await controller.CreateUserAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUserAsync_NuncaEnviaASenhaParaOLogger()
    {
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var authAdmin = new FakeSupabaseAuthAdminClient();
        var controller = MakeController(supervisor, authAdmin: authAdmin);
        var request = new CreateTeamUserRequest("Fulano", null, "fulano@empresa.com", "senha-secreta-123", null, null);

        await controller.CreateUserAsync(request, CancellationToken.None);

        Assert.Equal("senha-secreta-123", authAdmin.Calls.Single().Password);
    }
}
