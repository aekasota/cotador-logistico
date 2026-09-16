using CotadorLogistico.Api.Authentication;
using CotadorLogistico.Api.Controllers;
using CotadorLogistico.Api.Services;
using CotadorLogistico.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CotadorLogistico.Tests;

public sealed class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://placeholder.supabase.co",
                ["Supabase:SecretKey"] = "placeholder-secret-key",
                ["Supabase:DbConnectionString"] = "Host=localhost;Port=5432;Database=placeholder;Username=placeholder;Password=placeholder",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddCotadorInfrastructure(configuration);
        services.AddScoped<CurrentUserAccessor>();
        services.AddScoped<ICurrentUserAccessor>(sp => sp.GetRequiredService<CurrentUserAccessor>());
        services.AddSingleton<IntegrationStatusReader>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void ContainerDeDependencias_ConstroiSemErros()
    {
        using var provider = BuildProvider();
        Assert.NotNull(provider);
    }

    public static IEnumerable<object[]> ControllerTypes() =>
        typeof(CotadorControllerBase).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(ControllerTypes))]
    public void Controller_ConsegueSerInstanciadoComSuasDependenciasReais(Type controllerType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var instance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, controllerType);

        Assert.NotNull(instance);
    }
}
