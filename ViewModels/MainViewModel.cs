using System.Windows.Input;
using Imprelia.PrintAgent.Services;

namespace Imprelia.PrintAgent.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly LocalServer _server;

    private ViewModelBase _currentPage;
    private int _selectedNavIndex;

    public event EventHandler? MinimizeRequested;
    public event EventHandler? ExitRequested;

    public DashboardViewModel Dashboard { get; }
    public PrintersViewModel Printers { get; }
    public RoutesViewModel Routes { get; }
    public SettingsViewModel Settings { get; }
    public LogsViewModel Logs { get; }
    public RemoteBridgeViewModel Bridge { get; }

    public ViewModelBase CurrentPage { get => _currentPage; private set => Set(ref _currentPage, value); }
    public int SelectedNavIndex { get => _selectedNavIndex; private set => Set(ref _selectedNavIndex, value); }

    public string AgentVersion => LocalServer.Version;
    public int AgentPort => _server.BoundPort;
    public string ListeningAddress => $"127.0.0.1:{_server.BoundPort}";

    public ICommand NavDashboardCommand { get; }
    public ICommand NavPrintersCommand { get; }
    public ICommand NavRoutesCommand { get; }
    public ICommand NavSettingsCommand { get; }
    public ICommand NavLogsCommand { get; }
    public ICommand NavBridgeCommand { get; }
    public ICommand OpenApiGuideCommand { get; }
    public ICommand MinimizeCommand { get; }
    public ICommand ExitCommand { get; }

    public MainViewModel(AppConfig config, LocalServer server, AgentLogService log, RemoteBridgeService bridge)
    {
        _config = config;
        _server = server;
        _log = log;

        var discovery = new WindowsPrinterDiscoveryService(config);

        Dashboard = new DashboardViewModel(config, server, log, discovery, NavigateTo);
        Printers  = new PrintersViewModel(config, log, discovery);
        Routes    = new RoutesViewModel(config, log, discovery);
        Settings  = new SettingsViewModel(config, server, log);
        Logs      = new LogsViewModel(log);
        Bridge    = new RemoteBridgeViewModel(config, bridge, log);

        // Cuando el listener se re-vincula (cambio de puerto en caliente), refrescamos lo que muestra el puerto.
        _server.Restarted += (_, _) =>
        {
            OnPropertyChanged(nameof(AgentPort));
            OnPropertyChanged(nameof(ListeningAddress));
        };

        _currentPage = Dashboard;

        NavDashboardCommand = new RelayCommand(() => Navigate(Dashboard, 0));
        NavPrintersCommand  = new RelayCommand(() => Navigate(Printers,  1));
        NavRoutesCommand    = new RelayCommand(() => Navigate(Routes,    2));
        NavSettingsCommand  = new RelayCommand(() => Navigate(Settings,  3));
        NavLogsCommand      = new RelayCommand(() => Navigate(Logs,      4));
        NavBridgeCommand    = new RelayCommand(() => Navigate(Bridge,    5));
        OpenApiGuideCommand = new RelayCommand(OpenApiGuide);
        MinimizeCommand     = new RelayCommand(() => MinimizeRequested?.Invoke(this, EventArgs.Empty));
        ExitCommand         = new RelayCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));
    }

    public void NavigateTo(int index)
    {
        switch (index)
        {
            case 0: Navigate(Dashboard, 0); break;
            case 1: Navigate(Printers,  1); break;
            case 2: Navigate(Routes,    2); break;
            case 3: Navigate(Settings,  3); break;
            case 4: Navigate(Logs,      4); break;
            case 5: Navigate(Bridge,    5); break;
        }
    }

    private void Navigate(ViewModelBase page, int index)
    {
        CurrentPage = page;
        SelectedNavIndex = index;
        if (page == Dashboard) Dashboard.Refresh();
    }

    private void OpenApiGuide()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            { FileName = $"http://127.0.0.1:{_server.BoundPort}/docs", UseShellExecute = true });
        }
        catch { }
    }
}
