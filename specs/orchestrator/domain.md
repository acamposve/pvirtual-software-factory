# Domain — orchestrator

## Entidades

### AgentJob (aggregate root)

- `Id`: Guid.
- `Type`: enum — `ImplementSpec` (issue `ai-ready`), `FixCi` (futuro),
  extensible para Fase 7+.
- `State`: enum — `Pending, Dispatched, Running, Succeeded, Failed,
  Retrying, Escalated`.
- `Trigger`: origen (`IssueLabeled`, `CheckSuiteFailed`).
- `Payload`: referencia al trabajo (issue number, spec path, branch, run id).
- `AttemptCount`: int (0..3).
- `CreatedAt / DispatchedAt / CompletedAt`: timestamps UTC.
- `DeliveryId`: id de delivery de GitHub (idempotencia).

### Invariantes

- `AttemptCount ≤ 3` — al excederlo, el único estado destino es `Escalated`.
- Un job solo avanza hacia adelante en la máquina de estados; los únicos
  retrocesos válidos son `Failed → Retrying → Dispatched`.
- `DeliveryId` es único: un mismo webhook nunca crea dos jobs.
- No existen jobs sin `Trigger` registrado (auditoría).

## Eventos de dominio

| Evento | Cuándo | Efecto |
|---|---|---|
| `JobReceived` | webhook válido | Job `Pending` persistido |
| `JobDispatched` | dispatcher toma el job | `workflow_dispatch` a GitHub + estado `Dispatched` |
| `JobRunning` | run iniciado en GitHub | estado `Running` |
| `JobSucceeded` | run verde | estado final `Succeeded` |
| `JobFailed` | run rojo | `AttemptCount++`; si < 3 → `Retrying`, si no → `Escalated` |
| `JobEscalated` | presupuesto agotado | comentario en el issue + estado final |

## Servicios de dominio

- `JobDispatcher`: decide cuándo despachar (concurrencia, prioridad FIFO).
- `RetryPolicy`: max 3, backoff exponencial.
- `RunReconciler`: al reiniciar, consulta GitHub API y sincroniza estados.

## Notas

- No hay value objects complejos todavía: el dominio es una cola de jobs
  con máquina de estados. La riqueza de dominio crece en Fases 7+.
