# Installation and Build

## English

### Requirements

- Windows 10/11 or Windows Server with desktop printing support.
- .NET 8 SDK.
- At least one printer installed in Windows for real printing tests.
- Optional: Visual Studio 2022 or newer for WinForms Designer editing.

### Clone

```powershell
git clone https://github.com/RoberthDudiver/imprelia-print-agent.git
cd imprelia-print-agent
```

### Build

```powershell
dotnet build .\GastroManager.PrintAgent.sln
```

### Run

```powershell
dotnet run --project .\Imprelia.PrintAgent.csproj
```

The agent starts in the Windows tray. Double-click the tray icon to open settings.

### Publish

Framework-dependent build:

```powershell
dotnet publish .\Imprelia.PrintAgent.csproj -c Release -r win-x64 --self-contained false
```

Self-contained build:

```powershell
dotnet publish .\Imprelia.PrintAgent.csproj -c Release -r win-x64 --self-contained true
```

### Default URLs

```text
http://localhost:9100/docs
http://localhost:9100/openapi.json
http://localhost:9100/api/health
```

### Configuration Storage

Settings are stored under:

```text
%APPDATA%\GastroPrintAgent\config.json
```

Port changes require restarting the agent.

---

## Español

### Requisitos

- Windows 10/11 o Windows Server con soporte de impresión de escritorio.
- .NET 8 SDK.
- Al menos una impresora instalada en Windows para pruebas reales.
- Opcional: Visual Studio 2022 o superior para editar la UI con el diseñador WinForms.

### Clonar

```powershell
git clone https://github.com/RoberthDudiver/imprelia-print-agent.git
cd imprelia-print-agent
```

### Compilar

```powershell
dotnet build .\GastroManager.PrintAgent.sln
```

### Ejecutar

```powershell
dotnet run --project .\Imprelia.PrintAgent.csproj
```

El agente inicia en la bandeja de Windows. Haz doble click en el icono para abrir la configuración.

### Publicar

Build dependiente de framework:

```powershell
dotnet publish .\Imprelia.PrintAgent.csproj -c Release -r win-x64 --self-contained false
```

Build autocontenido:

```powershell
dotnet publish .\Imprelia.PrintAgent.csproj -c Release -r win-x64 --self-contained true
```

### URLs por defecto

```text
http://localhost:9100/docs
http://localhost:9100/openapi.json
http://localhost:9100/api/health
```

### Configuración

La configuración se guarda en:

```text
%APPDATA%\GastroPrintAgent\config.json
```

Los cambios de puerto requieren reiniciar el agente.
