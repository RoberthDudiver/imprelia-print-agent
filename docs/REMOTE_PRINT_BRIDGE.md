# Remote Print Bridge

The Imprelia Print Agent normally prints from the **same machine/LAN** via its
local API (`http://localhost:9100`). The **Remote Print Bridge** adds an optional
mode where a **backend** pushes print jobs to the agent over the internet — so you
can print to the venue's printer even when no browser/POS is open there (e.g. an
online order arrives, or you print from your phone on mobile data).

Security model: the agent **connects outbound** to a server URL you configure
(SignalR over WSS, or HTTP polling). It never opens public ports and is never
reachable directly from the internet. Remote mode is **off by default**.

```
Backend (Imprelia.Server)  ◄──outbound WSS/HTTPS──  Imprelia Print Agent  ──RAW──►  Printer
   pushes print jobs                                  (at the venue)
```

---

## 1. Configure the agent (Windows)

Open the agent → **Remote Bridge** tab:

| Field          | Value |
|----------------|-------|
| Enable         | Yes |
| **Server URL** | Origin of your backend, e.g. `https://api.midominio.com` (no `/api`, no trailing path — the hub is at `/imprelia/hub`) |
| **Agent ID**   | A unique id for this venue's agent, e.g. `kitchen-01` (your backend decides the convention) |
| **API Key**    | A shared secret your backend accepts (leave empty if the server runs in dev mode with no keys) |
| Mode           | `SignalR` (recommended) or `Polling HTTP` |
| Auto-reconnect | ✅ |

Click **Save**. The status dot turns **green / Connected** when the handshake
succeeds. **Test connection** does a `GET /imprelia/status` against the server.

Make sure the **Routes** tab maps the purposes you'll use (`ticket`,
`kitchen_order`, `label`, …) to real printers — remote jobs resolve the printer
from these routes.

### Connection states

`Disabled` · `Connecting` · `Connected` · `Disconnected` ·
`AuthenticationFailed` (wrong API key) · `ServerUnavailable` (can't reach server).

---

## 2. Add the bridge to your backend (`Imprelia.Server`)

The [`Imprelia.Server`](../../Imprelia.Server) NuGet package provides the SignalR
hub + HTTP endpoints the agent connects to.

```bash
dotnet add package Imprelia.Server
```

```csharp
using Imprelia.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddImpreliaServer(opts =>
{
    opts.AllowedApiKeys.Add("your-secret-key");   // empty list = dev mode (accept any)
    // opts.ExposeHttpJobApi = true;              // optional public POST /imprelia/jobs
});

var app = builder.Build();
app.MapImprelia();                                 // maps /imprelia/hub + agent endpoints
app.Run();
```

Send a job from anywhere in your app (authenticated code):

```csharp
public class PrintController(IImpreliaService imprelia) : ControllerBase
{
    [HttpPost("print-ticket")]
    public async Task<IActionResult> PrintTicket(string agentId, string escposBase64)
    {
        var job = await imprelia.SendPrintJobAsync(new PrintJobRequest
        {
            AgentId = agentId,        // must match the agent's Agent ID
            Route   = "ticket",       // a route configured in the agent
            Type    = "escpos",
            Content = escposBase64,   // base64 of the raw printer bytes
            Copies  = 1,
        });
        return Ok(new { job.JobId, job.Status });
    }
}
```

If the agent is offline, the job is **queued** and delivered when it reconnects.

A complete runnable backend is in [`examples/RemotePrintExample`](../../examples/RemotePrintExample).

---

## 3. Protocol (for non-.NET backends)

The agent talks to these endpoints. You can implement them in any stack.

**SignalR hub** — `/imprelia/hub` (headers `X-Agent-Id`, `X-Api-Key`):
1. Agent invokes `RegisterAgent(agentId, apiKey)`.
2. Server pushes the `PrintJob` method with a `RemotePrintJob` payload.
3. Agent invokes `ReportStatus(jobId, status, error?)`.

**HTTP (polling fallback / status)**, all with `X-Agent-Id` + `X-Api-Key`:

| Method | Path | Purpose |
|--------|------|---------|
| `GET`  | `/imprelia/status` | Health + connected agents (used by *Test connection*) |
| `GET`  | `/imprelia/jobs/pending?agentId=X` | Agent pulls queued jobs (polling mode) |
| `POST` | `/imprelia/jobs/{jobId}/status` | Agent reports `{ status, error?, agentId }` |
| `POST` | `/imprelia/jobs` | Enqueue a job *(only if `ExposeHttpJobApi=true`)* |

**`RemotePrintJob`**: `{ jobId, agentId, route, type, content (base64), copies, metadata? }`

**Job statuses**: `pending` → `received_by_agent` → `printing` → `printed`,
or `failed` / `printer_not_found` / `invalid_payload`.

---

## 4. Notes

- **Content types**: the route's content type wins. `raw`/`escpos`/`zpl`/`tspl`
  are sent to the printer as raw bytes; the `content` field is base64 of those bytes.
- **HTTPS/WSS only** in production. The agent refuses nothing locally, but you
  should terminate TLS at your server.
- **API keys**: configure `AllowedApiKeys` on the server and the matching key on
  each agent. An empty server list means "accept any" (dev only).
- See [`Imprelia.Server/PUBLISHING.md`](../../Imprelia.Server/PUBLISHING.md) to
  install the package from GitHub Packages.
