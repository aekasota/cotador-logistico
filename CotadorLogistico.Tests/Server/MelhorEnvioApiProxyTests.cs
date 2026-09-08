using System.Net;
using CotadorLogistico.Core.Server.ShippingProxies;
using CotadorLogistico.Core.Settings;
using CotadorLogistico.Tests.TestDoubles;
using Xunit;

namespace CotadorLogistico.Tests.Server;

public sealed class MelhorEnvioApiProxyTests
{
    [Fact]
    public void IsConfigured_SemToken_RetornaFalse()
    {
        var settings = new FakeSettingsService(new AppSettingsData { MelhorEnvioToken = "" });
        var proxy = new MelhorEnvioApiProxy(new StubHttpClientFactory(new StubHttpMessageHandler(new HttpResponseMessage())), settings);

        Assert.False(proxy.IsConfigured);
    }

    [Fact]
    public async Task ForwardAsync_MontaAuthorizationBearerComOTokenPuro()
    {
        // O usuário digita só o token, sem "Bearer " — a tela de Configurações
        // já mostra o prefixo fixo. Este teste garante que o proxy é quem
        // monta o header completo.
        var settings = new FakeSettingsService(new AppSettingsData { MelhorEnvioToken = "me-token-abc" });
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        });
        var proxy = new MelhorEnvioApiProxy(new StubHttpClientFactory(handler), settings);

        await proxy.ForwardAsync("{}", CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("me-token-abc", handler.LastRequest.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task ForwardAsync_EnviaUrlEUserAgentCorretos()
    {
        var settings = new FakeSettingsService(new AppSettingsData { MelhorEnvioToken = "qualquer" });
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        });
        var proxy = new MelhorEnvioApiProxy(new StubHttpClientFactory(handler), settings);

        await proxy.ForwardAsync("{}", CancellationToken.None);

        Assert.Equal(
            "https://www.melhorenvio.com.br/api/v2/me/shipment/calculate",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("CotadorLogistico", handler.LastRequest.Headers.UserAgent.ToString());
    }
}
