using System.Globalization;
using System.Text.Json;
using CotadorLogistico.Core.Shipping;

namespace CotadorLogistico.Core.Demo;

public interface IFakeQuoteGenerator
{
    ProxyResult Generate(string carrierName, string requestBody);
}

public sealed class FakeQuoteGenerator : IFakeQuoteGenerator
{
    private static readonly string[] FrenetCarriers =
        { "Jadlog .Package", "Jadlog .Com", "Correios SEDEX", "Correios PAC", "Total Express" };

    private static readonly string[] MelhorEnvioCarriers =
        { "Correios", "Jadlog", "Azul Cargo Express", "Loggi", "Buslog" };

    public ProxyResult Generate(string carrierName, string requestBody)
    {
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

    private static IEnumerable<string> PickCarriers(string[] pool, Random random)
    {
        int count = random.Next(1, 4);
        return pool.OrderBy(_ => random.Next()).Take(count);
    }

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
