using System.Net.Http.Headers;
using System.Text;
using CotadorLogistico.Core.Secrets;
using CotadorLogistico.Core.Shipping;

namespace CotadorLogistico.Infrastructure.Shipping;

public sealed class MelhorEnvioApiProxy : IShippingApiProxy
{
    private const string BaseUrl = "https://www.melhorenvio.com.br/";
    private const string CalculateEndpoint = "api/v2/me/shipment/calculate";

    private const string UserAgent = "CotadorLogistico/2.0 (contato@suaempresa.com.br)";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecretsStore _secretsStore;

    public MelhorEnvioApiProxy(IHttpClientFactory httpClientFactory, ISecretsStore secretsStore)
    {
        _httpClientFactory = httpClientFactory;
        _secretsStore = secretsStore;
    }

    public string CarrierName => "Melhor Envio";
    public string SettingsKey => SecretKeys.MelhorEnvioToken;

    public async Task<bool> IsConfiguredAsync(Guid userId, CancellationToken cancellationToken) =>
        await _secretsStore.IsConfiguredAsync(userId, SecretKeys.MelhorEnvioToken, cancellationToken);

    public async Task<ProxyResult> ForwardAsync(Guid userId, string requestBody, CancellationToken cancellationToken)
    {
        var token = await _secretsStore.GetAsync(userId, SecretKeys.MelhorEnvioToken, cancellationToken);

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
