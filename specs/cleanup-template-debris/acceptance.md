# Acceptance Criteria — cleanup-template-debris

## Escenario: frontend sin debris

- **Given** el repositorio en la branch del PR
- **When** se inspecciona `frontend/src/` y `frontend/public/`
- **Then** no existen `hero.png`, `react.svg`, `vite.svg` ni `icons.svg`
- **And** `App.tsx` no contiene contador, logos ni enlaces a vite.dev /
  react.dev
- **And** `index.html` tiene `<title>PVirtual Software Factory</title>`

## Escenario: build y lint de frontend

- **Given** el frontend limpio
- **When** se corre `npm run lint` y `npm run build` en `frontend/`
- **Then** ambos terminan sin errores

## Escenario: cobertura en CI

- **Given** el workflow de CI actualizado
- **When** corre el job backend
- **Then** el paso Test usa `--collect:"XPlat Code Coverage"`
- **And** el reporte se sube como artifact `coverage-report`

## Escenario: gate de paquetes vulnerables

- **Given** el workflow de CI actualizado
- **When** `dotnet list package --vulnerable --include-transitive`
  encuentra al menos un paquete vulnerable
- **Then** el job backend falla (el paso ya no tiene
  `continue-on-error: true`)

## Escenario: scripts eliminados

- **Given** el repositorio en la branch del PR
- **When** se lista la raíz
- **Then** no existen `scaffold-project.ps1`,
  `scaffold-health-check-tests.ps1`, `setup-github-repo.ps1`,
  `enable-ci-branch-protection.ps1` ni `fix-openapi-vulnerability.ps1`

## Escenario: backend intacto

- **Given** el PR completo
- **When** se corre `dotnet test`
- **Then** todos los tests de `Api.Tests` pasan sin cambios
