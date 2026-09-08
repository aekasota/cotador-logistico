using System.Text.Json.Serialization;

namespace CotadorLogistico.Core.Settings;

/// <summary>
/// Configurações do usuário que precisam sobreviver a reinicializações do
/// aplicativo: os tokens das transportadoras e a preferência de tema.
/// Este objeto é serializado como JSON em disco por <see cref="JsonFileSettingsService"/>.
/// </summary>
public sealed class AppSettingsData
{
    /// <summary>Token de autenticação da API da Frenet (vai no header customizado "token").</summary>
    public string FrenetToken { get; set; } = string.Empty;

    /// <summary>
    /// Token de autenticação da API do Melhor Envio, SEM o prefixo "Bearer ".
    /// O prefixo é adicionado automaticamente na hora de montar o header
    /// Authorization — o usuário só precisa colar o token puro na tela de
    /// Configurações.
    /// </summary>
    public string MelhorEnvioToken { get; set; } = string.Empty;

    /// <summary>Tema visual preferido: "light" ou "dark".</summary>
    public string Theme { get; set; } = "light";

    /// <summary>Indica se já existe um token da Frenet configurado.</summary>
    [JsonIgnore]
    public bool IsFrenetConfigured => !string.IsNullOrWhiteSpace(FrenetToken);

    /// <summary>Indica se já existe um token do Melhor Envio configurado.</summary>
    [JsonIgnore]
    public bool IsMelhorEnvioConfigured => !string.IsNullOrWhiteSpace(MelhorEnvioToken);
}
