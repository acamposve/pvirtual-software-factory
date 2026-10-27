# Spec — orchestrator (Fase 6: Azure Orchestrator)

## Problema

Hoy la orquestación vive repartida entre dos workflows de GitHub Actions
(`developer-agent.yml`, `agent-autofix.yml`). Funciona, pero:

- No hay estado persistente: el "estado" del loop vive en comentarios de
  PR, imposible de consultar programáticamente.
- No hay cola: dos issues etiquetados `ai-ready` a la vez corren en
  paralelo sin control de concurrencia ni prioridad.
- No hay política de reintento central: está hardcodeada en YAML.
- No hay base para scheduled tasks, multi-agente (Spec/Review/Testing
  Agents) ni cost control (Fases 7+).

## Objetivo

Un servicio orquestador (Fase 6 de `docs/vision.md`) que controle el
ciclo de vida de los agentes: recibir evento → determinar tarea → lanzar
ejecución → monitorear → recolectar resultado → retry/escalar/terminar.

## Alcance — Iteración 1 (este PR)

### Incluido

1. **Servicio `backend/Orchestrator/`** (ASP.NET Core, stack default del
   repo):
   - `POST /webhooks/github`: recibe webhooks de GitHub (issue labeled
     `ai-ready`, check_suite completed), valida firma HMAC-SHA256.
   - Persistencia en PostgreSQL: tabla `agent_jobs` (id, tipo, estado,
     payload, intentos, timestamps).
   - Dispatcher: para ejecutar el agente, invoca los workflows existentes
     vía GitHub API (`workflow_dispatch` / re-run) — NO reimplementa la
     ejecución del agente.
   - Máquina de estados: `Pending → Dispatched → Running → Succeeded |
     Failed → Retrying (max 3) → Escalated`.
   - `GET /jobs` y `GET /jobs/{id}` para observabilidad.
   - Logging estructurado de cada transición de estado.
2. **Proyecto de tests** `backend/Orchestrator.Tests/` (xUnit, mismo
   patrón que Api.Tests).
3. **Schema de PostgreSQL** con migraciones versionadas.

### Fuera de alcance (iteraciones siguientes)

- Deploy a Azure (Container Apps + Postgres flexible server) — iteración 2.
- Ejecución de agentes en sandboxes propias (Container Apps Jobs) — el
  dispatch sigue siendo vía GitHub Actions hasta que eso migre.
- Scheduled tasks, knowledge base, otros agentes (Fases 7+).
- Autenticación de usuarios del servicio (solo webhook HMAC + endpoints
  de lectura).

## Comportamiento

- Llegan 2 issues etiquetados `ai-ready` → se crean 2 jobs `Pending` →
  el dispatcher los despacha respetando el límite de concurrencia
  configurable (default: 1 job de implementación a la vez).
- Un job despachado cuyo run de Actions termina en failure → estado
  `Retrying` → re-dispatch, hasta 3 → `Escalated` (comentario en el issue).
- Webhook con firma inválida → 401, sin efecto en el estado.
- Reinicio del servicio → los jobs `Dispatched/Running` se reconcilian
  consultando el estado real del run en GitHub API (no se pierde trabajo).

## Reglas

- El orquestador NUNCA pushea código ni mergea: solo dispara workflows
  existentes y registra estado (los agentes sí lo hacen, dentro de sus
  límites).
- Máximo 3 reintentos por job (Constitution).
- Secretos (GitHub token, webhook secret) solo vía configuración segura
  (user-secrets en dev, Key Vault cuando haya deploy — Article III).
- Toda feature lleva tests automatizados (Article IV).

## Casos de error

- GitHub API caída al despachar → job queda `Pending`, reintento con
  backoff exponencial.
- Webhook duplicado (redelivery) → idempotencia por `delivery-id` de
  GitHub: el segundo se ignora.
- Job huérfano (run cancelado) → reconciliación lo marca `Failed` y
  aplica política de reintento.

## Criterios de aceptación

Ver `acceptance.md`.
