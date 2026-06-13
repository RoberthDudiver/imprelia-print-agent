# Ejemplo: backend ASP.NET Core con Imprelia.Server

Backend mínimo que recibe la conexión de un Imprelia Print Agent y le envía
trabajos de impresión remotos.

## Ejecutar

```bash
dotnet run
```

Arranca en `http://localhost:5080` (o el puerto que asigne).

## Conectar un agente

1. Abrí el Imprelia Print Agent → pestaña **Remote Bridge**.
2. Habilitar, y configurar:
   - **Server URL**: `http://localhost:5080` (el origen de este backend, sin `/api`)
   - **Agent ID**: `kitchen-01` (cualquier identificador)
   - **API Key**: `demo-api-key` (la de `AllowedApiKeys`)
3. Guardar → debe quedar **Conectado**.

## Probar

```bash
# ¿Qué agentes están conectados?
curl http://localhost:5080/agents

# Mandar un ticket de prueba al agente "kitchen-01"
curl -X POST "http://localhost:5080/print/test?agentId=kitchen-01"

# Ver el estado del job (usá el jobId que devolvió el anterior)
curl http://localhost:5080/print/<jobId>
```

El agente resuelve la impresora según su ruta `ticket` (configurable en la
pestaña **Rutas** del agente) e imprime los bytes ESC/POS.

## Cómo se referencia el paquete

En este repo el ejemplo usa un `ProjectReference` a `../../Imprelia.Server` para
compilar sin feed. En **tu** proyecto lo instalás como NuGet:

```bash
dotnet add package Imprelia.Server
```

Desde GitHub Packages — ver [PUBLISHING.md](../../Imprelia.Server/PUBLISHING.md).
