using CotadorLogistico.Core.Settings;
using Xunit;

namespace CotadorLogistico.Tests.Settings;

/// <summary>
/// Testa <see cref="JsonFileSettingsService"/> apontando sempre para um
/// arquivo temporário — nunca para o %APPDATA% de verdade da máquina que
/// roda os testes.
/// </summary>
public sealed class JsonFileSettingsServiceTests : IDisposable
{
    private readonly string _tempFilePath;

    public JsonFileSettingsServiceTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"cotador-logistico-tests-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
            File.Delete(_tempFilePath);
    }

    [Fact]
    public void Get_QuandoArquivoNaoExiste_RetornaValoresPadrao()
    {
        var service = new JsonFileSettingsService(_tempFilePath);

        var data = service.Get();

        Assert.False(data.IsFrenetConfigured);
        Assert.False(data.IsMelhorEnvioConfigured);
        Assert.Equal("light", data.Theme);
    }

    [Fact]
    public void Update_ComToken_MarcaComoConfigurado()
    {
        var service = new JsonFileSettingsService(_tempFilePath);

        service.Update(new SettingsUpdate(FrenetToken: "abc123", MelhorEnvioToken: null, Theme: null));

        var data = service.Get();
        Assert.True(data.IsFrenetConfigured);
        Assert.Equal("abc123", data.FrenetToken);
        Assert.False(data.IsMelhorEnvioConfigured);
    }

    [Fact]
    public void Update_ComCampoNulo_NaoAlteraOValorExistente()
    {
        var service = new JsonFileSettingsService(_tempFilePath);
        service.Update(new SettingsUpdate("token-original", "me-token-original", "light"));

        // Envia só uma mudança de tema; os tokens não deveriam ser tocados.
        service.Update(new SettingsUpdate(FrenetToken: null, MelhorEnvioToken: null, Theme: "dark"));

        var data = service.Get();
        Assert.Equal("token-original", data.FrenetToken);
        Assert.Equal("me-token-original", data.MelhorEnvioToken);
        Assert.Equal("dark", data.Theme);
    }

    [Fact]
    public void Update_ComStringVazia_TambemNaoAlteraOValorExistente()
    {
        var service = new JsonFileSettingsService(_tempFilePath);
        service.Update(new SettingsUpdate("token-original", null, "light"));

        // Simula o front-end enviando "" quando o usuário deixa o campo em
        // branco na tela de Configurações (para manter o token atual).
        service.Update(new SettingsUpdate(FrenetToken: "", MelhorEnvioToken: null, Theme: null));

        var data = service.Get();
        Assert.Equal("token-original", data.FrenetToken);
    }

    [Fact]
    public void Update_Persiste_EmNovaInstanciaDoServico()
    {
        var first = new JsonFileSettingsService(_tempFilePath);
        first.Update(new SettingsUpdate("token-persistido", null, "dark"));

        // Simula o aplicativo sendo fechado e aberto de novo: uma nova
        // instância do serviço, mesmo arquivo em disco.
        var second = new JsonFileSettingsService(_tempFilePath);

        var data = second.Get();
        Assert.Equal("token-persistido", data.FrenetToken);
        Assert.Equal("dark", data.Theme);
    }

    [Fact]
    public void Get_NaoExpoeReferenciaInternaMutavel()
    {
        var service = new JsonFileSettingsService(_tempFilePath);
        service.Update(new SettingsUpdate("token-a", null, null));

        var snapshot = service.Get();
        snapshot.FrenetToken = "alterado-por-fora";

        // Mutar o objeto devolvido por Get() não pode afetar o estado interno.
        var dataDeVerdade = service.Get();
        Assert.Equal("token-a", dataDeVerdade.FrenetToken);
    }
}
