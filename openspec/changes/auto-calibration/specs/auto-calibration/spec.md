## Auto-Calibration Specification

### Requirement: Ajustar automáticamente los parámetros del motor según los leaks detectados

El sistema DEBERÁ usar los datos del `ExploitabilityCalculator` para ajustar dinámicamente los thresholds (FoldBelow, ThinValue) basándose en los patrones de decisión observados.

#### Concepto
- **ExploitabilityCalculator** ya detecta y graba decisiones exploitables
- **Auto-Calibration** usará esos datos para ajustar los parámetros del `StrategyProfile`
- **Feedback loop**: Decisiones → Análisis → Ajuste de parámetros → Mejores decisiones

#### Escenarios

##### Scenario: Over-bluffing detectado → Reducir bluff frequency
- **GIVEN** ExploitabilityCalculator detecta categoría "OverBluffing" como top leak
- **WHEN** `AutoCalibrationService.Calibrate()` se ejecuta
- **THEN** ajusta:
  - `StrategyProfile.ThinValueAbove` +2-3 puntos
  - o ajusta `StrategyProfile.BluffFrequency` -10%

##### Scenario: Over-calling detectado → Aumentar FoldBelow
- **GIVEN** ExploitabilityCalculator detecta categoría "OverCalling" como top leak
- **WHEN** `AutoCalibrationService.Calibrate()` se ejecuta
- **THEN** ajusta:
  - `StrategyProfile.FoldBelow` +3-5 puntos para situaciones relevantes

##### Scenario: Under-bluffing detectado → Reducir FoldBelow
- **GIVEN** ExploitabilityCalculator detecta categoría "UnderBluffing" como top leak
- **WHEN** `AutoCalibrationService.Calibrate()` se ejecuta
- **THEN** ajusta:
  - `StrategyProfile.FoldBelow` -3-5 puntos

##### Scenario: Under-value detectado → Reducir ThinValueAbove
- **GIVEN** ExploitabilityCalculator detecta categoría "UnderValue" como top leak
- **WHEN** `AutoCalibrationService.Calibrate()` se ejecuta
- **THEN** ajusta:
  - `StrategyProfile.ThinValueAbove` -2-4 puntos

---

### Requirement: Ejecutar calibración automáticamente

El sistema DEBERÁ ejecutar la calibración periódicamente o bajo condiciones específicas.

#### Scenario: Calibración después de N decisiones
- **GIVEN** se han registrado N decisiones en ExploitabilityCalculator
- **WHEN** `ShouldRecalibrate()` devuelve true
- **THEN** ejecutar `Calibrate()` automáticamente

#### Scenario: Calibración manual desde UI
- **GIVEN** usuario hace clic en botón "Calibrar" en FrmMain
- **WHEN** `BtnCalibrate_Click()` se ejecuta
- **THEN** muestra diálogo de preview de cambios y pregunta confirmación

#### Condiciones para recalibrar
- Mínimo 20 decisiones registradas
- Última calibración hace más de 50 decisiones
- Explotabilidad promedio > threshold (configurable)

---

### Requirement: Interfaz pública del AutoCalibrationService

```csharp
public class AutoCalibrationService
{
    /// <summary>
    /// Ejecuta la calibración de parámetros basada en los datos actuales.
    /// </summary>
    public CalibrationResult Calibrate(
        ExploitabilityCalculator exploitabilityCalculator,
        StrategyProfile currentProfile);

    /// <summary>
    /// Determina si es momento de recalibrar.
    /// </summary>
    public bool ShouldRecalibrate(ExploitabilityCalculator calculator);

    /// <summary>
    /// Obtiene una vista previa de los ajustes sin aplicarlos.
    /// </summary>
    public CalibrationPreview GetPreview(
        ExploitabilityCalculator calculator,
        StrategyProfile profile);
}
```

```csharp
public class CalibrationResult
{
    public bool Success { get; set; }
    public int DecisionsAnalyzed { get; set; }
    public List<ParameterAdjustment> Adjustments { get; set; }
    public double PreviousExploitability { get; set; }
    public double EstimatedNewExploitability { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ParameterAdjustment
{
    public string ParameterName { get; set; }
    public double OldValue { get; set; }
    public double NewValue { get; set; }
    public string Reason { get; set; }
    public LeakCategory CausedBy { get; set; }
}

public class CalibrationPreview
{
    public List<ParameterAdjustment> ProposedAdjustments { get; set; }
    public Dictionary<LeakCategory, double> LeakReduction { get; set; }
}
```

---

### Constants y Thresholds

| Constant | Valor | Descripción |
|----------|-------|-------------|
| MinDecisionsForCalibration | 20 | Mínimo decisiones antes de calibrar |
| RecalibrateThreshold | 50 | Decisiones entre recalibraciones |
| ExploitabilityCalibrationThreshold | 15.0 | mbb/hand mínimo para activar calibración |
| MaxAdjustmentPerCycle | 5.0 | Máximo cambio por parámetro (%) |
| CalibrationMinImprovement | 2.0 | Mejora mínima esperada en mbb/hand |

---

### Flujo de Calibración

```
1. ExploitabilityCalculator graba decisiones
        ↓
2. ShouldRecalibrate() evalúa condiciones
        ↓
3. Si true → Calibrate():
   a. Obtener SessionAnalysis de ExploitabilityCalculator
   b. Identificar top leaks
   c. Calcular ajustes necesarios
   d. Aplicar a StrategyProfile (copia)
   e. Estimar nueva exploitabilidad
        ↓
4. Guardar CalibrationResult para historial
        ↓
5. [Opcional] Aplicar ajustes al StrategyProfile activo
```

---

### Casos Edge

- **Sin datos suficientes**: No ejecutar calibración, mostrar mensaje
- **Explotabilidad ya baja** (< 10 mbb): No calibrar, está "GTO"
- **Parámetros en límites**: No ajustar más si ya está al límite
- **Calibración inestable**: Si propuesta > MaxAdjustmentPerCycle, reducir

---

### Integración con UI

- **Botón "Calibrar"** en FrmMain (junto a "GTO")
- **Diálogo de preview** muestra:
  - Parámetros que cambiarían
  - Valores actuales vs nuevos
  - Razón del cambio
  - Confirmar/Cancelar

---

### Tests Requeridos

1. **Unit test**: Calibrate con Over-bluffing → ajusta ThinValue
2. **Unit test**: Calibrate con Over-calling → ajusta FoldBelow
3. **Unit test**: ShouldRecalibrate true después de 50 decisiones
4. **Unit test**: ShouldRecalibrate false con menos de 20 decisiones
5. **Integration test**: Calibración completa afecta decisiones