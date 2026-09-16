using CotadorLogistico.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CotadorLogistico.Tests.Configuration;

public sealed class ExchangeRateOptionsBindingTests
{
    private static ExchangeRateOptions Bind(Dictionary<string, string?>? configValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues ?? new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<ExchangeRateOptions>()
            .Bind(configuration.GetSection(ExchangeRateOptions.SectionName))
            .PostConfigure(options =>
            {
                if (options.Currencies.Length == 0) options.Currencies = ["USD", "MXN"];
            });

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ExchangeRateOptions>>().Value;
    }

    [Fact]
    public void ComCurrenciesNaConfiguracao_NaoDuplica()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["ExchangeRates:Currencies:0"] = "USD",
            ["ExchangeRates:Currencies:1"] = "MXN",
        });

        Assert.Equal(new[] { "USD", "MXN" }, options.Currencies);
    }

    [Fact]
    public void SemCurrenciesNaConfiguracao_UsaODefault()
    {
        var options = Bind(configValues: null);

        Assert.Equal(new[] { "USD", "MXN" }, options.Currencies);
    }

    [Fact]
    public void ComUmaUnicaMoedaNaConfiguracao_RespeitaSoEla()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["ExchangeRates:Currencies:0"] = "USD",
        });

        Assert.Equal(new[] { "USD" }, options.Currencies);
    }
}
