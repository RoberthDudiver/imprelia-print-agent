using System.ComponentModel;
using System.Windows;
using Imprelia.PrintAgent.Services;
using Imprelia.PrintAgent.ViewModels;

namespace Imprelia.PrintAgent.Views;

public partial class MainWindow : Window
{
    public event EventHandler? ExitRequested;

    private readonly MainViewModel _vm;
    private readonly AppConfig _config;

    public MainWindow(AppConfig config, int startedPort, AgentLogService log, RemoteBridgeService bridge,
                      PdfSpoolService spool, ClientSenderService sender)
    {
        _config = config;
        _vm = new MainViewModel(config, startedPort, log, bridge, spool, sender);
        _vm.MinimizeRequested += (_, _) => Hide();
        _vm.ExitRequested     += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        _vm.Settings.ReconfigureRequested += (_, _) => ShowSetup();

        DataContext = _vm;
        InitializeComponent();
    }

    private void ShowSetup()
    {
        var win = new SetupWindow(_config) { Owner = this };
        if (win.ShowDialog() == true)
        {
            var msg = _config.ClientMode.Enabled
                ? "Rol Cliente guardado. Reiniciá el agente (Salir y abrir de nuevo): se apaga el API local (:9100) y se levanta la impresión remota."
                : "Rol Principal guardado. Reiniciá el agente (Salir y abrir de nuevo): se levanta el API local (:9100) y la conexión al hub.";
            System.Windows.MessageBox.Show(msg, "Imprelia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    public void RefreshState() => _vm.Dashboard.Refresh();

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
