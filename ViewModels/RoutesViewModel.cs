using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Imprelia.PrintAgent.ViewModels;

public sealed class RoutesViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly IPrinterDiscoveryService _discovery;

    private PrinterRoute? _selectedRoute;
    private bool _isEditing;
    private bool _isNewRoute;
    private string _editPurpose = "";
    private string _editPrinterName = "";
    private string _editContentType = "epos";
    private string? _editError;

    public ObservableCollection<PrinterRoute> Routes { get; } = new();
    public ObservableCollection<string> AvailablePrinters { get; } = new();
    public ObservableCollection<string> ContentTypes { get; } = new(
        new[] { "epos", "zpl", "tspl", "epl", "dpl", "pdf", "text", "raw", "fiscal" });

    public PrinterRoute? SelectedRoute
    {
        get => _selectedRoute;
        set { Set(ref _selectedRoute, value); OnPropertyChanged(nameof(HasSelection)); }
    }

    public bool HasSelection => _selectedRoute != null;
    public bool IsEditing { get => _isEditing; private set => Set(ref _isEditing, value); }
    public bool IsNewRoute { get => _isNewRoute; private set { Set(ref _isNewRoute, value); OnPropertyChanged(nameof(RouteFormTitle)); } }

    public string RouteFormTitle => Localization.Loc.T(_isNewRoute ? "routes.newTitle" : "routes.editTitle");

    public string EditPurpose { get => _editPurpose; set => Set(ref _editPurpose, value); }
    public string EditPrinterName { get => _editPrinterName; set => Set(ref _editPrinterName, value); }
    public string EditContentType { get => _editContentType; set => Set(ref _editContentType, value); }
    public string? EditError { get => _editError; private set => Set(ref _editError, value); }

    public ICommand AddRouteCommand { get; }
    public ICommand EditRouteCommand { get; }
    public ICommand DeleteRouteCommand { get; }
    public ICommand TestRouteCommand { get; }
    public ICommand SaveEditCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand SelectRouteCommand { get; }

    public RoutesViewModel(AppConfig config, AgentLogService log, IPrinterDiscoveryService discovery)
    {
        _config = config;
        _log = log;
        _discovery = discovery;

        AddRouteCommand = new RelayCommand(StartAdd);
        EditRouteCommand = new RelayCommand(StartEdit, () => _selectedRoute != null);
        DeleteRouteCommand = new RelayCommand(DeleteSelected, () => _selectedRoute != null);
        TestRouteCommand = new RelayCommand(TestSelected, () => _selectedRoute != null);
        SaveEditCommand = new RelayCommand(SaveEdit);
        CancelEditCommand = new RelayCommand(CancelEdit);
        SelectRouteCommand = new RelayCommand<PrinterRoute>(r => SelectedRoute = r);

        LoadRoutes();
        RefreshPrinters();
    }

    private void LoadRoutes()
    {
        Routes.Clear();
        foreach (var r in _config.PrinterRoutes.Values.OrderBy(r => r.Purpose))
            Routes.Add(r);
    }

    private void RefreshPrinters()
    {
        AvailablePrinters.Clear();
        AvailablePrinters.Add("");
        foreach (var p in _discovery.ListPrinters()) AvailablePrinters.Add(p.Name);
    }

    private void StartAdd()
    {
        IsNewRoute = true;
        IsEditing = true;
        EditPurpose = "";
        EditPrinterName = "";
        EditContentType = "epos";
        EditError = null;
        RefreshPrinters();
    }

    private void StartEdit()
    {
        if (_selectedRoute == null) return;
        IsNewRoute = false;
        IsEditing = true;
        EditPurpose = _selectedRoute.Purpose;
        EditPrinterName = _selectedRoute.PrinterName ?? "";
        EditContentType = _selectedRoute.ContentType;
        EditError = null;
        RefreshPrinters();
    }

    private void SaveEdit()
    {
        var purpose = EditPurpose.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(purpose))
        { EditError = "El propósito es obligatorio."; return; }
        if (!System.Text.RegularExpressions.Regex.IsMatch(purpose, @"^[a-z0-9_]+$"))
        { EditError = "Solo letras minúsculas, números y guiones bajos."; return; }
        if (IsNewRoute && _config.PrinterRoutes.ContainsKey(purpose))
        { EditError = $"Ya existe una ruta con propósito '{purpose}'."; return; }

        if (!IsNewRoute && _selectedRoute != null && _selectedRoute.Purpose != purpose)
            _config.PrinterRoutes.Remove(_selectedRoute.Purpose);

        _config.PrinterRoutes[purpose] = new PrinterRoute
        {
            Purpose = purpose,
            PrinterName = string.IsNullOrWhiteSpace(EditPrinterName) ? null : EditPrinterName.Trim(),
            ContentType = EditContentType,
        };
        _config.Save();
        _log.Info($"Ruta '{purpose}' guardada.", "Rutas");
        IsEditing = false;
        LoadRoutes();
        SelectedRoute = _config.PrinterRoutes.TryGetValue(purpose, out var saved) ? saved : null;
    }

    private void CancelEdit()
    {
        IsEditing = false;
        EditError = null;
    }

    private void DeleteSelected()
    {
        if (_selectedRoute == null) return;
        _config.PrinterRoutes.Remove(_selectedRoute.Purpose);
        _config.Save();
        _log.Info($"Ruta '{_selectedRoute.Purpose}' eliminada.", "Rutas");
        SelectedRoute = null;
        LoadRoutes();
    }

    private void TestSelected()
    {
        if (_selectedRoute == null) return;
        var printer = _selectedRoute.PrinterName ?? _config.DefaultPrinter;
        if (string.IsNullOrWhiteSpace(printer))
        { _log.Warn($"Ruta '{_selectedRoute.Purpose}' no tiene impresora asignada.", "Rutas"); return; }

        var type = _discovery.ListPrinters().FirstOrDefault(p => p.Name.Equals(printer, StringComparison.OrdinalIgnoreCase))?.Type;
        var payload = PrintTestBuilder.Build(printer, _selectedRoute.ContentType, type);
        var bytes = payload.ContentType is "raw" or "epos"
            ? RawPrintAdapter.DecodeMaybeBase64(payload.Content)
            : System.Text.Encoding.ASCII.GetBytes(payload.Content);
        var err = RawPrinter.SendBytes(printer, bytes, payload.JobName);
        if (err == null) _log.Info($"Prueba ruta '{_selectedRoute.Purpose}' → {printer}.", "Rutas");
        else _log.Error($"Error ruta '{_selectedRoute.Purpose}': {err}", "Rutas");
    }
}
