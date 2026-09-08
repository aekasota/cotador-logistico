using System.Text.Json;
using CotadorLogistico.Core.Demo;
using Xunit;

namespace CotadorLogistico.Tests.Demo;

public sealed class FakeQuoteGeneratorTests
{
    private readonly FakeQuoteGenerator _generator = new();

    [Fact]
    public void Generate_ParaFrenet_DevolveNoFormatoQueOFrontEndEspera()
    {
        var result = _generator.Generate("Frenet", """{"RecipientCEP":"01001000"}""");

        Assert.Equal(200, result.StatusCode);

        using var doc = JsonDocument.Parse(result.Body);
        var services = doc.RootElement.GetProperty("ShippingSevicesArray");
        Assert.True(services.GetArrayLength() >= 1);

        var first = services[0];
        Assert.True(first.TryGetProperty("Carrier", out _));
        Assert.True(first.TryGetProperty("ShippingPrice", out _));
        Assert.True(first.TryGetProperty("DeliveryTime", out _));
    }

    [Fact]
    public void Generate_ParaMelhorEnvio_DevolveNoFormatoQueOFrontEndEspera()
    {
        var result = _generator.Generate("Melhor Envio", """{"to":{"postal_code":"01001000"}}""");

        Assert.Equal(200, result.StatusCode);

        using var doc = JsonDocument.Parse(result.Body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() >= 1);

        var first = doc.RootElement[0];
        Assert.True(first.TryGetProperty("company", out var company));
        Assert.True(company.TryGetProperty("name", out _));
        Assert.True(first.TryGetProperty("custom_price", out _));
        Assert.True(first.TryGetProperty("custom_delivery_time", out _));
    }

    [Fact]
    public void Generate_MesmoCep_DevolveSempreOMesmoResultado()
    {
        // Determinismo importa numa demonstração ao vivo: recalcular o
        // mesmo destino não pode fazer o preço "pular" para outro valor.
        var body = """{"RecipientCEP":"01001000"}""";

        var primeiraChamada = _generator.Generate("Frenet", body);
        var segundaChamada = _generator.Generate("Frenet", body);

        Assert.Equal(primeiraChamada.Body, segundaChamada.Body);
    }

    [Fact]
    public void Generate_CepsDiferentes_DevolveResultadosDiferentes()
    {
        var resultadoSaoPaulo = _generator.Generate("Frenet", """{"RecipientCEP":"01001000"}""");
        var resultadoRio = _generator.Generate("Frenet", """{"RecipientCEP":"20010000"}""");

        Assert.NotEqual(resultadoSaoPaulo.Body, resultadoRio.Body);
    }

    [Fact]
    public void Generate_ComCorpoInesperado_NaoLancaExcecao()
    {
        // Um corpo de requisição inválido/vazio não pode derrubar uma
        // demonstração ao vivo — o gerador deve sempre devolver algo.
        var result = _generator.Generate("Frenet", "isto não é um JSON válido");

        Assert.Equal(200, result.StatusCode);
    }
}
