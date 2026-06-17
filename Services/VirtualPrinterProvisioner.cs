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
/// Instala/quita la impresora IPP en Windows con un paso elevado (UAC). Activa la
/// característica "Cliente de impresión por Internet" si falta y prueba varios
/// métodos de alta (es la única forma confiable en Windows para una impresora IPP
/// que apunta a un servidor local).
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

    public static ProvisionResult Install(ClientVirtualPrinter vp, int ippPort)
    {
        if (string.IsNullOrWhiteSpace(vp.LocalName))
            return ProvisionResult.Fail("La impresora virtual no tiene nombre.");

        var name = Escape(vp.LocalName);
        var url  = $"http://localhost:{ippPort}/ipp/{vp.Id}";

        var body = $@"
$name='{name}'; $url='{url}'; $drv='Microsoft IPP Class Driver'

# 1) Asegurar la característica 'Cliente de impresión por Internet'.
try {{
    $f = Get-WindowsOptionalFeature -Online -FeatureName 'Printing-Foundation-InternetPrinting-Client' -ErrorAction SilentlyContinue
    if ($f -and $f.State -ne 'Enabled') {{
        Enable-WindowsOptionalFeature -Online -FeatureName 'Printing-Foundation-InternetPrinting-Client' -All -NoRestart -ErrorAction Stop | Out-Null
        Start-Sleep -Seconds 2
    }}
}} catch {{ }}

if (Get-Printer -Name $name -ErrorAction SilentlyContinue) {{ Remove-Printer -Name $name -ErrorAction SilentlyContinue }}

$ok=$false; $errs=@()
# A) Internet Print Provider (impresora compartida por nombre).
if (-not $ok) {{ try {{ Add-Printer -ConnectionName $url -ErrorAction Stop; $ok=$true }} catch {{ $errs += 'ConnName: ' + $_.Exception.Message }} }}
# B) DeviceURL.
if (-not $ok) {{ try {{ Add-Printer -Name $name -DeviceURL $url -ErrorAction Stop; $ok=$true }} catch {{ $errs += 'DeviceURL: ' + $_.Exception.Message }} }}
# C) Puerto con la URL + driver de clase IPP.
if (-not $ok) {{ try {{ if (-not (Get-PrinterPort -Name $url -ErrorAction SilentlyContinue)) {{ Add-PrinterPort -Name $url -ErrorAction Stop }}; Add-Printer -Name $name -DriverName $drv -PortName $url -ErrorAction Stop; $ok=$true }} catch {{ $errs += 'PortDrv: ' + $_.Exception.Message }} }}

if (-not $ok) {{ throw ($errs -join '  ||  ') }}";

        return RunElevated(body, $"Impresora '{vp.LocalName}' agregada a Windows.", "No se pudo agregar la impresora.");
    }

    public static ProvisionResult Uninstall(string localName)
    {
        if (string.IsNullOrWhiteSpace(localName))
            return ProvisionResult.Fail("Nombre de impresora vacío.");
        var name = Escape(localName);
        return RunElevated($"Remove-Printer -Name '{name}' -ErrorAction Stop",
            $"Impresora '{localName}' eliminada.", "No se pudo eliminar la impresora.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    private static string Shorten(string s)
    {
        s = s.Replace("\r", " ").Replace("\n", " ").Trim();
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        return s.Length > 400 ? s[..400] + "…" : s;
    }

    private static string Escape(string s) => s.Replace("'", "''");
}
