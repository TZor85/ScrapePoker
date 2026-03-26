## MODIFIED Requirements

### Requirement: Villain range fallback no contamina la equity
Cuando TryDrawFromRange falla después de 10 intentos (todas las manos del rango están bloqueadas por cartas simuladas), la iteración SHALL descartarse en lugar de usar cartas aleatorias.

#### Scenario: Rango bloqueado — skip en vez de random
- **GIVEN** un villain range reducido (ej: 4Bet = AA,KK,QQ,AK) y community cards que bloquean la mayoría de combos
- **WHEN** TryDrawFromRange falla en una iteración
- **THEN** la iteración se marca como skipped y no contribuye a la equity calculada

#### Scenario: El conteo de simulaciones refleja iteraciones efectivas
- **GIVEN** 50.000 iteraciones solicitadas con un rango que causa 5.000 skips
- **WHEN** se calcula equity
- **THEN** Simulations = 45.000 (solo iteraciones válidas) y la equity se calcula sobre ese total

### Requirement: Pre-compute totalWeight del rango
El peso total de los combos del villano SHALL calcularse una sola vez antes del loop de simulación.

#### Scenario: totalWeight no se recalcula por iteración
- **GIVEN** un villain range con 100 combos
- **WHEN** se ejecutan 50.000 simulaciones
- **THEN** ComputeTotalWeight se llama 1 vez (no 50.000)
