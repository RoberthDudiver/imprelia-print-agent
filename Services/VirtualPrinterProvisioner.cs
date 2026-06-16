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
/// Crea o quita colas de impresión en Windows que apuntan al servidor IPP local,
/// usando el driver de clase IPP incorporado (sin drivers de fabricante).
///
/// Requiere elevación (UAC): Add-Printer/Remove-Printer crean puertos del sistema.
/// El servidor IPP debe estar corriendo al instalar, porque Windows consulta sus
/// capacidades (Get-Printer-Attributes) durante el alta.
/// </summary>
public static class VirtualPrinterProvisioner
{
    /// <summary>¿Existe una cola con ese nombre en Windows?</summary>
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

    /// <summary>Instala una impresora virtual apuntando a http://localhost:{port}/ipp/{id}.</summary>
    public static ProvisionResult Install(ClientVirtualPrinter vp, int ippPort)
    {
        if (string.IsNullOrWhiteSpace(vp.LocalName))
            return ProvisionResult.Fail("La impresora virtual no tiene nombre.");

        var name = Escape(vp.LocalName);
        var url = $"http://localhost:{ippPort}/ipp/{vp.Id}";

        var script = $@"
$ErrorActionPreference = 'Stop'
try {{
    $existing = Get-Printer -Name '{name}' -ErrorAction SilentlyContinue
    if ($existing) {{ Remove-Printer -Name '{name}' -ErrorAction SilentlyContinue }}
    Add-Printer -Name '{name}' -DeviceURL '{url}'
    exit 0
}} catch {{
    Write-Error $_
    exit 1
}}";

        return RunElevated(script, $"Impresora '{vp.LocalName}' instalada.", "No se pudo instalar la impresora.");
    }

    /// <summary>Quita una impresora virtual de Windows.</summary>
    public static ProvisionResult Uninstall(string localName)
    {
        if (string.IsNullOrWhiteSpace(localName))
            return ProvisionResult.Fail("Nombre de impresora vacío.");

        var name = Escape(localName);
        var script = $@"
$ErrorActionPreference = 'Stop'
try {{
    Remove-Printer -Name '{name}' -ErrorAction SilentlyContinue
    exit 0
}} catch {{
    Write-Error $_
    exit 1
}}";

        return RunElevated(script, $"Impresora '{localName}' eliminada.", "No se pudo eliminar la impresora.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ProvisionResult RunElevated(string script, string okMsg, string failMsg)
    {
        string? temp = null;
        try
        {
            temp = Path.Combine(Path.GetTempPath(), $"imprelia-vp-{Guid.NewGuid():N}.ps1");
            File.WriteAllText(temp, script, new UTF8Encoding(false));

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{temp}\"",
                UseShellExecute = true,   // requerido para Verb=runas
                Verb = "runas",            // dispara UAC
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };

            using var p = Process.Start(psi);
            if (p == null) return ProvisionResult.Fail(failMsg + " (no se pudo iniciar PowerShell).");

            p.WaitForExit();
            return p.ExitCode == 0
                ? ProvisionResult.Ok(okMsg)
                : ProvisionResult.Fail($"{failMsg} PowerShell devolvió código {p.ExitCode}.");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // 1223 = ERROR_CANCELLED: el usuario rechazó el UAC.
            return ProvisionResult.Fail("Operación cancelada (se necesitan permisos de administrador).");
        }
        catch (Exception ex)
        {
            return ProvisionResult.Fail($"{failMsg} {ex.Message}");
        }
        finally
        {
            try { if (temp != null && File.Exists(temp)) File.Delete(temp); } catch { }
        }
    }

    private static string Escape(string s) => s.Replace("'", "''");
}
