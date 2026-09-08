using System.Text.Json;

namespace CotadorLogistico.Core.Settings;

/// <summary>
/// Guarda as configurações em um arquivo JSON dentro da pasta de dados do
/// usuário do Windows (por padrão, "%APPDATA%\CotadorLogistico\settings.json").
///
/// Por que este local, e não outro:
///   - Não é o wwwroot/HTML: os tokens nunca aparecem no "Ver código-fonte"
///     da página nem em nenhuma resposta enviada ao navegador.
///   - Não é o localStorage do navegador: quem realmente usa os tokens é o
///     backend (para montar os headers de autenticação das transportadoras),
///     então guardá-los só no navegador não traria nenhum ganho de segurança
///     — só obrigaria a reenviá-los ao backend a cada cotação calculada.
///   - Não fica dentro da pasta do projeto/repositório: nunca há risco de um
///     token real ser commitado sem querer no Git.
///   - Não é o appsettings.json publicado junto do .exe: quando o executável
///     é publicado como "single file", esse arquivo fica embutido e é
///     extraído para uma pasta temporária a cada execução — não é um lugar
///     confiável para *gravar* alterações feitas em tempo de execução.
///   - %APPDATA% é exatamente o local que a própria Microsoft recomenda para
///     dados específicos do usuário que precisam sobreviver a atualizações
///     do aplicativo.
/// </summary>
public sealed class JsonFileSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;
    private readonly object _lock = new();
    private AppSettingsData _cache;

    /// <summary>Usa o local padrão: %APPDATA%\CotadorLogistico\settings.json.</summary>
    public JsonFileSettingsService() : this(GetDefaultSettingsFilePath())
    {
    }

    /// <summary>
    /// Construtor que aceita um caminho customizado. Existe principalmente
    /// para os testes automatizados apontarem para um arquivo temporário em
    /// vez de mexer no %APPDATA% real da máquina.
    /// </summary>
    public JsonFileSettingsService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
        _cache = LoadFromDisk();
    }

    public AppSettingsData Get()
    {
        lock (_lock)
        {
            // Devolve uma cópia para quem chamar não conseguir alterar o
            // cache interno "por fora", sem passar pelo Update().
            return new AppSettingsData
            {
                FrenetToken = _cache.FrenetToken,
                MelhorEnvioToken = _cache.MelhorEnvioToken,
                Theme = _cache.Theme
            };
        }
    }

    public void Update(SettingsUpdate update)
    {
        lock (_lock)
        {
            if (!string.IsNullOrWhiteSpace(update.FrenetToken))
                _cache.FrenetToken = update.FrenetToken.Trim();

            if (!string.IsNullOrWhiteSpace(update.MelhorEnvioToken))
                _cache.MelhorEnvioToken = update.MelhorEnvioToken.Trim();

            if (!string.IsNullOrWhiteSpace(update.Theme))
                _cache.Theme = update.Theme.Trim();

            SaveToDisk(_cache);
        }
    }

    private AppSettingsData LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                return new AppSettingsData();

            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AppSettingsData>(json) ?? new AppSettingsData();
        }
        catch
        {
            // Um settings.json corrompido não pode derrubar o aplicativo
            // inteiro — melhor voltar aos valores padrão e deixar o usuário
            // reconfigurar do que quebrar a abertura do app.
            return new AppSettingsData();
        }
    }

    private void SaveToDisk(AppSettingsData data)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(data, SerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private static string GetDefaultSettingsFilePath()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataFolder, "CotadorLogistico", "settings.json");
    }
}
