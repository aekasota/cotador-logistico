using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Api.Controllers;
using CotadorLogistico.Api.Services;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Core.Secrets;
using CotadorLogistico.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CotadorLogistico.Tests.Controllers;

public sealed class SettingsControllerTests
{
    private static SettingsController MakeController(Profile actor, FakeSecretsStore? secrets = null, FakeUserIntegrationsRepository? audit = null)
    {
        secrets ??= new FakeSecretsStore();
        audit ??= new FakeUserIntegrationsRepository();
        return new SettingsController(
            new FakeCurrentUserAccessor(actor),
            secrets,
            audit,
            new IntegrationStatusReader(secrets, audit),
            NullLogger<SettingsController>.Instance);
    }

    [Fact]
    public async Task GetStatusAsync_Operator_ConsegueLerOProprioStatus()
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var secrets = new FakeSecretsStore(new() { [SecretKeys.FrenetToken] = "abc" }, userId: actor.Id);
        var controller = MakeController(actor, secrets);

        var result = await controller.GetStatusAsync(CancellationToken.None);
        var response = Assert.IsType<OkObjectResult>(result.Result).Value as SettingsStatusResponse;

        Assert.NotNull(response);
        Assert.True(response!.FrenetConfigured);
        Assert.False(response.MelhorEnvioConfigured);
    }

    [Theory]
    [InlineData(Role.Operator)]
    [InlineData(Role.Supervisor)]
    [InlineData(Role.Owner)]
    public async Task UpdateAsync_QualquerRole_ConsegueConfigurarAPropriaIntegracao(Role role)
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(role);
        var secrets = new FakeSecretsStore();
        var controller = MakeController(actor, secrets);
        var request = new UpdateSettingsRequest("novo-token-frenet", null, null);

        await controller.UpdateAsync(request, CancellationToken.None);

        Assert.Equal("novo-token-frenet", await secrets.GetAsync(actor.Id, SecretKeys.FrenetToken, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_CampoVazioOuNulo_NaoAlteraOTokenExistente()
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var secrets = new FakeSecretsStore(new() { [SecretKeys.FrenetToken] = "token-original" }, userId: actor.Id);
        var controller = MakeController(actor, secrets);
        var request = new UpdateSettingsRequest(FrenetToken: "", MelhorEnvioToken: null, GeminiApiKey: null);

        await controller.UpdateAsync(request, CancellationToken.None);

        Assert.Equal("token-original", await secrets.GetAsync(actor.Id, SecretKeys.FrenetToken, CancellationToken.None));
    }

    [Fact]
    public async Task GetStatusAsync_DoisUsuariosDiferentes_NuncaVeemAsChavesUmDoOutro()
    {
        var usuarioA = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var usuarioB = FakeCurrentUserAccessor.MakeProfile(Role.Operator, organizationId: usuarioA.OrganizationId);
        var secrets = new FakeSecretsStore();
        await secrets.SetAsync(usuarioA.Id, SecretKeys.FrenetToken, "token-do-usuario-a", CancellationToken.None);

        var controllerA = MakeController(usuarioA, secrets);
        var controllerB = MakeController(usuarioB, secrets);

        var statusA = Assert.IsType<OkObjectResult>((await controllerA.GetStatusAsync(CancellationToken.None)).Result).Value as SettingsStatusResponse;
        var statusB = Assert.IsType<OkObjectResult>((await controllerB.GetStatusAsync(CancellationToken.None)).Result).Value as SettingsStatusResponse;

        Assert.True(statusA!.FrenetConfigured);
        Assert.False(statusB!.FrenetConfigured);
    }

    [Fact]
    public async Task UpdateAsync_NuncaAlteraAIntegracaoDeOutroUsuario()
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var outroUsuarioId = Guid.NewGuid();
        var secrets = new FakeSecretsStore();
        var controller = MakeController(actor, secrets);

        await controller.UpdateAsync(new UpdateSettingsRequest("token-do-ator", null, null), CancellationToken.None);

        Assert.Null(await secrets.GetAsync(outroUsuarioId, SecretKeys.FrenetToken, CancellationToken.None));
    }

    [Fact]
    public async Task GetStatusAsync_RespostaNuncaContemOValorDoToken()
    {
        var actor = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var secrets = new FakeSecretsStore(new() { [SecretKeys.FrenetToken] = "segredo-nunca-deve-aparecer" }, userId: actor.Id);
        var controller = MakeController(actor, secrets);

        var result = await controller.GetStatusAsync(CancellationToken.None);
        var json = System.Text.Json.JsonSerializer.Serialize(
            (Assert.IsType<OkObjectResult>(result.Result).Value as SettingsStatusResponse)!);

        Assert.DoesNotContain("segredo-nunca-deve-aparecer", json);
    }
}
