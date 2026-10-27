# Spec — cleanup-template-debris

## Problema

El repo todavía arrastra "debris" de la fase de scaffolding (Fase 0):

1. El frontend es la página demo de Vite (contador, hero, logos de
   React/Vite, CSS demo) — no representa el proyecto.
2. El CI no reporta cobertura de tests, y el check de paquetes vulnerables
   es informativo (`continue-on-error: true`) a pesar de que ya hay
   paquetes reales en uso.
3. Quedan en la raíz 5 scripts PowerShell one-off de scaffolding
   (`scaffold-project.ps1`, `scaffold-health-check-tests.ps1`,
   `setup-github-repo.ps1`, `enable-ci-branch-protection.ps1`,
   `fix-openapi-vulnerability.ps1`) que ya cumplieron su propósito y no
   están referenciados por nada.

## Objetivo

Dejar el repositorio limpio: frontend mínimo del proyecto, CI con
cobertura y gates de seguridad bloqueantes, y sin scripts muertos.

## Alcance

### Incluido

- Reemplazar `frontend/src/App.tsx` y `frontend/src/App.css` por una
  página mínima (nombre del proyecto + estado de fase, sin lógica demo).
- Eliminar assets demo: `frontend/src/assets/hero.png`,
  `frontend/src/assets/react.svg`, `frontend/src/assets/vite.svg`,
  `frontend/public/icons.svg`.
- Actualizar `<title>` de `frontend/index.html` a "PVirtual Software Factory".
- Agregar reporte de cobertura al job backend de CI
  (`dotnet test --collect:"XPlat Code Coverage"` + subir el reporte como
  artifact).
- Hacer bloqueante el paso "Check for vulnerable packages" (quitar
  `continue-on-error: true`).
- Eliminar los 5 scripts one-off de la raíz.

### Fuera de alcance

- Diseño real del frontend (branding, routing, componentes de UI) — eso es
  una feature con su propia spec.
- Umbral mínimo de cobertura (coverage gate) — se agrega cuando haya más
  código de producción que testear.
- CodeQL ya existe y no se toca.

## Comportamiento

- `GET /` del frontend muestra una página mínima con el nombre del
  proyecto; no hay contador, logos ni enlaces demo.
- `npm run lint` y `npm run build` pasan sin errores ni warnings de
  imports sin usar.
- CI backend: el job genera un artifact `coverage-report` y falla si hay
  paquetes vulnerables.
- La raíz del repo no contiene scripts `.ps1` de scaffolding.

## Reglas

- No se modifica `docs/CONSTITUTION.md`, `.github/workflows/developer-agent.yml`
  ni `CODEOWNERS` (Article VII — requieren PR propio revisado).
- No se toca el backend (los endpoints `/health` y `/version` quedan igual).

## Casos de error

- Si `dotnet list package --vulnerable` encuentra un paquete vulnerable,
  el job de CI debe fallar (comportamiento esperado, no error de la spec).
- Si algún asset demo sigue referenciado tras el cleanup, el build de
  frontend falla — el PR no puede mergearse hasta corregirlo.

## Criterios de aceptación

Ver `acceptance.md`.
