## Range Polarization Specification

### Requirement: Adaptar el tipo de rango según board texture y posición

El sistema DEBERÁ determinar el tipo de rango óptimo (Linear/Polarized/Condensed) basándose en la textura del board, posición del jugador, y profundidad del SPR.

#### Concepto
- **Linear/Merged**: Rangos con muchas manos intermedias (value-heavy)
- **Polarized**: Rango con nuts muy fuertes y bluffs, pocas manos medias
- **Condensed**: Rango tight con pocas manos, fokus en premium hands

#### Scenario: Board seco + IP → Rango polarizado
- **GIVEN** board seco (Dry/Paired) Y jugador está en posición
- **WHEN** se llama a `RangePolarizer.GetOptimalRangeType()`
- **THEN** devuelve `RangeType.Polarized`
- **AND** permite más bluffs en continuación

#### Scenario: Board mojado + OOP → Rango lineal
- **GIVEN** board mojado (Wet/Coordinated/Monotone) Y jugador está fuera de posición
- **WHEN** se llama a `RangePolarizer.GetOptimalRangeType()`
- **THEN** devuelve `RangeType.Linear`
- **AND** menos bluffs, más calls para proteger manos medias

#### Scenario: SPR bajo → Rango condensado
- **GIVEN** SPR < 3 (stacks cortos)
- **WHEN** se llama a `RangePolarizer.GetOptimalRangeType()`
- **THEN** devuelve `RangeType.Condensed`
- **AND** range más tight, foco en manos fuertes

#### Scenario: River con nuts potenciales
- **GIVEN** street es River
- **WHEN** no hay más cartas por venir
- **THEN** rango es más polarizado (más value bets)

---

### Requirement: Ajustar umbrales de decisión según tipo de rango

El sistema DEBERÁ modificar los umbrales de FoldBelow/ThinValue según el tipo de rango determinado.

#### Scenario: IP Polarized → Umbrales más loose
- **GIVEN** tipo de rango es Polarized
- **WHEN** se calculan umbrales para bet
- **THEN** FoldBelow se reduce en 3-5 puntos
- **AND** ThinValue se reduce en 2-3 puntos

#### Scenario: OOP Linear → Umbrales más tight
- **GIVEN** tipo de rango es Linear Y OOP
- **WHEN** se calculan umbrales para bet
- **THEN** FoldBelow aumenta en 3-5 puntos
- **AND** ThinValue aumenta en 2-3 puntos

#### Scenario: Condensed → Umbrales muy tight
- **GIVEN** tipo de rango es Condensed
- **WHEN** se calculan umbrales
- **THEN** FoldBelow aumenta en 5-8 puntos
- **AND** solo manos fuertes pueden betting

---

### Requirement: Interfaz pública del RangePolarizer

```csharp
public enum RangeType
{
    Linear,      // Merged / value-heavy
    Polarized,   // Polarizado: nuts + bluffs
    Condensed    // Tight, pocas manos medias
}

public class RangePolarizer
{
    /// <summary>
    /// Obtiene el tipo de rango óptimo para la situación.
    /// </summary>
    RangeType GetOptimalRangeType(
        BoardTextureCategory texture,
        bool isInPosition,
        double spr,
        BoardPosition street);

    /// <summary>
    /// Calcula ajuste de umbrales según tipo de rango.
    /// </summary>
    (double foldBelowAdjust, double thinValueAdjust) GetThresholdAdjustment(
        RangeType rangeType,
        bool isInPosition);
}
```

---

### Lógica de Decisión

| Texture | Position | SPR | Street | Range Type |
|---------|----------|-----|--------|------------|
| Dry | IP | >5 | Flop | Polarized |
| Dry | OOP | >5 | Flop | Linear |
| Wet | IP | >5 | Flop | Linear |
| Wet | OOP | >5 | Flop | Linear |
| Monotone | Any | Any | Any | Linear (cuidado) |
| Paired | IP | >5 | Flop | Polarized |
| Paired | OOP | >5 | Flop | Linear |
| Any | Any | <3 | Any | Condensed |
| Any | Any | Any | River | More Polarized |

---

### Ajustes de Threshold

| Range Type | Position | FoldBelow Δ | ThinValue Δ |
|------------|----------|-------------|-------------|
| Polarized | IP | -4 | -3 |
| Polarized | OOP | -2 | -2 |
| Linear | IP | 0 | 0 |
| Linear | OOP | +3 | +2 |
| Condensed | Any | +6 | +4 |

---

### Constants

| Constant | Valor | Descripción |
|----------|-------|-------------|
| SPRCondensedThreshold | 3.0 | SPR menor → rango condensado |
| DryTextureThreshold | 25.0 | BoardWetnessScore < 25 → Dry |
| WetTextureThreshold | 50.0 | BoardWetnessScore > 50 → Wet |
