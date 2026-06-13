using Imprelia.Server.Models;
using Imprelia.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Imprelia.Server;

public static class ImpreliaServiceExtensions
{
    /// <summary>
    /// Registra los servicios necesarios para el Remote Print Bridge.
    /// Llamar en Program.cs antes de builder.Build().
    /// </summary>
    public static IServiceCollection AddImpreliaServer(
        this IServiceCollection services,
        Action<ImpreliaOptions>? configure = null)
    {
        services.AddSignalR();
        services.AddSingleton<AgentRegistry>();
        services.AddScoped<IImpreliaService, ImpreliaService>();

        var opts = new ImpreliaOptions();
        configure?.Invoke(opts);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(opts));

        return services;
    }

    /// <summary>
    /// Mapea el hub SignalR y los endpoints HTTP del bridge.
    /// Llamar después de app.UseRouting() o en app.MapGroup(...).
    /// </summary>
    public static IEndpointRouteBuilder MapImprelia(this IEndpointRouteBuilder endpoints)
    {
        // Hub SignalR — el agente se conecta a esta URL
        endpoints.MapHub<ImpreliaHub>("/imprelia/hub");

        // Estado (utilizado por el test de conexión del agente)
        endpoints.MapGet("/imprelia/status", (AgentRegistry registry) =>
            Results.Ok(new
            {
                status        = "ok",
                connectedAgents = registry.GetAllAgents().Count,
                agents        = registry.GetAllAgents().Select(a => new { a.AgentId, a.ConnectedAt }),
                utcNow        = DateTime.UtcNow,
            }));

        // Polling: obtener trabajos pendientes (agente → servidor)
        endpoints.MapGet("/imprelia/jobs/pending", (
            string agentId,
            AgentRegistry registry,
            HttpContext ctx,
            IOptions<ImpreliaOptions> opts) =>
        {
            if (!IsApiKeyValid(ctx, opts.Value))
                return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(agentId))
                return Results.BadRequest("agentId es obligatorio.");

            registry.TouchAgent(agentId);
            var jobs = registry.DequeuePending(agentId);
            return Results.Ok(jobs.Select(j => new RemotePrintJob
            {
                JobId    = j.JobId,
                AgentId  = j.AgentId,
                Route    = j.Route,
                Type     = j.Type,
                Content  = j.Content,
                Copies   = j.Copies,
                Metadata = j.Metadata,
            }).ToList());
        });

        // Polling: reportar estado de un trabajo (agente → servidor)
        endpoints.MapPost("/imprelia/jobs/{jobId}/status", async (
            string jobId,
            HttpContext ctx,
            AgentRegistry registry,
            IOptions<ImpreliaOptions> opts) =>
        {
            if (!IsApiKeyValid(ctx, opts.Value))
                return Results.Unauthorized();

            var body = await ctx.Request.ReadFromJsonAsync<StatusUpdate>();
            if (body == null) return Results.BadRequest("Body inválido.");

            registry.UpdateJobStatus(jobId, body.Status, body.Error);
            if (!string.IsNullOrWhiteSpace(body.AgentId))
                registry.TouchAgent(body.AgentId);

            return Results.Ok(new { updated = true });
        });

        // Endpoints de gestión HTTP — solo si ExposeHttpJobApi está activado.
        // En .NET preferí enviar trabajos vía IImpreliaService desde código autenticado.
        var options = endpoints.ServiceProvider.GetService<IOptions<ImpreliaOptions>>()?.Value;
        if (options?.ExposeHttpJobApi == true)
        {
            // Enviar un trabajo (backend → servidor)
            endpoints.MapPost("/imprelia/jobs", async (
                PrintJobRequest request,
                IImpreliaService service,
                HttpContext ctx,
                IOptions<ImpreliaOptions> opts,
                CancellationToken ct) =>
            {
                if (!IsApiKeyValid(ctx, opts.Value))
                    return Results.Unauthorized();
                var record = await service.SendPrintJobAsync(request, ct);
                return Results.Ok(new { record.JobId, record.Status, record.CreatedAt });
            });

            // Consultar estado de un trabajo
            endpoints.MapGet("/imprelia/jobs/{jobId}", (
                string jobId,
                AgentRegistry registry,
                HttpContext ctx,
                IOptions<ImpreliaOptions> opts) =>
            {
                if (!IsApiKeyValid(ctx, opts.Value))
                    return Results.Unauthorized();
                var job = registry.GetJob(jobId);
                return job != null ? Results.Ok(job) : Results.NotFound();
            });
        }

        return endpoints;
    }

    /// <summary>Valida el header X-Api-Key contra las keys permitidas (vacío = modo dev).</summary>
    private static bool IsApiKeyValid(HttpContext ctx, ImpreliaOptions options)
    {
        if (options.AllowedApiKeys.Count == 0) return true; // modo dev: aceptar todo
        var key = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
        return !string.IsNullOrEmpty(key) && options.AllowedApiKeys.Contains(key);
    }

    private sealed record StatusUpdate(string Status, string? Error, string? AgentId);
}
