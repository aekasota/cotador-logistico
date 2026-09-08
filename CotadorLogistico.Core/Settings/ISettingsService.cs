namespace CotadorLogistico.Core.Settings;

/// <summary>
/// Abstração para ler e persistir as configurações do usuário.
///
/// O restante do sistema depende desta interface, não de
/// <see cref="JsonFileSettingsService"/> diretamente — Princípio da Inversão
/// de Dependência (o "D" do SOLID). Isso traz dois benefícios concretos:
///   1. Testes automatizados podem usar uma implementação em memória, sem
///      tocar no disco de verdade.
///   2. Se um dia o armazenamento mudar (ex.: um banco local, um serviço de
///      nuvem), só é preciso escrever uma nova classe que implemente esta
///      interface — nada que consome <see cref="ISettingsService"/> precisa
///      mudar (Princípio Aberto/Fechado).
/// </summary>
public interface ISettingsService
{
    /// <summary>Retorna uma cópia das configurações atuais.</summary>
    AppSettingsData Get();

    /// <summary>
    /// Aplica as alterações descritas em <paramref name="update"/> e persiste
    /// imediatamente. Cada propriedade nula em <paramref name="update"/>
    /// significa "manter o valor atual" — é assim que a tela de Configurações
    /// consegue, por exemplo, trocar só o tema sem exigir que o usuário
    /// redigite os tokens de API toda vez que abre a janela.
    /// </summary>
    void Update(SettingsUpdate update);
}

/// <summary>
/// Alterações solicitadas pelo usuário na tela de Configurações.
/// Um campo nulo (ou vazio) é tratado como "não alterar esse campo".
/// </summary>
public sealed record SettingsUpdate(string? FrenetToken, string? MelhorEnvioToken, string? Theme);
