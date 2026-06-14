# Microsoft Store Release Process / Proceso de release para Microsoft Store

## English

This document is the operational checklist for generating a new Imprelia Print Agent version for Microsoft Store.

Every new release must be recorded in the release history table at the end of this file.

## 1. Choose the version

Microsoft Store MSIX packages must use this format:

```text
major.minor.build.0
```

The fourth number, also called revision, must always be `0`.

Valid examples:

```text
1.1.4.0
1.1.5.0
1.2.0.0
```

Invalid example:

```text
1.1.4.1
```

For the application version in the project file, use the three-part version:

```text
1.1.4
```

For the Store package, use:

```text
1.1.4.0
```

## 2. Update source version files

Update the project version in:

```text
Imprelia.PrintAgent.csproj
```

Check or update the API/health version in:

```text
LocalServer.cs
```

The values should match the release version without the Store revision.

Example for release `1.1.4.0`:

```text
Application/project version: 1.1.4
Store package version: 1.1.4.0
```

## 3. Review local changes

Before building, inspect the working tree:

```powershell
git status --short
```

Also check that no Git conflict markers remain:

```powershell
rg -n "^(<<<<<<< .+|=======$|>>>>>>> .+)" .
```

If this command returns no results, there are no conflict markers.

## 4. Build and test the app

Run the normal Release build:

```powershell
dotnet build .\Imprelia.PrintAgent.sln -c Release
```

If the build fails, fix the application before generating installers.

## 5. Generate the MSI installer

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Release.ps1 -Version 1.1.4
```

Expected output:

```text
installer\bin\Release\ImpreliaPrintAgent-Setup.msi
```

The MSI version uses the three-part version, for example `1.1.4`.

## 6. Generate the MSIX/MSIXUPLOAD package

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-MSIX.ps1 -Version 1.1.4.0 -CreateUploadZip
```

Expected output:

```text
artifacts\msix\out\ImpreliaPrintAgent-1.1.4.0.msix
artifacts\msix\out\ImpreliaPrintAgent-1.1.4.0.msixupload
```

Upload the `.msixupload` file to Microsoft Partner Center.

## 7. Verify the package manifest

After generating the package, verify the manifest:

```powershell
Get-Content .\artifacts\msix\package\AppxManifest.xml -TotalCount 22
```

Confirm these values:

```text
Name="Dudiver.ImpreliaPrintAgent"
Publisher="CN=18FB64AB-5A7B-47F7-AD1B-5E66071B7C0F"
Version="1.1.4.0"
ProcessorArchitecture="x64"
DisplayName: Imprelia Print Agent
PublisherDisplayName: Dudiver
```

The version must end in `.0`.

## 8. Verify generated files

Check the generated Store files:

```powershell
Get-ChildItem .\artifacts\msix\out\ImpreliaPrintAgent-1.1.4.0.* |
  Select-Object Name,Length,LastWriteTime
```

Check the MSI:

```powershell
Get-Item .\installer\bin\Release\ImpreliaPrintAgent-Setup.msi |
  Select-Object FullName,Length,LastWriteTime
```

## 9. Commit and push

Review the final diff:

```powershell
git status --short
git diff --stat
```

Commit the source/documentation changes:

```powershell
git add .
git commit -m "Release Imprelia Print Agent 1.1.4"
git push
```

If the remote has new commits:

```powershell
git pull --rebase
```

Resolve conflicts, build again, regenerate MSI/MSIX if needed, then continue:

```powershell
git rebase --continue
git push
```

## 10. Upload to Microsoft Partner Center

In Partner Center, upload:

```text
artifacts\msix\out\ImpreliaPrintAgent-1.1.4.0.msixupload
```

If Partner Center rejects the package because of the version, check that the fourth number is `0`.

## 11. Add the version to this document

Every version generated for Store must be added to the release history below.

Add:

- Store version
- App/MSI version
- Date
- Output file name
- Notes about important changes
- Whether it was uploaded to Partner Center

## Release History

| Date | Store version | App/MSI version | Store upload file | Notes | Partner Center |
| --- | --- | --- | --- | --- | --- |
| 2026-06-13 | 1.1.4.0 | 1.1.4 | ImpreliaPrintAgent-1.1.4.0.msixupload | Remote Print Bridge release and package regenerated after rebase. | Superseded by 1.1.5.0 |
| 2026-06-13 | 1.1.5.0 | 1.1.5 | ImpreliaPrintAgent-1.1.5.0.msixupload | Remote Print Bridge hub moved to /hubs/imprelia (reverse-proxy WebSocket fix). | Superseded by 1.1.6.0 |
| 2026-06-14 | 1.1.6.0 | 1.1.6 | ImpreliaPrintAgent-1.1.6.0.msixupload | Remote Bridge: re-register agent on automatic reconnect (fixes "worked then stopped printing until toggling the bridge"). | Pending upload |

---

## Espanol

Este documento es el checklist operativo para generar una nueva version de Imprelia Print Agent para Microsoft Store.

Cada nueva version debe registrarse en la tabla de historial al final de este archivo.

## 1. Elegir la version

Los paquetes MSIX de Microsoft Store deben usar este formato:

```text
major.minor.build.0
```

El cuarto numero, llamado revision, siempre debe ser `0`.

Ejemplos validos:

```text
1.1.4.0
1.1.5.0
1.2.0.0
```

Ejemplo invalido:

```text
1.1.4.1
```

Para la version de la aplicacion en el proyecto, usa la version de tres numeros:

```text
1.1.4
```

Para el paquete de Store, usa:

```text
1.1.4.0
```

## 2. Actualizar archivos de version

Actualiza la version del proyecto en:

```text
Imprelia.PrintAgent.csproj
```

Revisa o actualiza la version expuesta por la API/health en:

```text
LocalServer.cs
```

Los valores deben coincidir con la version del release sin la revision de Store.

Ejemplo para el release `1.1.4.0`:

```text
Version de aplicacion/proyecto: 1.1.4
Version del paquete Store: 1.1.4.0
```

## 3. Revisar cambios locales

Antes de compilar, revisa el estado del repo:

```powershell
git status --short
```

Tambien revisa que no queden marcadores de conflicto de Git:

```powershell
rg -n "^(<<<<<<< .+|=======$|>>>>>>> .+)" .
```

Si este comando no devuelve resultados, no hay marcadores de conflicto.

## 4. Compilar y probar la app

Ejecuta el build Release normal:

```powershell
dotnet build .\Imprelia.PrintAgent.sln -c Release
```

Si el build falla, corrige la aplicacion antes de generar instaladores.

## 5. Generar el instalador MSI

Ejecuta:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Release.ps1 -Version 1.1.4
```

Salida esperada:

```text
installer\bin\Release\ImpreliaPrintAgent-Setup.msi
```

El MSI usa la version de tres numeros, por ejemplo `1.1.4`.

## 6. Generar el paquete MSIX/MSIXUPLOAD

Ejecuta:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-MSIX.ps1 -Version 1.1.4.0 -CreateUploadZip
```

Salida esperada:

```text
artifacts\msix\out\ImpreliaPrintAgent-1.1.4.0.msix
artifacts\msix\out\ImpreliaPrintAgent-1.1.4.0.msixupload
```

El archivo que se sube a Microsoft Partner Center es el `.msixupload`.

## 7. Verificar el manifest del paquete

Despues de generar el paquete, revisa el manifest:

```powershell
Get-Content .\artifacts\msix\package\AppxManifest.xml -TotalCount 22
```

Confirma estos valores:

```text
Name="Dudiver.ImpreliaPrintAgent"
Publisher="CN=18FB64AB-5A7B-47F7-AD1B-5E66071B7C0F"
Version="1.1.4.0"
ProcessorArchitecture="x64"
DisplayName: Imprelia Print Agent
PublisherDisplayName: Dudiver
```

La version debe terminar en `.0`.

## 8. Verificar archivos generados

Revisa los archivos de Store:

```powershell
Get-ChildItem .\artifacts\msix\out\ImpreliaPrintAgent-1.1.4.0.* |
  Select-Object Name,Length,LastWriteTime
```

Revisa el MSI:

```powershell
Get-Item .\installer\bin\Release\ImpreliaPrintAgent-Setup.msi |
  Select-Object FullName,Length,LastWriteTime
```

## 9. Commit y push

Revisa el diff final:

```powershell
git status --short
git diff --stat
```

Haz commit de los cambios de codigo/documentacion:

```powershell
git add .
git commit -m "Release Imprelia Print Agent 1.1.4"
git push
```

Si el remoto tiene commits nuevos:

```powershell
git pull --rebase
```

Resuelve conflictos, vuelve a compilar, regenera MSI/MSIX si fue necesario y continua:

```powershell
git rebase --continue
git push
```

## 10. Subir a Microsoft Partner Center

En Partner Center, sube:

```text
artifacts\msix\out\ImpreliaPrintAgent-1.1.4.0.msixupload
```

Si Partner Center rechaza el paquete por la version, verifica que el cuarto numero sea `0`.

## 11. Agregar la version a este documento

Cada version generada para Store debe agregarse al historial de releases.

Agrega:

- Version Store
- Version App/MSI
- Fecha
- Nombre del archivo generado
- Notas de cambios importantes
- Estado en Partner Center

## Historial de releases

| Fecha | Version Store | Version App/MSI | Archivo Store upload | Notas | Partner Center |
| --- | --- | --- | --- | --- | --- |
| 2026-06-13 | 1.1.4.0 | 1.1.4 | ImpreliaPrintAgent-1.1.4.0.msixupload | Release con Remote Print Bridge y paquete regenerado despues del rebase. | Reemplazada por 1.1.5.0 |
| 2026-06-13 | 1.1.5.0 | 1.1.5 | ImpreliaPrintAgent-1.1.5.0.msixupload | Hub de Remote Print Bridge movido a /hubs/imprelia para WebSocket detras de reverse proxy. | Reemplazada por 1.1.6.0 |
| 2026-06-14 | 1.1.6.0 | 1.1.6 | ImpreliaPrintAgent-1.1.6.0.msixupload | Remote Bridge: re-registra el agente al reconectar automaticamente. | Pendiente de subir |
