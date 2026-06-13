using Imprelia.Server;
using Imprelia.Server.Models;
using Imprelia.Server.Services;

// ════════════════════════════════════════════════════════════════════════════
// Ejemplo mínimo de un backend ASP.NET Core que usa Imprelia.Server para enviar
// trabajos de impresión a un Imprelia Print Agent instalado en un local remoto.
//
// El agente se conecta hacia afuera a /imprelia/hub (no abre puertos públicos).
// Este backend empuja jobs vía IImpreliaService.
//
// Ejecutar:   dotnet run
// Probar:     ver los curl de abajo / Swagger en /swagger
// ════════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

// 1) Registrar el Remote Print Bridge.
builder.Services.AddImpreliaServer(opts =>
{
    // En producción cargá las keys desde config/secrets, no hardcodeadas.
    opts.AllowedApiKeys.Add("demo-api-key");
    // opts.ExposeHttpJobApi = true; // solo si querés el POST /imprelia/jobs público
});

var app = builder.Build();

// 2) Mapear el hub + endpoints del agente (polling/estado).
app.MapImprelia();

// ── Endpoints de ejemplo (tu app los protegería con auth) ───────────────────

// Ver qué agentes están conectados ahora mismo.
app.MapGet("/agents", (IImpreliaService imprelia) =>
    Results.Ok(imprelia.GetConnectedAgents()));

// Enviar un ticket de prueba (ESC/POS) a un agente.
app.MapPost("/print/test", async (string agentId, IImpreliaService imprelia) =>
{
    // "Hola desde Imprelia\n\n\n" + corte de papel (ESC/POS GS V).
    const string text = "Hola desde Imprelia!\n\n\n";
    var escpos = System.Text.Encoding.ASCII.GetBytes(text)
        .Concat(new byte[] { 0x1D, 0x56, 0x00 }) // GS V 0 = full cut
        .ToArray();

    var job = await imprelia.SendPrintJobAsync(new PrintJobRequest
    {
        AgentId = agentId,
        Route   = "ticket",                 // ruta configurada en el agente
        Type    = "escpos",
        Content = Convert.ToBase64String(escpos),
        Copies  = 1,
    });

    return Results.Ok(new { job.JobId, job.Status });
});

// Consultar el estado de un job.
app.MapGet("/print/{jobId}", (string jobId, IImpreliaService imprelia) =>
{
    var job = imprelia.GetJobStatus(jobId);
    return job is null ? Results.NotFound() : Results.Ok(new { job.JobId, job.Status, job.Error });
});

app.Run();
