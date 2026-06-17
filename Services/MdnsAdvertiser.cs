using System.Net;
using Makaretu.Dns;

namespace Imprelia.PrintAgent.Services;

/// <summary>
/// Anuncia cada impresora virtual por mDNS/DNS-SD como una impresora IPP Everywhere
/// (servicio _ipp._tcp). Así Windows la descubre sola y aparece en "Agregar
/// impresora" — sin drivers, sin admin, sin pegar URLs.
///
/// Se anuncia en 127.0.0.1: el servidor IPP escucha en loopback y Windows corre en
/// la misma máquina, así que resuelve y conecta sin problema.
/// </summary>
public sealed class MdnsAdvertiser : IDisposable
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;

    private ServiceDiscovery? _sd;
    private readonly List<ServiceProfile> _profiles = new();

    public MdnsAdvertiser(AppConfig config, AgentLogService log)
    {
        _config = config;
        _log = log;
    }

    public void Start()
    {
        if (!_config.ClientMode.Enabled) return;
        Stop();
        try
        {
            _sd = new ServiceDiscovery();
            AdvertiseAll();
        }
        catch (Exception ex)
        {
            _log.Error($"mDNS: no se pudo iniciar el anuncio: {ex.Message}", "Cliente");
        }
    }

    /// <summary>Reanuncia con la lista actual de impresoras virtuales.</summary>
    public void Refresh()
    {
        if (_sd == null) { Start(); return; }
        try
        {
            foreach (var p in _profiles) { try { _sd.Unadvertise(p); } catch { } }
            _profiles.Clear();
            AdvertiseAll();
        }
        catch (Exception ex) { _log.Error($"mDNS refresh falló: {ex.Message}", "Cliente"); }
    }

    private void AdvertiseAll()
    {
        if (_sd == null) return;
        var port = (ushort)(_config.ClientMode.IppPort > 0 ? _config.ClientMode.IppPort : 9110);
        var addrs = new[] { IPAddress.Loopback };

        foreach (var vp in _config.ClientMode.VirtualPrinters)
        {
            if (string.IsNullOrWhiteSpace(vp.LocalName)) continue;
            try
            {
                var profile = new ServiceProfile(vp.LocalName, "_ipp._tcp", port, addrs);
                profile.AddProperty("txtvers", "1");
                profile.AddProperty("qtotal", "1");
                profile.AddProperty("rp", $"ipp/{vp.Id}");
                profile.AddProperty("ty", vp.LocalName);
                profile.AddProperty("note", "Imprelia");
                profile.AddProperty("product", "(Imprelia)");
                profile.AddProperty("pdl", "application/pdf");
                profile.AddProperty("adminurl", $"http://localhost:{port}/");
                profile.AddProperty("priority", "50");
                profile.AddProperty("Color", "F");
                profile.AddProperty("Duplex", "F");
                profile.AddProperty("UUID", DeterministicGuid(vp.Id));
                // URF: hace que Windows lo reconozca como IPP Everywhere (la negociación
                // real de formato sigue siendo PDF, que es lo que el IPP server soporta).
                profile.AddProperty("URF", "V1.4,CP1,RS300,W8,SRGB24,DM1");

                _sd.Advertise(profile);
                _sd.Announce(profile);
                _profiles.Add(profile);
            }
            catch (Exception ex) { _log.Warn($"mDNS: no se pudo anunciar '{vp.LocalName}': {ex.Message}", "Cliente"); }
        }

        _log.Info($"mDNS: anunciando {_profiles.Count} impresora(s) para descubrimiento en Windows.", "Cliente");
    }

    public void Stop()
    {
        try { if (_sd != null) foreach (var p in _profiles) { try { _sd.Unadvertise(p); } catch { } } } catch { }
        _profiles.Clear();
        try { _sd?.Dispose(); } catch { }
        _sd = null;
    }

    private static string DeterministicGuid(string seed)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("imprelia-mdns-" + seed));
        return new Guid(hash).ToString();
    }

    public void Dispose() => Stop();
}
