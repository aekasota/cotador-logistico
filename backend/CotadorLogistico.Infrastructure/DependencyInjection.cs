using CotadorLogistico.Core.Ai;
using CotadorLogistico.Core.Demo;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Core.ExchangeRates;
using CotadorLogistico.Core.Secrets;
using CotadorLogistico.Core.Shipping;
using CotadorLogistico.Infrastructure.Ai;
using CotadorLogistico.Infrastructure.Auth;
using CotadorLogistico.Infrastructure.Configuration;
using CotadorLogistico.Infrastructure.Database;
using CotadorLogistico.Infrastructure.ExchangeRates;
using CotadorLogistico.Infrastructure.Presence;
using CotadorLogistico.Infrastructure.Profiles;
using CotadorLogistico.Infrastructure.Quotes;
using CotadorLogistico.Infrastructure.Secrets;
using CotadorLogistico.Infrastructure.Settings;
using CotadorLogistico.Infrastructure.Shipping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CotadorLogistico.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCotadorInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        Database.DapperTypeHandlers.RegisterOnce();

        services.AddOptions<SupabaseOptions>()
            .Bind(configuration.GetSection(SupabaseOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<SecretsOptions>().Bind(configuration.GetSection(SecretsOptions.SectionName));
        services.AddOptions<GeminiOptions>().Bind(configuration.GetSection(GeminiOptions.SectionName));
        services.AddOptions<ExchangeRateOptions>()
            .Bind(configuration.GetSection(ExchangeRateOptions.SectionName))
            .PostConfigure(options =>
            {
                if (options.Currencies.Length == 0) options.Currencies = ["USD", "MXN"];
            });

        services.AddOptions<SecurityOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                var raw = config[$"{SecurityOptions.SectionName}:AllowedEmailDomains"];
                if (!string.IsNullOrWhiteSpace(raw))
                    options.AllowedEmailDomains = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            });

        services.AddSingleton(sp =>
        {
            var supabase = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;
            return NpgsqlDataSourceFactory.Create(supabase.DbConnectionString);
        });

        services.AddHttpClient(nameof(FrenetApiProxy), c => c.Timeout = TimeSpan.FromSeconds(20));
        services.AddHttpClient(nameof(MelhorEnvioApiProxy), c => c.Timeout = TimeSpan.FromSeconds(20));
        services.AddHttpClient(nameof(FrankfurterExchangeRateClient), c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient(nameof(GeminiPackageDimensionEstimator), c => c.Timeout = TimeSpan.FromSeconds(25));
        services.AddHttpClient(nameof(SupabaseAuthAdminClient), c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient(nameof(SupabaseJwksProvider), c => c.Timeout = TimeSpan.FromSeconds(10));

        services.AddSingleton<IProfileRepository, ProfileRepository>();
        services.AddSingleton<IQuoteRepository, QuoteRepository>();
        services.AddSingleton<IPresenceRepository, PresenceRepository>();
        services.AddSingleton<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddSingleton<IUserIntegrationsRepository, UserIntegrationsRepository>();
        services.AddSingleton<IAuditLogRepository, AuditLogRepository>();

        services.AddSingleton<ISecretsStore>(sp =>
        {
            var mode = sp.GetRequiredService<IOptions<SecretsOptions>>().Value.Mode;
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();

            return mode switch
            {
                SecretsStoreMode.Aes => new AesGcmSecretsStore(dataSource, sp.GetRequiredService<IOptions<SecretsOptions>>()),
                _ => new VaultSecretsStore(dataSource)
            };
        });

        services.AddSingleton<FrenetApiProxy>();
        services.AddSingleton<MelhorEnvioApiProxy>();
        services.AddSingleton<IShippingApiProxy>(sp => sp.GetRequiredService<FrenetApiProxy>());
        services.AddSingleton<IShippingApiProxy>(sp => sp.GetRequiredService<MelhorEnvioApiProxy>());

        services.AddSingleton<IExchangeRateClient, FrankfurterExchangeRateClient>();
        services.AddHostedService<ExchangeRateUpdaterBackgroundService>();

        services.AddSingleton<IPackageDimensionEstimator, GeminiPackageDimensionEstimator>();

        services.AddSingleton<IFakeQuoteGenerator, FakeQuoteGenerator>();

        services.AddSingleton<ISupabaseAuthAdminClient, SupabaseAuthAdminClient>();
        services.AddSingleton(sp =>
        {
            var supabase = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;
            return new SupabaseJwksProvider(
                sp.GetRequiredService<IHttpClientFactory>(),
                supabase.Url,
                sp.GetRequiredService<ILogger<SupabaseJwksProvider>>());
        });

        return services;
    }
}
