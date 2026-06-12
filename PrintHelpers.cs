namespace Imprelia.PrintAgent;

public static class EscPosTest
{
    public static byte[] Build(string? printerName = null, string? status = null)
    {
        var enc = System.Text.Encoding.ASCII;
        var sb = new List<byte>();
        sb.AddRange(new byte[] { 0x1B, 0x40 });
        sb.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
        sb.AddRange(new byte[] { 0x1D, 0x21, 0x11 });
        sb.AddRange(enc.GetBytes("Imprelia\n"));
        sb.AddRange(new byte[] { 0x1D, 0x21, 0x00 });
        sb.AddRange(enc.GetBytes("Prueba de impresion\n"));
        if (!string.IsNullOrWhiteSpace(printerName))
            sb.AddRange(enc.GetBytes($"Impresora: {SanitizeAscii(printerName)}\n"));
        if (!string.IsNullOrWhiteSpace(status))
            sb.AddRange(enc.GetBytes($"Estado: {SanitizeAscii(status)}\n"));
        sb.AddRange(enc.GetBytes(DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "\n"));
        sb.AddRange(enc.GetBytes("--------------------------------\n"));
        sb.AddRange(enc.GetBytes("Si lees esto, funciona OK!\n"));
        sb.AddRange(new byte[] { 0x0A, 0x0A, 0x0A });
        sb.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 });
        return sb.ToArray();
    }

    private static string SanitizeAscii(string value) =>
        new(value.Select(ch => ch <= 127 ? ch : '?').ToArray());
}

public sealed record PrintTestPayload(string ContentType, string Content, string JobName);

public static class PrintTestBuilder
{
    public static PrintTestPayload Build(string? printerName, string? requestedContentType = null, string? printerType = null)
    {
        var printer = string.IsNullOrWhiteSpace(printerName) ? "Imprelia" : printerName;
        var requested = requestedContentType?.Trim().ToLowerInvariant();
        var effective = string.IsNullOrWhiteSpace(requested) ? GuessContentType(printer, printerType) : requested;

        if (effective == "raw" && IsLabelPrinter(printer, printerType))
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(TsplTest(printer));
            return new PrintTestPayload("raw", Convert.ToBase64String(bytes), "Prueba etiqueta RAW/TSPL");
        }

        return effective switch
        {
            "raw" or "epos" => new PrintTestPayload(effective, Convert.ToBase64String(EscPosTest.Build(printer, "Ready")), $"Prueba {effective}"),
            "zpl" => new PrintTestPayload("zpl", ZplTest(printer), "Prueba ZPL"),
            "tspl" => new PrintTestPayload("tspl", TsplTest(printer), "Prueba TSPL"),
            "epl" => new PrintTestPayload("epl", EplTest(printer), "Prueba EPL"),
            "dpl" => new PrintTestPayload("dpl", DplTest(printer), "Prueba DPL"),
            "text" => new PrintTestPayload("text", $"Imprelia Print Agent\r\nPrueba de impresion\r\nImpresora: {printer}\r\nFecha: {DateTime.Now:g}", "Prueba texto"),
            _ => new PrintTestPayload(effective, "", $"Prueba {effective}"),
        };
    }

    public static string GuessContentType(string? printerName, string? printerType = null)
    {
        var value = $"{printerName} {printerType}".ToLowerInvariant();
        if (value.Contains("zebra") || value.Contains("zdesigner") || value.Contains("zpl")) return "zpl";
        if (value.Contains("xprinter") || value.Contains("xp-470b") || value.Contains("tspl")) return "tspl";
        if (value.Contains("epl")) return "epl";
        if (value.Contains("dpl")) return "dpl";
        if (value.Contains("label") || value.Contains("barcode")) return "tspl";
        return "epos";
    }

    private static bool IsLabelPrinter(string? printerName, string? printerType)
    {
        var value = $"{printerName} {printerType}".ToLowerInvariant();
        return value.Contains("label") ||
               value.Contains("barcode") ||
               value.Contains("xprinter") ||
               value.Contains("xp-470b") ||
               value.Contains("zebra") ||
               value.Contains("zpl") ||
               value.Contains("tspl") ||
               value.Contains("epl") ||
               value.Contains("dpl");
    }

    private static string ZplTest(string printer) =>
        $"^XA^CF0,32^FO40,40^FDImprelia Print Agent^FS^CF0,24^FO40,90^FDPrueba de impresion^FS^FO40,130^FD{EscapeZpl(printer)}^FS^FO40,170^FD{DateTime.Now:g}^FS^XZ";

    private static string TsplTest(string printer) =>
        $"SIZE 100 mm,50 mm\r\nGAP 3 mm,0 mm\r\nCLS\r\nTEXT 40,40,\"3\",0,1,1,\"Imprelia Print Agent\"\r\nTEXT 40,90,\"2\",0,1,1,\"Prueba de impresion\"\r\nTEXT 40,130,\"2\",0,1,1,\"{EscapeLabelText(printer)}\"\r\nPRINT 1\r\n";

    private static string EplTest(string printer) =>
        $"N\r\nA40,40,0,4,1,1,N,\"Imprelia Print Agent\"\r\nA40,90,0,3,1,1,N,\"Prueba de impresion\"\r\nA40,130,0,3,1,1,N,\"{EscapeLabelText(printer)}\"\r\nP1\r\n";

    private static string DplTest(string printer) =>
        $"\u0002L\r\nD11\r\n191100000400040Imprelia Print Agent\r\n191100000900040Prueba de impresion\r\n191100001300040{EscapeLabelText(printer)}\r\nE\r\n";

    private static string EscapeZpl(string value) => value.Replace("^", " ").Replace("~", " ");

    private static string EscapeLabelText(string value) => value.Replace("\"", "'").Replace("\r", " ").Replace("\n", " ");
}
