using System.Net;
using CotadorLogistico.Core.Secrets;
using CotadorLogistico.Infrastructure.Shipping;
using CotadorLogistico.Tests.TestDoubles;

namespace CotadorLogistico.Tests.Shipping;

public sealed class FrenetApiProxyTests
{
    [Fact]
    public async Task IsConfiguredAsync_SemToken_RetornaFalse()
    {
        var secrets = new FakeSecretsStore();
        var proxy = new FrenetApiProxy(new StubHttpClientFactory(new StubHttpMessageHandler(new HttpResponseMessage())), secrets);

        Assert.False(await proxy.IsConfiguredAsync(FakeCurrentUserAccessor.DefaultUserId, CancellationToken.None));
    }

    [Fact]
    public async Task IsConfiguredAsync_ComToken_RetornaTrue()
    {
        var secrets = new FakeSecretsStore(new() { [SecretKeys.FrenetToken] = "abc" });
        var proxy = new FrenetApiProxy(new StubHttpClientFactory(new StubHttpMessageHandler(new HttpResponseMessage())), secrets);

        Assert.True(await proxy.IsConfiguredAsync(FakeCurrentUserAccessor.DefaultUserId, CancellationToken.None));
    }

    [Fact]
    public async Task ForwardAsync_EnviaHeaderTokenEUrlCorretos()
    {
        var secrets = new FakeSecretsStore(new() { [SecretKeys.FrenetToken] = "token-xyz" });
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ShippingSevicesArray\":[]}")
        });
        var proxy = new FrenetApiProxy(new StubHttpClientFactory(handler), secrets);

        var result = await proxy.ForwardAsync(FakeCurrentUserAccessor.DefaultUserId, "{\"SellerCEP\":\"01001000\"}", CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://api.frenet.com.br/shipping/quote", handler.LastRequest!.RequestUri!.ToString());

        var tokenEnviado = Assert.Single(handler.LastRequest.Headers.GetValues("token"));
        Assert.Equal("token-xyz", tokenEnviado);
    }

    [Fact]
    public async Task ForwardAsync_RepassaOStatusCodeDaRespostaDaFrenet()
    {
        var secrets = new FakeSecretsStore(new() { [SecretKeys.FrenetToken] = "qualquer" });
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":\"token invalido\"}")
        });
        var proxy = new FrenetApiProxy(new StubHttpClientFactory(handler), secrets);

        var result = await proxy.ForwardAsync(FakeCurrentUserAccessor.DefaultUserId, "{}", CancellationToken.None);

        Assert.Equal(401, result.StatusCode);
        Assert.Contains("token invalido", result.Body);
    }
}
