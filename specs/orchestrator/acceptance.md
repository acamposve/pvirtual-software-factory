# Acceptance Criteria — orchestrator

## Escenario: issue ai-ready crea un job

- **Given** el orquestador corriendo con webhook configurado
- **When** un issue recibe la etiqueta `ai-ready` (webhook válido)
- **Then** existe un job `Pending` en `agent_jobs` con el issue en el
  payload y el `delivery_id` registrado

## Escenario: redelivery no duplica jobs

- **When** GitHub re-entrega el mismo webhook (mismo `delivery_id`)
- **Then** no se crea un segundo job (respuesta 2xx, efecto no-op)

## Escenario: webhook sin firma válida

- **When** `POST /webhooks/github` sin header de firma o con firma
  inválida
- **Then** responde 401 y no crea ningún job

## Escenario: dispatch con control de concurrencia

- **Given** 2 jobs `Pending` y `MaxConcurrentJobs = 1`
- **When** el dispatcher corre
- **Then** solo 1 job pasa a `Dispatched` con un `github_run_id`
- **And** el segundo permanece `Pending` hasta que el primero termine

## Escenario: retry hasta 3 y escalada

- **Given** un job cuyo run de Actions falla 3 veces
- **Then** `AttemptCount` llega a 3 y el estado final es `Escalated`
- **And** el issue origen tiene un comentario de escalada
- **And** no hay un cuarto dispatch

## Escenario: reconciliación tras reinicio

- **Given** un job en `Running` cuyo run terminó `Succeeded` mientras el
  servicio estaba caído
- **When** el servicio reinicia y corre el reconciler
- **Then** el job pasa a `Succeeded` sin intervención humana

## Escenario: observabilidad

- **When** se consulta `GET /jobs?state=Failed`
- **Then** devuelve solo los jobs en ese estado con id, tipo, intentos y
  timestamps

## Escenario: tests en CI

- **Given** el PR con los nuevos proyectos
- **When** corre el job backend de CI
- **Then** `dotnet test` incluye los tests de `Orchestrator.Tests` y
  todos pasan
