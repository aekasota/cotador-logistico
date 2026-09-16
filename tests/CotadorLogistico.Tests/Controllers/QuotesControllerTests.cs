using CotadorLogistico.Api.Contracts;
using CotadorLogistico.Api.Controllers;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CotadorLogistico.Tests.Controllers;

public sealed class QuotesControllerTests
{
    private static readonly PersistQuoteOptionRequest ValidOption = new(
        "FRENET", "Jadlog", "Jadlog .Package", null, 25.90m, 5, true, true, false);

    private static PersistQuoteRequest ValidRequest(PersistQuoteOptionRequest? option = null) => new(
        "01001000", "20010000", "Rio de Janeiro, RJ",
        PackageWeightKg: 1.5m, PackageLengthCm: 20, PackageWidthCm: 15, PackageHeightCm: 10,
        PackageQuantity: 1, DeclaredValueBrl: 100, ComparisonMode: false, Currency: "BRL",
        ExchangeRateUsed: null, Options: new[] { option ?? ValidOption });

    private static QuotesController MakeController(Profile actor, FakeQuoteRepository? quotes = null) =>
        new(new FakeCurrentUserAccessor(actor), quotes ?? new FakeQuoteRepository(), NullLogger<QuotesController>.Instance);

    [Fact]
    public async Task PersistAsync_RequisicaoValida_PersisteComUserIdDoAtorEIsDemoFalso()
    {
        var operador = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var quotes = new FakeQuoteRepository();
        var controller = MakeController(operador, quotes);

        var result = await controller.PersistAsync(ValidRequest(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        var (quote, _) = quotes.Inserted.Single();
        Assert.Equal(operador.Id, quote.UserId);
        Assert.False(quote.IsDemo);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abcdefgh")]
    public async Task PersistAsync_CepInvalido_RetornaBadRequest(string cepInvalido)
    {
        var controller = MakeController(FakeCurrentUserAccessor.MakeProfile(Role.Operator));
        var request = ValidRequest() with { SourceCep = cepInvalido };

        var result = await controller.PersistAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PersistAsync_PesoZero_RetornaBadRequest()
    {
        var controller = MakeController(FakeCurrentUserAccessor.MakeProfile(Role.Operator));
        var request = ValidRequest() with { PackageWeightKg = 0 };

        var result = await controller.PersistAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PersistAsync_SemOpcoes_RetornaBadRequest()
    {
        var controller = MakeController(FakeCurrentUserAccessor.MakeProfile(Role.Operator));
        var request = ValidRequest() with { Options = Array.Empty<PersistQuoteOptionRequest>() };

        var result = await controller.PersistAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PersistAsync_ProviderInvalido_RetornaBadRequest()
    {
        var controller = MakeController(FakeCurrentUserAccessor.MakeProfile(Role.Operator));
        var request = ValidRequest(ValidOption with { Provider = "CORREIOS_DIRETO" });

        var result = await controller.PersistAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPriceAdvantageAsync_Operator_SoPedeAsPropriasCotacoes()
    {
        var operador = FakeCurrentUserAccessor.MakeProfile(Role.Operator);
        var quotes = new FakeQuoteRepository();
        var controller = MakeController(operador, quotes);

        await controller.GetPriceAdvantageAsync(CancellationToken.None);

        var userIds = quotes.AdvantageCallsUserIds.Single();
        Assert.NotNull(userIds);
        Assert.Equal(new[] { operador.Id }, userIds);
    }

    [Fact]
    public async Task GetPriceAdvantageAsync_Owner_NaoFiltraPorUsuario()
    {
        var owner = FakeCurrentUserAccessor.MakeProfile(Role.Owner);
        var quotes = new FakeQuoteRepository();
        var controller = MakeController(owner, quotes);

        await controller.GetPriceAdvantageAsync(CancellationToken.None);

        Assert.Null(quotes.AdvantageCallsUserIds.Single());
    }

    [Fact]
    public async Task GetPriceAdvantageAsync_Supervisor_IncluiOProprioIdEOTime()
    {
        var supervisor = FakeCurrentUserAccessor.MakeProfile(Role.Supervisor);
        var quotes = new FakeQuoteRepository();
        var membroDoTime = Guid.NewGuid();
        quotes.CountsByUser[membroDoTime] = 3;

        var controller = MakeController(supervisor, quotes);
        await controller.GetPriceAdvantageAsync(CancellationToken.None);

        var userIds = quotes.AdvantageCallsUserIds.Single();
        Assert.NotNull(userIds);
        Assert.Contains(supervisor.Id, userIds!);
        Assert.Contains(membroDoTime, userIds!);
    }
}
