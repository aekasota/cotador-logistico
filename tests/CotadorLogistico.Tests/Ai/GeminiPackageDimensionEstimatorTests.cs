using System.Net;
using System.Text;
using CotadorLogistico.Core.Secrets;
using CotadorLogistico.Infrastructure.Ai;
using CotadorLogistico.Infrastructure.Configuration;
using CotadorLogistico.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CotadorLogistico.Tests.Ai;

public sealed class GeminiPackageDimensionEstimatorTests
{
    private static GeminiPackageDimensionEstimator MakeEstimator(
        FakeSecretsStore? secrets = null, StubHttpMessageHandler? handler = null)
    {
        secrets ??= new FakeSecretsStore(new() { [SecretKeys.GeminiApiKey] = "fake-key" });
        handler ??= new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        return new GeminiPackageDimensionEstimator(
            new StubHttpClientFactory(handler),
            secrets,
            Options.Create(new GeminiOptions()),
            NullLogger<GeminiPackageDimensionEstimator>.Instance);
    }

    [Fact]
    public async Task EstimateAsync_DescricaoVazia_RecusaSemChamarAApi()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("não deveria chamar a rede"));
        var estimator = MakeEstimator(handler: handler);

        var result = await estimator.EstimateAsync(FakeCurrentUserAccessor.DefaultUserId, "", CancellationToken.None);

        Assert.False(result.Allowed);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task EstimateAsync_DescricaoMuitoLonga_RecusaSemChamarAApi()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("não deveria chamar a rede"));
        var estimator = MakeEstimator(handler: handler);

        var result = await estimator.EstimateAsync(FakeCurrentUserAccessor.DefaultUserId, new string('a', 501), CancellationToken.None);

        Assert.False(result.Allowed);
    }

    [Fact]
    public async Task EstimateAsync_GeminiNaoConfigurado_RecusaSemChamarAApi()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("não deveria chamar a rede"));
        var estimator = MakeEstimator(secrets: new FakeSecretsStore(), handler: handler);

        var result = await estimator.EstimateAsync(FakeCurrentUserAccessor.DefaultUserId, "Caixa de sapatos", CancellationToken.None);

        Assert.False(result.Allowed);
        Assert.Contains("não foi configurado", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseResponse_JsonValidoEDentroDoEscopo_DevolveSugestoes()
    {
        var estimator = MakeEstimator();
        var body = WrapAsGeminiEnvelope("""
            {"allowed":true,"suggestions":[{"lengthCm":20,"widthCm":15,"heightCm":10,"confidence":"medium","reason":"caixa de sapatos padrão"}],"warning":"Estimativa aproximada."}
            """);

        var result = estimator.ParseResponse(body);

        Assert.True(result.Allowed);
        Assert.Single(result.Suggestions);
        Assert.Equal(20, result.Suggestions[0].LengthCm);
    }

    [Fact]
    public void ParseResponse_AllowedTrueMasSemSugestoes_RecusaComoRedeDeSeguranca()
    {
        var estimator = MakeEstimator();
        var body = WrapAsGeminiEnvelope("""{"allowed":true,"suggestions":[],"warning":""}""");

        var result = estimator.ParseResponse(body);

        Assert.False(result.Allowed);
    }

    [Fact]
    public void ParseResponse_ForaDoEscopo_PreservaORecusoDoModelo()
    {
        var estimator = MakeEstimator();
        var body = WrapAsGeminiEnvelope(
            """{"allowed":false,"suggestions":[],"warning":"Esta ferramenta serve exclusivamente para estimar dimensões de produtos para transporte."}""");

        var result = estimator.ParseResponse(body);

        Assert.False(result.Allowed);
        Assert.Contains("exclusivamente", result.Warning);
    }

    [Fact]
    public void ParseResponse_SemWarning_AplicaOAvisoPadraoDeEstimativa()
    {
        var estimator = MakeEstimator();
        var body = WrapAsGeminiEnvelope("""
            {"allowed":true,"suggestions":[{"lengthCm":1,"widthCm":1,"heightCm":1,"confidence":"low","reason":"x"}]}
            """);

        var result = estimator.ParseResponse(body);

        Assert.False(string.IsNullOrWhiteSpace(result.Warning));
        Assert.Contains("estimad", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseResponse_JsonMalformado_RecusaEmVezDeLancarExcecao()
    {
        var estimator = MakeEstimator();

        var result = estimator.ParseResponse("isto não é um JSON válido");

        Assert.False(result.Allowed);
    }

    [Fact]
    public void ParseResponse_EnvelopeSemCandidates_RecusaEmVezDeLancarExcecao()
    {
        var estimator = MakeEstimator();

        var result = estimator.ParseResponse("""{"candidates":[]}""");

        Assert.False(result.Allowed);
    }

    private static string WrapAsGeminiEnvelope(string innerJsonText)
    {
        var escaped = innerJsonText.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
        return "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"" + escaped + "\"}]}}]}";
    }
}
