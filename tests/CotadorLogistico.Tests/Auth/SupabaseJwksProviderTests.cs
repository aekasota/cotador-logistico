using System.Net;
using CotadorLogistico.Infrastructure.Auth;
using CotadorLogistico.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace CotadorLogistico.Tests.Auth;

public sealed class SupabaseJwksProviderTests
{
    private const string SampleJwks = """
        {"keys":[{"kty":"RSA","kid":"test-key-1","use":"sig","alg":"RS256","n":"uqPMaPcBrS9gW8-lr0xMSbb4EYsGxiVmQVbslyM4VtmfQIygCwGoHgEsK5h4Yz5WlAAqtgMZHqa3C0j6v8d0Gw","e":"AQAB"}]}
        """;

    [Fact]
    public void ResolveSigningKeys_ComKidConhecido_NaoChamaARedeDeNovo()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SampleJwks) };
        });
        var provider = new SupabaseJwksProvider(
            new StubHttpClientFactory(handler), "https://example.supabase.co", NullLogger<SupabaseJwksProvider>.Instance);

        var firstCall = provider.ResolveSigningKeys("test-key-1").ToList();
        var secondCall = provider.ResolveSigningKeys("test-key-1").ToList();

        Assert.Single(firstCall);
        Assert.Single(secondCall);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ResolveSigningKeys_ComKidDesconhecido_ForcaUmaNovaBusca()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SampleJwks) };
        });
        var provider = new SupabaseJwksProvider(
            new StubHttpClientFactory(handler), "https://example.supabase.co", NullLogger<SupabaseJwksProvider>.Instance);

        provider.ResolveSigningKeys("test-key-1");
        provider.ResolveSigningKeys("kid-que-nao-existe");

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ResolveSigningKeys_FalhaDeRedeComCacheExistente_ReaproveitaOUltimoCacheValido()
    {
        var shouldFail = false;
        var handler = new StubHttpMessageHandler(_ =>
            shouldFail
                ? throw new HttpRequestException("rede fora do ar")
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SampleJwks) });
        var provider = new SupabaseJwksProvider(
            new StubHttpClientFactory(handler), "https://example.supabase.co", NullLogger<SupabaseJwksProvider>.Instance);

        provider.ResolveSigningKeys("test-key-1");
        shouldFail = true;

        var keys = provider.ResolveSigningKeys("outro-kid-desconhecido").ToList();

        Assert.Single(keys);
    }
}
