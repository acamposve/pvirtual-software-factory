# Spec — agent-ci-autofix

## Problema

Cuando el Developer Agent abre un PR y el CI falla, el loop se detiene:
alguien humano tiene que mirar el fallo, relanzar el agente (o arreglarlo
a mano) y volver a esperar. El MVP de `vision.md` (sección 22) exige que
el agente "automáticamente arregle errores simples" — hoy eso no existe.

Además, la ejecución del agente es una caja negra: el único rastro es el
log crudo del run de Actions; no hay resumen del resultado en un lugar
visible.

## Objetivo

Cerrar el loop: CI falla → el agente reintenta el fix automáticamente
(hasta 3 intentos totales), con visibilidad del estado en cada PR.

## Alcance

### Incluido

1. **Workflow de auto-fix** (`.github/workflows/agent-autofix.yml`):
   - Se dispara cuando CI falla en un PR cuya branch empieza con
     `feature/` o `chore/` (branches de agent work).
   - Verifica que el PR fue creado por el Developer Agent (comentario
     marcador o author association) para no relanzar sobre PRs humanos.
   - Cuenta los intentos previos buscando comentarios marcador
     `<!-- autofix-attempt:N -->` en el PR; si ya hay 3, se detiene y
     agrega un comentario de escalada humana.
   - Si hay presupuesto, agrega comentario marcador
     `<!-- autofix-attempt:N+1 -->` y relanza el Developer Agent con un
     prompt de fix: leer los logs de CI fallido, diagnosticar, corregir,
     verificar local (build + test), push a la misma branch.
2. **Observabilidad**: el workflow `developer-agent.yml` agrega
   `$GITHUB_STEP_SUMMARY` con: issue, spec, branch creada, PR creado,
   resultado.

### Fuera de alcance

- Azure Orchestrator (Fase 6) — spec aparte, después de este PR.
- Auto-fix para PRs de Dependabot (los maneja el flujo humano actual).
- Fix de fallos de CodeQL (solo fallos de build/test/lint).

## Comportamiento

- PR de agente + CI falla → en ≤ 5 min hay un comentario
  `<!-- autofix-attempt:1 -->` y un nuevo run del Developer Agent sobre
  esa branch.
- El agente pushea el fix → CI re-corre → si pasa, el loop termina; si
  falla, se repite hasta `autofix-attempt:3`.
- Al 3er fallo: comentario de escalada explicando qué falló, sin más
  reintentos.
- PRs humanos: el workflow no hace nada (no-op).

## Reglas

- Máximo 3 reintentos autónomos (Constitution, regla de escalada).
- El auto-fix NUNCA pushea a `main`, nunca mergea, nunca debilita tests
  (mismas reglas del Developer Agent, `docs/agents/developer-agent.md`).
- El workflow de auto-fix no modifica `developer-agent.yml` ni
  `CONSTITUTION.md` (Article VII).
- Cada intento queda registrado como comentario en el PR (auditoría).

## Casos de error

- El agente no puede arreglar el fallo en 3 intentos → escalada humana
  (comentario en el PR), el PR queda esperando review.
- El PR no es del agente → no-op silencioso.
- Fallo al relanzar el workflow → comentario de error en el PR para
  visibilidad.

## Criterios de aceptación

Ver `acceptance.md`.
