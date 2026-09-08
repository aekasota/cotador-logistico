namespace CotadorLogistico.Core.Demo;

/// <summary>
/// Diz se o Modo Demonstração está ativo no momento.
///
/// Existe como interface para que quem precisa checar isso (o decorator
/// <see cref="Server.ShippingProxies.DemoAwareShippingApiProxy{TReal}"/>)
/// dependa de um contrato, não de "como" a ativação é detectada — Princípio
/// da Inversão de Dependência.
/// </summary>
public interface IDemoModeService
{
    /// <summary>True quando o app deve devolver cotações fabricadas em vez de consultar as APIs reais.</summary>
    bool IsActive { get; }
}

/// <summary>
/// Ativa o Modo Demonstração quando o usuário digita um "código" especial
/// no lugar do token da Frenet, na tela de Configurações.
///
/// Por que usar o próprio campo de token em vez de um botão dedicado: o
/// pedido original foi exatamente esse (um "código" digitado no campo da
/// Frenet), e isso tem uma vantagem prática — não precisa de nenhuma tela,
/// switch ou opção nova para alguém tropeçar sem querer. Quem não conhece
/// o código não ativa o modo demonstração sem querer.
/// </summary>
public sealed class DemoModeService : IDemoModeService
{
    /// <summary>
    /// Valor que, se salvo como token da Frenet, ativa o Modo Demonstração.
    /// Fica público para o restante do código (e os testes) referenciarem
    /// em vez de duplicar a string mágica em vários lugares.
    /// </summary>
    public const string TriggerValue = "--demomode";

    private readonly Settings.ISettingsService _settingsService;

    public DemoModeService(Settings.ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public bool IsActive =>
        string.Equals(_settingsService.Get().FrenetToken, TriggerValue, StringComparison.OrdinalIgnoreCase);
}
