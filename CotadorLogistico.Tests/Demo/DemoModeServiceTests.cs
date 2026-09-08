using CotadorLogistico.Core.Demo;
using CotadorLogistico.Core.Settings;
using CotadorLogistico.Tests.TestDoubles;
using Xunit;

namespace CotadorLogistico.Tests.Demo;

public sealed class DemoModeServiceTests
{
    [Fact]
    public void IsActive_QuandoTokenFrenetEhOCodigoMagico_RetornaTrue()
    {
        var settings = new FakeSettingsService(new AppSettingsData { FrenetToken = "--demomode" });
        var service = new DemoModeService(settings);

        Assert.True(service.IsActive);
    }

    [Fact]
    public void IsActive_ComTokenNormal_RetornaFalse()
    {
        var settings = new FakeSettingsService(new AppSettingsData { FrenetToken = "um-token-de-verdade" });
        var service = new DemoModeService(settings);

        Assert.False(service.IsActive);
    }

    [Fact]
    public void IsActive_SemNenhumToken_RetornaFalse()
    {
        var settings = new FakeSettingsService(new AppSettingsData());
        var service = new DemoModeService(settings);

        Assert.False(service.IsActive);
    }

    [Fact]
    public void IsActive_NaoLigaParaMaiusculasMinusculas()
    {
        // Facilita o dia a dia: alguém digitando "--DemoMode" por engano
        // não devia impedir a ativação.
        var settings = new FakeSettingsService(new AppSettingsData { FrenetToken = "--DEMOMODE" });
        var service = new DemoModeService(settings);

        Assert.True(service.IsActive);
    }
}
