namespace Imprelia.Server.Models;

public sealed class AgentInfo
{
    public string AgentId { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }
}
