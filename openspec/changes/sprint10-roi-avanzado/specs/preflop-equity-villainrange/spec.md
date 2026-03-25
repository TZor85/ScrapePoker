## MODIFIED Requirements

### Requirement: Preflop equity debe usar VillainRange siempre, no lookup table random
Cuando no hay `handSituation` específica, el cálculo de equity preflop SHALL usar `VillainRange.GetForSituation(OpenRaise)` como fallback en vez de lookup table vs random.

#### Scenario: Preflop con situación definida (3Bet) usa VillainRange 3Bet
- **GIVEN** communityCards vacías, handSituation "ThreeBet"
- **WHEN** CalculateEquity se invoca
- **THEN** usa VillainRange.GetForSituation(ThreeBet) (comportamiento existente)

#### Scenario: Preflop sin situación usa VillainRange OpenRaise fallback
- **GIVEN** communityCards vacías, handSituation es null
- **WHEN** CalculateEquity se invoca
- **THEN** usa VillainRange.GetForSituation(OpenRaise) como fallback
- **AND** NO usa lookup table vs random

#### Scenario: Equity preflop vs VillainRange < equity vs random
- **GIVEN** hero tiene 7-2o, communityCards vacías
- **WHEN** equity se calcula vs VillainRange(OpenRaise) vs lookup table random
- **THEN** equity vs VillainRange es MENOR que vs random (villain tiene rango filtrado)

### Requirement: Fold equity debe reducirse en multiway
Cuando `numOpponents >= 2`, la fold equity SHALL reducirse proporcionalmente.

#### Scenario: Fold equity heads-up
- **GIVEN** numOpponents es 1, baseFoldEquity es 50%
- **WHEN** CalculateFoldEquity se invoca
- **THEN** fold equity no se reduce por multiway

#### Scenario: Fold equity 3-way
- **GIVEN** numOpponents es 3, baseFoldEquity es 50%
- **WHEN** CalculateFoldEquity se invoca
- **THEN** fold equity se multiplica por 1.0/(1.0 + 0.3×2) = 0.625
- **AND** resultado ≈ 31.25% (menor que heads-up)

#### Scenario: Fold equity 5-way
- **GIVEN** numOpponents es 5
- **WHEN** CalculateFoldEquity se invoca
- **THEN** fold equity se reduce aún más (factor ≈ 0.45)
