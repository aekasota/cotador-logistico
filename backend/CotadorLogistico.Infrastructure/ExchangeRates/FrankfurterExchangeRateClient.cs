using System.Text.Json;
using CotadorLogistico.Core.ExchangeRates;
using CotadorLogistico.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace CotadorLogistico.Infrastructure.ExchangeRates;

public sealed class FrankfurterExchangeRateClient : IExchangeRateClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExchangeRateOptions _options;

    public FrankfurterExchangeRateClient(IHttpClientFactory httpClientFactory, IOptions<ExchangeRateOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<IReadOnlyDictionary<string, ExchangeRateQuote>> GetLatestRatesToBrlAsync(
        IReadOnlyCollection<string> currencies, CancellationToken cancellationToken)
    {
        var quotes = string.Join(",", currencies);
        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/rates?base=BRL&quotes={quotes}";

        var client = _httpClientFactory.CreateClient(nameof(FrankfurterExchangeRateClient));
        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var records = await JsonSerializer.DeserializeAsync<List<FrankfurterRateRecord>>(stream, JsonOptions, cancellationToken);

        if (records is null || records.Count == 0)
            throw new InvalidOperationException("A API de câmbio não devolveu nenhuma taxa.");

        var result = new Dictionary<string, ExchangeRateQuote>();
        foreach (var record in records)
        {
            if (string.Equals(record.Quote, "BRL", StringComparison.OrdinalIgnoreCase)) continue;
            if (record.Rate <= 0) continue;

            result[record.Quote] = new ExchangeRateQuote(
                record.Quote, Math.Round(1m / record.Rate, 6), DateOnly.Parse(record.Date));
        }

        return result;
    }

    private sealed record FrankfurterRateRecord(string Date, string Base, string Quote, decimal Rate);
}
