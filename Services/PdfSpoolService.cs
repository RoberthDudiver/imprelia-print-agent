using System.IO;

namespace Imprelia.PrintAgent.Services;

/// <summary>
/// Modo cliente (estilo AnyDesk): cada impresora virtual es una impresora real de
/// Windows con el driver inbox "Microsoft Print to PDF" apuntada a un archivo en la
/// carpeta de spool. Cuando el usuario imprime desde cualquier app, Windows genera
/// el PDF en ese archivo SIN diálogos; este servicio lo detecta, lo manda al hub
/// (para imprimir en el principal) y lo borra.
///
/// No usa IPP, ni mDNS, ni drivers de fabricante. El alta de la impresora la hace
/// <see cref="VirtualPrinterProvisioner"/> con un paso elevado (UAC), una vez.
/// </summary>
public sealed class PdfSpoolService : IDisposable
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly ClientSenderService _sender;

    private FileSystemWatcher? _watcher;
    private readonly HashSet<string> _inFlight = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public bool IsRunning { get; private set; }

    public PdfSpoolService(AppConfig config, AgentLogService log, ClientSenderService sender)
    {
        _config = config;
        _log = log;
        _sender = sender;
    }

    /// <summary>Carpeta donde Windows escribe los PDF capturados (puerto de cada impresora).</summary>
    public static string SpoolDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImpreliaPrintAgent", "spool");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>Ruta del archivo de salida (puerto) de una impresora virtual.</summary>
    public static string OutputFile(string printerId) => Path.Combine(SpoolDir, $"{printerId}.pdf");

    public void Start()
    {
        if (!_config.ClientMode.Enabled) return;
        Stop();
        try
        {
            var dir = SpoolDir;
            _watcher = new FileSystemWatcher(dir, "*.pdf")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
            };
            _watcher.Created += (_, e) => OnFile(e.FullPath);
            _watcher.Changed += (_, e) => OnFile(e.FullPath);
            IsRunning = true;
            _log.Info($"Captura de impresión activa. Carpeta de spool: {dir}", "Cliente");

            // Procesar lo que pudiera haber quedado de una corrida anterior.
            foreach (var f in Directory.GetFiles(dir, "*.pdf")) OnFile(f);
        }
        catch (Exception ex)
        {
            IsRunning = false;
            _log.Error($"No se pudo iniciar la captura de impresión: {ex.Message}", "Cliente");
        }
    }

    public void Stop()
    {
        try { if (_watcher != null) { _watcher.EnableRaisingEvents = false; _watcher.Dispose(); } } catch { }
        _watcher = null;
        IsRunning = false;
    }

    private void OnFile(string path)
    {
        lock (_lock)
        {
            if (_inFlight.Contains(path)) return;
            _inFlight.Add(path);
        }
        _ = Task.Run(() => ProcessAsync(path));
    }

    private async Task ProcessAsync(string path)
    {
        try
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var vp = _config.ClientMode.VirtualPrinters
                .FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
            if (vp == null)
            {
                _log.Warn($"Captura: PDF para impresora desconocida '{id}'. Lo descarto.", "Cliente");
                TryDelete(path);
                return;
            }

            var bytes = await ReadWhenReadyAsync(path);
            if (bytes == null || bytes.Length < 5)
            {
                _log.Warn($"Captura: no se pudo leer el PDF de '{vp.LocalName}'.", "Cliente");
                TryDelete(path);
                return;
            }

            _log.Info($"Captura: trabajo de '{vp.LocalName}' ({bytes.Length} bytes) → enviando al hub…", "Cliente");
            var ok = await _sender.SendPdfAsync(vp, bytes);
            _log.Info(ok
                ? $"Captura: trabajo de '{vp.LocalName}' enviado al hub."
                : $"Captura: falló el envío del trabajo de '{vp.LocalName}'.", "Cliente");

            TryDelete(path);
        }
        catch (Exception ex)
        {
            _log.Error($"Captura: error procesando '{path}': {ex.Message}", "Cliente");
            TryDelete(path);
        }
        finally
        {
            lock (_lock) { _inFlight.Remove(path); }
        }
    }

    /// <summary>Espera a que el spooler termine de escribir y libere el archivo.</summary>
    private static async Task<byte[]?> ReadWhenReadyAsync(string path)
    {
        for (int i = 0; i < 60; i++) // hasta ~30s
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                if (fs.Length > 0)
                {
                    var buf = new byte[fs.Length];
                    int read = 0;
                    while (read < buf.Length)
                    {
                        int n = await fs.ReadAsync(buf.AsMemory(read));
                        if (n == 0) break;
                        read += n;
                    }
                    return buf;
                }
            }
            catch (IOException) { /* todavía bloqueado por el spooler */ }
            await Task.Delay(500);
        }
        return null;
    }

    private void TryDelete(string path)
    {
        for (int i = 0; i < 10; i++)
        {
            try { if (File.Exists(path)) File.Delete(path); return; }
            catch { Thread.Sleep(200); }
        }
    }

    public void Dispose() => Stop();
}
