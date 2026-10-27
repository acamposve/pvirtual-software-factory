# Domain — cleanup-template-debris

Tarea de limpieza (chore). No introduce entidades, value objects, aggregates
ni eventos de dominio nuevos.

## Entidades afectadas

Ninguna. El dominio del proyecto (aún sin modelar) no cambia.

## Invariantes que se preservan

- `GET /health` y `GET /version` del backend siguen respondiendo igual
  (no se toca `backend/`).
- El pipeline de CI sigue validando: build backend, tests backend, lint
  frontend, build frontend, CodeQL.
- Branch protection y CODEOWNERS no cambian.

## Decisiones de dominio

- La página mínima del frontend es un placeholder consciente: existe para
  que el deploy/build tenga contenido real del proyecto, no para definir
  UI. El diseño real llega con su propia spec.
