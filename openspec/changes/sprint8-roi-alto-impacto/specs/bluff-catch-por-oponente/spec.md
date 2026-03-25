## MODIFIED Requirements

### Requirement: Bluff catch threshold debe ajustarse por tipo de oponente
El bluff catching SHALL multiplicar su threshold por un factor dependiente de `villainType`, reflejando la probabilidad de que el villano esté bluffeando.

#### Scenario: Bluff catch vs LAG — threshold más bajo (call más amplio)
- **GIVEN** street es River, villainBetSize es Small, heroHandRank es OnePair
- **AND** equity es 28%, FoldBelow es 35, BluffCatchFoldBelowMultiplier es 0.75
- **AND** villainType es LAG
- **WHEN** se evalúa bluff catch
- **THEN** bluffCatchThreshold = 35 × 0.75 × 0.80 = 21.0
- **AND** 28% >= 21.0 → retorna "Call — bluff catch river"

#### Scenario: Bluff catch vs TP (Nit) — threshold más alto (fold más)
- **GIVEN** street es River, villainBetSize es Small, heroHandRank es OnePair
- **AND** equity es 28%, FoldBelow es 35, BluffCatchFoldBelowMultiplier es 0.75
- **AND** villainType es TP
- **WHEN** se evalúa bluff catch
- **THEN** bluffCatchThreshold = 35 × 0.75 × 1.20 = 31.5
- **AND** 28% < 31.5 → retorna "Fold"

#### Scenario: Bluff catch vs Unknown — sin ajuste
- **GIVEN** villainType es Unknown
- **WHEN** se evalúa bluff catch
- **THEN** opponentBluffMultiplier = 1.0 (sin cambio)

#### Scenario: Bluff catch vs LP — ligeramente más amplio
- **GIVEN** villainType es LP, equity es 25%, calculado bluffCatchThreshold sin oponente es 26.25
- **WHEN** se evalúa bluff catch
- **THEN** bluffCatchThreshold × 0.85 = 22.3
- **AND** 25% >= 22.3 → Call

#### Scenario: Bluff catch turn vs LAG con MiddlePair
- **GIVEN** street es Turn, villainBetSize es Small, heroHandRank es OnePair
- **AND** pairClassification es MiddlePair, villainType es LAG
- **WHEN** se evalúa bluff catch
- **THEN** aplica BluffCatchTurnEquityMultiplier × BluffCatchLAGMultiplier
- **AND** threshold resultante es más bajo que sin ajuste LAG
