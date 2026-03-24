## MODIFIED Requirements

### Requirement: ShouldBluff no debe depender de villainBetSize
`ShouldBluff()` SHALL evaluar condiciones de bluff sin recibir `BetSizeCategory betSize` como parámetro, ya que solo se invoca cuando `!isFacingBet` (villainBetSize siempre es `NoBet`).

#### Scenario: Bluff IP en board coordinated con condición IPCoordinatedSmallOnly
- **GIVEN** hero está IP, boardTexture es "Coordinated", street es Flop, `!isFacingBet`
- **AND** thresholds.BluffCondition es `BluffConditionType.IPCoordinatedSmallOnly`
- **WHEN** `ShouldBluff()` se evalúa
- **THEN** la condición puede retornar true (sujeto a frecuencia aleatoria)
- **AND** NO depende del valor de `villainBetSize`

#### Scenario: Bluff OOP en board coordinated con condición IPCoordinatedSmallOnly
- **GIVEN** hero está OOP, boardTexture es "Coordinated"
- **AND** thresholds.BluffCondition es `BluffConditionType.IPCoordinatedSmallOnly`
- **WHEN** `ShouldBluff()` se evalúa
- **THEN** retorna false (requiere IP)

#### Scenario: Bluff IP en board dry con condición IPCoordinatedSmallOnly
- **GIVEN** hero está IP, boardTexture es "Dry"
- **AND** thresholds.BluffCondition es `BluffConditionType.IPCoordinatedSmallOnly`
- **WHEN** `ShouldBluff()` se evalúa
- **THEN** retorna false (requiere Coordinated)

#### Scenario: Bluff con condición Always
- **GIVEN** thresholds.BluffCondition es `BluffConditionType.Always`
- **WHEN** `ShouldBluff()` se evalúa múltiples veces
- **THEN** retorna true aproximadamente en proporción a `bluffFrequency * BluffFrequencyMultiplier`

#### Scenario: Bluff con condición OOPOnly
- **GIVEN** hero está OOP, thresholds.BluffCondition es `BluffConditionType.OOPOnly`
- **WHEN** `ShouldBluff()` se evalúa
- **THEN** puede retornar true (sujeto a frecuencia)
- **GIVEN** hero está IP, thresholds.BluffCondition es `BluffConditionType.OOPOnly`
- **WHEN** `ShouldBluff()` se evalúa
- **THEN** retorna false

#### Scenario: HandleLowEquity invoca ShouldBluff sin betSize
- **GIVEN** equity < adjustedFoldBelow, `!isFacingBet`, `!isMultiway`, thresholds.CanBluff es true
- **WHEN** `HandleLowEquity()` evalúa bluff puro
- **THEN** invoca `ShouldBluff(thresholds, isInPosition, boardTexture, street)` sin parámetro betSize
