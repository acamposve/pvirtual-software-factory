# Domain — agent-ci-autofix

## Entidades

### FixAttempt (concepto, no código)

- `number`: int (1..3) — número de intento de fix.
- `prNumber`: int — PR donde se registra.
- `trigger`: "ci-failure".
- `outcome`: "pushed-fix" | "escalated" | "failed-to-launch".

## Eventos de dominio

| Evento | Disparador | Efecto |
|---|---|---|
| `CiFailedOnAgentPr` | CI rojo en PR de branch `feature/*` o `chore/*` creado por el agente | Se agenda intento de fix |
| `FixAttemptStarted` | presupuesto de intentos disponible | Comentario marcador + run del Developer Agent |
| `FixBudgetExhausted` | 3er intento fallido | Comentario de escalada humana, fin del loop |

## Invariantes

- `FixAttempt.number ∈ [1, 3]` — nunca más de 3 intentos por PR.
- Cada intento tiene exactamente un comentario marcador en el PR.
- El loop solo actúa sobre PRs creados por el Developer Agent.
- Ningún intento pushea a `main` ni mergea.

## Aggregates

Ninguno persistido: el estado del loop vive en los comentarios del PR
(marcadores `<!-- autofix-attempt:N -->`), que actúan como event store
humano-legible. No hay base de datos.
