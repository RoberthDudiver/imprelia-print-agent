using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Imprelia.PrintAgent.Services;

/// <summary>
/// Anuncia cada impresora virtual por mDNS/DNS-SD (servicio <c>_ipp._tcp</c>) como
/// una impresora IPP Everywhere, usando la <b>API nativa de Windows</b>
/// (<c>DnsServiceRegister</c> en dnsapi.dll). Así Windows la descubre sola y aparece
/// en "Agregar impresora" — sin drivers, sin admin, sin pegar URLs.
///
/// Por qué la API nativa y no una librería propia (Makaretu): el responder mDNS lo
/// maneja el sistema operativo, que ya es dueño del puerto UDP 5353. Una librería
/// que intente bindear el 5353 falla si Chrome/Bonjour lo ocupan; la API del SO
/// convive sin conflicto.
///
/// Se anuncia el host <c>imprelia.local → 127.0.0.1</c>: el servidor IPP escucha en
/// loopback, así que Windows (misma máquina) conecta sin exponer nada a la red.
/// </summary>
public sealed class MdnsAdvertiser : IDisposable
{
    private readonly AppConfig _config;
    private readonly AgentLogService _log;
    private readonly List<Registration> _active = new();
    private readonly List<Registration> _retired = new();
    private readonly object _lock = new();

    private const string HostName = "imprelia.local";

    public MdnsAdvertiser(AppConfig config, AgentLogService log)
    {
        _config = config;
        _log = log;
    }

    public void Start()
    {
        if (!_config.ClientMode.Enabled) return;
        Refresh();
    }

    /// <summary>Reanuncia con la lista actual de impresoras virtuales.</summary>
    public void Refresh()
    {
        if (!OperatingSystem.IsWindows()) return;
        lock (_lock)
        {
            DeregisterAll();
            if (!_config.ClientMode.Enabled) return;

            var port = (ushort)(_config.ClientMode.IppPort > 0 ? _config.ClientMode.IppPort : 9110);
            int ok = 0;
            foreach (var vp in _config.ClientMode.VirtualPrinters)
            {
                if (string.IsNullOrWhiteSpace(vp.LocalName)) continue;
                try { if (Advertise(vp, port)) ok++; }
                catch (Exception ex) { _log.Warn($"mDNS: no se pudo anunciar '{vp.LocalName}': {ex.Message}", "Cliente"); }
            }
            _log.Info($"mDNS (API nativa de Windows): {ok} impresora(s) anunciada(s) para descubrimiento.", "Cliente");
        }
    }

    public void Stop()
    {
        if (!OperatingSystem.IsWindows()) return;
        lock (_lock) { DeregisterAll(); }
    }

    // ── Registro nativo ───────────────────────────────────────────────────────

    private bool Advertise(ClientVirtualPrinter vp, ushort port)
    {
        var instanceName = $"{Sanitize(vp.LocalName)}._ipp._tcp.local";

        string[] keys = { "txtvers", "qtotal", "rp", "ty", "note", "product",
                          "pdl", "adminurl", "priority", "Color", "Duplex", "UUID", "URF" };
        string[] vals =
        {
            "1", "1", $"ipp/{vp.Id}", vp.LocalName, "Imprelia", "(Imprelia)",
            "application/pdf", $"http://localhost:{port}/", "50", "F", "F",
            DeterministicGuid(vp.Id),
            // URF: hace que Windows lo trate como IPP Everywhere (la negociación real
            // de formato sigue siendo PDF, que es lo que el servidor IPP soporta).
            "V1.4,CP1,RS300,W8,SRGB24,DM1",
        };

        // A record imprelia.local → 127.0.0.1 (IP4_ADDRESS = DWORD en orden de red).
        IntPtr ip4 = Marshal.AllocHGlobal(4);
        Marshal.WriteInt32(ip4, BitConverter.ToInt32(IPAddress.Loopback.GetAddressBytes(), 0));

        IntPtr instance = DnsServiceConstructInstance(
            instanceName, HostName, ip4, IntPtr.Zero,
            port, 0, 0, (uint)keys.Length, keys, vals);
        Marshal.FreeHGlobal(ip4);

        if (instance == IntPtr.Zero)
        {
            _log.Warn($"mDNS: ConstructInstance devolvió NULL para '{vp.LocalName}'.", "Cliente");
            return false;
        }

        var reg = new Registration { Instance = instance, Name = vp.LocalName };
        reg.Callback = (status, ctx, inst) =>
        {
            if (status != 0)
                _log.Warn($"mDNS: registro de '{reg.Name}' devolvió status {status}.", "Cliente");
        };

        var req = new DNS_SERVICE_REGISTER_REQUEST
        {
            Version = DNS_QUERY_REQUEST_VERSION1,
            InterfaceIndex = 0,
            pServiceInstance = instance,
            pRegisterCompletionCallback = Marshal.GetFunctionPointerForDelegate(reg.Callback),
            pQueryContext = IntPtr.Zero,
            hCredentials = IntPtr.Zero,
            unicastEnabled = 0,
        };

        int rc = DnsServiceRegister(ref req, IntPtr.Zero);
        // 9506 = DNS_REQUEST_PENDING (correcto: el registro completa async).
        if (rc != DNS_REQUEST_PENDING && rc != 0)
        {
            _log.Warn($"mDNS: DnsServiceRegister falló para '{vp.LocalName}' (rc={rc}).", "Cliente");
            DnsServiceFreeInstance(instance);
            return false;
        }

        _active.Add(reg);
        return true;
    }

    private void DeregisterAll()
    {
        foreach (var reg in _active)
        {
            try
            {
                var req = new DNS_SERVICE_REGISTER_REQUEST
                {
                    Version = DNS_QUERY_REQUEST_VERSION1,
                    pServiceInstance = reg.Instance,
                    pRegisterCompletionCallback = reg.Callback != null
                        ? Marshal.GetFunctionPointerForDelegate(reg.Callback) : IntPtr.Zero,
                };
                DnsServiceDeRegister(ref req, IntPtr.Zero);
            }
            catch { }
            // No liberamos la instancia de inmediato: el de-register completa async y
            // el SO sigue usando el puntero. La retiramos (manteniendo viva la delegate)
            // y se libera al cerrar.
            _retired.Add(reg);
        }
        _active.Clear();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            DeregisterAll();
            foreach (var reg in _retired)
            {
                try { DnsServiceFreeInstance(reg.Instance); } catch { }
            }
            _retired.Clear();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Sanitize(string name)
    {
        // El nombre de instancia mDNS admite espacios; solo quitamos '.' que rompen la etiqueta.
        var s = name.Replace('.', ' ').Trim();
        return string.IsNullOrEmpty(s) ? "Imprelia" : s;
    }

    private static string DeterministicGuid(string seed)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("imprelia-mdns-" + seed));
        return new Guid(hash).ToString();
    }

    private sealed class Registration
    {
        public IntPtr Instance;
        public RegisterComplete? Callback;   // mantener viva la delegate mientras esté registrada
        public string Name = "";
    }

    // ── P/Invoke: dnsapi.dll ──────────────────────────────────────────────────

    private const uint DNS_QUERY_REQUEST_VERSION1 = 1;
    private const int DNS_REQUEST_PENDING = 9506;

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private delegate void RegisterComplete(uint status, IntPtr context, IntPtr instance);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DNS_SERVICE_REGISTER_REQUEST
    {
        public uint Version;
        public uint InterfaceIndex;
        public IntPtr pServiceInstance;
        public IntPtr pRegisterCompletionCallback;
        public IntPtr pQueryContext;
        public IntPtr hCredentials;
        public byte unicastEnabled;
    }

    [DllImport("dnsapi.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DnsServiceConstructInstance(
        string pServiceName, string pHostName, IntPtr pIp4, IntPtr pIp6,
        ushort wPort, ushort wPriority, ushort wWeight,
        uint dwPropertiesCount, string[] keys, string[] values);

    [DllImport("dnsapi.dll")]
    private static extern void DnsServiceFreeInstance(IntPtr pInstance);

    [DllImport("dnsapi.dll", CharSet = CharSet.Unicode)]
    private static extern int DnsServiceRegister(ref DNS_SERVICE_REGISTER_REQUEST pRequest, IntPtr pCancel);

    [DllImport("dnsapi.dll", CharSet = CharSet.Unicode)]
    private static extern int DnsServiceDeRegister(ref DNS_SERVICE_REGISTER_REQUEST pRequest, IntPtr pCancel);
}
