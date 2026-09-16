namespace CotadorLogistico.Infrastructure.Configuration;

public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRates";

    public string[] Currencies { get; set; } = [];

    public string ApiBaseUrl { get; set; } = "https://api.frankfurter.dev/v2";

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan StaleAfter { get; set; } = TimeSpan.FromHours(24);
}
