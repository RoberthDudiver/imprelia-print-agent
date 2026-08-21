using System.Windows.Input;

namespace Imprelia.PrintAgent.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly LocalServer _server;
    private readonly AgentLogService _log;

    private int _port;
    private bool _allowCors;
    private string _allowedOrigins = "";
    private bool _autoStart;
    private string? _message;
    private bool _isError;

    public int Port { get => _port; set => Set(ref _port, value); }
    public bool AllowCors { get => _allowCors; set => Set(ref _allowCors, value); }
    public string AllowedOrigins { get => _allowedOrigins; set => Set(ref _allowedOrigins, value); }
    public bool AutoStart { get => _autoStart; set { if (Set(ref _autoStart, value)) StartupRegistry.SetEnabled(value); } }
    public string? Message { get => _message; private set => Set(ref _message, value); }
    public bool IsError { get => _isError; private set => Set(ref _isError, value); }

    public ICommand SaveCommand { get; }
    public ICommand RestoreDefaultsCommand { get; }
    public ICommand CopyUrlCommand { get; }

    public SettingsViewModel(AppConfig config, LocalServer server, AgentLogService log)
    {
        _config = config;
        _server = server;
        _log = log;

        SaveCommand = new RelayCommand(Save);
        RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        CopyUrlCommand = new RelayCommand(() => SetClipboard($"http://127.0.0.1:{_server.BoundPort}"));

        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        Port = _config.Port;
        AllowCors = _config.AllowCors;
        AllowedOrigins = string.Join(Environment.NewLine, _config.AllowedOrigins);
        AutoStart = StartupRegistry.IsEnabled();
    }

    private void Save()
    {
        if (Port < 1 || Port > 65535)
        {
            ShowMessage("Puerto inválido. Debe estar entre 1 y 65535.", isError: true);
            return;
        }

        bool portChanged = Port != _server.BoundPort;
        if (portChanged && !IsPortAvailable(Port))
        {
            ShowMessage($"El puerto {Port} está ocupado.", isError: true);
            return;
        }

        int previousPort = _server.BoundPort;
        _config.Port = Port;
        _config.AllowCors = AllowCors;
        _config.AllowedOrigins = AllowedOrigins
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim()).Where(o => o.Length > 0).ToList();
        _config.EnsureDefaults();
        _config.Save();
        _log.Info("Configuración guardada.", "Config");

        if (portChanged)
        {
            // Aplicamos el puerto EN CALIENTE: re-vinculamos el listener sin reiniciar la app.
            if (_server.TryRestart(out var error))
            {
                _log.Info($"Listener re-vinculado al puerto {Port}.", "Config");
                if (_server.LastBindWarning != null) _log.Error(_server.LastBindWarning);
                ShowMessage($"Puerto cambiado a {Port} y aplicado (sin reiniciar). Actualizá también la URL en la app web.", isError: false);
            }
            else
            {
                // Falló el re-vinculado → volvemos al puerto anterior para no dejar el agente caído.
                _config.Port = previousPort;
                _config.Save();
                _server.TryRestart(out _);
                Port = previousPort;
                _log.Error($"No se pudo cambiar al puerto {Port}: {error}", "Config");
                ShowMessage($"No se pudo aplicar el puerto: {error}", isError: true);
            }
        }
        else
        {
            ShowMessage("Configuración guardada correctamente.", isError: false);
        }
    }

    private void RestoreDefaults()
    {
        _config.Port = 9100;
        _config.Host = "127.0.0.1";
        _config.AllowCors = true;
        _config.AllowedOrigins = new List<string>();
        _config.EnsureDefaults();
        _config.Save();
        if (_server.BoundPort != _config.Port) _server.TryRestart(out _);
        LoadFromConfig();
        _log.Info("Configuración restaurada a valores por defecto.", "Config");
        ShowMessage("Valores restaurados.", isError: false);
    }

    private void ShowMessage(string msg, bool isError)
    {
        Message = msg;
        IsError = isError;
    }

    private static void SetClipboard(string text) { try { System.Windows.Clipboard.SetText(text); } catch { } }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            var active = System.Net.NetworkInformation.IPGlobalProperties
                .GetIPGlobalProperties().GetActiveTcpListeners();
            return !active.Any(ep => ep.Port == port);
        }
        catch { return true; }
    }
}
