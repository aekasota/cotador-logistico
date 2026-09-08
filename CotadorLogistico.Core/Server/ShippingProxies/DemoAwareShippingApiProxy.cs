using CotadorLogistico.Core.Demo;

namespace CotadorLogistico.Core.Server.ShippingProxies;

/// <summary>
/// Decora qualquer <see cref="IShippingApiProxy"/> real: quando o Modo
/// Demonstração está ativo, devolve dados fabricados por
/// <see cref="IFakeQuoteGenerator"/> em vez de contatar a transportadora de
/// verdade; caso contrário, repassa a chamada para o proxy real normalmente.
///
/// Isso existe como decorator — em vez de um "if está em modo demo" dentro
/// de <see cref="FrenetApiProxy"/>/<see cref="MelhorEnvioApiProxy"/> — para
/// não misturar duas responsabilidades numa mesma classe (Princípio da
/// Responsabilidade Única). Nenhuma das duas classes reais precisou ser
/// alterada para o Modo Demonstração passar a existir (Princípio
/// Aberto/Fechado): o <see cref="Server.LocalServer"/> simplesmente passou
/// a apontar os endpoints para a versão decorada.
/// </summary>
public sealed class DemoAwareShippingApiProxy<TReal> : IShippingApiProxy
    where TReal : IShippingApiProxy
{
    private readonly TReal _realProxy;
    private readonly IDemoModeService _demoModeService;
    private readonly IFakeQuoteGenerator _fakeQuoteGenerator;

    public DemoAwareShippingApiProxy(TReal realProxy, IDemoModeService demoModeService, IFakeQuoteGenerator fakeQuoteGenerator)
    {
        _realProxy = realProxy;
        _demoModeService = demoModeService;
        _fakeQuoteGenerator = fakeQuoteGenerator;
    }

    public string CarrierName => _realProxy.CarrierName;

    // Em Modo Demonstração, as duas transportadoras "parecem" configuradas
    // mesmo sem nenhum token real — é assim que o app fica 100% funcional
    // para uma apresentação sem exigir nenhuma credencial de verdade.
    public bool IsConfigured => _demoModeService.IsActive || _realProxy.IsConfigured;

    public Task<ProxyResult> ForwardAsync(string requestBody, CancellationToken cancellationToken)
    {
        if (_demoModeService.IsActive)
            return Task.FromResult(_fakeQuoteGenerator.Generate(CarrierName, requestBody));

        return _realProxy.ForwardAsync(requestBody, cancellationToken);
    }
}
