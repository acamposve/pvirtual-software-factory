# Spec — version-endpoint

> Copiado de specs/_template/. Este spec existe específicamente para probar
> el flujo del Developer Agent automatizado (Fase 4) end-to-end por primera
> vez: es intencionalmente trivial y de bajo riesgo. Ningún agente puede
> implementar esto hasta que este archivo esté aprobado (ver
> docs/CONSTITUTION.md, Article I).

## Problema

No hay forma de saber, desde afuera, qué versión del backend está corriendo
en un ambiente dado. Esto es útil para diagnosticar issues ("¿ya se
desplegó el fix?") y, más adelante, para que el propio pipeline de
agentes/CI pueda confirmar qué build quedó activo.

## Objetivo

Exponer un endpoint HTTP simple que devuelva la versión del backend y el
ambiente en el que corre.

## Alcance

**Incluye:**
- Endpoint `GET /version`, público (sin autenticación).
- Devuelve 200 OK con la versión del ensamblado (tomada de la propiedad
  `<Version>` del csproj) y el nombre del ambiente
  (`IWebHostEnvironment.EnvironmentName`).
- No depende de ninguna otra pieza del sistema (DB, servicios externos).

**No incluye (fuera de alcance):**
- Commit SHA / build number (requeriría inyectar metadata en tiempo de
  build vía CI — se puede sumar después sin romper este contrato).
- Autenticación/autorización.
- Información de dependencias/paquetes instalados.

## Comportamiento esperado

1. Un cliente hace `GET /version` sin ningún header especial.
2. El backend responde `200 OK` con:
   ```json
   {
     "version": "0.1.0",
     "environment": "Production"
   }
   ```
3. La respuesta es inmediata (sin I/O externo) y no requiere autenticación.

## Reglas de negocio

- La versión se toma de la propiedad `<Version>` en
  `backend/Api/Api.csproj` (SemVer) — nunca hardcodeada en el código C#,
  para que actualizar la versión sea un cambio de una sola línea en el
  csproj.
- Si `<Version>` no está definida, el default de .NET es `1.0.0` —
  aceptable como fallback, no es un error.

## Casos de error

| Caso | Comportamiento esperado |
|---|---|
| El proceso no puede responder en absoluto | No hay respuesta HTTP (timeout) — mismo comportamiento que `/health`, no es un caso que el código maneje explícitamente. |

## Criterios de aceptación

- [ ] `GET /version` devuelve `200 OK`.
- [ ] El body es JSON válido con al menos los campos `version` y
      `environment`.
- [ ] `version` coincide con la propiedad `<Version>` de `Api.csproj`.
- [ ] El endpoint no exige autenticación.
