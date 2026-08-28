# CLAUDE.md

Contexto de proyecto que se carga automáticamente en cada ejecución de un
agente en este repo. Esto es un resumen operativo — la fuente de verdad
completa es `docs/CONSTITUTION.md` y `AGENTS.md`; leelos igual si tenés
cualquier duda antes de implementar algo.

## Qué es este repo

AI Software Factory: agentes autónomos toman trabajo de GitHub (Issues con
label `ai-ready`), implementan sobre una spec ya aprobada en
`specs/<feature>/`, corren tests, y abren un PR para revisión humana. Ver
`docs/vision.md` para el plan completo.

## Reglas no negociables (resumen — texto completo en docs/CONSTITUTION.md)

- No implementar nada sin `specs/<feature>/spec.md` aprobado.
- No cambiar la arquitectura aprobada ni introducir dependencias/patrones
  nuevos sin autorización humana.
- No tocar secretos, ni `.github/workflows/*`, `docs/CONSTITUTION.md` o
  `CODEOWNERS`.
- Toda feature lleva tests automatizados; nunca borrar o debilitar tests
  para que pase CI.
- No abrir PR si el build o los tests fallan.
- Nunca push directo a `main`, nunca mergear tu propio PR.
- Ante ambigüedad de arquitectura, dominio, seguridad o negocio: parar y
  dejar un comentario explicando el bloqueo, no asumir.

## Stack y comandos

```bash
# Backend (.NET 10) - solución PVirtualSoftwareFactory.slnx en la raíz
dotnet restore
dotnet build
dotnet test
dotnet run --project backend/Api/Api.csproj

# Frontend (React + Vite + TS)
cd frontend && npm ci && npm run build && npm run lint
```

## Estructura relevante

- `backend/Api/` — Web API (.NET Minimal API, sin controllers).
- `backend/Api.Tests/` — xUnit + `WebApplicationFactory`. Es carpeta
  *sibling* de `backend/Api/` — nunca anidar un proyecto .NET dentro del
  directorio de otro proyecto .NET (rompe el build por glob recursivo del
  SDK-style csproj).
- `specs/<feature>/` — `spec.md`, `domain.md`, `architecture.md`,
  `tasks.md`, `acceptance.md`.
- `docs/agents/developer-agent.md` — instrucciones específicas del
  Developer Agent (qué leer, qué podés decidir, cuándo escalar, flujo
  esperado). Si sos el Developer Agent, ese archivo es tu contrato.
