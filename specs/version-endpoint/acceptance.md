# Acceptance Criteria — version-endpoint

## Escenario: consulta básica de versión

- **Given** el backend está corriendo
- **When** un cliente hace `GET /version`
- **Then** la respuesta es `200 OK` con un body JSON que incluye
  `version` y `environment`

## Escenario: no requiere autenticación

- **Given** el backend está corriendo
- **When** un cliente hace `GET /version` sin ningún header de
  autenticación
- **Then** la respuesta sigue siendo `200 OK` (no `401`/`403`)

## Escenario: la versión coincide con el csproj

- **Given** `Api.csproj` define `<Version>0.1.0</Version>`
- **When** un cliente hace `GET /version`
- **Then** el campo `version` del body es `"0.1.0"`
