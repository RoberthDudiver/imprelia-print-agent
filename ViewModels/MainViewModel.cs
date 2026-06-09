using System.Windows.Input;

namespace Imprelia.PrintAgent.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly int _startedPort;

    private ViewModelBase _currentPage;
    private int _selectedNavIndex;

    public event EventHandler? MinimizeRequested;
    public event EventHandler? ExitRequested;

    public DashboardViewModel Dashboard { get; }
    public PrintersViewModel Printers { get; }
    public RoutesViewModel Routes { get; }
    public SettingsViewModel Settings { get; }
    public LogsViewModel Logs { get; }

    public ViewModelBase CurrentPage { get => _currentPage; private set => Set(ref _currentPage, value); }
    public int SelectedNavIndex { get => _selectedNavIndex; private set => Set(ref _selectedNavIndex, value); }

    public string AgentVersion => LocalServer.Version;
    public int AgentPort => _startedPort;
    public string ListeningAddress => $"127.0.0.1:{_startedPort}";

    public ICommand NavDashboardCommand { get; }
    public ICommand NavPrintersCommand { get; }
    public ICommand NavRoutesCommand { get; }
    public ICommand NavSettingsCommand { get; }
    public ICommand NavLogsCommand { get; }
    public ICommand OpenApiGuideCommand { get; }
    public ICommand MinimizeCommand { get; }
    public ICommand ExitCommand { get; }

    public MainViewModel(AppConfig config, int startedPort, AgentLogService log)
    {
        _config = config;
        _startedPort = startedPort;
        _log = log;

        var discovery = new WindowsPrinterDiscoveryService(config);

        Dashboard = new DashboardViewModel(config, startedPort, log, discovery, NavigateTo);
        Printers = new PrintersViewModel(config, log, discovery);
        Routes = new RoutesViewModel(config, log, discovery);
        Settings = new SettingsViewModel(config, startedPort, log);
        Logs = new LogsViewModel(log);

        _currentPage = Dashboard;

        NavDashboardCommand = new RelayCommand(() => Navigate(Dashboard, 0));
        NavPrintersCommand = new RelayCommand(() => Navigate(Printers, 1));
        NavRoutesCommand = new RelayCommand(() => Navigate(Routes, 2));
        NavSettingsCommand = new RelayCommand(() => Navigate(Settings, 3));
        NavLogsCommand = new RelayCommand(() => Navigate(Logs, 4));
        OpenApiGuideCommand = new RelayCommand(OpenApiGuide);
        MinimizeCommand = new RelayCommand(() => MinimizeRequested?.Invoke(this, EventArgs.Empty));
        ExitCommand = new RelayCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));
    }

    public void NavigateTo(int index)
    {
        switch (index)
        {
            case 0: Navigate(Dashboard, 0); break;
            case 1: Navigate(Printers, 1); break;
            case 2: Navigate(Routes, 2); break;
            case 3: Navigate(Settings, 3); break;
            case 4: Navigate(Logs, 4); break;
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
            { FileName = $"http://localhost:{_startedPort}/docs", UseShellExecute = true });
        }
        catch { }
    }
}
