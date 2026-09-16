using CotadorLogistico.Core.Domain;
using Dapper;
using Npgsql;

namespace CotadorLogistico.Infrastructure.Quotes;

public sealed class QuoteRepository : IQuoteRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public QuoteRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<Guid> InsertAsync(Quote quote, IReadOnlyList<QuoteOption> options, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        var quoteId = await conn.ExecuteScalarAsync<Guid>(new CommandDefinition("""
            insert into public.quotes (
                user_id, source_cep, destination_cep, destination_label,
                package_weight_kg, package_length_cm, package_width_cm, package_height_cm,
                package_quantity, declared_value_brl, comparison_mode, currency,
                exchange_rate_used, is_demo)
            values (
                @UserId, @SourceCep, @DestinationCep, @DestinationLabel,
                @PackageWeightKg, @PackageLengthCm, @PackageWidthCm, @PackageHeightCm,
                @PackageQuantity, @DeclaredValueBrl, @ComparisonMode, @Currency,
                @ExchangeRateUsed, @IsDemo)
            returning id
            """,
            quote, transaction: tx, cancellationToken: cancellationToken));

        foreach (var option in options)
        {
            await conn.ExecuteAsync(new CommandDefinition("""
                insert into public.quote_options (
                    quote_id, provider, carrier, service_name, service_code,
                    price_brl, delivery_days, is_winner_price, is_winner_time,
                    was_selected_in_comparison, raw_metadata_json)
                values (
                    @QuoteId, @Provider, @Carrier, @ServiceName, @ServiceCode,
                    @PriceBrl, @DeliveryDays, @IsWinnerPrice, @IsWinnerTime,
                    @WasSelectedInComparison, @RawMetadataJson::jsonb)
                """,
                new
                {
                    QuoteId = quoteId,
                    option.Provider,
                    option.Carrier,
                    option.ServiceName,
                    option.ServiceCode,
                    option.PriceBrl,
                    option.DeliveryDays,
                    option.IsWinnerPrice,
                    option.IsWinnerTime,
                    option.WasSelectedInComparison,
                    option.RawMetadataJson
                },
                transaction: tx, cancellationToken: cancellationToken));
        }

        await tx.CommitAsync(cancellationToken);
        return quoteId;
    }

    public async Task<int> CountForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "select count(*) from public.quotes where user_id = @userId and is_demo = false",
            new { userId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountByUserForTeamAsync(Guid supervisorId, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<(Guid UserId, int Count)>(new CommandDefinition("""
            select p.id as "UserId", count(q.id) as "Count"
            from public.profiles p
            left join public.quotes q on q.user_id = p.id and q.is_demo = false
            where p.supervisor_id = @supervisorId
            group by p.id
            """,
            new { supervisorId }, cancellationToken: cancellationToken));
        return rows.ToDictionary(r => r.UserId, r => r.Count);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountByUserForAllAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<(Guid UserId, int Count)>(new CommandDefinition("""
            select p.id as "UserId", count(q.id) as "Count"
            from public.profiles p
            left join public.quotes q on q.user_id = p.id and q.is_demo = false
            where p.organization_id = @organizationId
            group by p.id
            """,
            new { organizationId }, cancellationToken: cancellationToken));
        return rows.ToDictionary(r => r.UserId, r => r.Count);
    }

    public async Task<IReadOnlyList<CapitalAdvantage>> GetPriceAdvantageByDestinationAsync(
        Guid organizationId, IReadOnlyCollection<Guid>? userIds, CancellationToken cancellationToken)
    {
        const string sql = """
            with best_per_quote as (
                select
                    q.id as quote_id,
                    q.destination_label,
                    min(qo.price_brl) filter (where qo.provider = 'FRENET') as frenet_price,
                    min(qo.price_brl) filter (where qo.provider = 'MELHOR_ENVIO') as me_price
                from public.quotes q
                join public.quote_options qo on qo.quote_id = q.id
                join public.profiles p on p.id = q.user_id
                where q.is_demo = false
                  and q.comparison_mode = true
                  and q.destination_label is not null
                  and p.organization_id = @organizationId
                  and (@filterUsers = false or q.user_id = any(@userIds))
                group by q.id, q.destination_label
            )
            select
                destination_label as "DestinationLabel",
                avg(frenet_price) as "FrenetAvgPriceBrl",
                avg(me_price) as "MelhorEnvioAvgPriceBrl",
                count(*) as "SampleSize"
            from best_per_quote
            where frenet_price is not null and me_price is not null
            group by destination_label
            order by destination_label
            """;

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<AdvantageRow>(new CommandDefinition(sql,
            new { organizationId, filterUsers = userIds is not null, userIds = userIds?.ToArray() ?? Array.Empty<Guid>() },
            cancellationToken: cancellationToken));

        return rows.Select(r =>
        {
            var higher = Math.Max(r.FrenetAvgPriceBrl, r.MelhorEnvioAvgPriceBrl);
            var diff = r.MelhorEnvioAvgPriceBrl - r.FrenetAvgPriceBrl;
            var percent = higher == 0 ? 0 : Math.Round(diff / higher * 100m, 1);

            return new CapitalAdvantage(
                r.DestinationLabel, r.FrenetAvgPriceBrl, r.MelhorEnvioAvgPriceBrl, percent, r.SampleSize);
        }).ToList();
    }

    private sealed record AdvantageRow(string DestinationLabel, decimal FrenetAvgPriceBrl, decimal MelhorEnvioAvgPriceBrl, int SampleSize);
}
