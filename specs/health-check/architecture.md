# Architecture — health-check

## Componentes involucrados

- `backend/Api` (`Program.cs`) — se agrega un endpoint nuevo ahí mismo.

## Dependencias

Ninguna. El endpoint no llama a otros servicios, no toca la base de datos.

## APIs

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/health` | Liveness check. 200 OK con `{ status, timestampUtc }`. |

## Persistencia

N/A — no lee ni escribe datos.

## Eventos (integración)

N/A

## Seguridad

Endpoint público, sin autenticación (ver `spec.md`, Reglas de negocio). No
expone información sensible ni de configuración interna.

## Decisiones arquitectónicas

- Se implementa como **Minimal API** (`app.MapGet(...)` en `Program.cs`),
  consistente con el estilo que ya trae el template (`/weatherforecast`).
  No se crea un `Controller` aparte para esto — el proyecto no usa
  controllers todavía y el endpoint es trivial.
- Si en el futuro `Program.cs` crece demasiado, se puede extraer este
  endpoint a un archivo de extensión (`HealthEndpoints.cs`) sin cambiar el
  contrato HTTP. Eso es un refactor interno, no requiere una spec nueva.
- El readiness check real (con PostgreSQL) queda explícitamente fuera de
  alcance hasta que exista integración con la base de datos — se abordará
  como una spec separada en ese momento.
