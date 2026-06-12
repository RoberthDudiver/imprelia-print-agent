using System.Collections.Concurrent;
using System.Drawing.Printing;
using System.IO;
using System.Text;

namespace Imprelia.PrintAgent;

public sealed class PrinterInfo
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsDefault { get; set; }
    public bool IsOnline { get; set; } = true;
    public string Status { get; set; } = "Ready";
    public string Type { get; set; } = "Unknown";
    public bool SupportsPdf { get; set; }
    public bool SupportsRaw { get; set; }
    public bool SupportsZpl { get; set; }
    public bool SupportsFiscal { get; set; }
    public List<string> PaperSizes { get; set; } = new();
    public string Source { get; set; } = "Windows";
}

public class UniversalPrintRequest
{
    public string? PrinterName { get; set; }
    public string? Printer { get; set; }
    public string? JobName { get; set; }
    public string ContentType { get; set; } = "epos";
    public string Content { get; set; } = "";
    public int Copies { get; set; } = 1;
    public PrintOptions Options { get; set; } = new();
}

public sealed class PrintOptions
{
    public string? PaperSize { get; set; }
    public string? Orientation { get; set; }
    public bool OpenCashDrawer { get; set; }
    public bool CutPaper { get; set; }
}

public sealed class LegacyPrintRequest
{
    public string? Printer { get; set; }
    public string DataBase64 { get; set; } = "";
}

public sealed class PrintByPurposeRequest : UniversalPrintRequest
{
    public string Purpose { get; set; } = "";
}

public sealed class PrinterTestRequest
{
    public string? PrinterName { get; set; }
    public string ContentType { get; set; } = "epos";
}

public sealed class PrintResponse
{
    public bool Success { get; set; }
    public string? JobId { get; set; }
    public string? PrinterName { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public object? Details { get; set; }
}

public sealed class PrinterRoute
{
    public string Purpose { get; set; } = "";
    public string? PrinterName { get; set; }
    public string ContentType { get; set; } = "epos";
}

public sealed class PrintJobHistory
{
    public string JobId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? Endpoint { get; set; }
    public string? PrinterName { get; set; }
    public string? JobName { get; set; }
    public string? ContentType { get; set; }
    public string Status { get; set; } = "";
    public string? Error { get; set; }
}

public interface IPrinterDiscoveryService
{
    List<PrinterInfo> ListPrinters();
    string? GetDefaultPrinter();
    bool Exists(string printerName);
}

public sealed class WindowsPrinterDiscoveryService : IPrinterDiscoveryService
{
    private readonly AppConfig _config;

    public WindowsPrinterDiscoveryService(AppConfig config) => _config = config;

    public List<PrinterInfo> ListPrinters()
    {
        var result = new List<PrinterInfo>();
        var defaultPrinter = GetDefaultPrinter();

        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            var info = BuildPrinterInfo(printer, defaultPrinter);
            result.Add(info);
        }

        return result.OrderByDescending(p => p.IsDefault).ThenBy(p => p.DisplayName).ToList();
    }

    public string? GetDefaultPrinter()
    {
        if (!string.IsNullOrWhiteSpace(_config.DefaultPrinter)) return _config.DefaultPrinter;

        try
        {
            var settings = new PrinterSettings();
            return settings.PrinterName;
        }
        catch
        {
            return null;
        }
    }

    public bool Exists(string printerName) =>
        PrinterSettings.InstalledPrinters.Cast<string>()
            .Any(p => string.Equals(p, printerName, StringComparison.OrdinalIgnoreCase));

    private PrinterInfo BuildPrinterInfo(string printer, string? defaultPrinter)
    {
        var type = DetectType(printer);
        var paperSizes = new List<string>();
        var status = "Ready";
        var isOnline = true;

        try
        {
            var settings = new PrinterSettings { PrinterName = printer };
            foreach (PaperSize size in settings.PaperSizes)
                if (!paperSizes.Contains(size.PaperName)) paperSizes.Add(size.PaperName);

            if (!settings.IsValid)
            {
                status = "Unavailable";
                isOnline = false;
            }
        }
        catch
        {
            status = "Unknown";
        }

        return new PrinterInfo
        {
            Name = printer,
            DisplayName = printer,
            IsDefault = string.Equals(printer, defaultPrinter, StringComparison.OrdinalIgnoreCase),
            IsOnline = isOnline,
            Status = status,
            Type = type,
            SupportsPdf = SupportsPdf(type, printer),
            SupportsRaw = SupportsRaw(type),
            SupportsZpl = SupportsZpl(type),
            SupportsFiscal = type.Equals("Fiscal", StringComparison.OrdinalIgnoreCase),
            PaperSizes = paperSizes,
        };
    }

    private string DetectType(string printer)
    {
        if (_config.PrinterTypes.TryGetValue(printer, out var configured) && !string.IsNullOrWhiteSpace(configured))
            return configured;

        var p = printer.ToLowerInvariant();
        if (p.Contains("zebra") || p.Contains("zdesigner") || p.Contains("zpl")) return "Label / ZPL";
        if (p.Contains("xprinter") && (p.Contains("xp-470b") || p.Contains("label") || p.Contains("barcode"))) return "Label / ZPL";
        if (p.Contains("tspl") || p.Contains("epl") || p.Contains("dpl")) return "Label / ZPL";
        if (p.Contains("fiscal")) return "Fiscal";
        if (p.Contains("pdf") || p.Contains("laser") || p.Contains("hp ") || p.Contains("laserjet")) return "PDF / Laser";
        if (p.Contains("epson") || p.Contains("tm-") || p.Contains("pos") || p.Contains("thermal")) return "EPOS / Thermal";
        return "Generic / Windows";
    }

    private static bool SupportsPdf(string type, string printer) =>
        type is "PDF / Laser" or "Generic / Windows" || printer.Contains("PDF", StringComparison.OrdinalIgnoreCase);

    private static bool SupportsRaw(string type) =>
        type is "EPOS / Thermal" or "Label / ZPL" or "Zebra / ZPL" or "Generic / Windows";

    private static bool SupportsZpl(string type) =>
        type is "Label / ZPL" or "Zebra / ZPL";
}

public interface IJobHistoryService
{
    void Add(PrintJobHistory job);
    List<PrintJobHistory> Recent(int count = 25);
}

public sealed class JobHistoryService : IJobHistoryService
{
    private readonly ConcurrentQueue<PrintJobHistory> _jobs = new();

    public void Add(PrintJobHistory job)
    {
        _jobs.Enqueue(job);
        while (_jobs.Count > 100 && _jobs.TryDequeue(out _)) { }
    }

    public List<PrintJobHistory> Recent(int count = 25) =>
        _jobs.Reverse().Take(count).ToList();
}

public interface IPrinterRouteService
{
    List<PrinterRoute> GetRoutes();
    PrinterRoute? GetRoute(string purpose);
    void SaveRoutes(IEnumerable<PrinterRoute> routes);
}

public sealed class PrinterRouteService : IPrinterRouteService
{
    private readonly AppConfig _config;

    public PrinterRouteService(AppConfig config) => _config = config;

    public List<PrinterRoute> GetRoutes() =>
        _config.PrinterRoutes.Values.OrderBy(r => r.Purpose).ToList();

    public PrinterRoute? GetRoute(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose)) return null;
        return _config.PrinterRoutes.TryGetValue(purpose, out var route) ? route : null;
    }

    public void SaveRoutes(IEnumerable<PrinterRoute> routes)
    {
        _config.PrinterRoutes = routes
            .Where(r => !string.IsNullOrWhiteSpace(r.Purpose))
            .ToDictionary(r => r.Purpose, r => r, StringComparer.OrdinalIgnoreCase);
        _config.Save();
    }
}

public interface IPrintAdapter
{
    bool CanHandle(string contentType);
    PrintResponse Print(string printerName, UniversalPrintRequest request);
}

public sealed class RawPrintAdapter : IPrintAdapter
{
    public bool CanHandle(string contentType) =>
        contentType is "raw" or "epos" or "zpl";

    public PrintResponse Print(string printerName, UniversalPrintRequest request)
    {
        var contentType = request.ContentType.ToLowerInvariant();
        var bytes = contentType == "raw" || contentType == "epos"
            ? DecodeMaybeBase64(request.Content)
            : Encoding.ASCII.GetBytes(request.Content ?? "");

        if (bytes.Length == 0)
            return Error("EMPTY_CONTENT", "No hay contenido para imprimir.");

        for (var i = 0; i < Math.Max(1, request.Copies); i++)
        {
            var err = RawPrinter.SendBytes(printerName, bytes, request.JobName);
            if (err != null) return Error("WINDOWS_SPOOLER_ERROR", err);
        }

        return Ok(printerName);
    }

    internal static byte[] DecodeMaybeBase64(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return Array.Empty<byte>();
        try { return Convert.FromBase64String(content); }
        catch { return Encoding.UTF8.GetBytes(content); }
    }

    internal static PrintResponse Ok(string printerName) => new() { Success = true, PrinterName = printerName };
    internal static PrintResponse Error(string code, string message) => new() { Success = false, ErrorCode = code, Message = message };
}

public sealed class TextPrintAdapter : IPrintAdapter
{
    public bool CanHandle(string contentType) => contentType == "text";

    public PrintResponse Print(string printerName, UniversalPrintRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return RawPrintAdapter.Error("EMPTY_CONTENT", "No hay texto para imprimir.");

        var text = request.Content;
        for (var i = 0; i < Math.Max(1, request.Copies); i++)
        {
            using var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = printerName;
            doc.DocumentName = string.IsNullOrWhiteSpace(request.JobName) ? "GastroManager Text" : request.JobName;
            doc.PrintPage += (_, e) =>
            {
                using var font = new Font("Consolas", 10f);
                e.Graphics?.DrawString(text, font, Brushes.Black, e.MarginBounds);
            };
            doc.Print();
        }

        return RawPrintAdapter.Ok(printerName);
    }
}

public sealed class PdfPrintAdapter : IPrintAdapter
{
    public bool CanHandle(string contentType) => contentType == "pdf";

    public PrintResponse Print(string printerName, UniversalPrintRequest request)
    {
        byte[] bytes;
        try { bytes = Convert.FromBase64String(request.Content); }
        catch { return RawPrintAdapter.Error("INVALID_PDF", "El contenido PDF debe venir en base64 válido."); }

        if (bytes.Length < 5 || Encoding.ASCII.GetString(bytes, 0, 4) != "%PDF")
            return RawPrintAdapter.Error("INVALID_PDF", "El contenido no parece ser un PDF válido.");

        var temp = Path.Combine(Path.GetTempPath(), $"gastro-print-{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(temp, bytes);

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = temp,
                Verb = "printto",
                Arguments = $"\"{printerName}\"",
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(psi);
            return RawPrintAdapter.Ok(printerName);
        }
        catch (Exception ex)
        {
            return RawPrintAdapter.Error("PDF_PRINT_NOT_AVAILABLE",
                "Windows no pudo imprimir el PDF con la aplicación asociada. Configure un lector PDF que soporte impresión silenciosa. " + ex.Message);
        }
    }
}

public sealed class FiscalPrintAdapter : IPrintAdapter
{
    public bool CanHandle(string contentType) => contentType == "fiscal";

    public PrintResponse Print(string printerName, UniversalPrintRequest request) =>
        RawPrintAdapter.Error("FISCAL_NOT_CONFIGURED", "La impresión fiscal requiere configurar el controlador fiscal.");
}

public interface IPrintService
{
    PrintResponse Print(UniversalPrintRequest request, string endpoint);
    PrintResponse PrintByPurpose(PrintByPurposeRequest request);
}

public sealed class PrintService : IPrintService
{
    private readonly IPrinterDiscoveryService _discovery;
    private readonly IPrinterRouteService _routes;
    private readonly IJobHistoryService _history;
    private readonly List<IPrintAdapter> _adapters;

    public PrintService(IPrinterDiscoveryService discovery, IPrinterRouteService routes, IJobHistoryService history)
    {
        _discovery = discovery;
        _routes = routes;
        _history = history;
        _adapters = new List<IPrintAdapter>
        {
            new RawPrintAdapter(),
            new TextPrintAdapter(),
            new PdfPrintAdapter(),
            new FiscalPrintAdapter(),
        };
    }

    public PrintResponse PrintByPurpose(PrintByPurposeRequest request)
    {
        var route = _routes.GetRoute(request.Purpose);
        if (route == null)
            return RawPrintAdapter.Error("PRINTER_ROUTE_NOT_FOUND", $"No hay una ruta de impresión configurada para '{request.Purpose}'.");

        request.PrinterName = !string.IsNullOrWhiteSpace(request.PrinterName) ? request.PrinterName : route.PrinterName;
        if (string.IsNullOrWhiteSpace(request.ContentType)) request.ContentType = route.ContentType;
        return Print(request, "/api/print/by-purpose");
    }

    public PrintResponse Print(UniversalPrintRequest request, string endpoint)
    {
        var job = new PrintJobHistory
        {
            JobId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.Now,
            Endpoint = endpoint,
            JobName = request.JobName,
            ContentType = request.ContentType,
        };

        try
        {
            var printer = FirstNonEmpty(request.PrinterName, request.Printer, _discovery.GetDefaultPrinter());
            job.PrinterName = printer;
            if (string.IsNullOrWhiteSpace(printer))
                return Finish(job, RawPrintAdapter.Error("NO_DEFAULT_PRINTER", "No hay impresora configurada ni impresora predeterminada de Windows."));

            if (!_discovery.Exists(printer))
                return Finish(job, RawPrintAdapter.Error("PRINTER_NOT_FOUND", $"No se encontró la impresora '{printer}'. Verifique que esté instalada en Windows."));

            request.ContentType = NormalizeContentType(request.ContentType);
            var adapter = _adapters.FirstOrDefault(a => a.CanHandle(request.ContentType));
            if (adapter == null)
                return Finish(job, RawPrintAdapter.Error("UNSUPPORTED_CONTENT_TYPE", $"Formato de impresión no soportado: '{request.ContentType}'."));

            var response = adapter.Print(printer, request);
            response.JobId = job.JobId;
            response.PrinterName ??= printer;
            return Finish(job, response);
        }
        catch (Exception ex)
        {
            return Finish(job, RawPrintAdapter.Error("PRINT_ERROR", ex.Message));
        }
    }

    private PrintResponse Finish(PrintJobHistory job, PrintResponse response)
    {
        job.Status = response.Success ? "Success" : "Error";
        job.Error = response.Success ? null : $"{response.ErrorCode}: {response.Message}";
        _history.Add(job);
        return response;
    }

    private static string NormalizeContentType(string? contentType) =>
        string.IsNullOrWhiteSpace(contentType) ? "epos" : contentType.Trim().ToLowerInvariant();

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
