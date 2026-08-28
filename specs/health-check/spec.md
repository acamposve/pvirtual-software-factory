# Spec — health-check

> Copiado de specs/_template/. Ningún agente puede implementar esto hasta
> que este archivo esté aprobado (ver docs/CONSTITUTION.md, Article I).

## Problema

Hoy no hay forma de saber, desde afuera, si el backend está vivo y
respondiendo. Esto es necesario para: monitoreo básico, y más adelante para
que un orquestador/sandbox efímero (Fase 5/6) o cualquier infraestructura de
despliegue pueda verificar que el servicio arrancó bien antes de darlo de
alta.

## Objetivo

Exponer un endpoint HTTP simple que confirme que el proceso del backend está
arriba y puede responder peticiones.

## Alcance

**Incluye:**
- Un endpoint `GET /health` público (sin autenticación).
- Devuelve 200 OK con un body JSON mínimo: estado y timestamp.
- No depende de ninguna otra pieza del sistema (DB, servicios externos).

**No incluye (fuera de alcance):**
- Chequeo de conectividad a PostgreSQL u otras dependencias externas
  ("readiness check" real). Todavía no hay integración con la base de datos
  en el backend — se agrega como spec separada cuando exista.
- Autenticación/autorización.
- Métricas detalladas (uso de memoria, uptime, versión de build). Se puede
  sumar después si hace falta, sin romper este contrato.

## Comportamiento esperado

1. Un cliente hace `GET /health` sin ningún header especial.
2. El backend responde `200 OK` con:
   ```json
   {
     "status": "healthy",
     "timestampUtc": "2026-08-28T15:04:05Z"
   }
   ```
3. La respuesta debe ser rápida (sin I/O a servicios externos) y no requiere
   autenticación.

## Reglas de negocio

- El endpoint siempre devuelve `status: "healthy"` si el proceso puede
  responder — no hay lógica condicional todavía. Si en el futuro se agrega
  un readiness check con dependencias, eso es una spec nueva, no una
  modificación silenciosa de este contrato.
- El endpoint no debe requerir autenticación (los orquestadores/monitors que
  lo van a usar no tienen credenciales de la app).

## Casos de error

| Caso | Comportamiento esperado |
|---|---|
| El proceso no puede responder en absoluto | No hay respuesta HTTP (timeout) — esto es lo que el caller interpreta como "unhealthy". No es un caso que el código maneje explícitamente. |

## Criterios de aceptación

- [ ] `GET /health` devuelve `200 OK`.
- [ ] El body es JSON válido con al menos los campos `status` y
      `timestampUtc`.
- [ ] `status` vale `"healthy"`.
- [ ] El endpoint no exige autenticación.
- [ ] El endpoint responde en <100ms en local (no hace I/O externo).
