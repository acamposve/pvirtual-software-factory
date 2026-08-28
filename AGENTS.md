# AGENTS.md

Este archivo es contexto obligatorio para cualquier agente (humano-asistido o
autónomo) que trabaje en este repositorio. Léelo junto con
[`docs/CONSTITUTION.md`](docs/CONSTITUTION.md) antes de implementar nada.

## Reglas no negociables

Ver [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md). En resumen:

- No implementar sin spec aprobada (`specs/<feature>/spec.md`).
- No romper la arquitectura aprobada sin autorización humana.
- No commitear secretos.
- Toda feature lleva tests automatizados.
- No crear un PR con build o tests en rojo.
- Nunca push directo a `main`.
- Ante ambigüedad de arquitectura, dominio, seguridad o negocio: escalar a un humano.
- Máximo 3 reintentos autónomos antes de escalar.

## Estructura del repositorio

```text
/
├── README.md
├── AGENTS.md              (este archivo)
├── CONTRIBUTING.md
├── docs/
│   ├── CONSTITUTION.md    (reglas obligatorias)
│   ├── vision.md          (plan y arquitectura completa)
│   └── architecture/      (ADRs y decisiones de arquitectura)
├── specs/                 (una carpeta por feature: spec.md, domain.md, architecture.md, tasks.md, acceptance.md)
├── scripts/                (scripts de automatización)
└── .github/
    ├── CODEOWNERS
    ├── PULL_REQUEST_TEMPLATE.md
    └── workflows/          (CI/CD — GitHub Actions)
```

## Stack por defecto

- Backend: **C#** (.NET)
- Frontend: **React**
- Base de datos: **PostgreSQL**

Otro lenguaje/stack puede usarse cuando la tarea lo justifique explícitamente
en su spec (`architecture.md`), pero el default de este repo es el anterior.

## Convención de branches

```text
feature/<issue#>-<slug>
fix/<issue#>-<slug>
chore/<issue#>-<slug>
docs/<issue#>-<slug>
```

Ejemplo: `feature/12-developer-agent-sandbox`

## Convención de commits

[Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`,
`chore:`, `docs:`, `refactor:`, `test:`, `ci:`. Versionado: SemVer.

## Flujo de trabajo

```text
spec aprobada → branch → implementación → tests → PR → CI verde → review humana → merge
```

## Comandos de build/test

```bash
# Backend (.NET) - solución PVirtualSoftwareFactory.slnx en la raíz
dotnet build
dotnet test        # no hay tests todavía (Fase 3/4)
dotnet run --project backend/Api.csproj

# Frontend (React + Vite + TS)
cd frontend
npm install         # primera vez / tras cambios en package.json
npm run dev
npm run build
npm run lint
```

Cada spec que lo requiera puede documentar comandos adicionales o distintos
en su propia carpeta (`specs/<feature>/tasks.md`) si difieren del stack por
defecto.
