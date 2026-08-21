using System.Text.Json;

namespace Imprelia.PrintAgent;

public static class ApiDocumentation
{
    public static string OpenApiJson()
    {
        var document = new
        {
            openapi = "3.0.3",
            info = new
            {
                title = "Imprelia Print Agent API",
                version = LocalServer.Version,
                description = "Local HTTP API for printing to Windows printers, EPOS/ESC-POS devices, TSPL/ZPL/EPL/DPL label printers, PDF-capable printers, and future fiscal adapters.",
                contact = new
                {
                    name = "Roberth Dudiver",
                    url = "https://dudiver.net",
                },
                license = new
                {
                    name = "Imprelia Print Agent Non-Commercial Source License",
                },
            },
            servers = new[]
            {
                new { url = "http://localhost:9100", description = "Default local agent URL" },
            },
            paths = new Dictionary<string, object>
            {
                ["/ping"] = Path("Legacy ping endpoint.", "Checks that the legacy API is alive.", "PingResponse"),
                ["/printers"] = Path("Legacy printers endpoint.", "Returns the legacy list of installed printer names.", "LegacyPrintersResponse"),
                ["/api/health"] = Path("Health check.", "Returns agent status, version, uptime, port, and printer count.", "HealthResponse"),
                ["/api/printers"] = Path("List Windows printers.", "Returns installed local, shared, network, and virtual printers with detected capabilities.", "PrintersResponse"),
                ["/api/settings"] = new
                {
                    get = Operation("Get settings.", "Returns server, security, default printer, printer types, and routes.", null, "SettingsResponse"),
                    put = Operation("Update settings.", "Updates server/security/printer settings. Port changes require restart.", "AgentSettings", "SettingsResponse"),
                },
                ["/api/settings/printer-routes"] = new
                {
                    get = Operation("Get printer routes.", "Returns configured printer routes by purpose.", null, "RoutesResponse"),
                    put = Operation("Update printer routes.", "Replaces configured printer routes by purpose.", "RoutesRequest", "RoutesResponse"),
                },
                ["/api/print"] = new
                {
                    post = Operation("Universal print.", "Prints a job by explicit printer name or default printer.", "PrintRequest", "PrintResponse"),
                },
                ["/api/print/by-purpose"] = new
                {
                    post = Operation("Print by purpose.", "Looks up a configured purpose route and sends the job to that printer.", "PrintByPurposeRequest", "PrintResponse"),
                },
                ["/api/printers/test"] = new
                {
                    post = Operation("Print test.", "Prints a content-type-aware test job to the selected printer.", "PrinterTestRequest", "PrintResponse"),
                },
                ["/api/jobs/recent"] = Path("Recent jobs.", "Returns recent in-memory print jobs and errors.", "JobsResponse"),
                ["/openapi.json"] = Path("OpenAPI document.", "Returns this OpenAPI JSON document.", "Object"),
                ["/docs"] = Path("API guide (offline).", "Self-contained interactive API guide rendered from this OpenAPI document. Works without internet.", "String"),
            },
            components = new
            {
                schemas = Schemas(),
                securitySchemes = new
                {
                    ApiKey = new
                    {
                        type = "apiKey",
                        @in = "header",
                        name = "X-Api-Key",
                        description = "Optional. Only required when requireApiKey is enabled in the agent settings.",
                    },
                },
            },
        };

        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Guía de la API 100% OFFLINE: HTML+CSS+JS autocontenido, sin CDN.
    /// Lee /openapi.json (local) y arma la referencia en el navegador. Funciona sin internet.
    /// </summary>
    public static string GuideHtml() =>
        """
        <!doctype html>
        <html lang="es">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>Imprelia Print Agent — Guía de la API</title>
          <style>
            :root { --bg:#0f1420; --card:#161c2b; --line:#26304a; --ink:#e2e8f0; --muted:#94a3b8; --accent:#5654e0; --accent2:#b06cff; --get:#16a34a; --post:#2563eb; --put:#d97706; }
            * { box-sizing: border-box; }
            body { margin:0; background:var(--bg); color:var(--ink); font-family:Segoe UI,system-ui,Arial,sans-serif; line-height:1.5; }
            header { padding:26px 28px; border-bottom:1px solid var(--line); background:linear-gradient(120deg,#141a29,#0f1420); }
            header h1 { margin:0 0 4px; font-size:20px; }
            header .sub { color:var(--muted); font-size:13px; }
            header .pill { display:inline-block; margin-top:10px; padding:4px 10px; border-radius:999px; background:linear-gradient(90deg,var(--accent),var(--accent2)); color:#fff; font-size:12px; font-weight:600; }
            main { max-width:960px; margin:0 auto; padding:24px 20px 64px; }
            .ep { background:var(--card); border:1px solid var(--line); border-radius:12px; margin:0 0 14px; overflow:hidden; }
            .ep summary { list-style:none; cursor:pointer; display:flex; align-items:center; gap:12px; padding:14px 16px; }
            .ep summary::-webkit-details-marker { display:none; }
            .m { font-weight:700; font-size:11px; letter-spacing:.5px; padding:4px 8px; border-radius:6px; color:#fff; min-width:52px; text-align:center; }
            .m.get{background:var(--get);} .m.post{background:var(--post);} .m.put{background:var(--put);} .m.delete{background:#dc2626;}
            .path { font-family:Consolas,monospace; font-size:14px; }
            .sum { color:var(--muted); font-size:13px; margin-left:auto; text-align:right; }
            .body { padding:0 16px 16px; border-top:1px solid var(--line); }
            .body p { color:var(--muted); font-size:13.5px; }
            .lbl { text-transform:uppercase; letter-spacing:.5px; font-size:11px; color:var(--muted); margin:14px 0 6px; }
            pre { background:#0b0f18; border:1px solid var(--line); border-radius:8px; padding:12px; overflow:auto; font-size:12.5px; }
            code { font-family:Consolas,monospace; }
            .err { background:#2a1620; border:1px solid #5b2130; color:#fca5a5; padding:16px; border-radius:10px; }
            a { color:var(--accent2); }
          </style>
        </head>
        <body>
          <header>
            <h1>Imprelia Print Agent — Guía de la API</h1>
            <div class="sub">Referencia local de la API de impresión. Esta página funciona sin internet.</div>
            <div class="pill" id="ver">cargando…</div>
          </header>
          <main id="app"><p style="color:var(--muted)">Cargando la especificación…</p></main>
          <script>
            const mc = { get:'get', post:'post', put:'put', delete:'delete' };
            function esc(s){ return String(s).replace(/[&<>]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;'}[c])); }
            function schemaName(ref){ return ref ? ref.split('/').pop() : null; }
            fetch('/openapi.json').then(r => r.json()).then(doc => {
              document.getElementById('ver').textContent = (doc.info?.title || 'API') + ' v' + (doc.info?.version || '');
              const app = document.getElementById('app');
              app.innerHTML = '';
              if (doc.info?.description) {
                const d = document.createElement('p'); d.style.color = 'var(--muted)'; d.textContent = doc.info.description; app.appendChild(d);
              }
              const schemas = doc.components?.schemas || {};
              const paths = doc.paths || {};
              for (const [path, ops] of Object.entries(paths)) {
                for (const [method, op] of Object.entries(ops)) {
                  if (!mc[method]) continue;
                  const det = document.createElement('details'); det.className = 'ep';
                  const reqRef = op.requestBody?.content?.['application/json']?.schema?.$ref;
                  const reqName = schemaName(reqRef);
                  let bodyExample = '';
                  if (reqName && schemas[reqName]) {
                    bodyExample = '<div class="lbl">Request body (' + esc(reqName) + ')</div><pre><code>' +
                                  esc(JSON.stringify(sample(schemas[reqName], schemas), null, 2)) + '</code></pre>';
                  }
                  const curlBody = reqName ? " \\\n  -H \"Content-Type: application/json\" \\\n  -d '" + JSON.stringify(sample(schemas[reqName], schemas)) + "'" : '';
                  const curl = 'curl -X ' + method.toUpperCase() + ' http://127.0.0.1:' +
                    (location.port || '9100') + esc(path) + curlBody;
                  det.innerHTML =
                    '<summary><span class="m ' + method + '">' + method.toUpperCase() + '</span>' +
                    '<span class="path">' + esc(path) + '</span>' +
                    '<span class="sum">' + esc(op.summary || '') + '</span></summary>' +
                    '<div class="body">' +
                      (op.description ? '<p>' + esc(op.description) + '</p>' : '') +
                      bodyExample +
                      '<div class="lbl">Ejemplo (cURL)</div><pre><code>' + esc(curl) + '</code></pre>' +
                    '</div>';
                  app.appendChild(det);
                }
              }
            }).catch(e => {
              document.getElementById('app').innerHTML =
                '<div class="err"><b>No se pudo cargar /openapi.json.</b><br>' + esc(e.message) +
                '<br><br>El documento crudo está en <a href="/openapi.json">/openapi.json</a>.</div>';
            });
            // Genera un objeto de ejemplo a partir de un schema (soporta $ref, properties, allOf, example).
            function sample(schema, all, depth){
              depth = depth || 0; if (!schema || depth > 6) return {};
              if (schema.$ref) return sample(all[schemaName(schema.$ref)], all, depth+1);
              if (schema.allOf) return Object.assign({}, ...schema.allOf.map(s => sample(s, all, depth+1)));
              if (schema.type === 'object' || schema.properties) {
                const o = {};
                for (const [k, p] of Object.entries(schema.properties || {})) o[k] = leaf(p, all, depth);
                return o;
              }
              return leaf(schema, all, depth);
            }
            function leaf(p, all, depth){
              if (p.$ref) return sample(all[schemaName(p.$ref)], all, depth+1);
              if ('example' in p) return p.example;
              if (p.type === 'array') return [ leaf(p.items || {}, all, depth+1) ];
              if (p.type === 'object' || p.properties) return sample(p, all, depth+1);
              if (p.type === 'integer' || p.type === 'number') return 0;
              if (p.type === 'boolean') return false;
              return '';
            }
          </script>
        </body>
        </html>
        """;

    private static object Path(string summary, string description, string responseSchema) => new
    {
        get = Operation(summary, description, null, responseSchema),
    };

    private static object Operation(string summary, string description, string? requestSchema, string responseSchema)
    {
        var operation = new Dictionary<string, object?>
        {
            ["summary"] = summary,
            ["description"] = description,
            ["responses"] = new Dictionary<string, object>
            {
                ["200"] = Response("Success", responseSchema),
                ["400"] = Response("Bad request", "ApiError"),
                ["404"] = Response("Not found", "ApiError"),
                ["500"] = Response("Server error", "ApiError"),
            },
        };

        if (requestSchema != null)
        {
            operation["requestBody"] = new
            {
                required = true,
                content = new Dictionary<string, object>
                {
                    ["application/json"] = new
                    {
                        schema = Ref(requestSchema),
                    },
                },
            };
        }

        return operation;
    }

    private static object Response(string description, string schema) => new
    {
        description,
        content = new Dictionary<string, object>
        {
            ["application/json"] = new
            {
                schema = Ref(schema),
            },
        },
    };

    private static object Ref(string schema) => new Dictionary<string, string>
    {
        ["$ref"] = $"#/components/schemas/{schema}",
    };

    private static object Schemas() => new Dictionary<string, object>
    {
        ["Object"] = new { type = "object", additionalProperties = true },
        ["String"] = new { type = "string" },
        ["ApiError"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["success"] = new { type = "boolean", example = false },
                ["errorCode"] = new { type = "string", example = "PRINTER_NOT_FOUND" },
                ["message"] = new { type = "string", example = "No se encontró la impresora." },
                ["details"] = new { nullable = true },
            },
        },
        ["PingResponse"] = new { type = "object" },
        ["LegacyPrintersResponse"] = new { type = "object" },
        ["HealthResponse"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["status"] = new { type = "string", example = "Running" },
                ["version"] = new { type = "string", example = "1.0.0" },
                ["port"] = new { type = "integer", example = 9100 },
                ["uptime"] = new { type = "string", example = "00.01:25:10" },
                ["printersCount"] = new { type = "integer", example = 4 },
            },
        },
        ["PrintersResponse"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["printers"] = new { type = "array", items = Ref("PrinterInfo") },
            },
        },
        ["PrinterInfo"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["name"] = new { type = "string" },
                ["displayName"] = new { type = "string" },
                ["isDefault"] = new { type = "boolean" },
                ["isOnline"] = new { type = "boolean" },
                ["status"] = new { type = "string" },
                ["type"] = new { type = "string", example = "EPOS / Thermal" },
                ["supportsPdf"] = new { type = "boolean" },
                ["supportsRaw"] = new { type = "boolean" },
                ["supportsZpl"] = new { type = "boolean" },
                ["supportsFiscal"] = new { type = "boolean" },
                ["paperSizes"] = new { type = "array", items = new { type = "string" } },
                ["source"] = new { type = "string", example = "Windows" },
            },
        },
        ["PrintRequest"] = new
        {
            type = "object",
            required = new[] { "contentType", "content" },
            properties = new Dictionary<string, object>
            {
                ["printerName"] = new { type = "string", nullable = true },
                ["jobName"] = new { type = "string", example = "Ticket #123" },
                ["contentType"] = new { type = "string", example = "epos", description = "epos, raw, text, pdf, zpl, tspl, epl, dpl, fiscal" },
                ["content"] = new { type = "string", description = "Plain text/label commands or base64 for PDF/RAW/EPOS." },
                ["copies"] = new { type = "integer", example = 1 },
                ["options"] = Ref("PrintOptions"),
            },
        },
        ["PrintByPurposeRequest"] = new
        {
            allOf = new object[]
            {
                Ref("PrintRequest"),
                new
                {
                    type = "object",
                    required = new[] { "purpose" },
                    properties = new Dictionary<string, object>
                    {
                        ["purpose"] = new { type = "string", example = "ticket" },
                    },
                },
            },
        },
        ["PrinterTestRequest"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["printerName"] = new { type = "string" },
                ["contentType"] = new { type = "string", example = "epos" },
            },
        },
        ["PrintOptions"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["paperSize"] = new { type = "string", example = "A4" },
                ["orientation"] = new { type = "string", example = "Portrait" },
                ["openCashDrawer"] = new { type = "boolean", example = false },
                ["cutPaper"] = new { type = "boolean", example = true },
            },
        },
        ["PrintResponse"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["success"] = new { type = "boolean", example = true },
                ["jobId"] = new { type = "string" },
                ["printerName"] = new { type = "string" },
            },
        },
        ["AgentSettings"] = new { type = "object" },
        ["SettingsResponse"] = new { type = "object" },
        ["PrinterRoute"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["purpose"] = new { type = "string", example = "ticket" },
                ["printerName"] = new { type = "string", example = "EPSON TM-T20III" },
                ["contentType"] = new { type = "string", example = "epos" },
            },
        },
        ["RoutesRequest"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["routes"] = new { type = "array", items = Ref("PrinterRoute") },
            },
        },
        ["RoutesResponse"] = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["routes"] = new { type = "array", items = Ref("PrinterRoute") },
            },
        },
        ["JobsResponse"] = new { type = "object" },
    };
}
