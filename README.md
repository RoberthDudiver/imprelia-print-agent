# Imprelia Print Agent

![Imprelia Print Agent logo](images/Sin%20título-1@0,5x.png)

**Imprelia Print Agent** is a Windows local printing agent created by **Roberth Dudiver** ([dudiver.net](https://dudiver.net)).

It exposes a local HTTP API so web applications can print to printers installed on the same Windows machine: ESC/POS thermal printers, raw printers, Zebra/ZPL label printers, PDF-capable printers, Windows printers, and future fiscal printer integrations.

The agent is designed to preserve backward compatibility with the original EPOS/RAW workflow while adding a more general API for multi-printer environments.

## Español

**Imprelia Print Agent** es un agente local de impresión para Windows creado por **Roberth Dudiver** ([dudiver.net](https://dudiver.net)).

Expone una API HTTP local para que aplicaciones web puedan imprimir en impresoras instaladas en la misma máquina Windows: impresoras térmicas ESC/POS, impresoras RAW, Zebra/ZPL, impresoras compatibles con PDF, impresoras Windows genéricas e integraciones fiscales futuras.

El agente mantiene compatibilidad con el flujo EPOS/RAW original y agrega una API más general para entornos con varias impresoras.

---

## Features

- Local Windows tray application.
- HTTP API bound to `127.0.0.1` by default.
- Backward-compatible legacy endpoints:
  - `GET /ping`
  - `GET /printers`
  - `POST /print`
- New API endpoints under `/api`.
- Printer discovery from Windows installed printers.
- Universal print endpoint with explicit `printerName`.
- Print by purpose/routes: `ticket`, `kitchen_order`, `report`, `label`, `fiscal`, or custom purposes.
- Test printing by printer/content type.
- Recent in-memory print job history.
- Configurable port, CORS, origins, default printer, and routes.
- Optional API key support.
- Local API guide powered by Scalar:
  - `GET /docs`
  - `GET /openapi.json`
- WinForms settings window editable with Visual Studio Designer.

## Características

- Aplicación local de Windows con icono en bandeja.
- API HTTP local escuchando en `127.0.0.1` por defecto.
- Endpoints legacy compatibles:
  - `GET /ping`
  - `GET /printers`
  - `POST /print`
- Nuevos endpoints bajo `/api`.
- Detección de impresoras instaladas en Windows.
- Endpoint universal de impresión con `printerName` explícito.
- Impresión por propósito/rutas: `ticket`, `kitchen_order`, `report`, `label`, `fiscal` o propósitos personalizados.
- Impresión de prueba por impresora/tipo de contenido.
- Historial reciente de trabajos en memoria.
- Puerto, CORS, orígenes, impresora predeterminada y rutas configurables.
- API key opcional.
- Guía local de API con Scalar:
  - `GET /docs`
  - `GET /openapi.json`
- Ventana WinForms editable con el diseñador visual de Visual Studio.

---

## Requirements

- Windows.
- .NET 8 SDK to build from source.
- Installed Windows printers for real printing.
- A PDF reader associated with `.pdf` files if you want to use the current Windows PDF print mechanism.

## Requisitos

- Windows.
- .NET 8 SDK para compilar desde código fuente.
- Impresoras instaladas en Windows para imprimir realmente.
- Un lector PDF asociado a archivos `.pdf` si quieres usar el mecanismo actual de impresión PDF de Windows.

---

## Build

```powershell
dotnet build .\GastroManager.PrintAgent.sln
```

The app binary is generated under:

```text
bin\Debug\net8.0-windows\win-x64\GastroPrintAgent.exe
```

For release:

```powershell
dotnet publish .\Imprelia.PrintAgent.csproj -c Release -r win-x64 --self-contained false
```

## Compilación

```powershell
dotnet build .\GastroManager.PrintAgent.sln
```

El ejecutable queda en:

```text
bin\Debug\net8.0-windows\win-x64\GastroPrintAgent.exe
```

Para publicar:

```powershell
dotnet publish .\Imprelia.PrintAgent.csproj -c Release -r win-x64 --self-contained false
```

---

## Running

Open the generated executable. The agent runs in the Windows tray.

Default local URL:

```text
http://localhost:9100
```

Open the API guide:

```text
http://localhost:9100/docs
```

The settings window also includes an **Abrir guia API** button.

## Ejecución

Abre el ejecutable generado. El agente queda corriendo en la bandeja de Windows.

URL local por defecto:

```text
http://localhost:9100
```

Guía de API:

```text
http://localhost:9100/docs
```

La ventana de configuración también incluye un botón **Abrir guia API**.

---

## API Overview

### Health

```http
GET /api/health
```

Example response:

```json
{
  "status": "Running",
  "version": "1.0.0",
  "port": 9100,
  "uptime": "00.01:25:10",
  "printersCount": 4
}
```

### List printers

```http
GET /api/printers
```

Example response:

```json
{
  "printers": [
    {
      "name": "EPSON TM-T20III",
      "displayName": "EPSON TM-T20III",
      "isDefault": true,
      "isOnline": true,
      "status": "Ready",
      "type": "EPOS / Thermal",
      "supportsPdf": false,
      "supportsRaw": true,
      "supportsZpl": false,
      "supportsFiscal": false,
      "paperSizes": [],
      "source": "Windows"
    }
  ]
}
```

### Universal print

```http
POST /api/print
Content-Type: application/json
```

EPOS/ESC-POS example:

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

ZPL example:

```json
{
  "printerName": "ZDesigner ZD420",
  "jobName": "Product label",
  "contentType": "zpl",
  "content": "^XA^FO50,50^ADN,36,20^FDHello^FS^XZ",
  "copies": 1
}
```

PDF example:

```json
{
  "printerName": "HP LaserJet Pro",
  "jobName": "Sales report",
  "contentType": "pdf",
  "content": "base64-pdf",
  "copies": 1,
  "options": {
    "paperSize": "A4",
    "orientation": "Portrait"
  }
}
```

### Print by purpose

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

The agent resolves `purpose` using configured printer routes. If `printerName` is sent explicitly in the request, it takes priority.

### Printer test

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

Example:

```json
{
  "routes": [
    {
      "purpose": "ticket",
      "printerName": "EPSON TM-T20III",
      "contentType": "epos"
    },
    {
      "purpose": "report",
      "printerName": "HP LaserJet Pro",
      "contentType": "pdf"
    }
  ]
}
```

### Settings

```http
GET /api/settings
PUT /api/settings
```

The port is configurable. If the port changes, restart the agent to bind the new listener.

Allowed origins are configurable. If the list is empty, the agent allows any origin for backward compatibility.

### Recent jobs

```http
GET /api/jobs/recent
```

---

## Legacy API Compatibility

Existing integrations can continue using:

```http
GET /ping
GET /printers
POST /print
```

Legacy print body:

```json
{
  "printer": "EPSON TM-T20III",
  "dataBase64": "base64-raw-bytes"
}
```

No existing legacy contract was intentionally removed.

## Compatibilidad Legacy

Las integraciones existentes pueden seguir usando:

```http
GET /ping
GET /printers
POST /print
```

Body legacy:

```json
{
  "printer": "EPSON TM-T20III",
  "dataBase64": "base64-raw-bytes"
}
```

No se eliminó intencionalmente ningún contrato legacy.

---

## Security

- The agent binds to `127.0.0.1` by default.
- Do not expose it to the network unless you understand the risk.
- CORS is configurable.
- Allowed origins are configurable.
- API key is optional and disabled by default for backward compatibility.
- If API key is enabled, clients must send:

```http
X-Api-Key: your-key
```

## Seguridad

- El agente escucha en `127.0.0.1` por defecto.
- No lo expongas en red si no entiendes el riesgo.
- CORS es configurable.
- Los orígenes permitidos son configurables.
- La API key es opcional y está desactivada por defecto para compatibilidad.
- Si activas API key, los clientes deben enviar:

```http
X-Api-Key: tu-clave
```

---

## Architecture

Main pieces:

- `LocalServer`: local HTTP server.
- `WindowsPrinterDiscoveryService`: discovers installed Windows printers.
- `PrintService`: central print dispatcher.
- `IPrintAdapter`: print adapter contract.
- `RawPrintAdapter`: EPOS/RAW/ZPL path through Windows spooler RAW mode.
- `TextPrintAdapter`: Windows text printing.
- `PdfPrintAdapter`: PDF handoff to Windows associated PDF application.
- `FiscalPrintAdapter`: placeholder with clear error until fiscal drivers are configured.
- `PrinterRouteService`: purpose-to-printer routes.
- `JobHistoryService`: recent job memory.

## Arquitectura

Piezas principales:

- `LocalServer`: servidor HTTP local.
- `WindowsPrinterDiscoveryService`: detecta impresoras instaladas en Windows.
- `PrintService`: despachador central de impresión.
- `IPrintAdapter`: contrato común de adaptadores.
- `RawPrintAdapter`: EPOS/RAW/ZPL vía spooler Windows en modo RAW.
- `TextPrintAdapter`: impresión de texto con Windows.
- `PdfPrintAdapter`: envío PDF a la aplicación PDF asociada en Windows.
- `FiscalPrintAdapter`: placeholder con error claro hasta configurar drivers fiscales.
- `PrinterRouteService`: rutas propósito -> impresora.
- `JobHistoryService`: historial reciente en memoria.

---

## Editing the UI in Visual Studio

The settings window uses the classic WinForms designer structure:

- `SettingsForm.cs`: logic and events.
- `SettingsForm.Designer.cs`: visual controls and layout.
- `SettingsForm.resx`: WinForms resources.

Open `SettingsForm.cs` in Visual Studio and choose **View Designer** to move controls visually.

## Editar la UI en Visual Studio

La ventana usa la estructura clásica de diseñador WinForms:

- `SettingsForm.cs`: lógica y eventos.
- `SettingsForm.Designer.cs`: controles y layout visual.
- `SettingsForm.resx`: recursos WinForms.

Abre `SettingsForm.cs` en Visual Studio y elige **View Designer** para mover controles visualmente.

---

## License

This project is distributed under the **Imprelia Print Agent Non-Commercial Source License**.

Summary:

- You may use it.
- You may modify the code.
- Improvements must be submitted back as Pull Requests.
- You may not sell it.
- You may not use it commercially without permission.
- Logos and visual assets may be used to identify this project and compatible builds, but not for unauthorized commercial branding or resale.

See [LICENSE.md](LICENSE.md).

## Licencia

Este proyecto se distribuye bajo la **Imprelia Print Agent Non-Commercial Source License**.

Resumen:

- Puedes usarlo.
- Puedes modificar el código.
- Las mejoras deben enviarse como Pull Requests.
- No puedes venderlo.
- No puedes usarlo comercialmente sin permiso.
- Los logos y recursos visuales pueden usarse para identificar este proyecto y builds compatibles, pero no para marca comercial no autorizada ni reventa.

Ver [LICENSE.md](LICENSE.md).

---

## Author

Created by **Roberth Dudiver**  
Website: [dudiver.net](https://dudiver.net)

## Autor

Creado por **Roberth Dudiver**  
Sitio web: [dudiver.net](https://dudiver.net)

---

## Contributing

Pull Requests are welcome for bug fixes, printer adapters, better Windows integration, UI improvements, documentation, and test coverage.

By contributing, you agree that your contribution will be distributed under this repository license.

## Contribuir

Se aceptan Pull Requests para correcciones, adaptadores de impresoras, mejor integración con Windows, mejoras de UI, documentación y pruebas.

Al contribuir, aceptas que tu aporte se distribuya bajo la licencia de este repositorio.
