namespace Imprelia.Server;

public sealed class ImpreliaOptions
{
    /// <summary>
    /// Lista de API keys válidas que los agentes pueden usar para autenticarse.
    /// Si está vacía, cualquier agente se acepta (modo desarrollo).
    /// </summary>
    public List<string> AllowedApiKeys { get; set; } = new();

    /// <summary>
    /// Tiempo máximo que un trabajo permanece en la cola antes de marcarse como offline.
    /// Default: 24 horas.
    /// </summary>
    public TimeSpan JobRetention { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Número máximo de trabajos retenidos en memoria por agente para el polling.
    /// </summary>
    public int MaxPendingJobsPerAgent { get; set; } = 100;

    /// <summary>
    /// Si es true, expone los endpoints HTTP de gestión sin autenticación:
    ///   POST /imprelia/jobs        (encolar un trabajo)
    ///   GET  /imprelia/jobs/{id}   (consultar estado)
    /// Útil para backends en otros lenguajes. APAGADO por defecto: en .NET el
    /// envío se hace vía IImpreliaService desde código autenticado, sin exponer
    /// un endpoint público que cualquiera podría usar para imprimir.
    /// </summary>
    public bool ExposeHttpJobApi { get; set; } = false;
}
