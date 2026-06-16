using System.Diagnostics;
using System.Windows.Threading;
using Imprelia.PrintAgent.Services;
using Imprelia.PrintAgent.Views;

namespace Imprelia.PrintAgent;

/// <summary>
/// Agente de impresión de GastroManager. Corre en la bandeja del sistema
/// (al lado del reloj) y escucha en localhost. Doble-click en el ícono abre
/// la ventana de configuración WPF.
/// </summary>
static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "ImpreliaPrintAgent_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("El agente de impresión ya está corriendo (mirá la bandeja del sistema, al lado del reloj).",
                "Imprelia Print Agent", MessageBoxButtons.OK, MessageBoxIcon.None);
            return;
        }

        ApplicationConfiguration.Initialize();

        // Registra la infraestructura WPF y carga los estilos globales en Application.Resources
        // para que los UserControls instanciados desde DataTemplates puedan resolverlos
        // con StaticResource (los recursos de Window.Resources no son visibles desde UserControls
        // en tiempo de parse de BAML).
        if (System.Windows.Application.Current == null)
        {
            var wpfApp = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
            var styles = new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Styles/MainStyles.xaml")
            };
            wpfApp.Resources.MergedDictionaries.Add(styles);
        }

        Application.Run(new TrayApp());
    }
}

/// <summary>
/// Contexto de la app: administra el ícono de la bandeja, el servidor local y
/// la ventana de configuración WPF (que se crea perezosamente y se reutiliza).
/// </summary>
public class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly AppConfig _config;
    private readonly LocalServer _server;
    private readonly AgentLogService _log;
    private readonly RemoteBridgeService _bridge;
    private readonly ClientSenderService _sender;
    private readonly IppPrintServer _ipp;
    private MainWindow? _mainWindow;

    public TrayApp()
    {
        _config = AppConfig.Load();
        Localization.Loc.SetLanguage(_config.Language);
        _log    = new AgentLogService(Dispatcher.CurrentDispatcher);
        _server = new LocalServer(_config, () => _config.DefaultPrinter);
        _bridge = new RemoteBridgeService(_config, _log);
        _sender = new ClientSenderService(_config, _log);
        _ipp    = new IppPrintServer(_config, _log, _sender);

        _tray = new NotifyIcon
        {
            Icon = AppAssets.AppIcon,
            Visible = true,
            Text = "Imprelia - Agente de impresion",
        };

        _tray.DoubleClick += (_, _) => ShowSettings();
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowSettings(); };

        var menu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("Abrir configuración");
        openItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(openItem);
        menu.Items.Add(new ToolStripSeparator());
        var aboutItem = new ToolStripMenuItem("Acerca de Imprelia Print Agent");
        aboutItem.Click += (_, _) => ShowAbout();
        menu.Items.Add(aboutItem);
        menu.Items.Add(new ToolStripSeparator());
        var exitItem = new ToolStripMenuItem("Salir");
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);
        _tray.ContextMenuStrip = menu;

        try
        {
            // En modo cliente la máquina es un emisor puro: NO levantamos el API
            // local (:9100). Así GastroManager no imprime local y usa el hub que
            // ya tiene programado. La impresión local solo existe en el principal.
            if (!_config.ClientMode.Enabled)
            {
                // PRINCIPAL: API local + receptor del hub (se registra, recibe, publica).
                _server.Start();
                _log.Info($"Escuchando en puerto {_config.Port}.");
                _ = _bridge.StartAsync();
            }
            else
            {
                // CLIENTE: emisor puro. NO levanta :9100 ni se registra en el hub
                // (si se registrara con el mismo AgentId del tenant, pisaría al
                // principal). Solo usa HTTP para descubrir y mandar trabajos.
                _log.Info("Modo cliente: API local (:9100) y receptor del hub desactivados. Solo emite al hub.", "Cliente");
            }

            _ipp.Start();
            UpdateTrayText();
            _log.Info("Agente iniciado correctamente.");

            _tray.ShowBalloonTip(3500, "Imprelia Print Agent",
                "Agente de impresion activo. Hace doble click aca para configurarlo.",
                ToolTipIcon.None);

            if (string.IsNullOrWhiteSpace(_config.DefaultPrinter))
                ShowSettings();
        }
        catch (Exception ex)
        {
            _log.Error($"No se pudo iniciar el servidor: {ex.Message}");
            MessageBox.Show($"No se pudo iniciar el servidor local en el puerto {_config.Port}.\n\n{ex.Message}\n\n" +
                "Puede que otro programa esté usando ese puerto.",
                "Imprelia Print Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowSettings()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow(_config, _config.Port, _log, _bridge, _ipp, _sender);
            // CLAVE: sin esto, los TextBox de WPF NO reciben teclado cuando la ventana
            // se muestra desde una app WinForms (el loop de mensajes es de WinForms por
            // el NotifyIcon). EnableModelessKeyboardInterop enruta el teclado a WPF.
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(_mainWindow);
            _mainWindow.ExitRequested += (_, _) => ExitApp();
        }

        _mainWindow.Show();
        _mainWindow.WindowState = System.Windows.WindowState.Normal;
        _mainWindow.Activate();
        UpdateTrayText();
    }

    private void UpdateTrayText()
    {
        var pr = _config.DefaultPrinter ?? "sin elegir impresora";
        var txt = $"Imprelia - {pr}";
        _tray.Text = txt.Length > 63 ? txt[..63] : txt;
    }

    private static void ShowAbout()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString(3) ?? "1.1.7";
        MessageBox.Show(
            $"Imprelia Print Agent  v{version}\n\n" +
            "Agente local de impresión para aplicaciones web.\n" +
            "Escucha en localhost y envía trabajos a impresoras Windows.\n\n" +
            "Autor:   Roberth Dudiver\n" +
            "Web:     www.dudiver.net\n" +
            "© 2026 Dudiver — Todos los derechos reservados.",
            "Acerca de Imprelia Print Agent",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ExitApp()
    {
        _server.Stop();
        _ipp.Stop();
        _bridge.StopAsync().Wait(3000);
        _bridge.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _mainWindow?.Close();
        Application.Exit();
    }
}

/// <summary>Maneja el arranque automático con Windows vía la clave Run del registro.</summary>
public static class StartupRegistry
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ImpreliaPrintAgent";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(ValueName) != null;
        }
        catch { return false; }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;
            if (enabled)
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe)) key.SetValue(ValueName, $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }
        }
        catch { }
    }
}
