namespace CotadorLogistico.Core.Domain;

public interface IQuoteRepository
{
    Task<Guid> InsertAsync(Quote quote, IReadOnlyList<QuoteOption> options, CancellationToken cancellationToken);

    Task<int> CountForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> CountByUserForTeamAsync(Guid supervisorId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> CountByUserForAllAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CapitalAdvantage>> GetPriceAdvantageByDestinationAsync(
        Guid organizationId, IReadOnlyCollection<Guid>? userIds, CancellationToken cancellationToken);
}

public sealed record CapitalAdvantage(
    string DestinationLabel,
    decimal FrenetAvgPriceBrl,
    decimal MelhorEnvioAvgPriceBrl,
    decimal AdvantagePercent,
    int SampleSize);
