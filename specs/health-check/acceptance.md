# Acceptance Criteria — health-check

## Escenario: chequeo básico de salud

- **Given** el backend está corriendo
- **When** un cliente hace `GET /health`
- **Then** la respuesta es `200 OK` con un body JSON que incluye
  `status: "healthy"` y `timestampUtc`

## Escenario: no requiere autenticación

- **Given** el backend está corriendo
- **When** un cliente hace `GET /health` sin ningún header de autenticación
- **Then** la respuesta sigue siendo `200 OK` (no `401`/`403`)

## Escenario: no depende de servicios externos

- **Given** el backend está corriendo (sin base de datos ni servicios
  externos disponibles)
- **When** un cliente hace `GET /health`
- **Then** la respuesta sigue siendo `200 OK` — el endpoint no hace ningún
  chequeo de dependencias externas (ver `spec.md`, fuera de alcance)
