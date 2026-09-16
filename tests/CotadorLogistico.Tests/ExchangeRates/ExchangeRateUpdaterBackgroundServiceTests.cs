using CotadorLogistico.Core.ExchangeRates;
using CotadorLogistico.Infrastructure.ExchangeRates;

namespace CotadorLogistico.Tests.ExchangeRates;

public sealed class ExchangeRateUpdaterBackgroundServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(24);

    [Fact]
    public void IsStaleOrMissing_MoedaNuncaBuscada_RetornaTrue()
    {
        var current = Array.Empty<ExchangeRateSnapshot>();

        Assert.True(ExchangeRateUpdaterBackgroundService.IsStaleOrMissing(current, "USD", StaleAfter, Now));
    }

    [Fact]
    public void IsStaleOrMissing_AtualizadoHaMenosDeUmDia_RetornaFalse()
    {
        var current = new[] { Snapshot("USD", Now.AddHours(-1)) };

        Assert.False(ExchangeRateUpdaterBackgroundService.IsStaleOrMissing(current, "USD", StaleAfter, Now));
    }

    [Fact]
    public void IsStaleOrMissing_AtualizadoHaMaisDeUmDia_RetornaTrue()
    {
        var current = new[] { Snapshot("USD", Now.AddHours(-25)) };

        Assert.True(ExchangeRateUpdaterBackgroundService.IsStaleOrMissing(current, "USD", StaleAfter, Now));
    }

    [Fact]
    public void IsStaleOrMissing_SoConsideraAMoedaPedida()
    {
        var current = new[] { Snapshot("USD", Now.AddHours(-1)) };

        Assert.False(ExchangeRateUpdaterBackgroundService.IsStaleOrMissing(current, "USD", StaleAfter, Now));
        Assert.True(ExchangeRateUpdaterBackgroundService.IsStaleOrMissing(current, "MXN", StaleAfter, Now));
    }

    private static ExchangeRateSnapshot Snapshot(string currency, DateTimeOffset updatedAt) => new()
    {
        Currency = currency,
        RateToBrl = 5m,
        RateFromBrl = 0.2m,
        Source = "frankfurter.dev",
        EffectiveDate = DateOnly.FromDateTime(updatedAt.Date),
        UpdatedAt = updatedAt
    };
}
