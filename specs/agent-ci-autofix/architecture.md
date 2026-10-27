# Architecture — agent-ci-autofix

## Componentes

| Componente | Tipo | Responsabilidad |
|---|---|---|
| `.github/workflows/agent-autofix.yml` | Workflow nuevo | Detectar fallo de CI en PR de agente, contar intentos, relanzar |
| `.github/workflows/developer-agent.yml` | Workflow existente (cambio menor) | Agregar step summary con resultado de la ejecución |
| `docs/agents/developer-agent.md` | Doc (cambio menor) | Documentar el modo "fix" del agente |

## Flujo

```text
CI falla en PR (check_suite completed, conclusion=failure)
    ↓
agent-autofix.yml se dispara (workflow_run / check_suite event)
    ↓
¿Branch feature/* o chore/*? ¿PR creado por el agente?
    ├─ no → no-op
    └─ sí → contar comentarios <!-- autofix-attempt:N -->
         ├─ N >= 3 → comentario de escalada, fin
         └─ N < 3 → comentario <!-- autofix-attempt:N+1 -->
                    + relanzar developer-agent (workflow_dispatch o
                      claude-code-action inline) con prompt de fix
```

## Decisiones

1. **Detección de "PR del agente"**: los PRs del Developer Agent salen de
   branches `feature/*` creadas por el token de `claude-code-action`.
   Se usa doble condición: prefijo de branch + comentario marcador que el
   developer-agent deja al abrir el PR. Si falta el marcador, no se
   relanza (fail-safe hacia humano).
2. **Contador de intentos en comentarios del PR**: sin estado externo, sin
   DB. Los comentarios `<!-- autofix-attempt:N -->` son el registro.
   Simple, auditable, y sobrevive re-runs del workflow.
3. **Reinvocación del agente**: se reusa `anthropics/claude-code-action`
   dentro del mismo workflow de auto-fix (no `workflow_dispatch` del
   developer-agent.yml, para no acoplar los dos workflows ni necesitar
   un PAT extra).
4. **Límite de tiempo**: timeout-minutes: 30 por intento, igual que el
   developer-agent.

## Seguridad

- Permisos mínimos: `contents: write`, `pull-requests: write`, `issues: write`
  (mismos que developer-agent.yml).
- El token OAuth del agente ya existe como secret (`CLAUDE_CODE_OAUTH_TOKEN`);
  no se agregan secretos nuevos.
- El prompt de fix prohíbe explícitamente: tocar `main`, mergear, debilitar
  tests, editar workflows.

## Costo

- Cada intento consume minutos de runner + tokens del agente. El tope de 3
  intentos acota el peor caso por PR.
