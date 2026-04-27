## 1. Agregar IMetricsCollector al constructor

- [x] 1.1 Agregar campo `private readonly IMetricsCollector _metrics;`
- [x] 1.2 Agregar al constructor
- [x] 1.3 Build (verificar compila)

## 2. Instrumentar StartNewHandAsync

- [x] 2.1 Llamar `_metrics.StartHand(handNumber.ToString())` tras crear _currentHand (línea ~116)
- [x] 2.2 Build

## 3. Instrumentar FinalizeAndPersistHandAsync

- [x] 3.1 Asignar `_currentHand.Telemetry = _metrics.EndHand()` ANTES del Store (línea ~210)
- [x] 3.2 Envolver persistencia con `using var _ = _metrics.RecordSessionOnly("Persistence.SaveHand")`
- [x] 3.3 Build

## 4. Aceptación

- [x] 4.1 Build exitoso
- [x] 4.2 Verificar HandRecord.Telemetry no es null tras persistir