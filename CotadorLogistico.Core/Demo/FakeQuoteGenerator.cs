using System.Globalization;
using System.Text.Json;
using CotadorLogistico.Core.Server.ShippingProxies;

namespace CotadorLogistico.Core.Demo;

/// <summary>
/// Gera respostas fabricadas, mas plausíveis, no formato exato que cada
/// transportadora devolveria de verdade — para que o front-end (que não
/// sabe, nem precisa saber, que está em Modo Demonstração) renderize os
/// resultados normalmente.
/// </summary>
public interface IFakeQuoteGenerator
{
    /// <param name="carrierName">Nome da transportadora, ex.: "Frenet" ou "Melhor Envio".</param>
    /// <param name="requestBody">O corpo da requisição original, usado só para extrair o CEP de destino.</param>
    ProxyResult Generate(string carrierName, string requestBody);
}

/// <inheritdoc cref="IFakeQuoteGenerator"/>
public sealed class FakeQuoteGenerator : IFakeQuoteGenerator
{
    private static readonly string[] FrenetCarriers =
        { "Jadlog .Package", "Jadlog .Com", "Correios SEDEX", "Correios PAC", "Total Express" };

    private static readonly string[] MelhorEnvioCarriers =
        { "Correios", "Jadlog", "Azul Cargo Express", "Loggi", "Buslog" };

    public ProxyResult Generate(string carrierName, string requestBody)
    {
        // Usa o CEP de destino como semente do gerador de números aleatórios:
        // o mesmo destino sempre devolve o mesmo resultado "aleatório" dentro
        // de uma mesma sessão, em vez de números diferentes a cada clique —
        // o que pareceria estranho numa demonstração ao vivo.
        var random = new Random(ExtractSeed(carrierName, requestBody));

        return carrierName == "Frenet"
            ? GenerateFrenetResponse(random)
            : GenerateMelhorEnvioResponse(random);
    }

    private static ProxyResult GenerateFrenetResponse(Random random)
    {
        var services = PickCarriers(FrenetCarriers, random).Select(carrier => new
        {
            Carrier = carrier,
            ServiceDescription = carrier,
            ShippingPrice = FormatPrice(GenerateRealisticPrice(random)),
            DeliveryTime = random.Next(2, 12).ToString(CultureInfo.InvariantCulture),
            Error = false
        });

        var json = JsonSerializer.Serialize(new { ShippingSevicesArray = services });
        return new ProxyResult(200, json);
    }

    private static ProxyResult GenerateMelhorEnvioResponse(Random random)
    {
        var services = PickCarriers(MelhorEnvioCarriers, random).Select(carrier => new
        {
            company = new { name = carrier },
            custom_price = FormatPrice(GenerateRealisticPrice(random)),
            custom_delivery_time = random.Next(2, 12)
        });

        var json = JsonSerializer.Serialize(services);
        return new ProxyResult(200, json);
    }

    /// <summary>Escolhe de 1 a 3 transportadoras da lista, sem repetir — imitando a variação da API real.</summary>
    private static IEnumerable<string> PickCarriers(string[] pool, Random random)
    {
        int count = random.Next(1, 4);
        return pool.OrderBy(_ => random.Next()).Take(count);
    }

    /// <summary>Preço entre R$ 15 e R$ 90, dentro da faixa realista de frete nacional.</summary>
    private static decimal GenerateRealisticPrice(Random random) =>
        Math.Round((decimal)(random.NextDouble() * 75 + 15), 2);

    private static string FormatPrice(decimal price) => price.ToString("F2", CultureInfo.InvariantCulture);

    private static int ExtractSeed(string carrierName, string requestBody)
    {
        try
        {
            using var document = JsonDocument.Parse(requestBody);
            var root = document.RootElement;

            var cep = carrierName == "Frenet"
                ? GetStringOrNull(root, "RecipientCEP")
                : GetStringOrNull(root.TryGetProperty("to", out var to) ? to : root, "postal_code");

            return string.IsNullOrEmpty(cep) ? Guid.NewGuid().GetHashCode() : cep.GetHashCode();
        }
        catch (JsonException)
        {
            // Corpo da requisição inesperado não pode derrubar a demonstração.
            return Guid.NewGuid().GetHashCode();
        }
    }

    private static string? GetStringOrNull(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
