# Proposal: Range Polarization Logic

**Change ID**: range-polarization  
**Fecha**: 2026-04-05  
**Prioridad**: Alta  
**Estimación**: ~4-6 horas

---

## Resumen

Implementar un `RangePolarizer` que determina el tipo de rango óptimo (Linear/Polarized/Condensed) basándose en board texture, posición, y SPR. Los umbrales de decisión se ajustarán según el tipo de rango.

## Problema

El motor de decisiones usa rangos estáticos por posición. No considera:
- Board texture → dry boards permiten más polarización
- SPR → stacks cortos requieren rangos más tight
- Street → river es más polarizado

## Solución

Crear `RangePolarizer` que:
1. Determina tipo de rango según situación
2. Calcula ajustes de threshold (FoldBelow/ThinValue)
3. Se integra con `PostflopDecisionService`

## Impacto

| Aspecto | Impacto |
|---------|---------|
| Precisión GTO | Alto - mejores decisiones según textura |
| Bet sizing | Medio - más preciso |
| Mantenibilidad | Alto - lógica centralizada |

## Riesgo

- **Bajo**: Lógica bien definida,tests cubren escenarios
- **Regresión**: Pequño riesgo si thresholds bien calibrados

## Acceptance Criteria

1. `GetOptimalRangeType()` devuelve tipo correcto según board/posición/SPR
2. `GetThresholdAdjustment()` calcula ajustes correctos
3. Integración con PostflopDecisionService sin regressions
4. Tests unitarios cubren principales escenarios
