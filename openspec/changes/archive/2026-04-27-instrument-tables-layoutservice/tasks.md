## 1. Agregar IMetricsCollector al constructor

- [x] 1.1 Agregar campo `private readonly IMetricsCollector _metrics;`
- [x] 1.2 Agregar al constructor
- [x] 1.3 Build (verificar compila)

## 2. Instrumentar métodos públicos

- [x] 2.1 Envolver `SetDealerPlayer` con `using var _ = _metrics.Measure("LayoutDealer");`
- [x] 2.2 Envolver `SetVillainPosition` con `using var _ = _metrics.Measure("LayoutPositions");`
- [x] 2.3 Build (verificar compila)

## 3. Aceptación

- [x] 3.1 Build exitoso
- [ ] 3.2 Ejecutar app y verificar métricas