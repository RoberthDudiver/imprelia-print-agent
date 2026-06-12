using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace Imprelia.PrintAgent.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly IPrinterDiscoveryService _discovery;
    private readonly int _port;
    private readonly Action<int> _navigateTo;
    private readonly DispatcherTimer _uptimeTimer;
    private readonly DateTime _startedAt = DateTime.Now;
    private string _uptime = "00:00:00";

    public string Status => "Activo";
    public bool IsRunning => true;
    public string Version => LocalServer.Version;
    public int Port => _port;
    public string LocalUrl => $"http://localhost:{_port}";
    public string TechnicalStatus => "Conectado y listo";

    public string Uptime { get => _uptime; private set => Set(ref _uptime, value); }

    public ObservableCollection<LogEntry> RecentLogs { get; } = new();
    public ObservableCollection<PrinterInfo> DetectedPrinters { get; } = new();
    public ObservableCollection<PrinterRoute> ConfiguredRoutes { get; } = new();

    private int _printerCount;
    public int PrinterCount { get => _printerCount; private set => Set(ref _printerCount, value); }
    private int _routeCount;
    public int RouteCount { get => _routeCount; private set => Set(ref _routeCount, value); }

    public ICommand CopyUrlCommand { get; }
    public ICommand CopyEndpointCommand { get; }
    public ICommand PrintTestCommand { get; }
    public ICommand OpenApiGuideCommand { get; }
    public ICommand NavPrintersCommand { get; }
    public ICommand NavRoutesCommand { get; }
    public ICommand NavLogsCommand { get; }

    public DashboardViewModel(AppConfig config, int port, AgentLogService log,
        IPrinterDiscoveryService discovery, Action<int> navigateTo)
    {
        _config = config;
        _port = port;
        _log = log;
        _discovery = discovery;
        _navigateTo = navigateTo;

        CopyUrlCommand = new RelayCommand(() => SetClipboard(LocalUrl));
        CopyEndpointCommand = new RelayCommand(() => SetClipboard($"http://localhost:{_port}/api/print"));
        PrintTestCommand = new RelayCommand(DoPrintTest);
        OpenApiGuideCommand = new RelayCommand(() => OpenUrl($"http://localhost:{_port}/docs"));
        NavPrintersCommand = new RelayCommand(() => navigateTo(1));
        NavRoutesCommand = new RelayCommand(() => navigateTo(2));
        NavLogsCommand = new RelayCommand(() => navigateTo(4));

        _log.LogAdded += (_, _) => RefreshLogs();

        _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uptimeTimer.Tick += (_, _) => Uptime = (DateTime.Now - _startedAt).ToString(@"hh\:mm\:ss");
        _uptimeTimer.Start();

        Refresh();
    }

    public void Refresh()
    {
        RefreshPrinters();
        RefreshRoutes();
        RefreshLogs();
    }

    private void RefreshPrinters()
    {
        var printers = _discovery.ListPrinters();
        DetectedPrinters.Clear();
        foreach (var p in printers) DetectedPrinters.Add(p);
        PrinterCount = printers.Count;
    }

    private void RefreshRoutes()
    {
        var routes = _config.PrinterRoutes.Values.OrderBy(r => r.Purpose).ToList();
        ConfiguredRoutes.Clear();
        foreach (var r in routes) ConfiguredRoutes.Add(r);
        RouteCount = routes.Count;
    }

    private void RefreshLogs()
    {
        RecentLogs.Clear();
        foreach (var e in _log.Entries.Take(5)) RecentLogs.Add(e);
    }

    private void DoPrintTest()
    {
        var printer = _config.DefaultPrinter;
        if (string.IsNullOrWhiteSpace(printer))
        {
            _log.Warn("No hay impresora predeterminada configurada.", "Dashboard");
            return;
        }
        var type = _discovery.ListPrinters().FirstOrDefault(p => p.Name.Equals(printer, StringComparison.OrdinalIgnoreCase))?.Type;
        var payload = PrintTestBuilder.Build(printer, null, type);
        var bytes = payload.ContentType is "raw" or "epos"
            ? RawPrintAdapter.DecodeMaybeBase64(payload.Content)
            : System.Text.Encoding.ASCII.GetBytes(payload.Content);
        var err = RawPrinter.SendBytes(printer, bytes, payload.JobName);
        if (err == null) _log.Info($"Prueba enviada a {printer}.", "Dashboard");
        else _log.Error($"Error prueba {printer}: {err}", "Dashboard");
    }

    private static void SetClipboard(string text)
    {
        try { System.Windows.Clipboard.SetText(text); } catch { }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            { FileName = url, UseShellExecute = true });
        }
        catch { }
    }
}
