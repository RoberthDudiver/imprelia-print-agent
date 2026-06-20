using System.Diagnostics;
using System.Drawing.Printing;

namespace Imprelia.PrintAgent.Services;

/// <summary>
/// Helpers de impresora del modo cliente. El alta real la hace Windows solo al
/// descubrir la impresora anunciada por mDNS (<see cref="MdnsAdvertiser"/>): aparece
/// en "Agregar impresora" y se instala con el driver de clase IPP inbox, por-usuario
/// y sin admin. Acá solo abrimos esa pantalla y permitimos quitar la cola.
/// </summary>
public static class VirtualPrinterProvisioner
{
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

    /// <summary>Abre la pantalla de "Agregar impresora" de Windows (descubrimiento mDNS).</summary>
    public static void OpenAddPrinter()
    {
        // La página moderna de Configuración lista las impresoras descubiertas por mDNS.
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "ms-settings:printers", UseShellExecute = true });
            return;
        }
        catch { }
        // Fallback: el asistente clásico.
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = "printui.dll,PrintUIEntry /il",
                UseShellExecute = true,
            });
        }
        catch { }
    }

    /// <summary>Quita la cola de Windows (por-usuario, sin admin).</summary>
    public static ProvisionResult Uninstall(string localName)
    {
        if (string.IsNullOrWhiteSpace(localName))
            return ProvisionResult.Fail("Nombre de impresora vacío.");

        // La impresora descubierta puede llamarse distinto al nombre local; quitamos
        // cualquier cola que coincida con el nombre o que apunte a nuestro host mDNS.
        var name = localName.Replace("'", "''");
        var script =
            $"$ErrorActionPreference='SilentlyContinue';" +
            $"Get-Printer | Where-Object {{ $_.Name -eq '{name}' -or $_.Name -like '*Imprelia*' -or $_.PortName -like '*imprelia.local*' }} | " +
            $"ForEach-Object {{ Remove-Printer -Name $_.Name }}";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return ProvisionResult.Fail("No se pudo iniciar PowerShell.");
            p.WaitForExit();
            return ProvisionResult.Ok($"Impresora '{localName}' quitada de Windows (si estaba instalada).");
        }
        catch (Exception ex)
        {
            return ProvisionResult.Fail($"No se pudo quitar la impresora: {ex.Message}");
        }
    }
}

public readonly record struct ProvisionResult(bool Success, string Message)
{
    public static ProvisionResult Ok(string msg) => new(true, msg);
    public static ProvisionResult Fail(string msg) => new(false, msg);
}
