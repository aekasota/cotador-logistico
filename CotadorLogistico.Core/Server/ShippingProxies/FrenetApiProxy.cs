using System.Net.Http.Headers;
using System.Text;
using CotadorLogistico.Core.Settings;

namespace CotadorLogistico.Core.Server.ShippingProxies;

/// <summary>
/// Repassa cotações para a API da Frenet (https://api.frenet.com.br).
/// A Frenet autentica através de um header HTTP próprio chamado "token"
/// (não é um Bearer token OAuth2 como o do Melhor Envio).
/// </summary>
public sealed class FrenetApiProxy : IShippingApiProxy
{
    private const string BaseUrl = "https://api.frenet.com.br/";
    private const string QuoteEndpoint = "shipping/quote";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsService _settingsService;

    public FrenetApiProxy(IHttpClientFactory httpClientFactory, ISettingsService settingsService)
    {
        _httpClientFactory = httpClientFactory;
        _settingsService = settingsService;
    }

    public string CarrierName => "Frenet";

    public bool IsConfigured => _settingsService.Get().IsFrenetConfigured;

    public async Task<ProxyResult> ForwardAsync(string requestBody, CancellationToken cancellationToken)
    {
        var token = _settingsService.Get().FrenetToken;

        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + QuoteEndpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("token", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = _httpClientFactory.CreateClient(nameof(FrenetApiProxy));
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new ProxyResult((int)response.StatusCode, body);
    }
}
