# Microsoft Store Submission

## English

Microsoft Store supports more than one path for desktop Win32 apps:

- **MSIX**: recommended when you want the full Store packaging experience.
- **MSI/EXE listing**: accepted for existing Win32 apps, but you must host a versioned secure download URL and comply with Store package requirements.

For Imprelia Print Agent v1, this repository can generate a **WiX MSI installer** and a local **MSIX/MSIXUPLOAD package**.

### Build MSI

```powershell
dotnet build .\installer\Imprelia.PrintAgent.Installer.wixproj -c Release -p:ProductVersion=1.0.0
```

Expected output:

```text
installer\bin\Release\ImpreliaPrintAgent-Setup.msi
```

### Build MSIX / MSIXUPLOAD

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-MSIX.ps1 -Version 1.0.0.0 -CreateUploadZip
```

Expected output:

```text
artifacts\msix\out\ImpreliaPrintAgent-1.0.0.0.msix
artifacts\msix\out\ImpreliaPrintAgent-1.0.0.0.msixupload
```

Before final Store submission, make sure the MSIX identity matches the app identity reserved in Partner Center:

```powershell
.\scripts\Build-MSIX.ps1 `
  -Version 1.0.0.0 `
  -PackageName "YourPartnerCenter.PackageName" `
  -Publisher "CN=Your Partner Center Publisher" `
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

### Generar MSI

```powershell
dotnet build .\installer\Imprelia.PrintAgent.Installer.wixproj -c Release -p:ProductVersion=1.0.0
```

Salida esperada:

```text
installer\bin\Release\ImpreliaPrintAgent-Setup.msi
```

### Generar MSIX / MSIXUPLOAD

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-MSIX.ps1 -Version 1.0.0.0 -CreateUploadZip
```

Salida esperada:

```text
artifacts\msix\out\ImpreliaPrintAgent-1.0.0.0.msix
artifacts\msix\out\ImpreliaPrintAgent-1.0.0.0.msixupload
```

Antes del envío final a Store, asegúrate de que la identidad MSIX coincida con la app reservada en Partner Center:

```powershell
.\scripts\Build-MSIX.ps1 `
  -Version 1.0.0.0 `
  -PackageName "TuPartnerCenter.PackageName" `
  -Publisher "CN=Tu Publisher de Partner Center" `
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
