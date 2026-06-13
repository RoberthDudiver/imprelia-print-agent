# Imprelia.Server

Integración ASP.NET Core para el **Imprelia Remote Print Bridge**. Provee el hub
SignalR y los endpoints HTTP a los que el agente Imprelia (instalado en Windows,
junto a la impresora) se conecta hacia afuera para recibir trabajos de impresión.

El agente nunca abre puertos públicos: él inicia la conexión saliente (WSS/HTTPS)
contra tu servidor.

## Uso

```csharp
// Program.cs
builder.Services.AddImpreliaServer(opts =>
{
    opts.AllowedApiKeys.Add("tu-api-key-secreta");
    // opts.ExposeHttpJobApi = true; // solo si querés el POST /imprelia/jobs público
});

var app = builder.Build();
app.MapImprelia();   // mapea /imprelia/hub + endpoints de polling/estado
```

Enviar un trabajo desde código autenticado:

```csharp
public class PrintController(IImpreliaService imprelia) : ControllerBase
{
    [HttpPost("print")]
    public async Task<IActionResult> Print()
    {
        var job = await imprelia.SendPrintJobAsync(new PrintJobRequest
        {
            AgentId = "agente-cocina",      // el AgentId configurado en el agente
            Route   = "kitchen_order",      // ruta definida en el agente
            Type    = "escpos",
            Content = base64EscPos,
        });
        return Ok(new { job.JobId, job.Status });
    }
}
```

## Protocolo (lado agente)

1. Conecta a `/imprelia/hub` con headers `X-Agent-Id` y `X-Api-Key`.
2. Invoca `RegisterAgent(agentId, apiKey)`.
3. Escucha el método `PrintJob` (recibe `RemotePrintJob`).
4. Invoca `ReportStatus(jobId, status, error?)` al terminar.

Estados: `pending`, `received_by_agent`, `printing`, `printed`, `failed`,
`printer_not_found`, `invalid_payload`.
