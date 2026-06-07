# Contributing

## English

Thanks for considering a contribution to Imprelia Print Agent.

### Before Opening a Pull Request

- Keep backward compatibility with legacy endpoints.
- Keep the default listener local-only unless there is an explicit configuration change.
- Add clear error messages for printer/spooler failures.
- Update documentation when changing API behavior.
- Build the project before submitting:

```powershell
dotnet build .\GastroManager.PrintAgent.sln
```

### Pull Request Checklist

- Describe the problem and the solution.
- Mention affected printer/content types.
- Include manual test notes.
- Include screenshots for UI changes.
- Confirm whether API behavior changed.

### License Agreement

By submitting a contribution, you agree that it may be distributed under the repository license.

---

## Español

Gracias por considerar contribuir a Imprelia Print Agent.

### Antes de abrir un Pull Request

- Mantén compatibilidad con los endpoints legacy.
- Mantén el listener local por defecto salvo configuración explícita.
- Agrega mensajes claros para errores de impresora/spooler.
- Actualiza documentación si cambia el comportamiento de la API.
- Compila el proyecto antes de enviar:

```powershell
dotnet build .\GastroManager.PrintAgent.sln
```

### Checklist del Pull Request

- Describe el problema y la solución.
- Menciona tipos de impresora/contenido afectados.
- Incluye notas de prueba manual.
- Incluye capturas si cambias la UI.
- Confirma si cambió la API.

### Acuerdo de Licencia

Al enviar una contribución, aceptas que se distribuya bajo la licencia del repositorio.
