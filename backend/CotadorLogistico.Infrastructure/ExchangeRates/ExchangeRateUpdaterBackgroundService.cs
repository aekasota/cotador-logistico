using CotadorLogistico.Core.ExchangeRates;
using CotadorLogistico.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CotadorLogistico.Infrastructure.ExchangeRates;

public sealed class ExchangeRateUpdaterBackgroundService : BackgroundService
{
    private const long AdvisoryLockKey = 918_273_645;

    private readonly IExchangeRateClient _client;
    private readonly IExchangeRateRepository _repository;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ExchangeRateOptions _options;
    private readonly ILogger<ExchangeRateUpdaterBackgroundService> _logger;

    public ExchangeRateUpdaterBackgroundService(
        IExchangeRateClient client,
        IExchangeRateRepository repository,
        NpgsqlDataSource dataSource,
        IOptions<ExchangeRateOptions> options,
        ILogger<ExchangeRateUpdaterBackgroundService> logger)
    {
        _client = client;
        _repository = repository;
        _dataSource = dataSource;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha inesperada no ciclo de atualização do câmbio.");
            }

            try
            {
                await Task.Delay(_options.CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var current = await _repository.GetAllAsync(cancellationToken);
        var staleOrMissing = _options.Currencies
            .Where(currency => IsStaleOrMissing(current, currency))
            .ToList();

        if (staleOrMissing.Count == 0)
        {
            _logger.LogDebug("Câmbio já está em dia, nenhuma chamada externa necessária.");
            return;
        }

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var lockCmd = conn.CreateCommand();
        lockCmd.CommandText = "select pg_try_advisory_lock(@key)";
        lockCmd.Parameters.AddWithValue("key", AdvisoryLockKey);
        var acquired = (bool)(await lockCmd.ExecuteScalarAsync(cancellationToken))!;

        if (!acquired)
        {
            _logger.LogInformation("Outra instância já está atualizando o câmbio neste ciclo; aguardando o próximo.");
            return;
        }

        try
        {
            _logger.LogInformation("Buscando câmbio atualizado para: {Currencies}", string.Join(", ", staleOrMissing));
            var rates = await _client.GetLatestRatesToBrlAsync(staleOrMissing, cancellationToken);

            foreach (var quote in rates.Values)
            {
                if (quote.RateToBrl <= 0) continue;

                await _repository.UpsertAsync(new ExchangeRateSnapshot
                {
                    Currency = quote.Currency,
                    RateToBrl = quote.RateToBrl,
                    RateFromBrl = Math.Round(1m / quote.RateToBrl, 6),
                    Source = "frankfurter.dev",
                    EffectiveDate = quote.Date,
                    UpdatedAt = DateTimeOffset.UtcNow
                }, cancellationToken);
            }

            _logger.LogInformation("Câmbio atualizado com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível atualizar o câmbio agora; mantendo o último valor válido.");
        }
        finally
        {
            await using var unlockCmd = conn.CreateCommand();
            unlockCmd.CommandText = "select pg_advisory_unlock(@key)";
            unlockCmd.Parameters.AddWithValue("key", AdvisoryLockKey);
            await unlockCmd.ExecuteScalarAsync(cancellationToken);
        }
    }

    private bool IsStaleOrMissing(IReadOnlyList<ExchangeRateSnapshot> current, string currency) =>
        IsStaleOrMissing(current, currency, _options.StaleAfter, DateTimeOffset.UtcNow);

    internal static bool IsStaleOrMissing(
        IReadOnlyList<ExchangeRateSnapshot> current, string currency, TimeSpan staleAfter, DateTimeOffset now)
    {
        var snapshot = current.FirstOrDefault(r => r.Currency == currency);
        if (snapshot is null) return true;
        return now - snapshot.UpdatedAt > staleAfter;
    }
}
