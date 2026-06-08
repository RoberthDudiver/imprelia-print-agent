using System.Runtime.InteropServices;

namespace Imprelia.PrintAgent;

/// <summary>
/// Envía bytes RAW (ESC/POS) directo a la cola de impresión de Windows usando
/// la API del spooler (winspool.drv). Al usar datatype "RAW", el driver NO
/// reinterpreta los bytes — pasan tal cual a la impresora, que es exactamente
/// lo que necesitamos para comandos ESC/POS de una térmica.
///
/// Funciona con cualquier impresora instalada en Windows, incluida la genérica
/// "Global TP-POS58" siempre que esté agregada como impresora del sistema.
/// </summary>
public static class RawPrinter
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOCINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFO di);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    /// <summary>
    /// Manda los bytes RAW al spooler de la impresora indicada.
    /// Devuelve null si todo OK, o un mensaje de error si falló.
    /// </summary>
    public static string? SendBytes(string printerName, byte[] data, string? jobName = null)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            return "No hay impresora seleccionada.";
        if (data == null || data.Length == 0)
            return "No hay datos para imprimir.";

        IntPtr hPrinter = IntPtr.Zero;
        IntPtr unmanaged = IntPtr.Zero;
        try
        {
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                return $"No se pudo abrir la impresora '{printerName}' (¿existe? ¿está encendida?). Error {Marshal.GetLastWin32Error()}.";

            var di = new DOCINFO
            {
                pDocName = string.IsNullOrWhiteSpace(jobName) ? "Imprelia Print Job" : jobName,
                pOutputFile = null,
                pDataType = "RAW",
            };

            if (!StartDocPrinter(hPrinter, 1, ref di))
                return $"StartDocPrinter falló. Error {Marshal.GetLastWin32Error()}.";

            if (!StartPagePrinter(hPrinter))
            {
                EndDocPrinter(hPrinter);
                return $"StartPagePrinter falló. Error {Marshal.GetLastWin32Error()}.";
            }

            unmanaged = Marshal.AllocCoTaskMem(data.Length);
            Marshal.Copy(data, 0, unmanaged, data.Length);

            bool ok = WritePrinter(hPrinter, unmanaged, data.Length, out int written);
            var err = ok ? 0 : Marshal.GetLastWin32Error();

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);

            if (!ok)
                return $"WritePrinter falló. Error {err}.";
            if (written != data.Length)
                return $"Se escribieron {written} de {data.Length} bytes.";

            return null; // éxito
        }
        catch (Exception ex)
        {
            return $"Excepción al imprimir: {ex.Message}";
        }
        finally
        {
            if (unmanaged != IntPtr.Zero) Marshal.FreeCoTaskMem(unmanaged);
            if (hPrinter != IntPtr.Zero) ClosePrinter(hPrinter);
        }
    }

    /// <summary>Lista los nombres de las impresoras instaladas en el sistema.</summary>
    public static List<string> ListPrinters()
    {
        var result = new List<string>();
        try
        {
            foreach (string p in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                result.Add(p);
        }
        catch { /* ignore */ }
        return result;
    }
}
