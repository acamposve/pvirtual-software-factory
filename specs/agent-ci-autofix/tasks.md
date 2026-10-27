# Tasks — agent-ci-autofix

- [ ] Crear `.github/workflows/agent-autofix.yml`:
  - trigger: `check_suite` (conclusion failure) filtrado a PRs de branches
    `feature/*` / `chore/*`.
  - step de guardia: verificar marcador de PR-de-agente; si no está, no-op.
  - step de contador: leer comentarios `<!-- autofix-attempt:N -->`.
  - step de decisión: escalar (N>=3) o relanzar agente con prompt de fix.
  - step de registro: comentar `<!-- autofix-attempt:N+1 -->` con link al run.
- [ ] Modificar `developer-agent.yml`: agregar step que escriba
  `$GITHUB_STEP_SUMMARY` (issue, spec, branch, PR, resultado).
- [ ] Modificar `docs/agents/developer-agent.md`: sección "Modo fix"
  (recibe logs de CI fallido, corrige, verifica, pushea a la misma branch).
- [ ] Verificación manual: simular fallo de CI en un PR de agente de prueba
  y observar un intento de fix completo.
- [ ] Abrir el PR desde `feature/31-agent-ci-autofix`.

## Nota

Los cambios en `.github/workflows/*` requieren revisión humana extra
según `docs/agents/developer-agent.md` — este PR mismo es ese cambio, así
que la review humana del PR es la autorización.
