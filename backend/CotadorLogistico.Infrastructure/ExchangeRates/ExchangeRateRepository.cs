using CotadorLogistico.Core.ExchangeRates;
using Dapper;
using Npgsql;

namespace CotadorLogistico.Infrastructure.ExchangeRates;

public sealed class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ExchangeRateRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<ExchangeRateSnapshot>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<Row>(new CommandDefinition("""
            select currency as "Currency", rate_to_brl as "RateToBrl", rate_from_brl as "RateFromBrl",
                   source as "Source", effective_date as "EffectiveDate", updated_at as "UpdatedAt"
            from public.exchange_rates
            """,
            cancellationToken: cancellationToken));

        return rows.Select(r => new ExchangeRateSnapshot
        {
            Currency = r.Currency,
            RateToBrl = r.RateToBrl,
            RateFromBrl = r.RateFromBrl,
            Source = r.Source,
            EffectiveDate = DateOnly.FromDateTime(r.EffectiveDate),
            UpdatedAt = new DateTimeOffset(DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc))
        }).ToList();
    }

    public async Task UpsertAsync(ExchangeRateSnapshot snapshot, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition("""
            insert into public.exchange_rates (currency, rate_to_brl, rate_from_brl, source, effective_date, updated_at)
            values (@Currency, @RateToBrl, @RateFromBrl, @Source, @EffectiveDate, @UpdatedAt)
            on conflict (currency) do update
                set rate_to_brl = excluded.rate_to_brl,
                    rate_from_brl = excluded.rate_from_brl,
                    source = excluded.source,
                    effective_date = excluded.effective_date,
                    updated_at = excluded.updated_at
            """,
            snapshot, cancellationToken: cancellationToken));
    }

    private sealed class Row
    {
        public string Currency { get; set; } = "";
        public decimal RateToBrl { get; set; }
        public decimal RateFromBrl { get; set; }
        public string Source { get; set; } = "";
        public DateTime EffectiveDate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
