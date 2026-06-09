using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Imprelia.PrintAgent.ViewModels;

public sealed class PrintersViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly IPrinterDiscoveryService _discovery;

    private PrinterInfo? _selectedPrinter;
    private string _searchText = "";
    private string? _notification;
    private bool _hasNotification;

    public ObservableCollection<PrinterInfo> Printers { get; } = new();
    public ObservableCollection<PrinterInfo> FilteredPrinters { get; } = new();

    public PrinterInfo? SelectedPrinter
    {
        get => _selectedPrinter;
        set { Set(ref _selectedPrinter, value); OnPropertyChanged(nameof(HasSelection)); }
    }

    public bool HasSelection => _selectedPrinter != null;

    public string SearchText
    {
        get => _searchText;
        set { Set(ref _searchText, value); ApplyFilter(); }
    }

    public string? Notification { get => _notification; private set => Set(ref _notification, value); }
    public bool HasNotification { get => _hasNotification; private set => Set(ref _hasNotification, value); }

    public ICommand RefreshCommand { get; }
    public ICommand SetAsDefaultCommand { get; }
    public ICommand PrintTestCommand { get; }
    public ICommand SelectCommand { get; }

    public PrintersViewModel(AppConfig config, AgentLogService log, IPrinterDiscoveryService discovery)
    {
        _config = config;
        _log = log;
        _discovery = discovery;

        RefreshCommand = new RelayCommand(Refresh);
        SetAsDefaultCommand = new RelayCommand(SetAsDefault, () => _selectedPrinter != null);
        PrintTestCommand = new RelayCommand(PrintTest, () => _selectedPrinter != null);
        SelectCommand = new RelayCommand<PrinterInfo>(p => SelectedPrinter = p);

        Refresh();
    }

    public void Refresh()
    {
        var printers = _discovery.ListPrinters();

        // Si el config no tiene impresora guardada, adoptar la predeterminada de Windows.
        if (string.IsNullOrWhiteSpace(_config.DefaultPrinter))
        {
            var winDefault = printers.FirstOrDefault(p => p.IsDefault);
            if (winDefault != null)
            {
                _config.DefaultPrinter = winDefault.Name;
                _config.Save();
                _log.Info($"Impresora predeterminada adoptada automáticamente: {winDefault.Name}", "Impresoras");
            }
        }

        // IsDefault en PrinterInfo refleja la impresora configurada en Imprelia (no la de Windows).
        foreach (var p in printers)
            p.IsDefault = string.Equals(p.Name, _config.DefaultPrinter, StringComparison.OrdinalIgnoreCase);

        Printers.Clear();
        foreach (var p in printers) Printers.Add(p);
        ApplyFilter();
        _log.Info("Lista de impresoras actualizada.", "Impresoras");
        ShowNotification("Lista actualizada.");
    }

    private void ApplyFilter()
    {
        FilteredPrinters.Clear();
        var term = _searchText.Trim().ToLowerInvariant();
        foreach (var p in Printers)
        {
            if (string.IsNullOrEmpty(term) ||
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Type.Contains(term, StringComparison.OrdinalIgnoreCase))
                FilteredPrinters.Add(p);
        }
    }

    private void SetAsDefault()
    {
        if (_selectedPrinter == null) return;
        _config.DefaultPrinter = _selectedPrinter.Name;
        _config.Save();
        _log.Info($"Impresora predeterminada: {_selectedPrinter.Name}", "Impresoras");
        ShowNotification($"Impresora predeterminada: {_selectedPrinter.Name}");
        Refresh();
    }

    private void PrintTest()
    {
        if (_selectedPrinter == null) return;
        var err = RawPrinter.SendBytes(_selectedPrinter.Name, EscPosTest.Build(_selectedPrinter.Name, "Ready"), "Prueba Imprelia");
        if (err == null)
        {
            _log.Info($"Prueba enviada a {_selectedPrinter.Name}.", "Impresoras");
            ShowNotification("Prueba enviada correctamente.");
        }
        else
        {
            _log.Error($"Error prueba {_selectedPrinter.Name}: {err}", "Impresoras");
            ShowNotification($"Error: {err}");
        }
    }

    private void ShowNotification(string msg)
    {
        Notification = msg;
        HasNotification = true;
    }
}
