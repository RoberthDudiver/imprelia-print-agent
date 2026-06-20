using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Imprelia.PrintAgent;

/// <summary>
/// Servidor HTTP local (solo localhost) que la web app contacta para imprimir.
///
/// Endpoints:
///   GET  /ping      → { ok, version, defaultPrinter }
///   GET  /printers  → { printers: [...] }
///   POST /print     → body { printer?, dataBase64 } → manda RAW a la impresora
///
/// CORS + Private Network Access: una página servida por HTTPS (la web app en
/// producción) que hace fetch a http://localhost dispara, en Chrome/Edge, un
/// preflight con el header Access-Control-Request-Private-Network. Respondemos
/// Access-Control-Allow-Private-Network: true para que el navegador lo permita.
/// </summary>
public class LocalServer
{
    private readonly HttpListener _listener = new();
    private readonly AppConfig _config;
    private readonly Func<string?> _getDefaultPrinter;
    private readonly DateTime _startedAt = DateTime.Now;
    private readonly IPrinterDiscoveryService _printers;
    private readonly IPrinterRouteService _routes;
    private readonly IJobHistoryService _history;
    private readonly IPrintService _printService;
    private CancellationTokenSource? _cts;

    public const string Version = "1.1.7";

    public LocalServer(AppConfig config, Func<string?> getDefaultPrinter)
    {
        _config = config;
        _config.EnsureDefaults();
        _getDefaultPrinter = getDefaultPrinter;
        _printers = new WindowsPrinterDiscoveryService(_config);
        _routes = new PrinterRouteService(_config);
        _history = new JobHistoryService();
        _printService = new PrintService(_printers, _routes, _history);
        // Escuchamos en 127.0.0.1 (no en 0.0.0.0) — solo accesible desde esta PC.
        _listener.Prefixes.Add($"http://{_config.Host}:{_config.Port}/");
        if (!_config.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            _listener.Prefixes.Add($"http://localhost:{_config.Port}/");
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener.Start();
        _ = Task.Run(() => LoopAsync(_cts.Token));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); _listener.Stop(); } catch { /* ignore */ }
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch { break; } // listener cerrado
            _ = Task.Run(() => HandleAsync(ctx));
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        // ── CORS + Private Network Access ────────────────────────────────────
        ApplyCors(req, res);
        try
        {
            // Preflight
            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            var path = req.Url?.AbsolutePath?.TrimEnd('/').ToLowerInvariant() ?? "";

            if (!IsAuthorized(req, path))
            {
                WriteJson(res, 401, ApiError("UNAUTHORIZED", "API key inválida o ausente.", null));
                return;
            }

            if (req.HttpMethod == "GET" && (path == "/docs" || path == "/api/docs"))
            {
                WriteHtml(res, 200, ApiDocumentation.ScalarHtml());
                return;
            }

            if (req.HttpMethod == "GET" && path == "/openapi.json")
            {
                WriteText(res, 200, "application/json; charset=utf-8", ApiDocumentation.OpenApiJson());
                return;
            }

            if (req.HttpMethod == "GET" && path == "/ping")
            {
                WriteJson(res, 200, new
                {
                    ok = true,
                    app = "ImpreliaPrintAgent",
                    version = Version,
                    defaultPrinter = _getDefaultPrinter(),
                });
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/health")
            {
                WriteJson(res, 200, new
                {
                    status = "Running",
                    version = Version,
                    port = _config.Port,
                    uptime = (DateTime.Now - _startedAt).ToString(@"dd\.hh\:mm\:ss"),
                    printersCount = _printers.ListPrinters().Count,
                });
                return;
            }

            if (req.HttpMethod == "GET" && (path == "/printers" || path == "/api/printers"))
            {
                if (path == "/printers")
                {
                    WriteJson(res, 200, new
                    {
                        ok = true,
                        printers = RawPrinter.ListPrinters(),
                        defaultPrinter = _getDefaultPrinter(),
                    });
                }
                else
                {
                    WriteJson(res, 200, new { printers = _printers.ListPrinters() });
                }
                return;
            }

            if (req.HttpMethod == "POST" && path == "/print")
            {
                var data = await ReadJson<LegacyPrintRequest>(req, res);
                if (data == null) return;

                if (data == null || string.IsNullOrEmpty(data.DataBase64))
                {
                    WriteJson(res, 400, new { ok = false, error = "Falta dataBase64" });
                    return;
                }

                var printer = !string.IsNullOrWhiteSpace(data.Printer) ? data.Printer! : _getDefaultPrinter();
                if (string.IsNullOrWhiteSpace(printer))
                {
                    WriteJson(res, 400, new { ok = false, error = "No hay impresora configurada. Elegí una en el agente." });
                    return;
                }

                byte[] bytes;
                try { bytes = Convert.FromBase64String(data.DataBase64); }
                catch { WriteJson(res, 400, new { ok = false, error = "dataBase64 no es base64 válido" }); return; }

                var err = RawPrinter.SendBytes(printer, bytes);
                if (err == null)
                    WriteJson(res, 200, new { ok = true, printer });
                else
                    WriteJson(res, 500, new { ok = false, error = err, printer });
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/print")
            {
                var data = await ReadJson<UniversalPrintRequest>(req, res);
                if (data == null) return;
                WritePrintResponse(res, _printService.Print(data, "/api/print"));
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/print/by-purpose")
            {
                var data = await ReadJson<PrintByPurposeRequest>(req, res);
                if (data == null) return;
                WritePrintResponse(res, _printService.PrintByPurpose(data));
                return;
            }

            if (req.HttpMethod == "POST" && path == "/api/printers/test")
            {
                var data = await ReadJson<PrinterTestRequest>(req, res);
                if (data == null) return;
                WritePrintResponse(res, PrintTest(data));
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/settings")
            {
                WriteJson(res, 200, ToSettingsPayload());
                return;
            }

            if (req.HttpMethod == "PUT" && path == "/api/settings")
            {
                var data = await ReadJson<AppConfig>(req, res);
                if (data == null) return;
                var oldPort = _config.Port;
                var oldHost = _config.Host;
                if (data.Port != oldPort && !IsPortAvailable(data.Port))
                {
                    WriteJson(res, 409, ApiError("PORT_IN_USE", $"El puerto {data.Port} está ocupado.", null));
                    return;
                }
                UpdateSettings(data);
                WriteJson(res, 200, ToSettingsPayload(restartRequired: data.Port != oldPort || data.Host != oldHost));
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/settings/printer-routes")
            {
                WriteJson(res, 200, new { routes = _routes.GetRoutes() });
                return;
            }

            if (req.HttpMethod == "PUT" && path == "/api/settings/printer-routes")
            {
                var data = await ReadJson<PrinterRoutesPayload>(req, res);
                if (data == null) return;
                _routes.SaveRoutes(data.Routes);
                WriteJson(res, 200, new { success = true, routes = _routes.GetRoutes() });
                return;
            }

            if (req.HttpMethod == "GET" && path == "/api/jobs/recent")
            {
                WriteJson(res, 200, new { jobs = _history.Recent() });
                return;
            }

            WriteJson(res, 404, new { ok = false, error = "Ruta no encontrada" });
        }
        catch (Exception ex)
        {
            try { WriteJson(res, 500, new { ok = false, error = ex.Message }); } catch { }
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private void ApplyCors(HttpListenerRequest req, HttpListenerResponse res)
    {
        if (!_config.AllowCors) return;

        var origin = req.Headers["Origin"];
        var allowOrigin = "*";
        if (!string.IsNullOrWhiteSpace(origin) && _config.AllowedOrigins.Count > 0)
        {
            allowOrigin = _config.AllowedOrigins.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase))
                ? origin
                : "null";
        }

        res.AddHeader("Access-Control-Allow-Origin", allowOrigin);
        res.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, OPTIONS");
        res.AddHeader("Access-Control-Allow-Headers", "Content-Type, X-Api-Key");
        res.AddHeader("Access-Control-Allow-Private-Network", "true");
    }

    private async Task<T?> ReadJson<T>(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        try { return JsonSerializer.Deserialize<T>(body, JsonOpts); }
        catch { WriteJson(res, 400, ApiError("INVALID_JSON", "JSON inválido.", null)); return default; }
    }

    private bool IsAuthorized(HttpListenerRequest req, string path)
    {
        if (!_config.RequireApiKey) return true;
        if (path is "/ping" or "/api/health" or "/docs" or "/api/docs" or "/openapi.json") return true;
        var provided = req.Headers["X-Api-Key"];
        return !string.IsNullOrWhiteSpace(_config.ApiKey) && provided == _config.ApiKey;
    }

    private static bool IsPortAvailable(int port)
    {
        if (port <= 0 || port > 65535) return false;
        try
        {
            var active = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return !active.Any(ep => ep.Port == port);
        }
        catch
        {
            return true;
        }
    }

    private PrintResponse PrintTest(PrinterTestRequest request)
    {
        var printer = !string.IsNullOrWhiteSpace(request.PrinterName) ? request.PrinterName : _getDefaultPrinter();
        if (string.IsNullOrWhiteSpace(printer))
            return new PrintResponse { Success = false, ErrorCode = "NO_DEFAULT_PRINTER", Message = "No hay impresora configurada." };

        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "epos" : request.ContentType.ToLowerInvariant();
        var printRequest = new UniversalPrintRequest
        {
            PrinterName = printer,
            ContentType = contentType,
            JobName = $"Prueba {contentType}",
            Copies = 1,
        };

        if (contentType == "fiscal")
            return new PrintResponse { Success = false, ErrorCode = "FISCAL_NOT_CONFIGURED", Message = "La prueba fiscal requiere configuración del controlador fiscal." };
        if (contentType == "pdf")
            return new PrintResponse { Success = false, ErrorCode = "PDF_TEST_NOT_AVAILABLE", Message = "La prueba PDF requiere enviar un PDF base64 desde la aplicación." };

        var payload = PrintTestBuilder.Build(printer, contentType);
        if (string.IsNullOrWhiteSpace(payload.Content))
            return new PrintResponse { Success = false, ErrorCode = "UNSUPPORTED_CONTENT_TYPE", Message = $"Tipo de contenido no soportado: {contentType}." };

        printRequest.ContentType = payload.ContentType;
        printRequest.Content = payload.Content;
        printRequest.JobName = payload.JobName;

        return _printService.Print(printRequest, "/api/printers/test");
    }

    private object ToSettingsPayload(bool restartRequired = false) => new
    {
        server = new { host = _config.Host, port = _config.Port, allowCors = _config.AllowCors },
        security = new
        {
            bindAddress = _config.Host,
            allowedOrigins = _config.AllowedOrigins,
            requireApiKey = _config.RequireApiKey,
            apiKey = _config.ApiKey,
        },
        defaultPrinter = _config.DefaultPrinter,
        printerTypes = _config.PrinterTypes,
        routes = _routes.GetRoutes(),
        restartRequired,
    };

    private void UpdateSettings(AppConfig incoming)
    {
        if (!string.IsNullOrWhiteSpace(incoming.DefaultPrinter)) _config.DefaultPrinter = incoming.DefaultPrinter;
        if (!string.IsNullOrWhiteSpace(incoming.Host)) _config.Host = incoming.Host;
        if (incoming.Port > 0) _config.Port = incoming.Port;
        _config.AllowCors = incoming.AllowCors;
        _config.RequireApiKey = incoming.RequireApiKey;
        _config.ApiKey = incoming.ApiKey;
        _config.AllowedOrigins = incoming.AllowedOrigins ?? new List<string>();
        _config.PrinterTypes = incoming.PrinterTypes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (incoming.PrinterRoutes.Count > 0) _config.PrinterRoutes = incoming.PrinterRoutes;
        _config.EnsureDefaults();
        _config.Save();
    }

    private static void WritePrintResponse(HttpListenerResponse res, PrintResponse response)
    {
        if (response.Success)
            WriteJson(res, 200, response);
        else
            WriteJson(res, StatusForError(response.ErrorCode), ApiError(response.ErrorCode, response.Message, response.Details));
    }

    private static int StatusForError(string? errorCode) => errorCode switch
    {
        "PRINTER_NOT_FOUND" or "PRINTER_ROUTE_NOT_FOUND" => 404,
        "INVALID_JSON" or "INVALID_PDF" or "EMPTY_CONTENT" or "UNSUPPORTED_CONTENT_TYPE" => 400,
        "FISCAL_NOT_CONFIGURED" or "PDF_TEST_NOT_AVAILABLE" => 422,
        _ => 500,
    };

    private static object ApiError(string? code, string? message, object? details) => new
    {
        success = false,
        errorCode = code ?? "ERROR",
        message = message ?? "Error inesperado.",
        details,
    };

    private static void WriteJson(HttpListenerResponse res, int status, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        WriteText(res, status, "application/json; charset=utf-8", json);
    }

    private static void WriteHtml(HttpListenerResponse res, int status, string html) =>
        WriteText(res, status, "text/html; charset=utf-8", html);

    private static void WriteText(HttpListenerResponse res, int status, string contentType, string body)
    {
        var buf = Encoding.UTF8.GetBytes(body);
        res.StatusCode = status;
        res.ContentType = contentType;
        res.ContentLength64 = buf.Length;
        res.OutputStream.Write(buf, 0, buf.Length);
        res.Close();
    }

    private sealed class PrinterRoutesPayload
    {
        public List<PrinterRoute> Routes { get; set; } = new();
    }
}
