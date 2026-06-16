namespace Imprelia.Server.Models;

public sealed class AgentInfo
{
    public string AgentId { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }
    /// <summary>Impresoras que el agente principal publicó (para descubrimiento por clientes).</summary>
    public List<RemotePrinterInfo> Printers { get; set; } = new();
}
