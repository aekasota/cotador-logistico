namespace CotadorLogistico.Core.Server.ShippingProxies;

/// <summary>
/// Contrato comum para repassar uma cotação de frete a uma transportadora
/// externa (Frenet, Melhor Envio, ou qualquer outra que venha a ser
/// adicionada no futuro).
///
/// Esta interface é o que permite ao <see cref="LocalServer"/> não precisar
/// saber nada sobre Frenet ou Melhor Envio especificamente — ele só enxerga
/// "algo que sabe repassar uma cotação e dizer se está configurado".
/// Adicionar uma nova transportadora no futuro significa criar uma nova
/// classe que implemente esta interface, sem alterar nenhuma classe já
/// existente (Princípio Aberto/Fechado do SOLID). E como qualquer
/// implementação pode ser usada onde a interface é esperada, sem surpresas
/// de comportamento, também respeitamos o Princípio da Substituição de
/// Liskov.
/// </summary>
public interface IShippingApiProxy
{
    /// <summary>Nome de exibição da transportadora, usado em mensagens de erro.</summary>
    string CarrierName { get; }

    /// <summary>
    /// Indica se as credenciais necessárias já foram configuradas pelo
    /// usuário. Permite recusar a chamada de forma clara em vez de disparar
    /// uma requisição à API real fadada a falhar.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Recebe o payload exatamente como o JavaScript enviou, adiciona as
    /// credenciais necessárias e repassa para a API real da transportadora.
    /// A resposta (status HTTP + corpo) volta sem qualquer modificação, para
    /// que a lógica de renderização do front-end continue funcionando
    /// exatamente como quando o navegador chamava a API diretamente.
    /// </summary>
    Task<ProxyResult> ForwardAsync(string requestBody, CancellationToken cancellationToken);
}

/// <summary>Resultado de uma chamada repassada a uma transportadora externa.</summary>
public sealed record ProxyResult(int StatusCode, string Body, string ContentType = "application/json");
