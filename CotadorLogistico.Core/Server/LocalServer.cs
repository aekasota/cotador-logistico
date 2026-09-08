using System.Net;
using System.Net.Sockets;
using CotadorLogistico.Core.Demo;
using CotadorLogistico.Core.Server.ShippingProxies;
using CotadorLogistico.Core.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CotadorLogistico.Core.Server;

/// <summary>
/// Sobe um servidor HTTP local (Kestrel) responsável por três coisas:
///
///   1. Servir os arquivos estáticos da pasta wwwroot (o HTML/CSS/JS da tela)
///      — visualmente idêntico a abrir o arquivo direto, só que numa origem
///      http://127.0.0.1, não mais file://.
///
///   2. Expor endpoints de proxy para a Frenet e o Melhor Envio. O
///      JavaScript chama esses endpoints locais em vez das APIs externas
///      diretamente; quem realmente conversa com a Frenet/Melhor Envio é
///      este servidor C#. Como CORS é uma regra aplicada pelo navegador a
///      chamadas feitas *pelo navegador*, uma chamada servidor-a-servidor
///      simplesmente não esbarra nela.
///
///   3. Expor endpoints para ler e gravar as configurações do usuário
///      (tokens de API e tema claro/escuro), delegando o trabalho pesado
///      para <see cref="ISettingsService"/>.
///
/// Esta classe só enxerga abstrações (<see cref="IShippingApiProxy"/>,
/// <see cref="ISettingsService"/>) — nunca menciona "Frenet" ou "Melhor
/// Envio" nem sabe onde as configurações ficam salvas em disco. Quem decide
/// essas amarrações concretas é o composition root (Program.cs do projeto
/// de aplicativo), seguindo o Princípio da Inversão de Dependência.
/// </summary>
public sealed class LocalServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    /// <summary>Endereço local em que o servidor está escutando, ex.: "http://127.0.0.1:53214".</summary>
    public string BaseUrl { get; }

    private LocalServer(WebApplication app, string baseUrl)
    {
        _app = app;
        BaseUrl = baseUrl;
    }

    /// <summary>
    /// Monta o servidor: escolhe uma porta local livre, registra as
    /// dependências e define as rotas. Não inicia o servidor ainda — para
    /// isso, chame <see cref="StartAsync"/>.
    /// </summary>
    /// <param name="webRootPath">Caminho absoluto da pasta wwwroot a ser servida.</param>
    /// <param name="settingsService">Serviço de configurações já pronto para uso.</param>
    public static LocalServer Create(string webRootPath, ISettingsService settingsService)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            WebRootPath = webRootPath
        });

        // O app final roda dentro de uma janela WinForms, sem console — não
        // faz sentido nenhum log ir pro console. Em vez disso, tudo passa a
        // fluir pelo Serilog (configurado por AppLogging.Configure(), chamado
        // pelo composition root antes de criar o servidor), que grava num
        // arquivo local. É o que permite pedir "me manda o log" quando algo
        // dá errado, em vez de tentar adivinhar o que aconteceu.
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog();

        builder.Services.AddSingleton(settingsService);
        builder.Services.AddHttpClient(nameof(FrenetApiProxy));
        builder.Services.AddHttpClient(nameof(MelhorEnvioApiProxy));
        builder.Services.AddHttpClient(ExchangeRateClientName);
        builder.Services.AddSingleton<FrenetApiProxy>();
        builder.Services.AddSingleton<MelhorEnvioApiProxy>();

        // Modo Demonstração: ver DemoModeService e DemoAwareShippingApiProxy
        // para o racional completo. Em resumo, os endpoints abaixo passam a
        // depender da versão "decorada" dos proxies, que decide em tempo
        // real se repassa pra API de verdade ou devolve dados fabricados.
        builder.Services.AddSingleton<IDemoModeService, DemoModeService>();
        builder.Services.AddSingleton<IFakeQuoteGenerator, FakeQuoteGenerator>();
        builder.Services.AddSingleton<DemoAwareShippingApiProxy<FrenetApiProxy>>();
        builder.Services.AddSingleton<DemoAwareShippingApiProxy<MelhorEnvioApiProxy>>();

        var port = GetFreeTcpPort();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        MapSettingsEndpoints(app);
        app.MapPost("/api/frenet", ForwardToCarrierAsync<DemoAwareShippingApiProxy<FrenetApiProxy>>);
        app.MapPost("/api/melhorenvio", ForwardToCarrierAsync<DemoAwareShippingApiProxy<MelhorEnvioApiProxy>>);
        app.MapGet("/api/exchange-rates", GetExchangeRatesAsync);

        return new LocalServer(app, $"http://127.0.0.1:{port}");
    }

    private const string ExchangeRateClientName = "ExchangeRates";
    private const string ExchangeRateApiUrl = "https://economia.awesomeapi.com.br/json/last/USD-BRL,MXN-BRL";

    /// <summary>
    /// Repassa a cotação de câmbio (USD-BRL, MXN-BRL) de uma API pública
    /// gratuita. Existe pelo mesmo motivo dos proxies da Frenet/Melhor
    /// Envio: essa API não devolve os headers de CORS necessários para
    /// aceitar uma chamada direta feita pelo JavaScript da janela, então o
    /// servidor local faz a chamada em nome do front-end.
    ///
    /// Diferente dos proxies de frete, este não exige nenhuma configuração
    /// prévia — é uma API pública, sem token — por isso vive como um
    /// endpoint simples em vez de uma implementação de
    /// <see cref="IShippingApiProxy"/>.
    /// </summary>
    private static async Task<IResult> GetExchangeRatesAsync(IHttpClientFactory httpClientFactory, ILogger<LocalServer> logger)
    {
        try
        {
            var client = httpClientFactory.CreateClient(ExchangeRateClientName);
            var response = await client.GetAsync(ExchangeRateApiUrl);
            var body = await response.Content.ReadAsStringAsync();
            return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao buscar as taxas de câmbio.");
            return Results.Json(new { error = "Câmbio indisponível no momento." }, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Inicia o servidor em segundo plano (não bloqueia a thread que chamou).</summary>
    public Task StartAsync() => _app.StartAsync();

    /// <summary>Encerra o servidor de forma graciosa.</summary>
    public Task StopAsync() => _app.StopAsync();

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    /// <summary>
    /// Handler genérico compartilhado por qualquer transportadora: valida se
    /// está configurada, lê o corpo da requisição, repassa e devolve a
    /// resposta. Adicionar uma nova transportadora no futuro é só criar a
    /// classe que implementa <see cref="IShippingApiProxy"/>, registrá-la no
    /// DI e adicionar uma linha de app.MapPost — nada aqui precisa mudar.
    /// </summary>
    private static async Task<IResult> ForwardToCarrierAsync<TProxy>(
        HttpContext context, TProxy proxy, ILogger<LocalServer> logger)
        where TProxy : IShippingApiProxy
    {
        if (!proxy.IsConfigured)
        {
            logger.LogWarning("Cotação recusada: {Carrier} ainda não está configurada.", proxy.CarrierName);
            return Results.Json(
                new { error = $"A API da {proxy.CarrierName} ainda não foi configurada." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        string requestBody;
        using (var reader = new StreamReader(context.Request.Body))
            requestBody = await reader.ReadToEndAsync();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await proxy.ForwardAsync(requestBody, context.RequestAborted);
            stopwatch.Stop();
            logger.LogInformation(
                "Cotação {Carrier}: HTTP {StatusCode} em {ElapsedMs} ms.",
                proxy.CarrierName, result.StatusCode, stopwatch.ElapsedMilliseconds);
            return Results.Content(result.Body, result.ContentType, statusCode: result.StatusCode);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Falha ao contatar {Carrier} após {ElapsedMs} ms.", proxy.CarrierName, stopwatch.ElapsedMilliseconds);
            return Results.Json(
                new { error = $"Falha ao contatar {proxy.CarrierName}: {ex.Message}" },
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static void MapSettingsEndpoints(WebApplication app)
    {
        app.MapGet("/api/settings", (ISettingsService settingsService, IDemoModeService demoModeService) =>
            Results.Json(BuildSettingsStatus(settingsService.Get(), demoModeService)));

        app.MapPost("/api/settings", (
            SettingsUpdateRequest request,
            ISettingsService settingsService,
            IDemoModeService demoModeService,
            ILogger<LocalServer> logger) =>
        {
            settingsService.Update(new SettingsUpdate(request.FrenetToken, request.MelhorEnvioToken, request.Theme));

            // Nunca logar os tokens em si — só o fato de que algo mudou.
            logger.LogInformation(
                "Configurações atualizadas (frenetToken alterado: {FrenetChanged}, melhorEnvioToken alterado: {MeChanged}, tema: {Theme}).",
                !string.IsNullOrWhiteSpace(request.FrenetToken),
                !string.IsNullOrWhiteSpace(request.MelhorEnvioToken),
                request.Theme ?? "(inalterado)");

            return Results.Json(BuildSettingsStatus(settingsService.Get(), demoModeService));
        });
    }

    /// <summary>
    /// Molda a resposta pública de "status" das configurações. Note que os
    /// tokens em si NUNCA são devolvidos ao front-end — só um booleano
    /// dizendo se cada um está configurado. Depois de salvo, um token é
    /// "somente escrita" do ponto de vista da tela de Configurações, assim
    /// como a maioria dos sistemas trata campos de senha/API key.
    ///
    /// Em Modo Demonstração, as duas transportadoras são reportadas como
    /// configuradas (veja <see cref="DemoAwareShippingApiProxy{TReal}"/>),
    /// e <c>demoModeActive</c> avisa o front-end para exibir o aviso visual
    /// correspondente — importante para nunca deixar dúvida entre dados
    /// reais e fabricados numa apresentação.
    /// </summary>
    private static object BuildSettingsStatus(AppSettingsData data, IDemoModeService demoModeService) => new
    {
        frenetConfigured = data.IsFrenetConfigured || demoModeService.IsActive,
        melhorEnvioConfigured = data.IsMelhorEnvioConfigured || demoModeService.IsActive,
        theme = data.Theme,
        demoModeActive = demoModeService.IsActive
    };

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

/// <summary>Corpo aceito por POST /api/settings. Cada campo nulo significa "não alterar".</summary>
public sealed record SettingsUpdateRequest(string? FrenetToken, string? MelhorEnvioToken, string? Theme);
