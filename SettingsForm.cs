namespace Imprelia.PrintAgent;

/// <summary>
/// Logica del panel local del agente. El layout vive en SettingsForm.Designer.cs
/// para poder editarlo visualmente desde el diseñador de WinForms en Visual Studio.
/// </summary>
public partial class SettingsForm : Form
{
    private readonly AppConfig _config;
    private readonly int _startedPort;
    private readonly IPrinterDiscoveryService _discovery;
    private bool _loadingPrinters;

    public event EventHandler? ExitRequested;

    public SettingsForm(AppConfig config, int port)
    {
        _config = config;
        _config.EnsureDefaults();
        _startedPort = port;
        _discovery = new WindowsPrinterDiscoveryService(_config);

        InitializeComponent();
        Icon = AppAssets.AppIcon;
        logoPictureBox.Image = AppAssets.Logo;
        RefreshState();
    }

    public void RefreshState()
    {
        statusLabel.Text = $"Activo - puerto {_startedPort} - version {LocalServer.Version}";
        urlLabel.Text = $"URL local: http://localhost:{_startedPort}";
        portInput.Value = Math.Min(Math.Max(_config.Port, 1), 65535);
        corsCheck.Checked = _config.AllowCors;
        originsText.Text = string.Join(Environment.NewLine, _config.AllowedOrigins);
        autoStartCheck.Checked = StartupRegistry.IsEnabled();
        LoadPrinters();
        LoadRoutes();
        AddLog("Panel actualizado.");
    }

    private void LoadPrinters()
    {
        _loadingPrinters = true;
        var printers = _discovery.ListPrinters();

        printersList.Items.Clear();
        defaultPrinterCombo.Items.Clear();

        foreach (var p in printers)
        {
            defaultPrinterCombo.Items.Add(p.Name);
            var item = new ListViewItem(p.Name);
            item.SubItems.Add(p.IsDefault ? "Si" : "");
            item.SubItems.Add(p.Status);
            item.SubItems.Add(p.Type);
            item.SubItems.Add(p.SupportsPdf ? "Si" : "");
            item.SubItems.Add(p.SupportsRaw ? "Si" : "");
            item.SubItems.Add(p.SupportsZpl ? "Si" : "");
            printersList.Items.Add(item);
        }

        if (printers.Count == 0)
        {
            defaultPrinterCombo.Items.Add("(no hay impresoras instaladas)");
            defaultPrinterCombo.SelectedIndex = 0;
            defaultPrinterCombo.Enabled = false;
            _loadingPrinters = false;
            return;
        }

        defaultPrinterCombo.Enabled = true;
        var selected = _config.DefaultPrinter ?? _discovery.GetDefaultPrinter();
        var idx = defaultPrinterCombo.Items.IndexOf(selected);
        if (idx >= 0) defaultPrinterCombo.SelectedIndex = idx;
        _loadingPrinters = false;
    }

    private void LoadRoutes()
    {
        routesGrid.Rows.Clear();
        foreach (var route in _config.PrinterRoutes.Values.OrderBy(r => r.Purpose))
            routesGrid.Rows.Add(route.Purpose, route.PrinterName, route.ContentType);
    }

    private void SaveSelectedPrinter()
    {
        if (_loadingPrinters) return;
        if (defaultPrinterCombo.SelectedItem is string p && !p.StartsWith("("))
        {
            _config.DefaultPrinter = p;
            _config.Save();
            AddLog($"Impresora default: {p}");
        }
    }

    private void UseSelectedPrinterAsDefault()
    {
        var printer = SelectedPrinterFromList();
        if (string.IsNullOrWhiteSpace(printer)) return;
        _config.DefaultPrinter = printer;
        _config.Save();
        RefreshState();
        AddLog($"Impresora marcada como predeterminada: {printer}");
    }

    private void PrintTest()
    {
        var printer = SelectedPrinterFromList();
        if (string.IsNullOrWhiteSpace(printer))
            printer = _config.DefaultPrinter;

        if (string.IsNullOrWhiteSpace(printer))
        {
            MessageBox.Show("Primero elegi una impresora.", "Imprelia Print Agent", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var contentType = testTypeCombo.SelectedItem?.ToString() ?? "epos";
        string? err;
        if (contentType is "epos" or "raw")
            err = RawPrinter.SendBytes(printer, EscPosTest.Build(printer, "Ready"), "Prueba Imprelia");
        else if (contentType == "zpl")
            err = RawPrinter.SendBytes(printer, System.Text.Encoding.ASCII.GetBytes(BuildZplTest(printer)), "Prueba ZPL");
        else if (contentType == "text")
            err = PrintPlainText(printer);
        else if (contentType == "fiscal")
            err = "La prueba fiscal requiere configuracion del controlador fiscal.";
        else
            err = "La prueba PDF requiere enviar un PDF desde la aplicacion web.";

        if (err != null)
        {
            AddLog($"Error prueba {printer}: {err}");
            MessageBox.Show($"No se pudo imprimir:\n{err}", "Imprelia Print Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            AddLog($"Prueba enviada a {printer} ({contentType}).");
            MessageBox.Show("Prueba enviada a la impresora.", "Imprelia Print Agent", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }

    private void SaveRoutes()
    {
        var routes = new Dictionary<string, PrinterRoute>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in routesGrid.Rows)
        {
            if (row.IsNewRow) continue;
            var purpose = row.Cells[0].Value?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(purpose)) continue;
            routes[purpose] = new PrinterRoute
            {
                Purpose = purpose,
                PrinterName = row.Cells[1].Value?.ToString()?.Trim(),
                ContentType = string.IsNullOrWhiteSpace(row.Cells[2].Value?.ToString()) ? "epos" : row.Cells[2].Value!.ToString()!.Trim(),
            };
        }

        _config.PrinterRoutes = routes;
        _config.EnsureDefaults();
        _config.Save();
        LoadRoutes();
        AddLog("Rutas de impresion guardadas.");
    }

    private void SaveSettings()
    {
        var newPort = (int)portInput.Value;
        if (newPort != _startedPort && !IsPortAvailable(newPort))
        {
            MessageBox.Show($"El puerto {newPort} esta ocupado.", "Imprelia Print Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _config.Port = newPort;
        _config.AllowCors = corsCheck.Checked;
        _config.AllowedOrigins = originsText.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim())
            .Where(o => o.Length > 0)
            .ToList();
        _config.EnsureDefaults();
        _config.Save();
        AddLog("Configuracion guardada.");

        if (_config.Port != _startedPort)
            MessageBox.Show("Puerto guardado. Reinicie el agente para aplicar el nuevo listener.", "Imprelia Print Agent", MessageBoxButtons.OK, MessageBoxIcon.None);
    }

    private void RestoreDefaults()
    {
        _config.Port = 9100;
        _config.Host = "127.0.0.1";
        _config.AllowCors = true;
        _config.AllowedOrigins = new List<string>();
        _config.EnsureDefaults();
        _config.Save();
        RefreshState();
        AddLog("Configuracion default restaurada.");
    }

    private string? SelectedPrinterFromList()
    {
        if (printersList.SelectedItems.Count > 0)
            return printersList.SelectedItems[0].Text;
        return defaultPrinterCombo.SelectedItem as string;
    }

    private string? PrintPlainText(string printer)
    {
        try
        {
            using var doc = new System.Drawing.Printing.PrintDocument();
            doc.PrinterSettings.PrinterName = printer;
            doc.DocumentName = "Prueba Imprelia";
            doc.PrintPage += (_, e) =>
            {
                using var font = new Font("Segoe UI", 12f);
                e.Graphics?.DrawString($"Imprelia Print Agent\r\nPrueba de impresion\r\nImpresora: {printer}\r\nFecha: {DateTime.Now:g}",
                    font, Brushes.Black, e.MarginBounds);
            };
            doc.Print();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private void AddLog(string message)
    {
        eventsList.Items.Insert(0, $"{DateTime.Now:HH:mm:ss} - {message}");
        while (eventsList.Items.Count > 200) eventsList.Items.RemoveAt(eventsList.Items.Count - 1);
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            var active = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return !active.Any(ep => ep.Port == port);
        }
        catch { return true; }
    }

    private static string BuildZplTest(string printer) =>
        $"^XA^CF0,32^FO40,40^FDImprelia^FS^CF0,24^FO40,90^FDPrueba de impresion^FS^FO40,130^FD{printer.Replace("^", " ")}^FS^FO40,170^FD{DateTime.Now:g}^FS^XZ";

    private void refreshDashboardButton_Click(object sender, EventArgs e) => RefreshState();
    private void dashboardTestButton_Click(object sender, EventArgs e) => PrintTest();
    private void refreshPrintersButton_Click(object sender, EventArgs e) => LoadPrinters();
    private void useSelectedPrinterButton_Click(object sender, EventArgs e) => UseSelectedPrinterAsDefault();
    private void printerTestButton_Click(object sender, EventArgs e) => PrintTest();
    private void saveRoutesButton_Click(object sender, EventArgs e) => SaveRoutes();
    private void saveSettingsButton_Click(object sender, EventArgs e) => SaveSettings();
    private void restoreDefaultsButton_Click(object sender, EventArgs e) => RestoreDefaults();
    private void clearEventsButton_Click(object sender, EventArgs e) => eventsList.Items.Clear();
    private void minimizeButton_Click(object sender, EventArgs e) => Hide();
    private void exitButton_Click(object sender, EventArgs e) => ExitRequested?.Invoke(this, EventArgs.Empty);
    private void defaultPrinterCombo_SelectedIndexChanged(object sender, EventArgs e) => SaveSelectedPrinter();
    private void autoStartCheck_CheckedChanged(object sender, EventArgs e) => StartupRegistry.SetEnabled(autoStartCheck.Checked);
    private void apiGuideButton_Click(object sender, EventArgs e)
    {
        var url = $"http://localhost:{_startedPort}/docs";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo abrir la guia API:\n{ex.Message}", "Imprelia Print Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }
}

public static class EscPosTest
{
    public static byte[] Build(string? printerName = null, string? status = null)
    {
        var enc = System.Text.Encoding.ASCII;
        var sb = new List<byte>();
        sb.AddRange(new byte[] { 0x1B, 0x40 });
        sb.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
        sb.AddRange(new byte[] { 0x1D, 0x21, 0x11 });
        sb.AddRange(enc.GetBytes("Imprelia\n"));
        sb.AddRange(new byte[] { 0x1D, 0x21, 0x00 });
        sb.AddRange(enc.GetBytes("Prueba de impresion\n"));
        if (!string.IsNullOrWhiteSpace(printerName))
            sb.AddRange(enc.GetBytes($"Impresora: {SanitizeAscii(printerName)}\n"));
        if (!string.IsNullOrWhiteSpace(status))
            sb.AddRange(enc.GetBytes($"Estado: {SanitizeAscii(status)}\n"));
        sb.AddRange(enc.GetBytes(DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "\n"));
        sb.AddRange(enc.GetBytes("--------------------------------\n"));
        sb.AddRange(enc.GetBytes("Si lees esto, funciona OK!\n"));
        sb.AddRange(new byte[] { 0x0A, 0x0A, 0x0A });
        sb.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 });
        return sb.ToArray();
    }

    private static string SanitizeAscii(string value)
    {
        var chars = value.Select(ch => ch <= 127 ? ch : '?').ToArray();
        return new string(chars);
    }
}
