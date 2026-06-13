using System.IO;
using System.Text.Json;

namespace Imprelia.PrintAgent;

/// <summary>
/// Configuración del puente remoto. Desactivado por defecto: si Enabled=false
/// el agente no intenta conectarse a ningún servidor externo.
/// </summary>
public class RemoteBridgeConfig
{
    public bool Enabled { get; set; } = false;
    public string ServerUrl { get; set; } = "";
    public string AgentId { get; set; } = "";
    public string ApiKey { get; set; } = "";
    /// <summary>"signalr" (recomendado) o "polling".</summary>
    public string Mode { get; set; } = "signalr";
    public int FallbackPollingSeconds { get; set; } = 10;
    public bool AutoReconnect { get; set; } = true;
}

/// <summary>
/// Configuración persistente del agente, guardada en
/// %APPDATA%\ImpreliaPrintAgent\config.json. Sobrevive reinicios.
/// </summary>
public class AppConfig
{
    public string? DefaultPrinter { get; set; }
    public int Port { get; set; } = 9100;
    public string Host { get; set; } = "127.0.0.1";
    public bool AllowCors { get; set; } = true;
    public bool RequireApiKey { get; set; }
    public string? ApiKey { get; set; }
    public List<string> AllowedOrigins { get; set; } = new();

    public Dictionary<string, PrinterRoute> PrinterRoutes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ticket"] = new PrinterRoute { Purpose = "ticket", ContentType = "epos" },
        ["kitchen_order"] = new PrinterRoute { Purpose = "kitchen_order", ContentType = "epos" },
        ["report"] = new PrinterRoute { Purpose = "report", ContentType = "pdf" },
        // "raw": GastroManager manda la etiqueta como bitmap TSPL (XP-470B y
        // compatibles). NO usar "zpl" — ese modo va en ASCII y rompe el binario.
        ["label"] = new PrinterRoute { Purpose = "label", ContentType = "raw" },
        ["fiscal"] = new PrinterRoute { Purpose = "fiscal", ContentType = "fiscal" },
    };

    public Dictionary<string, string> PrinterTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Puente remoto (opcional). Desactivado por defecto.</summary>
    public RemoteBridgeConfig RemoteBridge { get; set; } = new();

    private static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImpreliaPrintAgent");

    private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg != null)
                {
                    cfg.EnsureDefaults();
                    return cfg;
                }
            }
        }
        catch { /* config corrupta → usar defaults */ }
        return new AppConfig();
    }

    public void EnsureDefaults()
    {
        if (string.IsNullOrWhiteSpace(Host)) Host = "127.0.0.1";
        if (Port <= 0) Port = 9100;
        AllowedOrigins ??= new List<string>();
        PrinterRoutes ??= new Dictionary<string, PrinterRoute>(StringComparer.OrdinalIgnoreCase);
        PrinterTypes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        RemoteBridge ??= new RemoteBridgeConfig();

        AddRouteDefault("ticket", "epos");
        AddRouteDefault("kitchen_order", "epos");
        AddRouteDefault("report", "pdf");
        AddRouteDefault("label", "raw");
        AddRouteDefault("fiscal", "fiscal");
    }

    private void AddRouteDefault(string purpose, string contentType)
    {
        if (!PrinterRoutes.ContainsKey(purpose))
            PrinterRoutes[purpose] = new PrinterRoute { Purpose = purpose, ContentType = contentType };
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { /* no-op: si no podemos guardar, igual seguimos funcionando en memoria */ }
    }
}
