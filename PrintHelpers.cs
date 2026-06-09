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
