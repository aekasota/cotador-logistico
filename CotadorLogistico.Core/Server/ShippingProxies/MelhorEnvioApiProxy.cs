using System.Net.Http.Headers;
using System.Text;
using CotadorLogistico.Core.Settings;

namespace CotadorLogistico.Core.Server.ShippingProxies;

/// <summary>
/// Repassa cotações para a API do Melhor Envio
/// (https://www.melhorenvio.com.br/api/v2/me/shipment/calculate).
/// Autentica via OAuth2 Bearer token e exige um header User-Agent
/// identificando a aplicação que está chamando — é uma exigência da própria
/// API deles, não uma escolha nossa.
/// </summary>
public sealed class MelhorEnvioApiProxy : IShippingApiProxy
{
    private const string BaseUrl = "https://www.melhorenvio.com.br/";
    private const string CalculateEndpoint = "api/v2/me/shipment/calculate";

    // TODO: troque pelo e-mail de contato real da sua empresa antes de
    // publicar. A própria documentação do Melhor Envio pede um User-Agent
    // que identifique a aplicação e um contato válido.
    private const string UserAgent = "CotadorLogistico/1.0 (contato@suaempresa.com.br)";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsService _settingsService;

    public MelhorEnvioApiProxy(IHttpClientFactory httpClientFactory, ISettingsService settingsService)
    {
        _httpClientFactory = httpClientFactory;
        _settingsService = settingsService;
    }

    public string CarrierName => "Melhor Envio";

    public bool IsConfigured => _settingsService.Get().IsMelhorEnvioConfigured;

    public async Task<ProxyResult> ForwardAsync(string requestBody, CancellationToken cancellationToken)
    {
        var token = _settingsService.Get().MelhorEnvioToken;

        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + CalculateEndpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _httpClientFactory.CreateClient(nameof(MelhorEnvioApiProxy));
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new ProxyResult((int)response.StatusCode, body);
    }
}
