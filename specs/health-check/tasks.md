# Tasks — health-check

- [ ] Agregar `app.MapGet("/health", ...)` en `backend/Api/Program.cs` que
      devuelva 200 OK con `{ status: "healthy", timestampUtc: <UTC ISO8601> }`.
- [ ] Confirmar que el endpoint no requiere autenticación (hoy el proyecto no
      tiene auth configurada, así que por default ya es público — solo
      verificar que se mantenga así a futuro).
- [ ] Crear el primer proyecto de tests del repo: `backend/Api.Tests`
      (xUnit + `Microsoft.AspNetCore.Mvc.Testing`), agregarlo a
      `PVirtualSoftwareFactory.slnx`.
- [ ] Test de integración: `GET /health` devuelve 200, el body deserializa
      correctamente, `status == "healthy"`.
- [ ] Una vez que exista este test, sacar el `continue-on-error: true` del
      step "Test" en `.github/workflows/ci.yml` (hasta ahora no bloqueaba
      porque no había ningún test que correr).
- [ ] Abrir el PR desde una branch `feature/<issue#>-health-check` (ver
      convención en `AGENTS.md`).

## Fuera de alcance / follow-ups

- Limpiar el endpoint de ejemplo `/weatherforecast` que trae el template
  (no es parte de esta spec — se puede sacar en un PR aparte).
- Readiness check con PostgreSQL, cuando exista integración con la base de
  datos.
