# Architecture — orchestrator

## Componentes

```text
GitHub (webhooks: issues.labeled, check_suite.completed)
    ↓ HMAC-SHA256
POST /webhooks/github  (backend/Orchestrator)
    ↓
PostgreSQL (agent_jobs)  ←→  JobDispatcher (BackgroundService)
    ↓ workflow_dispatch via GitHub API
.github/workflows/developer-agent.yml / agent-autofix.yml (existentes)
```

| Componente | Proyecto | Nota |
|---|---|---|
| API + webhook receiver | `backend/Orchestrator/` | ASP.NET Core minimal API |
| Dispatcher + reconciler | `BackgroundService`s en el mismo host | polling con backoff |
| Persistencia | PostgreSQL + migraciones SQL versionadas | tabla `agent_jobs` + `processed_deliveries` |
| Tests | `backend/Orchestrator.Tests/` | xUnit + Testcontainers o Testcontainers-like (decidir en implementación) |

## APIs

| Endpoint | Método | Propósito |
|---|---|---|
| `/webhooks/github` | POST | ingest de webhooks (HMAC obligatorio) |
| `/jobs` | GET | lista de jobs con filtros por estado |
| `/jobs/{id}` | GET | detalle de un job |
| `/health` | GET | liveness (mismo patrón que Api) |

## Persistencia (iteración 1)

```sql
agent_jobs (
  id uuid PK,
  type text NOT NULL,
  state text NOT NULL,
  trigger_type text NOT NULL,
  payload jsonb NOT NULL,
  attempt_count int NOT NULL DEFAULT 0,
  delivery_id text NOT NULL UNIQUE,   -- idempotencia
  github_run_id bigint NULL,
  created_at timestamptz NOT NULL,
  dispatched_at timestamptz NULL,
  completed_at timestamptz NULL
)
```

## Decisiones

1. **Dispatch vía GitHub Actions API (no ejecución propia)**: reusa la
   infraestructura probada (runner, secretos, claude-code-action). El
   salto a Container Apps Jobs es la iteración 2, con la misma interfaz
   `IJobExecutor`.
2. **PostgreSQL**: stack default del repo; la cola es una tabla con
   `FOR UPDATE SKIP LOCKED` — sin colas externas (no hay necesidad de
   Service Bus en iteración 1).
3. **Idempotencia por `delivery_id`**: GitHub reintenta webhooks; la
   tabla `processed_deliveries` evita doble procesamiento.
4. **Concurrencia configurable** (`Orchestrator:MaxConcurrentJobs`,
   default 1): evita saturar minutos de Actions y tokens del agente.
5. **Reconciliación al arranque**: los jobs `Dispatched/Running` se
   consultan contra GitHub API — el servicio es stateless-friendly.
6. **Nuevo proyecto en la solución**: `backend/Orchestrator/` +
   `backend/Orchestrator.Tests/`, agregados a `PVirtualSoftwareFactory.slnx`.

## Seguridad

- Webhook: validación HMAC-SHA256 obligatoria (secret en user-secrets /
  Key Vault). Sin firma válida → 401.
- GitHub token con scope mínimo (repo: actions:write, issues:write).
- Endpoints de lectura (`/jobs`) sin datos sensibles en el payload
  expuesto (solo ids, estados, timestamps).

## Observabilidad

- Log estructurado (JSON) por transición de estado con `JobId`.
- `/jobs` como fuente de verdad consultable.
- Métricas básicas (contador de jobs por estado) — iteración 2 con
  OpenTelemetry cuando haya deploy.
