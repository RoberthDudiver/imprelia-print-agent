# Impresión remota (modo cliente / IPP)

Permite que **cualquier app de Windows** en una máquina (cliente) imprima a una
impresora física conectada a **otra máquina** (principal), pasando por el hub.
Como una impresora de red, pero a través del hub — útil para mandar comandas de
Rappi / pedidos desde una PC a la impresora de la cocina en otra PC.

No requiere drivers de fabricante: usa el driver de clase **IPP** incorporado en
Windows. El documento viaja como **PDF** y el principal lo imprime con el driver
real de la impresora.

```
[App cualquiera] --Ctrl+P--> [Impresora "Cocina (Remota)"]   (driver IPP inbox)
                                    | HTTP application/ipp (PDF)
                                    v
        [Agente CLIENTE: servidor IPP 127.0.0.1:9110 captura el PDF]
                                    | POST {hub}/imprelia/jobs  {agentId, printer, type:pdf}
                                    v
                                 [HUB] --SignalR--> [Agente PRINCIPAL]
                                                     imprime el PDF en "Cocina"
                                                            v
                                                        🖨 impresora real
```

## Roles

| Máquina | Rol | Configuración |
|---|---|---|
| Donde está la impresora | **Principal** | Remote Bridge habilitado, registrado en el hub con su `AgentId`. Ya imprime PDF por ruta o por impresora explícita. |
| Desde donde se imprime | **Cliente** | Modo cliente habilitado, con impresoras virtuales que apuntan al `AgentId` del principal. |
| Servidor | **Hub** | El servidor GastroManager con `ImpreliaServer`. Debe tener `ExposeHttpJobApi = true`. |

## Requisito del hub

El cliente envía los trabajos vía `POST /imprelia/jobs`. Ese endpoint solo existe
si el hub se configuró con:

```csharp
services.AddImpreliaServer(o =>
{
    o.ExposeHttpJobApi = true;          // requerido para el modo cliente
    o.AllowedApiKeys.Add("tu-api-key"); // opcional pero recomendado
});
```

## Configurar el PRINCIPAL (máquina con la impresora)

1. Abrí el agente → **Remote Bridge**.
2. Habilitá, poné la URL del hub, un `AgentId` (ej. `cocina-pc`) y la API key.
3. Guardá. El estado debe quedar **Conectado**.
4. En **Impresoras**, asegurate de que la impresora real exista (ej. `Cocina`).

## Configurar el CLIENTE (máquina desde donde imprimís)

1. Abrí el agente → **Impresión remota**.
2. Habilitá **modo cliente** (puerto IPP por defecto: 9110). Guardá.
3. La URL y API key del hub se toman de **Remote Bridge** (configurá esa sección
   con la misma URL del hub; no hace falta habilitar el bridge en el cliente).
4. **+ Nueva** impresora virtual:
   - **Nombre local**: como la verás en Windows (ej. `Cocina (Remota)`).
   - **Agente destino**: el `AgentId` del principal (ej. `cocina-pc`).
   - **Impresora en el principal**: el nombre exacto (ej. `Cocina`),
     **o** una **Ruta** configurada en el principal (ej. `kitchen_order`).
5. Seleccioná la impresora y **Instalar en Windows** (pide permisos de admin / UAC).
6. Listo: en cualquier app, **Archivo → Imprimir → "Cocina (Remota)"**.

## Qué imprime y qué no

- ✅ Cualquier app, cualquier documento (reportes, comandas, recibos) como contenido visual.
- ✅ Impresoras normales, láser, y térmicas con driver Windows.
- ⚠️ **No** transporta comandos crudos dinámicos (abrir cajón según pago, ZPL de
  precisión). El corte/cajón estáticos configurados en el driver del principal sí
  funcionan. Para control crudo dinámico, usá la API directa del agente.

## Solución de problemas

- **"El hub rechazó el job (404)"** → el hub no tiene `ExposeHttpJobApi = true`.
- **"documento recibido no es PDF"** → Windows mandó otro formato; verificá que la
  impresora se haya agregado con el **driver de clase IPP** (se crea con
  *Instalar en Windows*, que usa `Add-Printer -DeviceURL`).
- **No imprime nada en el principal** → revisá que el `AgentId` destino coincida y
  que el principal esté **Conectado** al hub. Mirá **Logs** en ambas máquinas.
- **El alta de la impresora falla** → se necesita aceptar el UAC (admin).

## Notas de distribución

- El servidor IPP es loopback (127.0.0.1) en C# user-mode: **compatible con
  Microsoft Store**. No instala drivers ni port monitors.
- El alta de la impresora (`Add-Printer`) necesita elevación una vez; en la versión
  Store puede requerir que el usuario confirme el UAC del helper.
