# Microsoft Store Submission

## English

Microsoft Store supports more than one path for desktop Win32 apps:

- **MSIX**: recommended when you want the full Store packaging experience.
- **MSI/EXE listing**: accepted for existing Win32 apps, but you must host a versioned secure download URL and comply with Store package requirements.

For Imprelia Print Agent v1, this repository generates a **WiX MSI installer**:

```powershell
dotnet build .\installer\Imprelia.PrintAgent.Installer.wixproj -c Release -p:ProductVersion=1.0.0
```

Expected output:

```text
installer\bin\Release\ImpreliaPrintAgent-Setup.msi
```

### Important Store Requirements for MSI/EXE

Before submitting an MSI/EXE app to Partner Center:

- The installer and every PE file must be digitally signed with a trusted code-signing certificate.
- Partner Center requires a versioned secure URL pointing to the MSI/EXE package.
- The package must pass Microsoft Store certification.
- Updates for MSI/EXE apps are handled by your installer/update mechanism, not Store package versioning in the same way as MSIX.

### Recommended v1 Release Flow

1. Build the MSI.
2. Sign all binaries and the MSI.
3. Create GitHub Release `v1.0.0`.
4. Attach `ImpreliaPrintAgent-Setup.msi`.
5. Use the release asset URL or your CDN URL in Partner Center.
6. Complete Store listing metadata, screenshots, privacy/security notes, and support URL.

### Code Signing

You need a code-signing certificate trusted by Windows/Microsoft. Example signing command:

```powershell
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\file.exe
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\ImpreliaPrintAgent-Setup.msi
```

Do not publish to the Store unsigned.

---

## Español

Microsoft Store soporta más de un camino para apps Win32 de escritorio:

- **MSIX**: recomendado si quieres toda la experiencia moderna de empaquetado Store.
- **Listado MSI/EXE**: aceptado para apps Win32 existentes, pero debes hospedar una URL segura versionada y cumplir requisitos de Store.

Para Imprelia Print Agent v1, este repositorio genera un **instalador MSI con WiX**:

```powershell
dotnet build .\installer\Imprelia.PrintAgent.Installer.wixproj -c Release -p:ProductVersion=1.0.0
```

Salida esperada:

```text
installer\bin\Release\ImpreliaPrintAgent-Setup.msi
```

### Requisitos importantes para MSI/EXE en Store

Antes de enviar un MSI/EXE a Partner Center:

- El instalador y todos los archivos PE deben estar firmados con certificado de firma de código confiable.
- Partner Center requiere una URL segura y versionada apuntando al MSI/EXE.
- El paquete debe pasar certificación de Microsoft Store.
- Las actualizaciones de MSI/EXE dependen de tu instalador/mecanismo de updates, no del versionado Store como MSIX.

### Flujo recomendado v1

1. Compilar el MSI.
2. Firmar binarios y MSI.
3. Crear GitHub Release `v1.0.0`.
4. Adjuntar `ImpreliaPrintAgent-Setup.msi`.
5. Usar la URL del release asset o CDN en Partner Center.
6. Completar metadata de Store, screenshots, privacidad/seguridad y URL de soporte.

### Firma de código

Necesitas un certificado de firma de código confiable por Windows/Microsoft. Ejemplo:

```powershell
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\file.exe
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a path\to\ImpreliaPrintAgent-Setup.msi
```

No publiques en Store sin firmar.
