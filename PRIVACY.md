# Privacy Policy / Política de Privacidad

Effective date / Fecha de vigencia: June 7, 2026

Product / Producto: Imprelia Print Agent  
Developer / Desarrollador: Roberth Dudiver  
Website / Sitio web: https://dudiver.net  
Repository / Repositorio: https://github.com/RoberthDudiver/imprelia-print-agent

---

## English

### 1. Overview

Imprelia Print Agent is a local Windows printing agent. It runs on the user's computer and exposes a local HTTP API so web applications can send print jobs to printers installed on that same Windows device.

The application is designed to operate locally. It does not require a cloud account, does not include advertising, and does not sell personal data.

### 2. Data We Collect

Imprelia Print Agent does not intentionally collect, transmit, sell, rent, or share personal data with the developer or third parties.

The agent may process the following information locally on the user's device only:

- Installed printer names and basic printer capabilities.
- Print job metadata such as job name, printer name, content type, status, timestamp, and error messages.
- Local configuration such as selected printer, print routes, HTTP port, CORS settings, allowed origins, and optional API key.
- Print content sent by an authorized local web application to the agent for printing.

### 3. Where Data Is Stored

Configuration is stored locally on the user's Windows profile, under:

```text
%APPDATA%\GastroPrintAgent\config.json
```

Recent print job history is currently kept in memory while the agent is running. It is not uploaded to the developer.

### 4. Network Communication

By default, the agent listens on:

```text
http://127.0.0.1:9100
http://localhost:9100
```

This means it is intended to be accessed only from the same computer.

The agent does not contact external servers for analytics, advertising, tracking, telemetry, or profiling.

The local API documentation page may load Scalar API Reference assets from a public CDN when the user opens:

```text
http://localhost:9100/docs
```

This is used only to render local API documentation in the browser. It is not required for printing.

### 5. Print Content

Print content is supplied by the web application or local client calling the agent. The agent forwards that content to the selected Windows printer or print adapter.

Users and integrating applications are responsible for ensuring they only send content they are authorized to print.

### 6. Third-Party Services

The application does not integrate with third-party analytics, advertising, crash reporting, or tracking services.

Windows printing, printer drivers, PDF readers, or device-specific drivers may process print jobs according to their own behavior and privacy terms.

### 7. Security

The agent is local-first and binds to localhost by default. Users may configure CORS settings and an optional API key.

If the user changes the bind address or exposes the agent on a local network, they are responsible for securing that environment.

### 8. Children's Privacy

Imprelia Print Agent is a utility for local printing. It is not directed to children and does not knowingly collect personal data from children.

### 9. Changes to This Policy

This policy may be updated when the application changes. Updates will be published in the repository or on the developer's website.

### 10. Contact

For privacy questions, contact:

Roberth Dudiver  
https://dudiver.net

---

## Español

### 1. Resumen

Imprelia Print Agent es un agente local de impresión para Windows. Se ejecuta en la computadora del usuario y expone una API HTTP local para que aplicaciones web puedan enviar trabajos de impresión a impresoras instaladas en ese mismo equipo Windows.

La aplicación está diseñada para funcionar localmente. No requiere cuenta en la nube, no incluye publicidad y no vende datos personales.

### 2. Datos que Recopilamos

Imprelia Print Agent no recopila, transmite, vende, alquila ni comparte intencionalmente datos personales con el desarrollador ni con terceros.

El agente puede procesar localmente en el equipo del usuario la siguiente información:

- Nombres de impresoras instaladas y capacidades básicas.
- Metadatos de trabajos de impresión como nombre del trabajo, nombre de impresora, tipo de contenido, estado, fecha/hora y mensajes de error.
- Configuración local como impresora seleccionada, rutas de impresión, puerto HTTP, CORS, orígenes permitidos y API key opcional.
- Contenido de impresión enviado por una aplicación web local/autorizada al agente.

### 3. Dónde se Guardan los Datos

La configuración se guarda localmente en el perfil Windows del usuario, en:

```text
%APPDATA%\GastroPrintAgent\config.json
```

El historial reciente de trabajos se mantiene actualmente en memoria mientras el agente está en ejecución. No se sube al desarrollador.

### 4. Comunicación de Red

Por defecto, el agente escucha en:

```text
http://127.0.0.1:9100
http://localhost:9100
```

Esto significa que está pensado para ser accedido solo desde la misma computadora.

El agente no contacta servidores externos para analítica, publicidad, tracking, telemetría o perfilado.

La página local de documentación de API puede cargar recursos de Scalar API Reference desde un CDN público cuando el usuario abre:

```text
http://localhost:9100/docs
```

Esto se usa únicamente para mostrar la documentación local de la API en el navegador. No es necesario para imprimir.

### 5. Contenido de Impresión

El contenido de impresión es enviado por la aplicación web o cliente local que llama al agente. El agente reenvía ese contenido a la impresora Windows o adaptador seleccionado.

Los usuarios y aplicaciones integradoras son responsables de enviar únicamente contenido que estén autorizados a imprimir.

### 6. Servicios de Terceros

La aplicación no integra servicios de analítica, publicidad, reportes de errores ni tracking de terceros.

Windows, los drivers de impresora, lectores PDF o controladores específicos pueden procesar trabajos de impresión según su propio comportamiento y términos de privacidad.

### 7. Seguridad

El agente está diseñado con enfoque local y escucha en localhost por defecto. Los usuarios pueden configurar CORS y una API key opcional.

Si el usuario cambia la dirección de escucha o expone el agente en red local, es responsable de proteger ese entorno.

### 8. Privacidad de Menores

Imprelia Print Agent es una utilidad de impresión local. No está dirigida a menores y no recopila conscientemente datos personales de menores.

### 9. Cambios a esta Política

Esta política puede actualizarse cuando cambie la aplicación. Las actualizaciones se publicarán en el repositorio o en el sitio web del desarrollador.

### 10. Contacto

Para consultas de privacidad, contactar a:

Roberth Dudiver  
https://dudiver.net
