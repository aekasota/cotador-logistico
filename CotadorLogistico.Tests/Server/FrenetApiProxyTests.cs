using System.Net;
using CotadorLogistico.Core.Server.ShippingProxies;
using CotadorLogistico.Core.Settings;
using CotadorLogistico.Tests.TestDoubles;
using Xunit;

namespace CotadorLogistico.Tests.Server;

public sealed class FrenetApiProxyTests
{
    [Fact]
    public void IsConfigured_SemToken_RetornaFalse()
    {
        var settings = new FakeSettingsService(new AppSettingsData { FrenetToken = "" });
        var proxy = new FrenetApiProxy(new StubHttpClientFactory(new StubHttpMessageHandler(new HttpResponseMessage())), settings);

        Assert.False(proxy.IsConfigured);
    }

    [Fact]
    public void IsConfigured_ComToken_RetornaTrue()
    {
        var settings = new FakeSettingsService(new AppSettingsData { FrenetToken = "abc" });
        var proxy = new FrenetApiProxy(new StubHttpClientFactory(new StubHttpMessageHandler(new HttpResponseMessage())), settings);

        Assert.True(proxy.IsConfigured);
    }

    [Fact]
    public async Task ForwardAsync_EnviaHeaderTokenEUrlCorretos()
    {
        var settings = new FakeSettingsService(new AppSettingsData { FrenetToken = "token-xyz" });
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ShippingSevicesArray\":[]}")
        });
        var proxy = new FrenetApiProxy(new StubHttpClientFactory(handler), settings);

        var result = await proxy.ForwardAsync("{\"SellerCEP\":\"01001000\"}", CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://api.frenet.com.br/shipping/quote", handler.LastRequest!.RequestUri!.ToString());

        var tokenEnviado = Assert.Single(handler.LastRequest.Headers.GetValues("token"));
        Assert.Equal("token-xyz", tokenEnviado);
    }

    [Fact]
    public async Task ForwardAsync_RepassaOStatusCodeDaRespostaDaFrenet()
    {
        var settings = new FakeSettingsService(new AppSettingsData { FrenetToken = "qualquer" });
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":\"token invalido\"}")
        });
        var proxy = new FrenetApiProxy(new StubHttpClientFactory(handler), settings);

        var result = await proxy.ForwardAsync("{}", CancellationToken.None);

        // O proxy não deve "engolir" nem reinterpretar erros da Frenet —
        // só repassar exatamente o que ela respondeu.
        Assert.Equal(401, result.StatusCode);
        Assert.Contains("token invalido", result.Body);
    }
}
