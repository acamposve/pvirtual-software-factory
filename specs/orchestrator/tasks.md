# Tasks — orchestrator (iteración 1)

- [ ] Crear `backend/Orchestrator/` (ASP.NET Core minimal API) y agregarlo
      a `PVirtualSoftwareFactory.slnx`.
- [ ] Modelo `AgentJob` + máquina de estados con transiciones válidas
      encapsuladas (métodos que validen la transición, no setters libres).
- [ ] Schema PostgreSQL: `agent_jobs` + `processed_deliveries` + migraciones
      versionadas en `backend/Orchestrator/Migrations/`.
- [ ] `POST /webhooks/github`: validación HMAC, idempotencia por
      `delivery_id`, creación de jobs `Pending` para `issues.labeled`
      (label `ai-ready`).
- [ ] `JobDispatcher` (BackgroundService): toma jobs `Pending/Retrying`
      respetando `MaxConcurrentJobs`, invoca `workflow_dispatch` vía
      GitHub API, registra `github_run_id`.
- [ ] `RunReconciler` (BackgroundService): sincroniza jobs
      `Dispatched/Running` con el estado real del run en GitHub API.
- [ ] Política de reintento: max 3, backoff exponencial, `Escalated` +
      comentario en el issue al agotar.
- [ ] `GET /jobs`, `GET /jobs/{id}`, `GET /health`.
- [ ] Proyecto `backend/Orchestrator.Tests/`: tests de máquina de estados,
      idempotencia, retry policy y endpoints (webhook con firma válida/
      inválida).
- [ ] CI: el job backend ya compila y testea toda la solución — verificar
      que los nuevos proyectos entren en el pipeline sin cambios extra.
- [ ] Abrir el PR desde `feature/33-orchestrator-spec` (o la branch de
      implementación que surja del issue).

## Orden de implementación sugerido

1. Dominio puro (AgentJob + transiciones) con tests — sin I/O.
2. Persistencia + migraciones.
3. Webhook receiver con HMAC + idempotencia.
4. Dispatcher + reconciler.
5. Endpoints de lectura + health.
