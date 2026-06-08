using System.Diagnostics;

namespace Imprelia.PrintAgent;

/// <summary>
/// Agente de impresión de GastroManager. Corre en la bandeja del sistema
/// (al lado del reloj) y escucha en localhost. Doble-click en el ícono abre
/// la ventana de configuración.
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
        Application.Run(new TrayApp());
    }
}

/// <summary>
/// Contexto de la app: administra el ícono de la bandeja, el servidor local y
/// la ventana de configuración (que se crea perezosamente y se reutiliza).
/// </summary>
public class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly AppConfig _config;
    private readonly LocalServer _server;
    private SettingsForm? _settings;

    public TrayApp()
    {
        _config = AppConfig.Load();
        _server = new LocalServer(_config, () => _config.DefaultPrinter);

        _tray = new NotifyIcon
        {
            Icon = AppAssets.AppIcon,
            Visible = true,
            Text = "Imprelia - Agente de impresion",
        };

        // Doble-click (o click izquierdo) abre la ventana de configuración.
        _tray.DoubleClick += (_, _) => ShowSettings();
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowSettings(); };

        // Menú en clic derecho: Abrir / Acerca de / Salir.
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
            _server.Start();
            UpdateTrayText();
            _tray.ShowBalloonTip(3500, "Imprelia Print Agent",
                "Agente de impresion activo. Hace doble click aca para configurarlo.",
                ToolTipIcon.None);

            // Si todavía no eligió impresora, abrir la ventana directamente para
            // que no quede "perdido" sin saber qué hacer.
            if (string.IsNullOrWhiteSpace(_config.DefaultPrinter))
                ShowSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo iniciar el servidor local en el puerto {_config.Port}.\n\n{ex.Message}\n\n" +
                "Puede que otro programa esté usando ese puerto.",
                "Imprelia Print Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowSettings()
    {
        if (_settings == null || _settings.IsDisposed)
        {
            _settings = new SettingsForm(_config, _config.Port);
            _settings.ExitRequested += (_, _) => ExitApp();
            _settings.FormClosed += (_, _) => UpdateTrayText();
        }
        _settings.RefreshState();
        _settings.Show();
        _settings.WindowState = FormWindowState.Normal;
        _settings.Activate();
        _settings.BringToFront();
    }

    private void UpdateTrayText()
    {
        var pr = _config.DefaultPrinter ?? "sin elegir impresora";
        var txt = $"Imprelia - {pr}";
        _tray.Text = txt.Length > 63 ? txt.Substring(0, 63) : txt;
    }

    private static void ShowAbout()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString(3) ?? "1.0.0";
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
        _tray.Visible = false;
        _tray.Dispose();
        _settings?.Dispose();
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
        catch { /* ignore */ }
    }
}
