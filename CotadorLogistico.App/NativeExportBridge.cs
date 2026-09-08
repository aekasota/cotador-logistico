using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows.Forms;
using CotadorLogistico.Core.Logging;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace CotadorLogistico.App;

public sealed class NativeExportBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly CoreWebView2 _coreWebView;

    public NativeExportBridge(CoreWebView2 coreWebView)
    {
        _coreWebView = coreWebView;
        _coreWebView.WebMessageReceived += OnWebMessageReceived;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = JsonSerializer.Deserialize<BridgeMessage>(e.WebMessageAsJson, JsonOptions);
            if (message is null)
                return;

            switch (message.Type)
            {
                case "export-file":
                    HandleExportFile(message);
                    break;
                case "export-logs":
                    HandleExportLogs();
                    break;
                default:
                    Log.Warning("Mensagem do WebView2 com tipo desconhecido: {Type}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falha ao processar mensagem recebida do WebView2.");
            SendResult(success: false, error: "Erro interno ao processar a exportação.");
        }
    }

    private void HandleExportFile(BridgeMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.FileName) || string.IsNullOrWhiteSpace(message.Base64Data))
        {
            SendResult(success: false, error: "Dados de exportação incompletos.");
            return;
        }

        var (filter, defaultExtension) = message.Kind switch
        {
            "xlsx" => ("Planilha do Excel (*.xlsx)|*.xlsx", "xlsx"),
            "png" => ("Imagem PNG (*.png)|*.png", "png"),
            _ => ("Todos os arquivos (*.*)|*.*", "")
        };

        using var dialog = new SaveFileDialog
        {
            FileName = message.FileName,
            Filter = filter,
            DefaultExt = defaultExtension,
            AddExtension = true
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            SendResult(success: false, cancelled: true);
            return;
        }

        try
        {
            var bytes = Convert.FromBase64String(message.Base64Data);
            File.WriteAllBytes(dialog.FileName, bytes);
            Log.Information("Arquivo exportado: {FilePath} ({Bytes} bytes).", dialog.FileName, bytes.Length);
            SendResult(success: true, filePath: dialog.FileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falha ao gravar o arquivo exportado em {FilePath}.", dialog.FileName);
            SendResult(success: false, error: "Não foi possível salvar o arquivo.");
        }
    }

    private void HandleExportLogs()
    {
        using var dialog = new SaveFileDialog
        {
            FileName = $"cotador-logistico-logs-{DateTime.Now:yyyy-MM-dd}.zip",
            Filter = "Arquivo ZIP (*.zip)|*.zip",
            DefaultExt = "zip",
            AddExtension = true
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            SendResult(success: false, cancelled: true);
            return;
        }

        try
        {
            if (!Directory.Exists(AppLogging.LogsDirectory) || Directory.GetFiles(AppLogging.LogsDirectory).Length == 0)
            {
                SendResult(success: false, error: "Ainda não há nenhum log registrado.");
                return;
            }

            if (File.Exists(dialog.FileName))
                File.Delete(dialog.FileName);

            ZipFile.CreateFromDirectory(AppLogging.LogsDirectory, dialog.FileName);
            Log.Information("Logs exportados para {FilePath}.", dialog.FileName);
            SendResult(success: true, filePath: dialog.FileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falha ao exportar os logs para {FilePath}.", dialog.FileName);
            SendResult(success: false, error: "Não foi possível exportar os logs.");
        }
    }

    private void SendResult(bool success, string? filePath = null, string? error = null, bool cancelled = false)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "export-result",
            success,
            filePath,
            error,
            cancelled
        });
        _coreWebView.PostWebMessageAsJson(payload);
    }

    private sealed record BridgeMessage(string Type, string? Kind, string? FileName, string? Base64Data);
}
