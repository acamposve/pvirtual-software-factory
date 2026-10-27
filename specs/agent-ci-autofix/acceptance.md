# Acceptance Criteria — agent-ci-autofix

## Escenario: auto-fix exitoso al primer intento

- **Given** un PR creado por el Developer Agent (branch `feature/*` con
  marcador de agente)
- **When** el CI falla por un error de build o test
- **Then** dentro de ~5 min aparece el comentario `<!-- autofix-attempt:1 -->`
- **And** se lanza un run del agente en modo fix
- **And** el agente pushea un fix a la misma branch
- **When** el CI re-corre y pasa
- **Then** no hay más intentos ni comentarios de escalada

## Escenario: presupuesto agotado

- **Given** un PR de agente con comentarios `autofix-attempt:1..3` y CI
  fallando en cada uno
- **When** el CI falla por cuarta vez
- **Then** el workflow NO relanza el agente
- **And** agrega un comentario de escalada humana resumiendo los fallos

## Escenario: PR humano es intocable

- **Given** un PR de branch `feature/*` creado por un humano (sin marcador
  de agente)
- **When** el CI falla
- **Then** el workflow de auto-fix no comenta ni relanza nada

## Escenario: observabilidad del developer-agent

- **Given** una ejecución del Developer Agent (label `ai-ready`)
- **When** el run termina
- **Then** el run summary muestra: issue disparador, spec implementada,
  branch creada, PR creado (o error), y resultado final

## Escenario: límites respetados

- **Given** cualquier secuencia de fallos de CI
- **Then** nunca hay más de 3 comentarios `autofix-attempt` por PR
- **And** ningún push del loop va a `main` y ningún merge es automático
