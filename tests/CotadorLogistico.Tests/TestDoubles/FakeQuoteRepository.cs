using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Tests.TestDoubles;

internal sealed class FakeQuoteRepository : IQuoteRepository
{
    public List<(Quote Quote, IReadOnlyList<QuoteOption> Options)> Inserted { get; } = new();
    public Dictionary<Guid, int> CountsByUser { get; } = new();
    public List<CapitalAdvantage> Advantages { get; } = new();
    public List<IReadOnlyCollection<Guid>?> AdvantageCallsUserIds { get; } = new();
    public List<Guid> AdvantageCallsOrganizationId { get; } = new();

    public Task<Guid> InsertAsync(Quote quote, IReadOnlyList<QuoteOption> options, CancellationToken cancellationToken)
    {
        Inserted.Add((quote, options));
        return Task.FromResult(Guid.NewGuid());
    }

    public Task<int> CountForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(CountsByUser.GetValueOrDefault(userId));

    public Task<IReadOnlyDictionary<Guid, int>> CountByUserForTeamAsync(Guid supervisorId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(CountsByUser);

    public Task<IReadOnlyDictionary<Guid, int>> CountByUserForAllAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(CountsByUser);

    public Task<IReadOnlyList<CapitalAdvantage>> GetPriceAdvantageByDestinationAsync(
        Guid organizationId, IReadOnlyCollection<Guid>? userIds, CancellationToken cancellationToken)
    {
        AdvantageCallsOrganizationId.Add(organizationId);
        AdvantageCallsUserIds.Add(userIds);
        return Task.FromResult<IReadOnlyList<CapitalAdvantage>>(Advantages);
    }
}
