# Specs

Specification Driven Development: ninguna implementación puede empezar sin
una spec aprobada acá (ver `docs/CONSTITUTION.md`, Article I).

Cada feature vive en su propia carpeta:

```text
/specs
  /<feature-name>
    spec.md          # problema, objetivo, alcance, comportamiento, reglas, casos de error, criterios de aceptación
    domain.md         # entidades, value objects, aggregates, domain services, eventos, invariantes
    architecture.md   # componentes, dependencias, APIs, persistencia, eventos, seguridad, decisiones
    tasks.md           # spec transformada en tareas implementables
    acceptance.md      # criterios de aceptación verificables
```
