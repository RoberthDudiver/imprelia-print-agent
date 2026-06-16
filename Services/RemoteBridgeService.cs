using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace Imprelia.PrintAgent.Services;

public enum BridgeConnectionState
{
    Disabled,
    Connecting,
    Connected,
    Disconnected,
    AuthenticationFailed,
    ServerUnavailable,
}

/// <summary>
/// Puente remoto opcional. Cuando está habilitado, se conecta hacia afuera a un
/// servidor remoto (SignalR o polling HTTP) para recibir trabajos de impresión.
///
/// NO abre puertos públicos. NO modifica el servidor local (localhost:9100).
/// Usa el mismo motor de impresión y las mismas rutas configuradas en AppConfig.
/// </summary>
public sealed class RemoteBridgeService : IDisposable
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly IPrintService _print;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private HubConnection? _hub;
    private CancellationTokenSource? _cts;
    private volatile BridgeConnectionState _state = BridgeConnectionState.Disabled;
    private DateTime? _lastConnectedAt;
    private string? _lastError;
    private bool _disposed;

    public event EventHandler<BridgeConnectionState>? StateChanged;

    public BridgeConnectionState State => _state;
    public DateTime? LastConnectedAt => _lastConnectedAt;
    public string? LastError => _lastError;

    public RemoteBridgeService(AppConfig config, AgentLogService log)
    {
        _config = config;
        _log = log;
        _print = new PrintService(
            new WindowsPrinterDiscoveryService(config),
            new PrinterRouteService(config),
            new JobHistoryService());
    }

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    public async Task StartAsync()
    {
        if (_disposed) return;

        if (!_config.RemoteBridge.Enabled)
        {
            SetState(BridgeConnectionState.Disabled);
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.RemoteBridge.ServerUrl) ||
            string.IsNullOrWhiteSpace(_config.RemoteBridge.AgentId))
        {
            _log.Warn("Bridge: ServerUrl o AgentId sin configurar. Bridge no iniciado.", "Bridge");
            return;
        }

        // Cancelar conexión anterior si la hay
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        var mode = (_config.RemoteBridge.Mode ?? "signalr").ToLowerInvariant();
        if (mode == "polling")
            _ = Task.Run(() => RunPollingAsync(_cts.Token), _cts.Token);
        else
            _ = Task.Run(() => RunSignalRAsync(_cts.Token), _cts.Token);
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_hub != null)
        {
            try { await _hub.StopAsync(); } catch { }
            try { await _hub.DisposeAsync(); } catch { }
            _hub = null;
        }
        SetState(BridgeConnectionState.Disabled);
        _log.Info("Remote bridge detenido.", "Bridge");
    }

    // ── Modo SignalR ──────────────────────────────────────────────────────────

    private async Task RunSignalRAsync(CancellationToken ct)
    {
        var cfg = _config.RemoteBridge;
        // El hub vive bajo /hubs/ (los reverse proxies suelen tener el WebSocket
        // configurado para ese prefijo). Los endpoints HTTP siguen en /imprelia/*.
        var hubUrl = cfg.ServerUrl.TrimEnd('/') + "/hubs/imprelia";

        _log.Info($"Conectando al servidor remoto: {hubUrl}", "Bridge");
        SetState(BridgeConnectionState.Connecting);

        try
        {
            if (_hub != null)
            {
                try { await _hub.DisposeAsync(); } catch { }
                _hub = null;
            }

            _hub = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.Headers["X-Agent-Id"] = cfg.AgentId;
                    options.Headers["X-Api-Key"] = cfg.ApiKey ?? "";
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) })
                .Build();

            _hub.On<RemotePrintJob>("PrintJob", job =>
            {
                _ = Task.Run(() => HandleJobAsync(job, ct), ct);
            });

            _hub.Reconnecting += ex =>
            {
                SetState(BridgeConnectionState.Connecting);
                _log.Warn($"Bridge reconectando: {ex?.Message ?? "conexión perdida"}", "Bridge");
                return Task.CompletedTask;
            };

            _hub.Reconnected += async _ =>
            {
                // CLAVE: al reconectar, la connectionId cambia → hay que volver a
                // registrarse, si no el servidor no sabe a quién mandarle los trabajos
                // (síntoma: "andaba y de golpe no imprime hasta deshabilitar/habilitar").
                try { await _hub.InvokeAsync("RegisterAgent", cfg.AgentId, cfg.ApiKey ?? ""); await PublishPrintersAsync(); }
                catch (Exception ex) { _log.Warn($"Bridge: re-registro tras reconexión falló — {ex.Message}", "Bridge"); }
                SetState(BridgeConnectionState.Connected);
                _lastConnectedAt = DateTime.Now;
                _log.Info("Bridge reconectado y re-registrado.", "Bridge");
            };

            _hub.Closed += async ex =>
            {
                SetState(BridgeConnectionState.Disconnected);
                _log.Warn($"Bridge desconectado: {ex?.Message ?? "sin error"}", "Bridge");
                if (cfg.AutoReconnect && !ct.IsCancellationRequested)
                {
                    await Task.Delay(30_000, ct);
                    await RunSignalRAsync(ct);
                }
            };

            await _hub.StartAsync(ct);

            // Registrar el agente en el servidor
            await _hub.InvokeAsync("RegisterAgent", cfg.AgentId, cfg.ApiKey ?? "", ct);

            // Publicar las impresoras locales para que los clientes las descubran.
            await PublishPrintersAsync();

            SetState(BridgeConnectionState.Connected);
            _lastConnectedAt = DateTime.Now;
            _lastError = null;
            _log.Info($"Bridge conectado como agente '{cfg.AgentId}'.", "Bridge");

            // Mantener el task vivo mientras el hub esté conectado
            while (!ct.IsCancellationRequested && _hub.State == HubConnectionState.Connected)
                await Task.Delay(5_000, ct);
        }
        catch (OperationCanceledException) { /* salida limpia */ }
        catch (Exception ex) when (IsAuthError(ex))
        {
            _lastError = ex.Message;
            SetState(BridgeConnectionState.AuthenticationFailed);
            _log.Error($"Bridge: autenticación rechazada — {ex.Message}", "Bridge");
            // No reintentar si la auth falla: el usuario debe corregir la API key
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            SetState(BridgeConnectionState.ServerUnavailable);
            _log.Error($"Bridge: no se pudo conectar — {ex.Message}", "Bridge");

            if (cfg.AutoReconnect && !ct.IsCancellationRequested)
            {
                await Task.Delay(30_000, ct);
                if (!ct.IsCancellationRequested)
                    await RunSignalRAsync(ct);
            }
        }
    }

    // ── Modo Polling ──────────────────────────────────────────────────────────

    private async Task RunPollingAsync(CancellationToken ct)
    {
        var cfg = _config.RemoteBridge;
        var delay = TimeSpan.FromSeconds(Math.Max(5, cfg.FallbackPollingSeconds));

        _log.Info($"Bridge en modo polling cada {delay.TotalSeconds}s.", "Bridge");
        SetState(BridgeConnectionState.Connecting);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var url = $"{cfg.ServerUrl.TrimEnd('/')}/imprelia/jobs/pending?agentId={Uri.EscapeDataString(cfg.AgentId)}";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("X-Agent-Id", cfg.AgentId);
                req.Headers.TryAddWithoutValidation("X-Api-Key", cfg.ApiKey ?? "");

                using var res = await _http.SendAsync(req, ct);

                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    SetState(BridgeConnectionState.AuthenticationFailed);
                    _log.Error("Bridge polling: credenciales rechazadas. Polling detenido.", "Bridge");
                    return;
                }

                res.EnsureSuccessStatusCode();
                SetState(BridgeConnectionState.Connected);
                _lastConnectedAt = DateTime.Now;
                _lastError = null;

                var body = await res.Content.ReadAsStringAsync(ct);
                var jobs = JsonSerializer.Deserialize<List<RemotePrintJob>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (jobs != null)
                    foreach (var job in jobs)
                        _ = Task.Run(() => HandleJobAsync(job, ct), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                SetState(BridgeConnectionState.ServerUnavailable);
                _lastError = ex.Message;
                _log.Warn($"Bridge polling error: {ex.Message}", "Bridge");
            }

            await Task.Delay(delay, ct);
        }

        SetState(BridgeConnectionState.Disabled);
    }

    // ── Procesamiento de trabajos ─────────────────────────────────────────────

    private async Task HandleJobAsync(RemotePrintJob job, CancellationToken ct)
    {
        // Una impresora explícita (modo cliente/impresora virtual) llega por metadata.
        var explicitPrinter = GetMeta(job, "printer");

        _log.Info(
            $"Job remoto: {job.JobId} → {(string.IsNullOrWhiteSpace(explicitPrinter) ? $"ruta '{job.Route}'" : $"impresora '{explicitPrinter}'")}",
            "Bridge");
        await ReportStatusAsync(job, "received_by_agent", null);

        if (string.IsNullOrWhiteSpace(job.Route) && string.IsNullOrWhiteSpace(explicitPrinter))
        {
            await ReportStatusAsync(job, "invalid_payload", "Falta Route o printer");
            _log.Warn($"Job {job.JobId} rechazado: sin ruta ni impresora.", "Bridge");
            return;
        }

        try
        {
            await ReportStatusAsync(job, "printing", null);

            var jobName = $"Remote/{job.JobId[..Math.Min(8, job.JobId.Length)]}";

            PrintResponse response;
            if (!string.IsNullOrWhiteSpace(explicitPrinter))
            {
                // Impresión directa a una impresora nombrada en el principal.
                response = _print.Print(new UniversalPrintRequest
                {
                    PrinterName = explicitPrinter,
                    ContentType = NormalizeType(job.Type),
                    Content = job.Content,
                    Copies = Math.Max(1, job.Copies),
                    JobName = jobName,
                }, "/remote/explicit");
            }
            else
            {
                // Impresión por ruta/propósito configurado.
                response = _print.PrintByPurpose(new PrintByPurposeRequest
                {
                    Purpose = job.Route,
                    ContentType = NormalizeType(job.Type),
                    Content = job.Content,
                    Copies = Math.Max(1, job.Copies),
                    JobName = jobName,
                });
            }

            if (response.Success)
            {
                await ReportStatusAsync(job, "printed", null);
                _log.Info($"Job {job.JobId} impreso OK.", "Bridge");
            }
            else
            {
                var status = response.ErrorCode switch
                {
                    "PRINTER_ROUTE_NOT_FOUND" => "printer_not_found",
                    "PRINTER_NOT_FOUND" => "printer_not_found",
                    _ => "failed"
                };
                await ReportStatusAsync(job, status, response.Message);
                _log.Warn($"Job {job.JobId} falló ({response.ErrorCode}): {response.Message}", "Bridge");
            }
        }
        catch (Exception ex)
        {
            await ReportStatusAsync(job, "failed", ex.Message);
            _log.Error($"Error en job {job.JobId}: {ex.Message}", "Bridge");
        }
    }

    private async Task ReportStatusAsync(RemotePrintJob job, string status, string? error)
    {
        try
        {
            if (_hub?.State == HubConnectionState.Connected)
            {
                await _hub.InvokeAsync("ReportStatus", job.JobId, status, error);
                return;
            }

            // Fallback HTTP si el hub no está disponible (modo polling o hub caído)
            var cfg = _config.RemoteBridge;
            if (string.IsNullOrWhiteSpace(cfg.ServerUrl)) return;

            var url = $"{cfg.ServerUrl.TrimEnd('/')}/imprelia/jobs/{Uri.EscapeDataString(job.JobId)}/status";
            var body = JsonSerializer.Serialize(new { status, error, agentId = cfg.AgentId });
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("X-Agent-Id", cfg.AgentId);
            req.Headers.TryAddWithoutValidation("X-Api-Key", cfg.ApiKey ?? "");
            using var _ = await _http.SendAsync(req, CancellationToken.None);
        }
        catch { /* fire-and-forget: no propagamos errores de reporte */ }
    }

    // ── Prueba de conexión (llamada desde el ViewModel) ───────────────────────

    public async Task TestConnectionAsync()
    {
        var cfg = _config.RemoteBridge;
        if (string.IsNullOrWhiteSpace(cfg.ServerUrl))
            throw new InvalidOperationException("ServerUrl no configurada.");

        var url = $"{cfg.ServerUrl.TrimEnd('/')}/imprelia/status";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("X-Agent-Id", cfg.AgentId);
        req.Headers.TryAddWithoutValidation("X-Api-Key", cfg.ApiKey ?? "");

        using var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetState(BridgeConnectionState state)
    {
        _state = state;
        StateChanged?.Invoke(this, state);
    }

    private static bool IsAuthError(Exception ex)
    {
        var msg = ex.Message;
        return msg.Contains("401") || msg.Contains("403") ||
               msg.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("Forbidden", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Envía la lista de impresoras locales al hub (descubrimiento por clientes).</summary>
    private async Task PublishPrintersAsync()
    {
        try
        {
            if (_hub == null || _hub.State != HubConnectionState.Connected) return;

            // En modo cliente NO publicamos: este agente es un emisor, no expone
            // impresoras al hub. Si lo hiciéramos, otros clientes nos verían como
            // un "principal" y descubrirían las impresoras locales del cliente.
            // Además, publicamos una lista vacía para limpiar cualquier publicación
            // anterior (cuando el agente venía operando como principal).
            if (_config.ClientMode.Enabled)
            {
                try
                {
                    await _hub.InvokeAsync("PublishPrinters",
                        _config.RemoteBridge.AgentId, new List<object>());
                }
                catch { /* el hub puede no soportarlo: no es crítico */ }
                _log.Info("Bridge: modo cliente — impresoras locales no publicadas.", "Bridge");
                return;
            }

            var discovery = new WindowsPrinterDiscoveryService(_config);
            var printers = discovery.ListPrinters()
                .Select(p => new { p.Name, p.Type, p.IsDefault })
                .ToList();

            await _hub.InvokeAsync("PublishPrinters", _config.RemoteBridge.AgentId, printers);
            _log.Info($"Bridge: {printers.Count} impresoras publicadas para descubrimiento.", "Bridge");
        }
        catch (Exception ex)
        {
            // El hub puede no soportar PublishPrinters (versión vieja): no es crítico.
            _log.Warn($"Bridge: no se pudieron publicar impresoras — {ex.Message}", "Bridge");
        }
    }

    private static string? GetMeta(RemotePrintJob job, string key) =>
        job.Metadata != null && job.Metadata.TryGetValue(key, out var v) ? v : null;

    private static string NormalizeType(string? type) =>
        (type?.ToLowerInvariant() ?? "escpos") switch
        {
            "escpos" or "epos" => "epos",
            "zpl" => "zpl",
            "pdf" => "pdf",
            "text" => "text",
            "raw" => "raw",
            _ => "epos"
        };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        try { _hub?.DisposeAsync().AsTask().Wait(2000); } catch { }
        _http.Dispose();
    }
}
