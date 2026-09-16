using System.Net;
using CotadorLogistico.Core.Secrets;
using CotadorLogistico.Infrastructure.Shipping;
using CotadorLogistico.Tests.TestDoubles;

namespace CotadorLogistico.Tests.Shipping;

public sealed class MelhorEnvioApiProxyTests
{
    [Fact]
    public async Task IsConfiguredAsync_SemToken_RetornaFalse()
    {
        var secrets = new FakeSecretsStore();
        var proxy = new MelhorEnvioApiProxy(new StubHttpClientFactory(new StubHttpMessageHandler(new HttpResponseMessage())), secrets);

        Assert.False(await proxy.IsConfiguredAsync(FakeCurrentUserAccessor.DefaultUserId, CancellationToken.None));
    }

    [Fact]
    public async Task ForwardAsync_MontaAuthorizationBearerComOTokenPuro()
    {
        var secrets = new FakeSecretsStore(new() { [SecretKeys.MelhorEnvioToken] = "me-token-abc" });
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });
        var proxy = new MelhorEnvioApiProxy(new StubHttpClientFactory(handler), secrets);

        await proxy.ForwardAsync(FakeCurrentUserAccessor.DefaultUserId, "{}", CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("me-token-abc", handler.LastRequest.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task ForwardAsync_EnviaUrlEUserAgentCorretos()
    {
        var secrets = new FakeSecretsStore(new() { [SecretKeys.MelhorEnvioToken] = "qualquer" });
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });
        var proxy = new MelhorEnvioApiProxy(new StubHttpClientFactory(handler), secrets);

        await proxy.ForwardAsync(FakeCurrentUserAccessor.DefaultUserId, "{}", CancellationToken.None);

        Assert.Equal(
            "https://www.melhorenvio.com.br/api/v2/me/shipment/calculate",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("CotadorLogistico", handler.LastRequest.Headers.UserAgent.ToString());
    }
}
