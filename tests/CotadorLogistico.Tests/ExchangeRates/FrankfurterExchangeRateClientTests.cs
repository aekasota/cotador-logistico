using System.Net;
using System.Text;
using CotadorLogistico.Infrastructure.Configuration;
using CotadorLogistico.Infrastructure.ExchangeRates;
using CotadorLogistico.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CotadorLogistico.Tests.ExchangeRates;

public sealed class FrankfurterExchangeRateClientTests
{
    [Fact]
    public async Task GetLatestRatesToBrlAsync_InverteARelacaoDevolvidaPelaApi()
    {
        var json = """[{"date":"2026-09-14","base":"BRL","quote":"USD","rate":0.2},{"date":"2026-09-15","base":"BRL","quote":"MXN","rate":3.5}]""";
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = new FrankfurterExchangeRateClient(
            new StubHttpClientFactory(handler),
            Options.Create(new ExchangeRateOptions()));

        var rates = await client.GetLatestRatesToBrlAsync(new[] { "USD", "MXN" }, CancellationToken.None);

        Assert.Equal(5m, rates["USD"].RateToBrl);
        Assert.Equal(0.285714m, rates["MXN"].RateToBrl);
        Assert.Equal(new DateOnly(2026, 9, 14), rates["USD"].Date);
        Assert.Equal(new DateOnly(2026, 9, 15), rates["MXN"].Date);
    }

    [Fact]
    public async Task GetLatestRatesToBrlAsync_IgnoraORegistroIdentidadeDaMoedaBase()
    {
        var json = """[{"date":"2026-09-14","base":"BRL","quote":"BRL","rate":1},{"date":"2026-09-14","base":"BRL","quote":"USD","rate":0.2}]""";
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = new FrankfurterExchangeRateClient(
            new StubHttpClientFactory(handler),
            Options.Create(new ExchangeRateOptions()));

        var rates = await client.GetLatestRatesToBrlAsync(new[] { "USD" }, CancellationToken.None);

        Assert.False(rates.ContainsKey("BRL"));
        Assert.True(rates.ContainsKey("USD"));
    }

    [Fact]
    public async Task GetLatestRatesToBrlAsync_IgnoraTaxasZeroOuNegativas()
    {
        var json = """[{"date":"2026-09-14","base":"BRL","quote":"USD","rate":0},{"date":"2026-09-14","base":"BRL","quote":"MXN","rate":3.5}]""";
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = new FrankfurterExchangeRateClient(
            new StubHttpClientFactory(handler),
            Options.Create(new ExchangeRateOptions()));

        var rates = await client.GetLatestRatesToBrlAsync(new[] { "USD", "MXN" }, CancellationToken.None);

        Assert.False(rates.ContainsKey("USD"));
        Assert.True(rates.ContainsKey("MXN"));
    }

    [Fact]
    public async Task GetLatestRatesToBrlAsync_RespostaVazia_LancaExcecao()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        });
        var client = new FrankfurterExchangeRateClient(
            new StubHttpClientFactory(handler),
            Options.Create(new ExchangeRateOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetLatestRatesToBrlAsync(new[] { "USD" }, CancellationToken.None));
    }

    [Fact]
    public async Task GetLatestRatesToBrlAsync_HttpComErro_LancaExcecao()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = new FrankfurterExchangeRateClient(
            new StubHttpClientFactory(handler),
            Options.Create(new ExchangeRateOptions()));

        await Assert.ThrowsAnyAsync<HttpRequestException>(
            () => client.GetLatestRatesToBrlAsync(new[] { "USD" }, CancellationToken.None));
    }

    [Fact]
    public async Task GetLatestRatesToBrlAsync_ChamaOEndpointEQueryParamsCorretos()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"date":"2026-09-14","base":"BRL","quote":"USD","rate":0.2}]""", Encoding.UTF8, "application/json")
        });
        var client = new FrankfurterExchangeRateClient(
            new StubHttpClientFactory(handler),
            Options.Create(new ExchangeRateOptions()));

        await client.GetLatestRatesToBrlAsync(new[] { "USD" }, CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("/rates", uri);
        Assert.Contains("base=BRL", uri);
        Assert.Contains("quotes=USD", uri);
        Assert.DoesNotContain("/latest", uri);
    }
}
