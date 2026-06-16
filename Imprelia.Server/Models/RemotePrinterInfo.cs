namespace Imprelia.Server.Models;

/// <summary>
/// Descriptor de una impresora real publicada por un agente principal.
/// El cliente lo usa para crear impresoras virtuales que la espejan.
/// </summary>
public sealed class RemotePrinterInfo
{
    public string Name { get; set; } = "";
    /// <summary>Tipo detectado: "EPOS / Thermal", "Label / ZPL", "PDF / Laser", etc.</summary>
    public string Type { get; set; } = "";
    public bool IsDefault { get; set; }
}
