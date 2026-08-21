using System.ComponentModel;
using System.Windows;
using Imprelia.PrintAgent.Services;
using Imprelia.PrintAgent.ViewModels;

namespace Imprelia.PrintAgent.Views;

public partial class MainWindow : Window
{
    public event EventHandler? ExitRequested;

    private readonly MainViewModel _vm;

    public MainWindow(AppConfig config, LocalServer server, AgentLogService log, RemoteBridgeService bridge)
    {
        _vm = new MainViewModel(config, server, log, bridge);
        _vm.MinimizeRequested += (_, _) => Hide();
        _vm.ExitRequested     += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        DataContext = _vm;
        InitializeComponent();
    }

    public void RefreshState() => _vm.Dashboard.Refresh();

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
