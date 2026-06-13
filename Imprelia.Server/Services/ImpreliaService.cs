using Imprelia.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace Imprelia.Server.Services;

/// <summary>Implementación por defecto de <see cref="IImpreliaService"/>.</summary>
public sealed class ImpreliaService : IImpreliaService
{
    private readonly AgentRegistry _registry;
    private readonly IHubContext<ImpreliaHub> _hub;

    /// <summary>Crea el servicio con el registro de agentes y el contexto del hub.</summary>
    public ImpreliaService(AgentRegistry registry, IHubContext<ImpreliaHub> hub)
    {
        _registry = registry;
        _hub      = hub;
    }

    /// <inheritdoc/>
    public async Task<PrintJobRecord> SendPrintJobAsync(PrintJobRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AgentId))
            throw new ArgumentException("AgentId es obligatorio.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Route))
            throw new ArgumentException("Route es obligatorio.", nameof(request));

        var record = _registry.EnqueueJob(request);

        var connectionId = _registry.GetConnectionId(request.AgentId);
        if (connectionId != null)
        {
            var job = new RemotePrintJob
            {
                JobId    = record.JobId,
                AgentId  = record.AgentId,
                Route    = record.Route,
                Type     = record.Type,
                Content  = record.Content,
                Copies   = record.Copies,
                Metadata = record.Metadata,
            };

            try
            {
                await _hub.Clients.Client(connectionId).SendAsync("PrintJob", job, ct);
            }
            catch
            {
                // El trabajo queda en cola para polling si el envío SignalR falla
            }
        }

        return record;
    }

    /// <inheritdoc/>
    public AgentInfo? GetAgentInfo(string agentId) =>
        _registry.GetAllAgents().FirstOrDefault(a => a.AgentId == agentId);

    /// <inheritdoc/>
    public IReadOnlyList<AgentInfo> GetConnectedAgents() =>
        _registry.GetAllAgents();

    /// <inheritdoc/>
    public PrintJobRecord? GetJobStatus(string jobId) =>
        _registry.GetJob(jobId);
}
