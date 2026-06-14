# Microsoft Store Submission

## English

Microsoft Store supports more than one path for desktop Win32 apps:

- **MSIX**: recommended when you want the full Store packaging experience.
- **MSI/EXE listing**: accepted for existing Win32 apps, but you must host a versioned secure download URL and comply with Store package requirements.

For Imprelia Print Agent v1, this repository can generate a **WiX MSI installer** and a local **MSIX/MSIXUPLOAD package**.

For the step-by-step release checklist, version rules, verification commands, and release history, see [STORE_RELEASE_PROCESS.md](STORE_RELEASE_PROCESS.md).

### Partner Center Identity

Use the identity assigned by Microsoft Partner Center:

```text
Package/Identity/Name: Dudiver.ImpreliaPrintAgent
Package/Identity/Publisher: CN=18FB64AB-5A7B-47F7-AD1B-5E66071B7C0F
Package/Properties/PublisherDisplayName: Dudiver
Package Family Name: Dudiver.ImpreliaPrintAgent_8yv65d0br4jdr
Store ID: 9NCJR51W2DTP
Store URL: https://apps.microsoft.com/detail/9NCJR51W2DTP
Store protocol link: ms-windows-store://pdp/?productid=9NCJR51W2DTP
MSA app ID: 6cba8127-096b-432a-8e82-87278f33a9e4
```

### Build MSI

```powershell
dotnet build .\installer\Imprelia.PrintAgent.Installer.wixproj -c Release -p:ProductVersion=1.0.0
```

Expected output:

```text
installer\bin\Release\ImpreliaPrintAgent-Setup.msi
```

### Build MSIX / MSIXUPLOAD

Microsoft Store requires MSIX package versions to use `major.minor.build.0`. The fourth number, revision, must be `0`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-MSIX.ps1 -Version 1.0.0.0 -CreateUploadZip
```

Expected output:

```text
artifacts\msix\out\ImpreliaPrintAgent-1.0.0.0.msix
artifacts\msix\out\ImpreliaPrintAgent-1.0.0.0.msixupload
```

The default script values already match the Partner Center identity above. If you need to override them explicitly:

```powershell
.\scripts\Build-MSIX.ps1 `
  -Version 1.0.0.0 `
  -PackageName "Dudiver.ImpreliaPrintAgent" `
  -Publisher "CN=18FB64AB-5A7B-47F7-AD1B-5E66071B7C0F" `
  -PublisherDisplayName "Dudiver" `
  -CreateUploadZip
```

### Important Store Requirements

Before submitting an MSI/EXE or MSIX app to Partner Center:

- The installer and every PE file must be digitally signed with a trusted code-signing certificate.
- Partner Center requires a versioned secure URL pointing to the MSI/EXE package.
- The package must pass Microsoft Store certification.
- Updates for MSI/EXE apps are handled by your installer/update mechanism, not Store package versioning in the same way as MSIX.

### Recommended v1 Release Flow

1. Reserve the app in Partner Center.
2. Copy the exact package identity and publisher from Partner Center.
3. Build MSIX/MSIXUPLOAD with those identity values.
4. Sign the package if required by your submission flow.
5. Upload `ImpreliaPrintAgent-1.0.0.0.msixupload` or the accepted package format in Partner Center.
6. Complete Store listing metadata, screenshots, privacy/security notes, and support URL.

### Code Signing

You need a code-signing certificate trusted by Windows/Microsoft. Example signing command:

```powershell
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\file.exe
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\ImpreliaPrintAgent-Setup.msi
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\ImpreliaPrintAgent-1.0.0.0.msix
```

Do not publish to the Store unsigned.

---

## Español

Microsoft Store soporta más de un camino para apps Win32 de escritorio:

- **MSIX**: recomendado si quieres toda la experiencia moderna de empaquetado Store.
- **Listado MSI/EXE**: aceptado para apps Win32 existentes, pero debes hospedar una URL segura versionada y cumplir requisitos de Store.

Para Imprelia Print Agent v1, este repositorio puede generar un **instalador MSI con WiX** y un paquete local **MSIX/MSIXUPLOAD**.

Para el checklist paso a paso de release, reglas de versionado, comandos de verificacion e historial de versiones, consulta [STORE_RELEASE_PROCESS.md](STORE_RELEASE_PROCESS.md).

### Identidad de Partner Center

Usa la identidad asignada por Microsoft Partner Center:

```text
Package/Identity/Name: Dudiver.ImpreliaPrintAgent
Package/Identity/Publisher: CN=18FB64AB-5A7B-47F7-AD1B-5E66071B7C0F
Package/Properties/PublisherDisplayName: Dudiver
Package Family Name: Dudiver.ImpreliaPrintAgent_8yv65d0br4jdr
Store ID: 9NCJR51W2DTP
Store URL: https://apps.microsoft.com/detail/9NCJR51W2DTP
Store protocol link: ms-windows-store://pdp/?productid=9NCJR51W2DTP
MSA app ID: 6cba8127-096b-432a-8e82-87278f33a9e4
```

### Generar MSI

```powershell
dotnet build .\installer\Imprelia.PrintAgent.Installer.wixproj -c Release -p:ProductVersion=1.0.0
```

Salida esperada:

```text
installer\bin\Release\ImpreliaPrintAgent-Setup.msi
```

### Generar MSIX / MSIXUPLOAD

Microsoft Store requiere que las versiones MSIX usen `major.minor.build.0`. El cuarto número, revisión, debe ser `0`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-MSIX.ps1 -Version 1.0.0.0 -CreateUploadZip
```

Salida esperada:

```text
artifacts\msix\out\ImpreliaPrintAgent-1.0.0.0.msix
artifacts\msix\out\ImpreliaPrintAgent-1.0.0.0.msixupload
```

Los valores por defecto del script ya coinciden con la identidad de Partner Center anterior. Si necesitas pasarlos explícitamente:

```powershell
.\scripts\Build-MSIX.ps1 `
  -Version 1.0.0.0 `
  -PackageName "Dudiver.ImpreliaPrintAgent" `
  -Publisher "CN=18FB64AB-5A7B-47F7-AD1B-5E66071B7C0F" `
  -PublisherDisplayName "Dudiver" `
  -CreateUploadZip
```

### Requisitos importantes para Store

Antes de enviar MSI/EXE o MSIX a Partner Center:

- El instalador y todos los archivos PE deben estar firmados con certificado de firma de código confiable.
- Partner Center requiere una URL segura y versionada apuntando al MSI/EXE.
- El paquete debe pasar certificación de Microsoft Store.
- Las actualizaciones de MSI/EXE dependen de tu instalador/mecanismo de updates, no del versionado Store como MSIX.

### Flujo recomendado v1

1. Reservar la app en Partner Center.
2. Copiar la identidad y publisher exactos desde Partner Center.
3. Generar MSIX/MSIXUPLOAD con esos valores.
4. Firmar el paquete si tu flujo de publicación lo requiere.
5. Subir `ImpreliaPrintAgent-1.0.0.0.msixupload` o el formato aceptado en Partner Center.
6. Completar metadata de Store, screenshots, privacidad/seguridad y URL de soporte.

### Firma de código

Necesitas un certificado de firma de código confiable por Windows/Microsoft. Ejemplo:

```powershell
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\file.exe
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\ImpreliaPrintAgent-Setup.msi
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\ImpreliaPrintAgent-1.0.0.0.msix
```

No publiques en Store sin firmar.
