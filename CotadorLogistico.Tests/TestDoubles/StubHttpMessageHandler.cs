namespace CotadorLogistico.Tests.TestDoubles;

/// <summary>
/// Handler HTTP falso que nunca sai para a rede de verdade: devolve uma
/// resposta pré-definida e guarda a última requisição recebida, para os
/// testes conseguirem inspecionar exatamente quais headers/URL foram
/// montados pelos proxies.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public HttpRequestMessage? LastRequest { get; private set; }

    public StubHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_response);
    }
}
