using System.Windows.Input;
using Imprelia.PrintAgent.Localization;

namespace Imprelia.PrintAgent.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly int _startedPort;
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

    /// <summary>Idioma de la interfaz. Cambia en vivo y se guarda al instante.</summary>
    public bool IsSpanish
    {
        get => _config.Language != "en";
        set { if (value) ApplyLanguage("es"); }
    }
    public bool IsEnglish
    {
        get => _config.Language == "en";
        set { if (value) ApplyLanguage("en"); }
    }

    public string? Message { get => _message; private set => Set(ref _message, value); }
    public bool IsError { get => _isError; private set => Set(ref _isError, value); }

    public ICommand SaveCommand { get; }
    public ICommand RestoreDefaultsCommand { get; }
    public ICommand CopyUrlCommand { get; }
    public ICommand ReconfigureCommand { get; }

    /// <summary>Pide reabrir el onboarding (rol/token). La vista lo maneja.</summary>
    public event EventHandler? ReconfigureRequested;

    /// <summary>Rol actual para mostrar en la UI.</summary>
    public string RoleLabel => _config.ClientMode.Enabled
        ? Localization.Loc.T("settings.roleClient")
        : Localization.Loc.T("settings.rolePrincipal");

    public SettingsViewModel(AppConfig config, int startedPort, AgentLogService log)
    {
        _config = config;
        _startedPort = startedPort;
        _log = log;

        SaveCommand = new RelayCommand(Save);
        RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        CopyUrlCommand = new RelayCommand(() => SetClipboard($"http://localhost:{_startedPort}"));
        ReconfigureCommand = new RelayCommand(() => ReconfigureRequested?.Invoke(this, EventArgs.Empty));

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

        if (Port != _startedPort && !IsPortAvailable(Port))
        {
            ShowMessage($"El puerto {Port} está ocupado.", isError: true);
            return;
        }

        _config.Port = Port;
        _config.AllowCors = AllowCors;
        _config.AllowedOrigins = AllowedOrigins
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim()).Where(o => o.Length > 0).ToList();
        _config.EnsureDefaults();
        _config.Save();
        _log.Info("Configuración guardada.", "Config");

        if (Port != _startedPort)
            ShowMessage("Puerto guardado. Reinicia el agente para aplicar el nuevo listener.", isError: false);
        else
            ShowMessage("Configuración guardada correctamente.", isError: false);
    }

    private void RestoreDefaults()
    {
        _config.Port = 9100;
        _config.Host = "127.0.0.1";
        _config.AllowCors = true;
        _config.AllowedOrigins = new List<string>();
        _config.EnsureDefaults();
        _config.Save();
        LoadFromConfig();
        _log.Info("Configuración restaurada a valores por defecto.", "Config");
        ShowMessage("Valores restaurados.", isError: false);
    }

    private void ApplyLanguage(string lang)
    {
        if (_config.Language == lang) return;
        _config.Language = lang;
        _config.Save();
        Loc.SetLanguage(lang);
        OnPropertyChanged(nameof(IsSpanish));
        OnPropertyChanged(nameof(IsEnglish));
        _log.Info($"Idioma: {lang}", "Config");
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
