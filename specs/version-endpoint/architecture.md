# Architecture — version-endpoint

## Componentes involucrados

- `backend/Api/Api.csproj` — agregar `<Version>0.1.0</Version>` al
  `<PropertyGroup>` existente.
- `backend/Api/Program.cs` — se agrega el endpoint nuevo ahí mismo, mismo
  patrón que `/health`.

## Dependencias

Ninguna. El endpoint no llama a otros servicios, no toca la base de datos.

## APIs

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/version` | Devuelve version + environment. 200 OK. |

## Persistencia

N/A — no lee ni escribe datos.

## Eventos (integración)

N/A

## Seguridad

Endpoint público, sin autenticación. No expone secretos ni configuración
sensible — solo el número de versión y el nombre del ambiente, que no son
información sensible.

## Decisiones arquitectónicas

- Minimal API (`app.MapGet(...)`), mismo estilo que `/health` — no se
  introduce un patrón nuevo.
- La versión se obtiene vía
  `Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion`
  (poblado automáticamente por el SDK a partir de `<Version>` en el
  csproj), en lugar de hardcodearla en `Program.cs`.
- El ambiente se obtiene de `app.Environment.EnvironmentName`, ya
  disponible vía `WebApplicationBuilder` — no requiere configuración
  adicional.
