using System.Net.Http.Headers;
using System.Text;
using CotadorLogistico.Core.Secrets;
using CotadorLogistico.Core.Shipping;

namespace CotadorLogistico.Infrastructure.Shipping;

public sealed class FrenetApiProxy : IShippingApiProxy
{
    private const string BaseUrl = "https://api.frenet.com.br/";
    private const string QuoteEndpoint = "shipping/quote";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecretsStore _secretsStore;

    public FrenetApiProxy(IHttpClientFactory httpClientFactory, ISecretsStore secretsStore)
    {
        _httpClientFactory = httpClientFactory;
        _secretsStore = secretsStore;
    }

    public string CarrierName => "Frenet";
    public string SettingsKey => SecretKeys.FrenetToken;

    public async Task<bool> IsConfiguredAsync(Guid userId, CancellationToken cancellationToken) =>
        await _secretsStore.IsConfiguredAsync(userId, SecretKeys.FrenetToken, cancellationToken);

    public async Task<ProxyResult> ForwardAsync(Guid userId, string requestBody, CancellationToken cancellationToken)
    {
        var token = await _secretsStore.GetAsync(userId, SecretKeys.FrenetToken, cancellationToken);

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
