namespace Imprelia.Server.Models;

public sealed class PrintJobRecord
{
    public string JobId { get; set; } = "";
    public string AgentId { get; set; } = "";
    public string Route { get; set; } = "";
    public string Type { get; set; } = "escpos";
    public string Content { get; set; } = "";
    public int Copies { get; set; } = 1;
    public string Status { get; set; } = "pending";
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
