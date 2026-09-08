using Serilog;

namespace CotadorLogistico.Core.Logging;

/// <summary>
/// Configura o logger global (Serilog) usado pelo aplicativo inteiro —
/// tanto pelo servidor local (Kestrel) quanto pela janela WinForms.
///
/// Os logs existem para um propósito bem prático: quando alguém da equipe
/// disser "não funcionou", em vez de tentar adivinhar o que aconteceu,
/// dá pra pedir pra essa pessoa abrir Configurações → Exportar Logs e
/// mandar o arquivo. Ver <see cref="LogsDirectory"/>.
/// </summary>
public static class AppLogging
{
    /// <summary>Pasta onde os arquivos de log ficam gravados.</summary>
    public static string LogsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CotadorLogistico",
        "logs");

    /// <summary>
    /// Monta o logger global (Serilog.Log.Logger). Deve ser chamado uma
    /// única vez, o mais cedo possível na inicialização do aplicativo —
    /// antes de criar o servidor local ou qualquer janela.
    /// </summary>
    public static void Configure()
    {
        Directory.CreateDirectory(LogsDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            // Deixa os logs internos do ASP.NET Core (Kestrel, roteamento)
            // mais discretos — só o que realmente importa pro diagnóstico.
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(LogsDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
