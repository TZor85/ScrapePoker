# Proposal: Auto-Calibration Service

**Change ID**: auto-calibration  
**Fecha**: 2026-04-06  
**Prioridad**: Alta  
**Estimación**: ~3-4 horas

---

## Resumen

Implementar un `AutoCalibrationService` que usa los datos del `ExploitabilityCalculator` para ajustar automáticamente los parámetros del motor de decisiones (FoldBelow, ThinValue, BluffFrequency) basándose en los leaks detectados.

## Problema

El motor de decisiones usa parámetros estáticos definidos en `StrategyProfile`. No hay mecanismo para:
- Detectar patrones de decisiones subóptimas
- Ajustar parámetros dinámicamente
- Mejorar progresivamente basado en experiencia

## Solución

1. **AutoCalibrationService** analiza SessionAnalysis de ExploitabilityCalculator
2. Identifica los mayores leaks (OverBluffing, OverCalling, UnderBluffing, UnderValue)
3. Propone ajustes proporcionales a los parámetros relevantes
4. Puede ejecutarse automáticamente o manualmente

## Impacto

| Aspecto | Impacto |
|---------|---------|
| Precisión GTO | Alto - parámetros se adaptan a patrones reales |
| Mantenibilidad | Medio - feedback loop automático |
| UI | Medio - nuevo botón y diálogo |

## Riesgo

- **Bajo**: Ajustes limitados (max 5% por ciclo)
- **Testing**: Requiere suficientes decisiones para datos significativos

## Acceptance Criteria

1. `Calibrate()` ajusta parámetros según leaks detectados
2. `ShouldRecalibrate()` evalúa condiciones correctamente
3. `GetPreview()` muestra ajustes sin aplicar
4. Botón en UI para calibración manual
5. Tests unitarios cubren principales escenarios