# Security Policy

## English

Imprelia Print Agent is a local Windows printing bridge. Treat it as local infrastructure.

### Supported Versions

Only the latest version on `main` is currently maintained.

### Reporting a Vulnerability

Please report security issues privately to Roberth Dudiver through:

- Website: https://dudiver.net

Do not open public issues for sensitive vulnerabilities.

### Security Defaults

- The agent binds to `127.0.0.1` by default.
- API key is optional and disabled by default for backward compatibility.
- CORS is configurable.
- Empty allowed origins means permissive CORS for compatibility.
- Network exposure should be enabled only by users who understand the risk.

---

## Español

Imprelia Print Agent es un puente local de impresión para Windows. Debe tratarse como infraestructura local.

### Versiones soportadas

Actualmente solo se mantiene la última versión en `main`.

### Reportar vulnerabilidades

Reporta problemas de seguridad de forma privada a Roberth Dudiver:

- Sitio web: https://dudiver.net

No abras issues públicos para vulnerabilidades sensibles.

### Defaults de seguridad

- El agente escucha en `127.0.0.1` por defecto.
- API key opcional y desactivada por defecto por compatibilidad.
- CORS configurable.
- Orígenes permitidos vacíos implica CORS permisivo por compatibilidad.
- La exposición en red debe activarse solo si el usuario entiende el riesgo.
