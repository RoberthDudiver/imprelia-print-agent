using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Imprelia.PrintAgent.Services;

/// <summary>Impresora descubierta de un agente principal vía el hub.</summary>
public sealed class DiscoveredPrinter
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsDefault { get; set; }
}

/// <summary>Agente conectado al hub (principal candidato), tal como lo lista el hub.</summary>
public sealed class RemoteAgent
{
    public string AgentId { get; set; } = "";
    public int PrinterCount { get; set; }
}

/// <summary>
/// Emisor del modo cliente. Toma un documento capturado (PDF) por una impresora
/// virtual y lo envía al hub para que el agente principal lo imprima en la
/// impresora real configurada.
///
/// Reutiliza ServerUrl/ApiKey de RemoteBridge (el mismo hub). El hub debe tener
/// habilitado el endpoint productor POST /imprelia/jobs (ExposeHttpJobApi=true).
/// </summary>
public sealed class ClientSenderService
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public ClientSenderService(AppConfig config, AgentLogService log)
    {
        _config = config;
        _log = log;
    }

    /// <summary>Envía un PDF capturado al hub para la impresora virtual indicada.</summary>
    public async Task<bool> SendPdfAsync(ClientVirtualPrinter vp, byte[] pdfBytes, CancellationToken ct = default)
    {
        var server = _config.RemoteBridge.ServerUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(server))
        {
            _log.Error("Cliente: ServerUrl del hub sin configurar. No se puede enviar.", "Cliente");
            return false;
        }
        if (string.IsNullOrWhiteSpace(vp.TargetAgentId))
        {
            _log.Error($"Cliente: la impresora virtual '{vp.LocalName}' no tiene agente destino.", "Cliente");
            return false;
        }
        if (pdfBytes.Length < 5)
        {
            _log.Warn($"Cliente: documento vacío para '{vp.LocalName}'. Ignorado.", "Cliente");
            return false;
        }

        // El servidor exige Route no vacío. Si la impresora virtual apunta a una
        // impresora explícita, mandamos un placeholder y el destino la lee de metadata.
        var route = !string.IsNullOrWhiteSpace(vp.Route) ? vp.Route : "__direct__";

        var meta = new Dictionary<string, string>
        {
            ["source"] = "client-ipp",
            ["virtual"] = vp.LocalName,
        };
        if (!string.IsNullOrWhiteSpace(vp.TargetPrinter))
            meta["printer"] = vp.TargetPrinter;

        var payload = new
        {
            agentId = vp.TargetAgentId,
            route,
            type = "pdf",
            content = Convert.ToBase64String(pdfBytes),
            copies = 1,
            metadata = meta,
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var url = $"{server}/imprelia/jobs";
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            req.Headers.TryAddWithoutValidation("X-Api-Key", _config.RemoteBridge.ApiKey ?? "");
            req.Headers.TryAddWithoutValidation("X-Agent-Id", _config.RemoteBridge.AgentId ?? "");

            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await SafeRead(res, ct);
                _log.Error($"Cliente: hub rechazó el job ({(int)res.StatusCode}). {body}", "Cliente");
                return false;
            }

            var target = !string.IsNullOrWhiteSpace(vp.TargetPrinter) ? vp.TargetPrinter : $"ruta {route}";
            _log.Info($"Cliente: '{vp.LocalName}' → {vp.TargetAgentId}/{target} ({pdfBytes.Length / 1024} KB) enviado.", "Cliente");
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"Cliente: error enviando job de '{vp.LocalName}': {ex.Message}", "Cliente");
            return false;
        }
    }

    /// <summary>Lista los agentes conectados al hub (para elegir el principal).</summary>
    public async Task<List<RemoteAgent>> DiscoverAgentsAsync(CancellationToken ct = default)
    {
        var server = _config.RemoteBridge.ServerUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(server))
            throw new InvalidOperationException("ServerUrl del hub sin configurar.");

        var url = $"{server}/imprelia/agents";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("X-Api-Key", _config.RemoteBridge.ApiKey ?? "");
        req.Headers.TryAddWithoutValidation("X-Agent-Id", _config.RemoteBridge.AgentId ?? "");

        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync(ct);
        var list = JsonSerializer.Deserialize<List<RemoteAgent>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return list ?? new List<RemoteAgent>();
    }

    /// <summary>Descubre las impresoras que publicó un agente principal en el hub.</summary>
    public async Task<List<DiscoveredPrinter>> DiscoverPrintersAsync(string agentId, CancellationToken ct = default)
    {
        var server = _config.RemoteBridge.ServerUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(server))
            throw new InvalidOperationException("ServerUrl del hub sin configurar.");
        if (string.IsNullOrWhiteSpace(agentId))
            throw new InvalidOperationException("Indicá el AgentId del agente principal.");

        var url = $"{server}/imprelia/agents/{Uri.EscapeDataString(agentId)}/printers";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("X-Api-Key", _config.RemoteBridge.ApiKey ?? "");
        req.Headers.TryAddWithoutValidation("X-Agent-Id", _config.RemoteBridge.AgentId ?? "");

        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync(ct);
        var list = JsonSerializer.Deserialize<List<DiscoveredPrinter>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return list ?? new List<DiscoveredPrinter>();
    }

    /// <summary>Prueba que el hub responde (mismo endpoint de estado del bridge).</summary>
    public async Task TestConnectionAsync()
    {
        var server = _config.RemoteBridge.ServerUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(server))
            throw new InvalidOperationException("ServerUrl del hub sin configurar.");

        var url = $"{server}/imprelia/status";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("X-Api-Key", _config.RemoteBridge.ApiKey ?? "");
        req.Headers.TryAddWithoutValidation("X-Agent-Id", _config.RemoteBridge.AgentId ?? "");
        using var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    private static async Task<string> SafeRead(HttpResponseMessage res, CancellationToken ct)
    {
        try { return await res.Content.ReadAsStringAsync(ct); }
        catch { return ""; }
    }
}
