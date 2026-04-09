# Tareas: Auto-Calibration Service

## Fase 1: Estructura Base

- [ ] **T1.1** Crear archivo `AutoCalibrationService.cs` en `src/OpenScrape.DecisionMaker/Services/`
- [ ] **T1.2** Definir clases `CalibrationResult`, `ParameterAdjustment`, `CalibrationPreview`
- [ ] **T1.3** Registrar servicio en DI container (`Program.cs`)

## Fase 2: Lógica de Calibración

- [ ] **T2.1** Implementar método `Calibrate()` - análisis de leaks
- [ ] **T2.2** Implementar lógica de ajustes según leak category
- [ ] **T2.3** Implementar método `ShouldRecalibrate()`
- [ ] **T2.4** Implementar método `GetPreview()`

## Fase 3: Integración con ExploitabilityCalculator

- [ ] **T3.1** Conectar con ExploitabilityCalculator existente
- [ ] **T3.2** Integrar con FrmMain (botón de calibración)

## Fase 4: Tests

- [ ] **T4.1** Tests para `Calibrate()` (5+ escenarios)
- [ ] **T4.2** Tests para `ShouldRecalibrate()` (3+ casos)
- [ ] **T4.3** Tests para `GetPreview()`

---

## Estimación

| Fase | Horas |
|------|-------|
| Fase 1 | 0.5 hr |
| Fase 2 | 2 hrs |
| Fase 3 | 1 hr |
| Fase 4 | 1 hr |
| **Total** | **~4.5 hrs** |