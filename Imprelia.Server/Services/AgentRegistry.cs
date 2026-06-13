using System.Collections.Concurrent;
using Imprelia.Server.Models;

namespace Imprelia.Server.Services;

/// <summary>
/// Mantiene el mapa agentId → connectionId (SignalR) y la cola de trabajos
/// pendientes para el modo polling.
/// </summary>
public sealed class AgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentInfo> _agents = new();
    private readonly ConcurrentDictionary<string, Queue<PrintJobRecord>> _pendingJobs = new();
    private readonly ConcurrentDictionary<string, PrintJobRecord> _allJobs = new();

    public void Register(string agentId, string connectionId)
    {
        _agents[agentId] = new AgentInfo
        {
            AgentId      = agentId,
            ConnectionId = connectionId,
            ConnectedAt  = DateTime.UtcNow,
            LastSeenAt   = DateTime.UtcNow,
        };
    }

    public void Unregister(string connectionId)
    {
        var entry = _agents.FirstOrDefault(kv => kv.Value.ConnectionId == connectionId);
        if (entry.Key != null)
            _agents.TryRemove(entry.Key, out _);
    }

    public string? GetConnectionId(string agentId) =>
        _agents.TryGetValue(agentId, out var info) ? info.ConnectionId : null;

    public IReadOnlyList<AgentInfo> GetAllAgents() =>
        _agents.Values.ToList();

    public bool IsConnected(string agentId) =>
        _agents.ContainsKey(agentId);

    // ── Gestión de trabajos ───────────────────────────────────────────────────

    public PrintJobRecord EnqueueJob(PrintJobRequest request)
    {
        var record = new PrintJobRecord
        {
            JobId   = Guid.NewGuid().ToString("N"),
            AgentId = request.AgentId,
            Route   = request.Route,
            Type    = request.Type,
            Content = request.Content,
            Copies  = request.Copies,
            Metadata = request.Metadata,
            Status  = "pending",
        };

        _allJobs[record.JobId] = record;

        // Encolar para polling
        var queue = _pendingJobs.GetOrAdd(request.AgentId, _ => new Queue<PrintJobRecord>());
        lock (queue) queue.Enqueue(record);

        return record;
    }

    public List<PrintJobRecord> DequeuePending(string agentId)
    {
        if (!_pendingJobs.TryGetValue(agentId, out var queue))
            return new List<PrintJobRecord>();

        lock (queue)
        {
            var items = queue.ToList();
            queue.Clear();
            return items;
        }
    }

    public void UpdateJobStatus(string jobId, string status, string? error)
    {
        if (_allJobs.TryGetValue(jobId, out var job))
        {
            job.Status    = status;
            job.Error     = error;
            job.UpdatedAt = DateTime.UtcNow;
        }
    }

    public PrintJobRecord? GetJob(string jobId) =>
        _allJobs.TryGetValue(jobId, out var job) ? job : null;

    public void TouchAgent(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var info))
            info.LastSeenAt = DateTime.UtcNow;
    }
}
