using System.IO;
using System.Net;
using System.Text;

namespace Imprelia.PrintAgent.Services;

/// <summary>
/// Servidor IPP (Internet Printing Protocol) local. Windows agrega cada impresora
/// virtual usando su driver de clase IPP inbox y le manda los trabajos como PDF a
/// http://127.0.0.1:{IppPort}/ipp/{id}. Este servidor captura ese PDF y lo
/// reenvía al hub vía <see cref="ClientSenderService"/>.
///
/// No requiere drivers de fabricante ni port monitor. Escucha solo en loopback.
/// </summary>
public sealed class IppPrintServer
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly ClientSenderService _sender;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private int _jobCounter = 1000;

    public bool IsRunning { get; private set; }
    public int Port => _config.ClientMode.IppPort;

    public IppPrintServer(AppConfig config, AgentLogService log, ClientSenderService sender)
    {
        _config = config;
        _log = log;
        _sender = sender;
    }

    public void Start()
    {
        if (!_config.ClientMode.Enabled)
        {
            _log.Info("Modo cliente desactivado: servidor IPP no iniciado.", "Cliente");
            return;
        }

        Stop();

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _cts = new CancellationTokenSource();
            _listener.Start();
            IsRunning = true;
            _ = Task.Run(() => LoopAsync(_cts.Token));
            _log.Info($"Servidor IPP escuchando en 127.0.0.1:{Port}.", "Cliente");
        }
        catch (Exception ex)
        {
            IsRunning = false;
            _log.Error($"No se pudo iniciar el servidor IPP en el puerto {Port}: {ex.Message}", "Cliente");
        }
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
        _listener = null;
        IsRunning = false;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch { break; }
            _ = Task.Run(() => HandleAsync(ctx));
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            var id = ExtractId(path);
            var vp = FindPrinter(id);

            // GET → algunos clientes prueban la URL antes de imprimir.
            if (ctx.Request.HttpMethod == "GET")
            {
                WriteText(ctx, 200, "text/plain", $"Imprelia IPP printer '{vp?.LocalName ?? id}' ready.");
                return;
            }

            // Leer el cuerpo IPP completo.
            byte[] body;
            using (var ms = new MemoryStream())
            {
                await ctx.Request.InputStream.CopyToAsync(ms);
                body = ms.ToArray();
            }

            var req = IppRequest.Parse(body);
            byte[] response = req.OperationId switch
            {
                Ipp.OpGetPrinterAttributes => BuildPrinterAttributes(req, vp, id),
                Ipp.OpValidateJob          => BuildOkJobless(req),
                Ipp.OpCreateJob            => BuildJobResponse(req, NextJobId(), id, "pending"),
                Ipp.OpGetJobs              => BuildOkJobless(req),
                Ipp.OpGetJobAttributes     => BuildJobResponse(req, req.RequestId, id, "completed"),
                Ipp.OpCancelJob            => BuildOkJobless(req),
                Ipp.OpPrintJob             => await HandlePrintAsync(req, vp, id),
                Ipp.OpSendDocument         => await HandlePrintAsync(req, vp, id),
                _                          => new IppWriter().Header(Ipp.ServerErrorNotSupported, req.RequestId)
                                                  .Group(Ipp.TagOperation)
                                                  .Str(Ipp.ValCharset, "attributes-charset", "utf-8")
                                                  .Str(Ipp.ValLanguage, "attributes-natural-language", "en")
                                                  .End().ToArray(),
            };

            WriteIpp(ctx, response);
        }
        catch (Exception ex)
        {
            _log.Error($"IPP: error procesando solicitud: {ex.Message}", "Cliente");
            try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
        }
    }

    // ── Operaciones de impresión ──────────────────────────────────────────────

    private async Task<byte[]> HandlePrintAsync(IppRequest req, ClientVirtualPrinter? vp, string id)
    {
        var jobId = NextJobId();

        if (req.Document.Length < 5)
        {
            // Create-Job vacío todavía no trae documento; respondemos OK igual.
            return BuildJobResponse(req, jobId, id, "pending");
        }

        if (vp == null)
        {
            _log.Warn($"IPP: trabajo para impresora virtual desconocida '{id}'. Descartado.", "Cliente");
            return BuildJobResponse(req, jobId, id, "aborted");
        }

        // Windows IPP class driver envía PDF cuando se advierte application/pdf.
        bool isPdf = req.Document.Length >= 4 &&
                     req.Document[0] == '%' && req.Document[1] == 'P' &&
                     req.Document[2] == 'D' && req.Document[3] == 'F';

        if (!isPdf)
        {
            _log.Warn($"IPP: documento recibido no es PDF (Windows mandó otro formato) para '{vp.LocalName}'. " +
                      "Verificá que la impresora use el driver de clase IPP.", "Cliente");
            return BuildJobResponse(req, jobId, id, "aborted");
        }

        var ok = await _sender.SendPdfAsync(vp, req.Document);
        return BuildJobResponse(req, jobId, id, ok ? "completed" : "aborted");
    }

    // ── Construcción de respuestas IPP ────────────────────────────────────────

    private byte[] BuildPrinterAttributes(IppRequest req, ClientVirtualPrinter? vp, string id)
    {
        var name = vp?.LocalName ?? "Imprelia Remote Printer";
        var uri = $"ipp://localhost:{Port}/ipp/{id}";
        var uuid = $"urn:uuid:{DeterministicGuid(id)}";

        var w = new IppWriter()
            .Header(Ipp.OkStatus, req.RequestId)
            .Group(Ipp.TagOperation)
            .Str(Ipp.ValCharset, "attributes-charset", "utf-8")
            .Str(Ipp.ValLanguage, "attributes-natural-language", "en")
            .Group(Ipp.TagPrinter);

        // Identidad y estado
        w.Str(Ipp.ValUri, "printer-uri-supported", uri);
        w.Str(Ipp.ValKeyword, "uri-authentication-supported", "none");
        w.Str(Ipp.ValKeyword, "uri-security-supported", "none");
        w.Str(Ipp.ValName, "printer-name", name);
        w.Str(Ipp.ValText, "printer-info", name);
        w.Str(Ipp.ValText, "printer-make-and-model", "Imprelia Remote Printer");
        w.Str(Ipp.ValUri, "printer-uuid", uuid);
        w.Int(Ipp.ValEnum, "printer-state", 3); // idle
        w.Str(Ipp.ValKeyword, "printer-state-reasons", "none");
        w.Bool("printer-is-accepting-jobs", true);
        w.Int(Ipp.ValInteger, "printer-up-time", (int)(Environment.TickCount64 / 1000));
        w.Int(Ipp.ValInteger, "queued-job-count", 0);

        // Versiones y operaciones
        w.Str(Ipp.ValKeyword, "ipp-versions-supported", "1.1").Add(Ipp.ValKeyword, "2.0");
        w.Str(Ipp.ValKeyword, "ipp-features-supported", "ipp-everywhere");
        w.Int(Ipp.ValEnum, "operations-supported", Ipp.OpPrintJob)
            .AddInt(Ipp.ValEnum, Ipp.OpValidateJob)
            .AddInt(Ipp.ValEnum, Ipp.OpCreateJob)
            .AddInt(Ipp.ValEnum, Ipp.OpSendDocument)
            .AddInt(Ipp.ValEnum, Ipp.OpCancelJob)
            .AddInt(Ipp.ValEnum, Ipp.OpGetJobAttributes)
            .AddInt(Ipp.ValEnum, Ipp.OpGetJobs)
            .AddInt(Ipp.ValEnum, Ipp.OpGetPrinterAttributes);

        // Charset / idioma
        w.Str(Ipp.ValCharset, "charset-configured", "utf-8");
        w.Str(Ipp.ValCharset, "charset-supported", "utf-8");
        w.Str(Ipp.ValLanguage, "natural-language-configured", "en");
        w.Str(Ipp.ValLanguage, "generated-natural-language-supported", "en");

        // Formato de documento — SOLO PDF para forzar a Windows a mandar PDF.
        w.Str(Ipp.ValMimeType, "document-format-default", "application/pdf");
        w.Str(Ipp.ValMimeType, "document-format-supported", "application/pdf");
        w.Str(Ipp.ValKeyword, "compression-supported", "none");
        w.Str(Ipp.ValKeyword, "pdl-override-supported", "attempted");

        // Capacidades de trabajo (mínimas pero suficientes para IPP Everywhere)
        w.Bool("color-supported", true);
        w.Str(Ipp.ValKeyword, "print-color-mode-supported", "auto").Add(Ipp.ValKeyword, "color").Add(Ipp.ValKeyword, "monochrome");
        w.Str(Ipp.ValKeyword, "print-color-mode-default", "auto");
        w.Str(Ipp.ValKeyword, "sides-supported", "one-sided");
        w.Str(Ipp.ValKeyword, "sides-default", "one-sided");
        w.Int(Ipp.ValEnum, "print-quality-supported", 3).AddInt(Ipp.ValEnum, 4).AddInt(Ipp.ValEnum, 5);
        w.Int(Ipp.ValEnum, "print-quality-default", 4);
        w.Resolution("printer-resolution-supported", 300, 300);
        w.Resolution("printer-resolution-default", 300, 300);
        w.Int(Ipp.ValEnum, "finishings-supported", 3);
        w.Int(Ipp.ValEnum, "finishings-default", 3);
        w.Int(Ipp.ValEnum, "orientation-requested-supported", 3).AddInt(Ipp.ValEnum, 4);
        w.Int(Ipp.ValEnum, "orientation-requested-default", 3);
        w.Range("copies-supported", 1, 99);
        w.Int(Ipp.ValInteger, "copies-default", 1);
        w.Bool("multiple-document-jobs-supported", false);

        // Tamaños de papel
        w.Str(Ipp.ValKeyword, "media-supported", "iso_a4_210x297mm")
            .Add(Ipp.ValKeyword, "na_letter_8.5x11in")
            .Add(Ipp.ValKeyword, "om_80x297mm_80x297mm")
            .Add(Ipp.ValKeyword, "om_58x210mm_58x210mm");
        w.Str(Ipp.ValKeyword, "media-default", "iso_a4_210x297mm");
        w.Str(Ipp.ValKeyword, "media-ready", "iso_a4_210x297mm");

        // Atributos aceptados al crear un trabajo
        w.Str(Ipp.ValKeyword, "job-creation-attributes-supported", "copies")
            .Add(Ipp.ValKeyword, "media")
            .Add(Ipp.ValKeyword, "sides")
            .Add(Ipp.ValKeyword, "print-color-mode")
            .Add(Ipp.ValKeyword, "orientation-requested")
            .Add(Ipp.ValKeyword, "print-quality");
        w.Str(Ipp.ValKeyword, "which-jobs-supported", "completed").Add(Ipp.ValKeyword, "not-completed");
        w.Bool("job-ids-supported", true);

        return w.End().ToArray();
    }

    private static byte[] BuildOkJobless(IppRequest req) =>
        new IppWriter()
            .Header(Ipp.OkStatus, req.RequestId)
            .Group(Ipp.TagOperation)
            .Str(Ipp.ValCharset, "attributes-charset", "utf-8")
            .Str(Ipp.ValLanguage, "attributes-natural-language", "en")
            .End().ToArray();

    private byte[] BuildJobResponse(IppRequest req, int jobId, string id, string state)
    {
        var stateCode = state switch
        {
            "pending" => 3,
            "processing" => 5,
            "completed" => 9,
            "aborted" => 8,
            _ => 9,
        };
        var jobUri = $"ipp://localhost:{Port}/ipp/{id}/{jobId}";

        return new IppWriter()
            .Header(Ipp.OkStatus, req.RequestId)
            .Group(Ipp.TagOperation)
            .Str(Ipp.ValCharset, "attributes-charset", "utf-8")
            .Str(Ipp.ValLanguage, "attributes-natural-language", "en")
            .Group(Ipp.TagJob)
            .Int(Ipp.ValInteger, "job-id", jobId)
            .Str(Ipp.ValUri, "job-uri", jobUri)
            .Int(Ipp.ValEnum, "job-state", stateCode)
            .Str(Ipp.ValKeyword, "job-state-reasons", state == "aborted" ? "aborted-by-system" : "none")
            .End().ToArray();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int NextJobId() => Interlocked.Increment(ref _jobCounter);

    private static string ExtractId(string path)
    {
        // /ipp/{id}  o  /ipp/{id}/...
        var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[0].Equals("ipp", StringComparison.OrdinalIgnoreCase))
            return parts[1];
        return parts.Length > 0 ? parts[^1] : "";
    }

    private ClientVirtualPrinter? FindPrinter(string id) =>
        _config.ClientMode.VirtualPrinters
            .FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    private static string DeterministicGuid(string seed)
    {
        // GUID estable derivado del id (no aleatorio → no rompe resume/persistencia).
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("imprelia-ipp-" + seed));
        return new Guid(hash).ToString();
    }

    private static void WriteIpp(HttpListenerContext ctx, byte[] body)
    {
        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = "application/ipp";
        ctx.Response.ContentLength64 = body.Length;
        ctx.Response.OutputStream.Write(body, 0, body.Length);
        ctx.Response.OutputStream.Close();
    }

    private static void WriteText(HttpListenerContext ctx, int status, string contentType, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = contentType;
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        ctx.Response.OutputStream.Close();
    }
}
