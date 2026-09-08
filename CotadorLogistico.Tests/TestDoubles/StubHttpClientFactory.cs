namespace CotadorLogistico.Tests.TestDoubles;

/// <summary>
/// <see cref="IHttpClientFactory"/> falso que sempre devolve o mesmo
/// <see cref="HttpClient"/>, construído sobre um <see cref="StubHttpMessageHandler"/>.
/// Evita qualquer chamada de rede real durante os testes.
/// </summary>
internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public StubHttpClientFactory(StubHttpMessageHandler handler)
    {
        _client = new HttpClient(handler);
    }

    public HttpClient CreateClient(string name) => _client;
}
