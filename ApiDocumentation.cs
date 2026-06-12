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
                ["/docs"] = Path("Scalar API guide.", "Interactive API documentation rendered with Scalar.", "String"),
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

    public static string ScalarHtml() =>
        """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>Imprelia Print Agent API</title>
          <style>
            body { margin: 0; }
          </style>
        </head>
        <body>
          <script
            id="api-reference"
            data-url="/openapi.json"
            data-theme="default"
            data-layout="modern">
          </script>
          <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
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
