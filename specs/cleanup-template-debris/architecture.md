# Architecture — cleanup-template-debris

## Componentes afectados

| Componente | Cambio |
|---|---|
| `frontend/src/App.tsx` | Reescrito: página mínima sin estado ni assets demo |
| `frontend/src/App.css` | Reescrito: estilos mínimos |
| `frontend/src/assets/` | Se eliminan `hero.png`, `react.svg`, `vite.svg` |
| `frontend/public/icons.svg` | Se elimina (solo lo usaba la página demo) |
| `frontend/index.html` | `<title>` actualizado |
| `.github/workflows/ci.yml` | Cobertura + gate de paquetes vulnerables |
| Raíz del repo | Se eliminan 5 scripts `.ps1` one-off |

## Dependencias

- No se agregan ni actualizan paquetes NuGet ni npm.
- La cobertura usa `coverlet.collector`, ya referenciado en
  `backend/Api.Tests/Api.Tests.csproj` — no hay nueva dependencia.

## CI (cambios en `.github/workflows/ci.yml`)

1. Paso "Test" del job backend:
   ```yaml
   run: dotnet test PVirtualSoftwareFactory.slnx --no-build --configuration Release --collect:"XPlat Code Coverage" --results-directory ./coverage
   ```
2. Nuevo paso "Upload coverage report" (`actions/upload-artifact@v4`) con
   `if: always()` para que el artifact exista también en fallo.
3. Paso "Check for vulnerable packages": se elimina
   `continue-on-error: true` → pasa a ser bloqueante.

## Seguridad

- El gate de paquetes vulnerables pasa a bloqueante (Article III).
- No se introducen secretos ni permisos nuevos en los workflows.

## Decisiones

- **No se agrega coverage gate con umbral** todavía: con 2 endpoints y
  tests de integración el número es poco significativo; se define umbral
  cuando haya lógica de dominio real.
- **Los scripts se eliminan y no se archivan**: su historial queda en git
  y `scripts/README.md` documenta la automatización vigente.
