# API Reference

The complete interactive documentation is available from the running agent:

```text
http://localhost:9100/docs
```

The OpenAPI document is available at:

```text
http://localhost:9100/openapi.json
```

## English

### Legacy Compatibility

Existing integrations may keep using:

```http
GET /ping
GET /printers
POST /print
```

Legacy print request:

```json
{
  "printer": "EPSON TM-T20III",
  "dataBase64": "base64-raw-bytes"
}
```

### Health

```http
GET /api/health
```

```json
{
  "status": "Running",
  "version": "1.0.0",
  "port": 9100,
  "uptime": "00.01:25:10",
  "printersCount": 4
}
```

### Printers

```http
GET /api/printers
```

### Universal Print

```http
POST /api/print
Content-Type: application/json
```

```json
{
  "printerName": "EPSON TM-T20III",
  "jobName": "Ticket #1023",
  "contentType": "epos",
  "content": "base64-or-plain-content",
  "copies": 1,
  "options": {
    "cutPaper": true,
    "openCashDrawer": true
  }
}
```

Supported content types:

- `epos`
- `raw`
- `text`
- `pdf`
- `zpl`
- `tspl`
- `epl`
- `dpl`
- `fiscal`

### Print by Purpose

```http
POST /api/print/by-purpose
Content-Type: application/json
```

```json
{
  "purpose": "ticket",
  "jobName": "Ticket #123",
  "contentType": "epos",
  "content": "base64-or-plain-content",
  "copies": 1
}
```

If `printerName` is provided in the request, it takes priority over the route printer.

### Printer Test

```http
POST /api/printers/test
Content-Type: application/json
```

```json
{
  "printerName": "EPSON TM-T20III",
  "contentType": "epos"
}
```

### Routes

```http
GET /api/settings/printer-routes
PUT /api/settings/printer-routes
```

```json
{
  "routes": [
    {
      "purpose": "ticket",
      "printerName": "EPSON TM-T20III",
      "contentType": "epos"
    },
    {
      "purpose": "label",
      "printerName": "ZDesigner ZD420",
      "contentType": "zpl"
    }
  ]
}
```

### Settings

```http
GET /api/settings
PUT /api/settings
```

Important notes:

- The default host is `127.0.0.1`.
- The default port is `9100`.
- Port changes require restart.
- Empty `allowedOrigins` means permissive CORS for backward compatibility.
- API key is optional and disabled by default.

### Recent Jobs

```http
GET /api/jobs/recent
```

### Error Shape

```json
{
  "success": false,
  "errorCode": "PRINTER_NOT_FOUND",
  "message": "No se encontró la impresora 'HP LaserJet Pro'.",
  "details": null
}
```

---

## Español

### Compatibilidad Legacy

Las integraciones existentes pueden seguir usando:

```http
GET /ping
GET /printers
POST /print
```

Request legacy:

```json
{
  "printer": "EPSON TM-T20III",
  "dataBase64": "bytes-raw-en-base64"
}
```

### Salud

```http
GET /api/health
```

### Impresoras

```http
GET /api/printers
```

### Impresión Universal

```http
POST /api/print
Content-Type: application/json
```

Tipos soportados:

- `epos`
- `raw`
- `text`
- `pdf`
- `zpl`
- `tspl`
- `epl`
- `dpl`
- `fiscal`

### Impresión por Propósito

```http
POST /api/print/by-purpose
Content-Type: application/json
```

Si el request trae `printerName`, tiene prioridad sobre la impresora configurada en la ruta.

### Rutas

```http
GET /api/settings/printer-routes
PUT /api/settings/printer-routes
```

### Configuración

```http
GET /api/settings
PUT /api/settings
```

Notas importantes:

- El host por defecto es `127.0.0.1`.
- El puerto por defecto es `9100`.
- Cambiar el puerto requiere reiniciar.
- `allowedOrigins` vacío permite CORS amplio por compatibilidad.
- La API key es opcional y está desactivada por defecto.
