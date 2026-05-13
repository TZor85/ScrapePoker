## Context

Las features de telemetría (instrumentación de FrmMain, UI de métricas) deben validarse antes de hacer merge a develop. No hay cambios de lógica de negocio, solo instrumentation y UI.

## Goals

- Garantizar calidad de código antes del merge
- Documentar checklist de verificación
- Ejecutar smoke test manual

## Decisions

1. **Crear checklist de verificación** con comandos exactos a ejecutar
2. **Smoke test manual** de 8 pasos para validar funcionalidad
3. **Documentar resultados** para referencia futura

## Risks

- Ninguno - solo verificaciones, no cambios de código