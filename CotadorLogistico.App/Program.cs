using System;
using System.IO;
using System.Windows.Forms;
using CotadorLogistico.Core.Logging;
using CotadorLogistico.Core.Server;
using CotadorLogistico.Core.Settings;
using Serilog;

namespace CotadorLogistico.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        AppLogging.Configure();

        try
        {
            Log.Information("Cotador Logístico iniciando");

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (_, e) => Log.Error(e.Exception, "Exceção não tratada na thread de UI.");

            var settingsService = new JsonFileSettingsService();
            var webRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");

            var server = LocalServer.Create(webRootPath, settingsService);
            server.StartAsync().GetAwaiter().GetResult();
            Log.Information("Servidor local no ar em {BaseUrl}.", server.BaseUrl);

            try
            {
                using var mainForm = new MainForm(server.BaseUrl);
                Application.Run(mainForm);
            }
            finally
            {
                server.StopAsync().GetAwaiter().GetResult();
                Log.Information("===== Cotador Logístico encerrado =====");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "O aplicativo encerrou inesperadamente.");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
