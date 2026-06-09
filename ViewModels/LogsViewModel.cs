using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Imprelia.PrintAgent.ViewModels;

public sealed class LogsViewModel : ViewModelBase
{
    private readonly AgentLogService _log;
    private string _filterLevel = "Todos";
    private string _searchText = "";
    private bool _autoRefresh = true;

    public ObservableCollection<LogEntry> FilteredLogs { get; } = new();

    public string FilterLevel
    {
        get => _filterLevel;
        set { Set(ref _filterLevel, value); ApplyFilter(); RefreshStats(); }
    }

    public string SearchText
    {
        get => _searchText;
        set { Set(ref _searchText, value); ApplyFilter(); }
    }

    public bool AutoRefresh { get => _autoRefresh; set => Set(ref _autoRefresh, value); }

    private int _totalToday;
    private int _infoCount;
    private int _warnCount;
    private int _errorCount;
    private DateTime? _lastUpdate;
    private DateTime? _firstToday;

    public int TotalToday { get => _totalToday; private set => Set(ref _totalToday, value); }
    public int InfoCount { get => _infoCount; private set => Set(ref _infoCount, value); }
    public int WarnCount { get => _warnCount; private set => Set(ref _warnCount, value); }
    public int ErrorCount { get => _errorCount; private set => Set(ref _errorCount, value); }
    public string LastUpdateFormatted => _lastUpdate?.ToString("HH:mm:ss") ?? "—";
    public string FirstTodayFormatted => _firstToday?.ToString("HH:mm:ss") ?? "—";

    public ICommand CopyLogsCommand { get; }
    public ICommand ClearLogsCommand { get; }
    public ICommand FilterAllCommand { get; }
    public ICommand FilterInfoCommand { get; }
    public ICommand FilterWarnCommand { get; }
    public ICommand FilterErrorCommand { get; }

    public LogsViewModel(AgentLogService log)
    {
        _log = log;
        _log.LogAdded += (_, _) => { if (_autoRefresh) { ApplyFilter(); RefreshStats(); } };

        CopyLogsCommand = new RelayCommand(CopyLogs);
        ClearLogsCommand = new RelayCommand(ClearLogs);
        FilterAllCommand = new RelayCommand(() => FilterLevel = "Todos");
        FilterInfoCommand = new RelayCommand(() => FilterLevel = "Info");
        FilterWarnCommand = new RelayCommand(() => FilterLevel = "Warning");
        FilterErrorCommand = new RelayCommand(() => FilterLevel = "Error");

        ApplyFilter();
        RefreshStats();
    }

    private void ApplyFilter()
    {
        FilteredLogs.Clear();
        var term = _searchText.Trim().ToLowerInvariant();
        foreach (var e in _log.Entries)
        {
            if (_filterLevel != "Todos" && !e.Level.StartsWith(_filterLevel.ToUpper()[..1])) continue;
            if (!string.IsNullOrEmpty(term) &&
                !e.Message.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                !e.Module.Contains(term, StringComparison.OrdinalIgnoreCase)) continue;
            FilteredLogs.Add(e);
        }
    }

    private void RefreshStats()
    {
        var today = _log.Entries.Where(e => e.Time.Date == DateTime.Today).ToList();
        TotalToday = today.Count;
        InfoCount = today.Count(e => e.Level == "INFO");
        WarnCount = today.Count(e => e.Level == "WARNING");
        ErrorCount = today.Count(e => e.Level == "ERROR");
        _lastUpdate = today.FirstOrDefault()?.Time;
        _firstToday = today.LastOrDefault()?.Time;
        OnPropertyChanged(nameof(LastUpdateFormatted));
        OnPropertyChanged(nameof(FirstTodayFormatted));
    }

    private void CopyLogs()
    {
        var text = string.Join(Environment.NewLine, FilteredLogs.Select(e =>
            $"{e.TimeFormatted} [{e.Level}] {e.Module}: {e.Message}"));
        try { System.Windows.Clipboard.SetText(text); } catch { }
    }

    private void ClearLogs()
    {
        _log.Entries.Clear();
        FilteredLogs.Clear();
        RefreshStats();
    }
}
