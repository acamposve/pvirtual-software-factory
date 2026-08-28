# Tasks — version-endpoint

- [ ] Agregar `<Version>0.1.0</Version>` a `backend/Api/Api.csproj`.
- [ ] Agregar `app.MapGet("/version", ...)` en `backend/Api/Program.cs` que
      devuelva 200 OK con `{ version, environment }`, leyendo la versión
      vía `AssemblyInformationalVersionAttribute` y el ambiente vía
      `app.Environment.EnvironmentName`.
- [ ] Agregar tests de integración en `backend/Api.Tests/` (mismo patrón
      que `HealthEndpointTests.cs`): 200 OK, body deserializa, `version` no
      vacío, `environment` no vacío.
- [ ] Abrir el PR desde una branch `feature/<issue#>-version-endpoint` (ver
      convención en `AGENTS.md`).

## Fuera de alcance / follow-ups

- Commit SHA / build number en la respuesta (requiere inyección de
  metadata en CI).
