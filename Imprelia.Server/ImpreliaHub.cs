using Imprelia.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Imprelia.Server;

/// <summary>
/// Hub SignalR al que se conecta el agente Imprelia instalado en Windows.
/// El agente inicia la conexión saliente; el servidor no necesita acceso
/// directo al agente.
///
/// Protocolo (lado agente):
///   1. Conectar con headers X-Agent-Id + X-Api-Key
///   2. Invocar RegisterAgent(agentId, apiKey)
///   3. Escuchar el método "PrintJob" (recibe RemotePrintJob)
///   4. Invocar ReportStatus(jobId, status, error?) al terminar
/// </summary>
public sealed class ImpreliaHub : Hub
{
    private readonly AgentRegistry _registry;
    private readonly ImpreliaOptions _options;

    public ImpreliaHub(AgentRegistry registry, IOptions<ImpreliaOptions> options)
    {
        _registry = registry;
        _options  = options.Value;
    }

    /// <summary>El agente llama a este método al conectarse para registrarse.</summary>
    public async Task RegisterAgent(string agentId, string apiKey)
    {
        if (!IsApiKeyValid(apiKey))
        {
            await Clients.Caller.SendAsync("AuthError", "API key inválida.");
            Context.Abort();
            return;
        }

        _registry.Register(agentId, Context.ConnectionId);

        // Entregar trabajos pendientes (encolados mientras el agente estaba offline)
        var pending = _registry.DequeuePending(agentId);
        foreach (var job in pending)
        {
            await Clients.Caller.SendAsync("PrintJob", new
            {
                job.JobId, job.AgentId, job.Route,
                job.Type,  job.Content, job.Copies, job.Metadata
            });
        }
    }

    /// <summary>El agente reporta el resultado de un trabajo.</summary>
    public Task ReportStatus(string jobId, string status, string? error = null)
    {
        _registry.UpdateJobStatus(jobId, status, error);
        var agentId = GetAgentId();
        if (agentId != null) _registry.TouchAgent(agentId);
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _registry.Unregister(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsApiKeyValid(string key)
    {
        if (_options.AllowedApiKeys.Count == 0) return true; // modo dev: aceptar todo
        return _options.AllowedApiKeys.Contains(key);
    }

    private string? GetAgentId() =>
        Context.GetHttpContext()?.Request.Headers["X-Agent-Id"].FirstOrDefault();
}
