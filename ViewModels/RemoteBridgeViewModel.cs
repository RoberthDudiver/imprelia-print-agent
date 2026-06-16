using Imprelia.PrintAgent.Services;
using WpfApp = System.Windows.Application;

namespace Imprelia.PrintAgent.ViewModels;

public sealed class RemoteBridgeViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly RemoteBridgeService _bridge;
    private readonly AgentLogService _log;

    // ── Campos enlazados ──────────────────────────────────────────────────────

    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set { Set(ref _enabled, value); }
    }

    private string _serverUrl = "";
    public string ServerUrl
    {
        get => _serverUrl;
        set { Set(ref _serverUrl, value); OnPropertyChanged(nameof(ClientToken)); }
    }

    private string _agentId = "";
    public string AgentId
    {
        get => _agentId;
        set { Set(ref _agentId, value); OnPropertyChanged(nameof(ClientToken)); }
    }

    private string _apiKey = "";
    public string ApiKey
    {
        get => _apiKey;
        set { Set(ref _apiKey, value); OnPropertyChanged(nameof(ClientToken)); }
    }

    /// <summary>Token base64 para configurar clientes con un solo pegado.</summary>
    public string ClientToken => Services.SetupToken.Encode(_serverUrl, _agentId, _apiKey);

    private string _mode = "signalr";
    public string Mode
    {
        get => _mode;
        set { Set(ref _mode, value); OnPropertyChanged(nameof(IsPollingMode)); }
    }

    private int _pollingSeconds = 10;
    public int PollingSeconds
    {
        get => _pollingSeconds;
        set { Set(ref _pollingSeconds, value); }
    }

    private bool _autoReconnect = true;
    public bool AutoReconnect
    {
        get => _autoReconnect;
        set { Set(ref _autoReconnect, value); }
    }

    public bool IsPollingMode => _mode == "polling";

    // ── Estado de conexión (read-only, actualizado por el servicio) ───────────

    private BridgeConnectionState _connectionState = BridgeConnectionState.Disabled;
    public BridgeConnectionState ConnectionState
    {
        get => _connectionState;
        private set
        {
            Set(ref _connectionState, value);
            OnPropertyChanged(nameof(ConnectionStateLabel));
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsConnecting));
            OnPropertyChanged(nameof(IsError));
        }
    }

    public string ConnectionStateLabel => _connectionState switch
    {
        BridgeConnectionState.Disabled             => "Desactivado",
        BridgeConnectionState.Connecting           => "Conectando…",
        BridgeConnectionState.Connected            => "Conectado",
        BridgeConnectionState.Disconnected         => "Desconectado",
        BridgeConnectionState.AuthenticationFailed => "Error de autenticación",
        BridgeConnectionState.ServerUnavailable    => "Servidor no disponible",
        _                                          => "Desconocido",
    };

    public bool IsConnected  => _connectionState == BridgeConnectionState.Connected;
    public bool IsConnecting => _connectionState == BridgeConnectionState.Connecting;
    public bool IsError      => _connectionState is BridgeConnectionState.AuthenticationFailed
                                                  or BridgeConnectionState.ServerUnavailable;

    private string _lastConnectedAt = "";
    public string LastConnectedAt
    {
        get => _lastConnectedAt;
        private set { Set(ref _lastConnectedAt, value); }
    }

    private string _lastError = "";
    public string LastError
    {
        get => _lastError;
        private set { Set(ref _lastError, value); }
    }

    private bool _isTesting;
    public bool IsTesting
    {
        get => _isTesting;
        private set { Set(ref _isTesting, value); }
    }

    private string _testResult = "";
    public string TestResult
    {
        get => _testResult;
        private set { Set(ref _testResult, value); OnPropertyChanged(nameof(TestResultIsSuccess)); }
    }

    public bool TestResultIsSuccess => _testResult.StartsWith("✓");

    // ── Comandos ─────────────────────────────────────────────────────────────

    public RelayCommand SaveCommand { get; }
    public RelayCommand TestConnectionCommand { get; }
    public RelayCommand CopyTokenCommand { get; }

    public RemoteBridgeViewModel(AppConfig config, RemoteBridgeService bridge, AgentLogService log)
    {
        _config = config;
        _bridge = bridge;
        _log = log;

        var rb = config.RemoteBridge;
        _enabled        = rb.Enabled;
        _serverUrl      = rb.ServerUrl;
        _agentId        = rb.AgentId;
        _apiKey         = rb.ApiKey;
        _mode           = rb.Mode;
        _pollingSeconds = rb.FallbackPollingSeconds;
        _autoReconnect  = rb.AutoReconnect;

        SaveCommand           = new RelayCommand(SaveAndRestart, () => !IsTesting);
        TestConnectionCommand = new RelayCommand(async () => await RunTestAsync(),
                                                () => !IsTesting &&
                                                      !string.IsNullOrWhiteSpace(ServerUrl) &&
                                                      !string.IsNullOrWhiteSpace(AgentId));

        CopyTokenCommand = new RelayCommand(() =>
        {
            try { System.Windows.Clipboard.SetText(ClientToken); TestResult = "✓ Token copiado. Pegalo en el cliente."; } catch { }
        }, () => !string.IsNullOrWhiteSpace(ServerUrl) && !string.IsNullOrWhiteSpace(AgentId));

        _bridge.StateChanged += OnBridgeStateChanged;
        OnBridgeStateChanged(null, _bridge.State);
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void OnBridgeStateChanged(object? _, BridgeConnectionState state)
    {
        WpfApp.Current?.Dispatcher.Invoke(() =>
        {
            ConnectionState = state;
            LastConnectedAt = _bridge.LastConnectedAt.HasValue
                ? _bridge.LastConnectedAt.Value.ToString("dd/MM/yyyy HH:mm:ss")
                : "";
            LastError = _bridge.LastError ?? "";
        });
    }

    // ── Acciones ─────────────────────────────────────────────────────────────

    private void SaveAndRestart()
    {
        ApplyToConfig();
        _config.Save();
        TestResult = "";
        _log.Info("Remote Bridge: configuración guardada.", "Bridge");
        ConfigApplied?.Invoke(this, EventArgs.Empty);

        _ = Task.Run(async () =>
        {
            await _bridge.StopAsync();
            await _bridge.StartAsync();
        });
    }

    /// <summary>Se dispara cuando la config (en particular AgentId) cambió y otros VMs deben refrescar.</summary>
    public event EventHandler? ConfigApplied;

    private void ApplyToConfig()
    {
        var rb = _config.RemoteBridge;
        rb.Enabled                = Enabled;
        rb.ServerUrl              = ServerUrl.Trim();
        rb.AgentId                = AgentId.Trim();
        rb.ApiKey                 = ApiKey.Trim();
        rb.Mode                   = Mode;
        rb.FallbackPollingSeconds = PollingSeconds;
        rb.AutoReconnect          = AutoReconnect;
    }

    private async Task RunTestAsync()
    {
        IsTesting  = true;
        TestResult = "";
        ApplyToConfig();

        try
        {
            await _bridge.TestConnectionAsync();
            TestResult = "✓ Conexión exitosa — el servidor responde correctamente.";
            _log.Info("Remote Bridge: prueba de conexión OK.", "Bridge");
        }
        catch (Exception ex)
        {
            TestResult = $"✗ Error: {ex.Message}";
            _log.Warn($"Remote Bridge: prueba de conexión falló — {ex.Message}", "Bridge");
        }
        finally
        {
            IsTesting = false;
        }
    }
}
