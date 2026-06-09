using System.ComponentModel;
using System.Windows;
using Imprelia.PrintAgent.ViewModels;

namespace Imprelia.PrintAgent.Views;

public partial class MainWindow : Window
{
    public event EventHandler? ExitRequested;

    private readonly MainViewModel _vm;

    public MainWindow(AppConfig config, int startedPort, AgentLogService log)
    {
        _vm = new MainViewModel(config, startedPort, log);
        _vm.MinimizeRequested += (_, _) => Hide();
        _vm.ExitRequested += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

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
