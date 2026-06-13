# Publicar e instalar `Imprelia.Server`

El paquete se publica en **GitHub Packages** bajo el usuario `RoberthDudiver`
y queda asociado al repo público [`imprelia-print-agent`](https://github.com/RoberthDudiver/imprelia-print-agent)
(vía `RepositoryUrl` en el `.csproj`).

---

## Publicar (mantenedor)

### Opción A — Automático (recomendado)

1. Subí la versión en `Imprelia.Server.csproj` (`<Version>`).
2. Creá y empujá un tag:
   ```bash
   git tag server-v1.0.2
   git push origin server-v1.0.2
   ```
3. El workflow [`publish-nuget.yml`](../.github/workflows/publish-nuget.yml)
   packea y publica solo usando el `GITHUB_TOKEN` del repo (no necesitás PAT).

### Opción B — Manual desde tu PC

```bash
# 1. Packear
dotnet pack Imprelia.Server -c Release -o nupkgs

# 2. Agregar el feed de GitHub Packages (una vez). Usuario = tu usuario de GitHub,
#    password = PAT classic con write:packages.
dotnet nuget add source "https://nuget.pkg.github.com/RoberthDudiver/index.json" \
  --name github-imprelia \
  --username RoberthDudiver \
  --password TU_PAT_CON_write:packages \
  --store-password-in-clear-text

# 3. Publicar
dotnet nuget push "nupkgs/Imprelia.Server.1.0.2.nupkg" \
  --source github-imprelia --skip-duplicate
```

> Las versiones en GitHub Packages son **inmutables**: para republicar hay que
> subir el número de versión.

---

## Instalar (programador que consume el paquete)

GitHub Packages requiere autenticarse incluso para **leer** paquetes. El consumidor
necesita un PAT classic con scope `read:packages`.

1. Crear/editar un `nuget.config` en su solución:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
       <add key="imprelia" value="https://nuget.pkg.github.com/RoberthDudiver/index.json" />
     </packageSources>
     <packageSourceCredentials>
       <imprelia>
         <add key="Username" value="SU_USUARIO_GITHUB" />
         <add key="ClearTextPassword" value="SU_PAT_CON_read:packages" />
       </imprelia>
     </packageSourceCredentials>
   </configuration>
   ```
   > En CI, en vez de hardcodear el PAT, usar variables de entorno o el secret del runner.

2. Instalar:
   ```bash
   dotnet add package Imprelia.Server
   ```

3. Usar (ver el [README](README.md) del paquete):
   ```csharp
   builder.Services.AddImpreliaServer(o => o.AllowedApiKeys.Add("…"));
   app.MapImprelia();
   ```

---

## Pasar a nuget.org (público, futuro)

Cuando quieras que cualquiera lo instale sin PAT:

1. Verificá que el `PackageId` `Imprelia.Server` esté libre en nuget.org.
2. `dotnet nuget push ... --source https://api.nuget.org/v3/index.json --api-key NUGET_ORG_KEY`
3. Quitá el `nuget.config` con credenciales de los consumidores (nuget.org es anónimo para leer).
