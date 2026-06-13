using Imprelia.Server.Models;

namespace Imprelia.Server.Services;

public interface IImpreliaService
{
    /// <summary>Envía un trabajo de impresión al agente. Devuelve el JobId asignado.</summary>
    Task<PrintJobRecord> SendPrintJobAsync(PrintJobRequest request, CancellationToken ct = default);

    /// <summary>Retorna la info del agente, o null si no está registrado.</summary>
    AgentInfo? GetAgentInfo(string agentId);

    /// <summary>Lista todos los agentes conectados actualmente.</summary>
    IReadOnlyList<AgentInfo> GetConnectedAgents();

    /// <summary>Retorna el estado de un trabajo por su ID.</summary>
    PrintJobRecord? GetJobStatus(string jobId);
}
