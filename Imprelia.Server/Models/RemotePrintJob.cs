namespace Imprelia.Server.Models;

public sealed class RemotePrintJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString("N");
    public string AgentId { get; set; } = "";
    public string Route { get; set; } = "";
    public string Type { get; set; } = "escpos";
    public string Content { get; set; } = "";
    public int Copies { get; set; } = 1;
    public Dictionary<string, string>? Metadata { get; set; }
}
