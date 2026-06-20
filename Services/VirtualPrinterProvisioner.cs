using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Text;

namespace Imprelia.PrintAgent.Services;

public readonly record struct ProvisionResult(bool Success, string Message)
{
    public static ProvisionResult Ok(string msg) => new(true, msg);
    public static ProvisionResult Fail(string msg) => new(false, msg);
}

/// <summary>
/// Instala/quita la impresora virtual del modo cliente con un paso elevado (UAC),
/// estilo AnyDesk: una impresora real de Windows con el driver inbox
/// "Microsoft Print to PDF" apuntada a un archivo de spool. Imprimir a ella genera
/// el PDF sin diálogos; <see cref="PdfSpoolService"/> lo captura y lo manda al hub.
///
/// No requiere drivers de fabricante ni descubrimiento de red. Solo funciona en la
/// versión descargada/portable (la de Store, MSIX, no puede instalar impresoras).
/// </summary>
public static class VirtualPrinterProvisioner
{
    private const string DriverName = "Microsoft Print to PDF";

    public static bool PrinterExists(string localName)
    {
        if (string.IsNullOrWhiteSpace(localName)) return false;
        try
        {
            return PrinterSettings.InstalledPrinters.Cast<string>()
                .Any(p => string.Equals(p, localName, StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    public static ProvisionResult Install(ClientVirtualPrinter vp)
    {
        if (string.IsNullOrWhiteSpace(vp.LocalName))
            return ProvisionResult.Fail("La impresora virtual no tiene nombre.");

        // El archivo de salida (puerto) se calcula en contexto del usuario; el script
        // elevado usa la ruta literal (no recalcula, porque el admin tiene otro perfil).
        var outFile = PdfSpoolService.OutputFile(vp.Id);
        try { Directory.CreateDirectory(Path.GetDirectoryName(outFile)!); } catch { }

        var name = Escape(vp.LocalName);
        var file = Escape(outFile);
        var (wmm, hmm) = PaperMm(vp.PaperSize);

        var body = $@"
$name='{name}'; $port='{file}'; $drv='{DriverName}'

# Limpiar cualquier impresora/puerto previo con el mismo nombre o ruta.
Get-Printer -Name $name -ErrorAction SilentlyContinue | Remove-Printer -ErrorAction SilentlyContinue
Get-PrinterPort -Name $port -ErrorAction SilentlyContinue | Remove-PrinterPort -ErrorAction SilentlyContinue

# Puerto = archivo de salida (Local Port). El spooler escribe el PDF acá.
Add-PrinterPort -Name $port -ErrorAction Stop
Add-Printer -Name $name -DriverName $drv -PortName $port -ErrorAction Stop

# Tamaño de papel por defecto, según el tipo (80mm térmica, etiqueta, A4…), para
# que cualquier app (PedidosYa, Word, etc.) arme la página del tamaño correcto.
try {{
    Add-Type -AssemblyName System.Printing -ErrorAction Stop
    $srv = New-Object System.Printing.LocalPrintServer
    $q = $srv.GetPrintQueue($name)
    $t = $q.DefaultPrintTicket
    $toDip = {{ param($mm) [double]($mm / 25.4 * 96) }}
    $t.PageMediaSize = New-Object System.Printing.PageMediaSize((& $toDip {wmm}), (& $toDip {hmm}))
    $q.DefaultPrintTicket = $t
    $q.Commit()
}} catch {{ }}";

        return RunElevated(body,
            $"Impresora '{vp.LocalName}' instalada. Imprimí a ella desde cualquier app.",
            "No se pudo instalar la impresora.");
    }

    public static ProvisionResult Uninstall(ClientVirtualPrinter vp)
    {
        if (string.IsNullOrWhiteSpace(vp.LocalName))
            return ProvisionResult.Fail("Nombre de impresora vacío.");

        var outFile = PdfSpoolService.OutputFile(vp.Id);
        var name = Escape(vp.LocalName);
        var file = Escape(outFile);

        var body = $@"
Get-Printer -Name '{name}' -ErrorAction SilentlyContinue | Remove-Printer -ErrorAction SilentlyContinue
Get-PrinterPort -Name '{file}' -ErrorAction SilentlyContinue | Remove-PrinterPort -ErrorAction SilentlyContinue";

        return RunElevated(body, $"Impresora '{vp.LocalName}' eliminada de Windows.",
            "No se pudo eliminar la impresora.");
    }

    // ── Ejecución elevada ─────────────────────────────────────────────────────

    private static ProvisionResult RunElevated(string body, string okMsg, string failMsg)
    {
        string? temp = null, errFile = null;
        try
        {
            errFile = Path.Combine(Path.GetTempPath(), $"imprelia-err-{Guid.NewGuid():N}.txt");
            var errPath = errFile.Replace("'", "''");

            var script = $@"
$ErrorActionPreference='Stop'
try {{
{body}
}} catch {{
    ($_ | Out-String) | Set-Content -LiteralPath '{errPath}' -Encoding UTF8
    exit 1
}}
exit 0";

            temp = Path.Combine(Path.GetTempPath(), $"imprelia-vp-{Guid.NewGuid():N}.ps1");
            File.WriteAllText(temp, script, new UTF8Encoding(false));

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{temp}\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };

            using var p = Process.Start(psi);
            if (p == null) return ProvisionResult.Fail(failMsg + " (no se pudo iniciar PowerShell).");
            p.WaitForExit();
            if (p.ExitCode == 0) return ProvisionResult.Ok(okMsg);

            var err = "";
            try { if (File.Exists(errFile)) err = File.ReadAllText(errFile).Trim(); } catch { }
            err = Shorten(err);
            return ProvisionResult.Fail(string.IsNullOrEmpty(err) ? $"{failMsg} (código {p.ExitCode})." : $"{failMsg} — {err}");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return ProvisionResult.Fail("Operación cancelada (se necesitan permisos de administrador).");
        }
        catch (Exception ex)
        {
            return ProvisionResult.Fail($"{failMsg} {ex.Message}");
        }
        finally
        {
            try { if (temp != null && File.Exists(temp)) File.Delete(temp); } catch { }
            try { if (errFile != null && File.Exists(errFile)) File.Delete(errFile); } catch { }
        }
    }

    /// <summary>Ancho×alto en mm del papel por defecto según el tipo de impresora.</summary>
    private static (double w, double h) PaperMm(string? paperSize) => (paperSize ?? "a4").ToLowerInvariant() switch
    {
        "thermal80" => (80, 297),   // rollo térmico 80mm (largo generoso; el corte lo da la térmica)
        "thermal58" => (58, 297),   // rollo térmico 58mm
        "letter"    => (216, 279),  // carta
        _           => (210, 297),  // A4
    };

    private static string Shorten(string s)
    {
        s = s.Replace("\r", " ").Replace("\n", " ").Trim();
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        return s.Length > 400 ? s[..400] + "…" : s;
    }

    private static string Escape(string s) => s.Replace("'", "''");
}
