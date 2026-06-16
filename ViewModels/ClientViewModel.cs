using System.Collections.ObjectModel;
using Imprelia.PrintAgent.Localization;
using Imprelia.PrintAgent.Services;
using WpfApp = System.Windows.Application;

namespace Imprelia.PrintAgent.ViewModels;

/// <summary>
/// Modo cliente: este agente expone impresoras virtuales locales (vía IPP) que
/// reenvían los trabajos al hub para imprimirlos en la máquina principal.
/// </summary>
public sealed class ClientViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly IppPrintServer _ipp;
    private readonly ClientSenderService _sender;

    public ObservableCollection<ClientVirtualPrinter> Printers { get; } = new();
    public ObservableCollection<DiscoveredPrinter> Discovered { get; } = new();

    private string _discoverAgentId = "";
    public string DiscoverAgentId { get => _discoverAgentId; set => Set(ref _discoverAgentId, value); }

    private bool _isDiscovering;
    public bool IsDiscovering { get => _isDiscovering; private set => Set(ref _isDiscovering, value); }

    private bool _enabled;
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }

    private int _ippPort;
    public int IppPort { get => _ippPort; set => Set(ref _ippPort, value); }

    private ClientVirtualPrinter? _selected;
    public ClientVirtualPrinter? Selected
    {
        get => _selected;
        set { Set(ref _selected, value); OnPropertyChanged(nameof(HasSelection)); }
    }
    public bool HasSelection => _selected != null;

    // ── Estado del servidor IPP ───────────────────────────────────────────────

    public bool ServerRunning => _ipp.IsRunning;
    public string ServerStatusLabel => _ipp.IsRunning
        ? $"{Loc.T("client.serverRunning")} (127.0.0.1:{_ipp.Port})"
        : Loc.T("client.serverStopped");

    // ── Edición inline ────────────────────────────────────────────────────────

    private bool _isEditing;
    public bool IsEditing { get => _isEditing; private set => Set(ref _isEditing, value); }

    private bool _isNew;
    public bool IsNew { get => _isNew; private set { Set(ref _isNew, value); OnPropertyChanged(nameof(EditFormTitle)); } }

    public string EditFormTitle => Loc.T(_isNew ? "client.newTitle" : "client.editTitle");

    private string _editLocalName = "";
    public string EditLocalName { get => _editLocalName; set => Set(ref _editLocalName, value); }

    private string _editAgentId = "";
    public string EditAgentId { get => _editAgentId; set => Set(ref _editAgentId, value); }

    private string _editPrinter = "";
    public string EditPrinter { get => _editPrinter; set => Set(ref _editPrinter, value); }

    private string _editRoute = "";
    public string EditRoute { get => _editRoute; set => Set(ref _editRoute, value); }

    private string _editPaperSize = "a4";
    public string EditPaperSize { get => _editPaperSize; set => Set(ref _editPaperSize, value); }

    // ── Banner de mensajes ────────────────────────────────────────────────────

    private string _message = "";
    public string Message { get => _message; private set { Set(ref _message, value); OnPropertyChanged(nameof(HasMessage)); } }
    public bool HasMessage => !string.IsNullOrEmpty(_message);

    private bool _isError;
    public bool IsError { get => _isError; private set => Set(ref _isError, value); }

    // ── Comandos ─────────────────────────────────────────────────────────────

    public RelayCommand SaveCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand SaveEditCommand { get; }
    public RelayCommand CancelEditCommand { get; }
    public RelayCommand InstallCommand { get; }
    public RelayCommand UninstallCommand { get; }
    public RelayCommand TestHubCommand { get; }
    public RelayCommand DiscoverCommand { get; }
    public RelayCommand<DiscoveredPrinter> InstallDiscoveredCommand { get; }

    public ClientViewModel(AppConfig config, AgentLogService log, IppPrintServer ipp, ClientSenderService sender)
    {
        _config = config;
        _log = log;
        _ipp = ipp;
        _sender = sender;

        _enabled = config.ClientMode.Enabled;
        _ippPort = config.ClientMode.IppPort;
        foreach (var vp in config.ClientMode.VirtualPrinters) Printers.Add(vp);

        SaveCommand       = new RelayCommand(Save);
        AddCommand        = new RelayCommand(BeginAdd);
        EditCommand       = new RelayCommand(BeginEdit, () => HasSelection);
        DeleteCommand     = new RelayCommand(Delete, () => HasSelection);
        SaveEditCommand   = new RelayCommand(CommitEdit);
        CancelEditCommand = new RelayCommand(() => { IsEditing = false; Message = ""; });
        InstallCommand    = new RelayCommand(InstallSelected, () => HasSelection);
        UninstallCommand  = new RelayCommand(UninstallSelected, () => HasSelection);
        TestHubCommand    = new RelayCommand(async () => await TestHubAsync());
        DiscoverCommand   = new RelayCommand(async () => await DiscoverAsync(), () => !IsDiscovering);
        InstallDiscoveredCommand = new RelayCommand<DiscoveredPrinter>(InstallDiscovered);
    }

    // ── Descubrimiento de impresoras del principal ────────────────────────────

    private async Task DiscoverAsync()
    {
        IsDiscovering = true;
        Discovered.Clear();
        Message = "";
        try
        {
            var list = await _sender.DiscoverPrintersAsync(DiscoverAgentId.Trim());
            foreach (var p in list) Discovered.Add(p);
            if (list.Count == 0)
                ShowOk(Loc.T("client.discoverEmpty"));
            else
                ShowOk(string.Format(Loc.T("client.discoverOk"), list.Count, DiscoverAgentId.Trim()));
        }
        catch (Exception ex)
        {
            ShowError($"{Loc.T("client.discoverError")} {ex.Message}");
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    private void InstallDiscovered(DiscoveredPrinter? p)
    {
        if (p == null) return;

        // Crear la impresora virtual espejando la real del principal.
        var vp = new ClientVirtualPrinter
        {
            LocalName = $"{p.Name} (Remota)",
            TargetAgentId = DiscoverAgentId.Trim(),
            TargetPrinter = p.Name,
            Route = "",
            PaperSize = PaperSizeForType(p.Type),
        };

        // Evitar duplicados por nombre local.
        var existing = Printers.FirstOrDefault(x =>
            string.Equals(x.LocalName, vp.LocalName, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { Printers.Remove(existing); }

        Printers.Add(vp);
        Selected = vp;
        Save();

        if (!_ipp.IsRunning)
        {
            ShowError(Loc.T("client.enableSaveFirst"));
            return;
        }

        var result = VirtualPrinterProvisioner.Install(vp, _config.ClientMode.IppPort);
        if (result.Success)
        {
            vp.Installed = true;
            Save();
            ShowOk(result.Message);
        }
        else ShowError(result.Message);
    }

    private static string PaperSizeForType(string? type)
    {
        var t = (type ?? "").ToLowerInvariant();
        if (t.Contains("thermal") || t.Contains("epos") || t.Contains("pos")) return "thermal80";
        if (t.Contains("label") || t.Contains("zpl") || t.Contains("tspl") || t.Contains("epl") || t.Contains("dpl")) return "thermal80";
        return "a4";
    }

    // ── Acciones de configuración ─────────────────────────────────────────────

    private void Save()
    {
        _config.ClientMode.Enabled = Enabled;
        _config.ClientMode.IppPort = IppPort > 0 ? IppPort : 9110;
        _config.ClientMode.VirtualPrinters = Printers.ToList();
        _config.Save();

        // Reiniciar el servidor IPP con la nueva config.
        _ipp.Stop();
        _ipp.Start();
        RefreshServerStatus();

        ShowOk(Enabled
            ? "Modo cliente guardado. Servidor IPP reiniciado."
            : "Modo cliente desactivado.");
        _log.Info("Modo cliente: configuración guardada.", "Cliente");
    }

    // ── Edición de impresoras virtuales ───────────────────────────────────────

    private void BeginAdd()
    {
        IsNew = true;
        IsEditing = true;
        EditLocalName = "";
        EditAgentId = "";
        EditPrinter = "";
        EditRoute = "";
        EditPaperSize = "a4";
        Message = "";
    }

    private void BeginEdit()
    {
        if (Selected == null) return;
        IsNew = false;
        IsEditing = true;
        EditLocalName = Selected.LocalName;
        EditAgentId = Selected.TargetAgentId;
        EditPrinter = Selected.TargetPrinter;
        EditRoute = Selected.Route;
        EditPaperSize = string.IsNullOrWhiteSpace(Selected.PaperSize) ? "a4" : Selected.PaperSize;
        Message = "";
    }

    private void CommitEdit()
    {
        // Validación con mensaje claro (el botón Guardar siempre está habilitado).
        if (string.IsNullOrWhiteSpace(EditLocalName) || string.IsNullOrWhiteSpace(EditAgentId))
        {
            ShowError(Loc.T("client.needNameAgent"));
            return;
        }

        if (IsNew)
        {
            var vp = new ClientVirtualPrinter
            {
                LocalName = EditLocalName.Trim(),
                TargetAgentId = EditAgentId.Trim(),
                TargetPrinter = EditPrinter.Trim(),
                Route = EditRoute.Trim(),
                PaperSize = EditPaperSize,
            };
            Printers.Add(vp);
            Selected = vp;
        }
        else if (Selected != null)
        {
            Selected.LocalName = EditLocalName.Trim();
            Selected.TargetAgentId = EditAgentId.Trim();
            Selected.TargetPrinter = EditPrinter.Trim();
            Selected.Route = EditRoute.Trim();
            Selected.PaperSize = EditPaperSize;
            // refrescar la lista (ClientVirtualPrinter no notifica)
            var idx = Printers.IndexOf(Selected);
            if (idx >= 0) { Printers[idx] = Selected; Selected = Printers[idx]; }
        }

        IsEditing = false;
        Save();
    }

    private void Delete()
    {
        if (Selected == null) return;
        var name = Selected.LocalName;
        var wasInstalled = Selected.Installed;
        Printers.Remove(Selected);
        Selected = null;
        Save();

        if (wasInstalled && VirtualPrinterProvisioner.PrinterExists(name))
            ShowOk($"Ruta eliminada. La impresora '{name}' sigue en Windows — usá Quitar de Windows para borrarla.");
    }

    // ── Provisión en Windows ──────────────────────────────────────────────────

    private void InstallSelected()
    {
        if (Selected == null) return;

        if (!_ipp.IsRunning)
        {
            ShowError(Loc.T("client.enableSaveFirst"));
            return;
        }

        var result = VirtualPrinterProvisioner.Install(Selected, _config.ClientMode.IppPort);
        if (result.Success)
        {
            Selected.Installed = true;
            Save();
            ShowOk(result.Message);
        }
        else ShowError(result.Message);
    }

    private void UninstallSelected()
    {
        if (Selected == null) return;
        var result = VirtualPrinterProvisioner.Uninstall(Selected.LocalName);
        if (result.Success)
        {
            Selected.Installed = false;
            Save();
            ShowOk(result.Message);
        }
        else ShowError(result.Message);
    }

    private async Task TestHubAsync()
    {
        try
        {
            await _sender.TestConnectionAsync();
            ShowOk("✓ El hub responde correctamente.");
        }
        catch (Exception ex)
        {
            ShowError($"✗ No se pudo contactar el hub: {ex.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public void RefreshServerStatus()
    {
        OnPropertyChanged(nameof(ServerRunning));
        OnPropertyChanged(nameof(ServerStatusLabel));
    }

    private void ShowOk(string msg)
    {
        WpfApp.Current?.Dispatcher.Invoke(() => { Message = msg; IsError = false; });
    }

    private void ShowError(string msg)
    {
        WpfApp.Current?.Dispatcher.Invoke(() => { Message = msg; IsError = true; });
        _log.Warn($"Cliente: {msg}", "Cliente");
    }
}
