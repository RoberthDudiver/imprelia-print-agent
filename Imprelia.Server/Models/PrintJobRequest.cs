namespace Imprelia.Server.Models;

public sealed class PrintJobRequest
{
    public string AgentId { get; set; } = "";
    public string Route { get; set; } = "";
    public string Type { get; set; } = "escpos";
    public string Content { get; set; } = "";
    public int Copies { get; set; } = 1;
    public Dictionary<string, string>? Metadata { get; set; }
}
