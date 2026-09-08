using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CotadorLogistico.App;

public sealed class MainForm : Form
{
    private readonly string _serverBaseUrl;
    private readonly WebView2 _webView;
    private NativeExportBridge? _exportBridge;

    public MainForm(string serverBaseUrl)
    {
        _serverBaseUrl = serverBaseUrl;

        Text = "Cotador Logístico";
        Width = 1180;
        Height = 860;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 560);

        _webView = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_webView);

        Load += async (_, _) => await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {

        await _webView.EnsureCoreWebView2Async();

        var coreWebView = _webView.CoreWebView2
            ?? throw new InvalidOperationException("Falha ao inicializar o WebView2.");

        LockDownBrowserSurface(coreWebView);
        _exportBridge = new NativeExportBridge(coreWebView);

        coreWebView.Navigate(_serverBaseUrl);
    }

    private static void LockDownBrowserSurface(CoreWebView2 coreWebView)
    {
        var settings = coreWebView.Settings;
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;
    }
}
