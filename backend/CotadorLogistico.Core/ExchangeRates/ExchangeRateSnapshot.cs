namespace CotadorLogistico.Core.ExchangeRates;

public sealed class ExchangeRateSnapshot
{
    public required string Currency { get; init; }
    public required decimal RateToBrl { get; init; }
    public required decimal RateFromBrl { get; init; }
    public required string Source { get; init; }
    public required DateOnly EffectiveDate { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public interface IExchangeRateClient
{
    Task<IReadOnlyDictionary<string, ExchangeRateQuote>> GetLatestRatesToBrlAsync(
        IReadOnlyCollection<string> currencies, CancellationToken cancellationToken);
}

public sealed record ExchangeRateQuote(string Currency, decimal RateToBrl, DateOnly Date);

public interface IExchangeRateRepository
{
    Task<IReadOnlyList<ExchangeRateSnapshot>> GetAllAsync(CancellationToken cancellationToken);

    Task UpsertAsync(ExchangeRateSnapshot snapshot, CancellationToken cancellationToken);
}
