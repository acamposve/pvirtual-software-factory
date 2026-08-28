# Contributing

## Flujo obligatorio

1. Debe existir una spec aprobada en `specs/<feature>/` antes de implementar
   (ver `docs/CONSTITUTION.md`, Article I).
2. Crear una branch desde `main` siguiendo la convención de nombres (ver
   `AGENTS.md`).
3. Implementar, con tests automatizados incluidos.
4. Abrir un Pull Request hacia `main`. El build y los tests deben estar en
   verde antes de abrirlo.
5. Esperar CI verde + al menos 1 review aprobado.
6. Merge (squash recomendado) — nunca push directo a `main`.

## Convención de branches

`feature/<issue#>-<slug>`, `fix/<issue#>-<slug>`, `chore/<issue#>-<slug>`,
`docs/<issue#>-<slug>`.

## Convención de commits

[Conventional Commits](https://www.conventionalcommits.org/):

```text
feat: agrega X
fix: corrige Y
docs: actualiza Z
chore: tarea de mantenimiento
refactor: cambio interno sin alterar comportamiento
test: agrega o corrige tests
ci: cambios en pipelines
```

## Versionado

[SemVer](https://semver.org/) (`MAJOR.MINOR.PATCH`).

## Seguridad

Nunca commitear secretos, API keys ni credenciales. Ver `docs/CONSTITUTION.md`,
Article III.
