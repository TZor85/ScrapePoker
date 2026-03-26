## ADDED Requirements

### Requirement: Ajuste de umbrales postflop en 3-bet/4-bet pots
Cuando la mano se desarrolla en un pot 3-beteado, 4-beteado o squeeze, el sistema SHALL ajustar los umbrales de decisión postflop para reflejar el rango más estrecho del villano.

#### Scenario: 3-bet pot — umbrales más estrictos
- **GIVEN** `situation == HandSituation.ThreeBet` o `situation == HandSituation.OpenRaiseVs3Bet`
- **WHEN** se calculan umbrales ajustados
- **THEN** `adjustedFoldBelow += ThreeBetPostflopFoldIncrease` (default +5) y `adjustedThinValueAbove += ThreeBetPostflopValueIncrease` (default +3)

#### Scenario: 4-bet pot — umbrales aún más estrictos
- **GIVEN** `situation == HandSituation.FourBet`
- **WHEN** se calculan umbrales ajustados
- **THEN** `adjustedFoldBelow += FourBetPostflopFoldIncrease` (default +8) y `adjustedThinValueAbove += FourBetPostflopValueIncrease` (default +5)

#### Scenario: Squeeze pot — mismos ajustes que 3-bet
- **GIVEN** `situation == HandSituation.Squeeze`
- **WHEN** se calculan umbrales ajustados
- **THEN** se aplican los mismos ajustes que ThreeBet

#### Scenario: Open raise normal — sin ajuste
- **GIVEN** `situation == HandSituation.OpenRaise` o `HandSituation.RaiseOverLimper`
- **WHEN** se calculan umbrales ajustados
- **THEN** no se aplica ajuste por tipo de pot (comportamiento actual preservado)

#### Scenario: 3-bet pot + villain barreling — acumulativo con range narrowing
- **GIVEN** pot 3-beteado Y villain apostó en 2+ calles (range narrowing activo)
- **WHEN** se calculan umbrales ajustados
- **THEN** ambos ajustes se acumulan: 3-bet penalty + range narrowing penalty (el villano en 3-bet pot que barrelea tiene rango muy estrecho)

### Requirement: Nuevos parámetros en StrategyProfile
SHALL existir 4 nuevos parámetros en StrategyProfile para configurar los ajustes de 3-bet/4-bet pots.

#### Scenario: Parámetros por defecto
- **GIVEN** StrategyProfile con valores por defecto
- **THEN** `ThreeBetPostflopFoldIncrease = 5.0`, `ThreeBetPostflopValueIncrease = 3.0`, `FourBetPostflopFoldIncrease = 8.0`, `FourBetPostflopValueIncrease = 5.0`
