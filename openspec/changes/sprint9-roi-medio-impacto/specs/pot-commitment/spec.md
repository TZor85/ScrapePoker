## NEW Requirements

### Requirement: Pot commitment previene folds -EV cuando hero está committed
Cuando SPR < 0.5 (pot committed), el bot SHALL calcular EV(call) antes de fold. Si EV(call) > 0, retorna Call en vez de Fold.

#### Scenario: Pot committed con EV(call) positivo — Call
- **GIVEN** street es River, heroStack es 30, potSize es 100 (SPR = 0.3)
- **AND** equity es 30%, isFacingBet es true
- **AND** EV(call) = 0.30 × (100 + 30) - 0.70 × 30 = 39 - 21 = 18 > 0
- **WHEN** HandleFacingBet o HandleLowEquity evalúa fold
- **THEN** retorna "Call — pot committed (SPR=0.30, EV call=18.0)"

#### Scenario: Pot committed con EV(call) negativo — Fold
- **GIVEN** street es River, heroStack es 30, potSize es 100 (SPR = 0.3)
- **AND** equity es 10%
- **AND** EV(call) = 0.10 × 130 - 0.90 × 30 = 13 - 27 = -14 < 0
- **WHEN** HandleFacingBet evalúa fold
- **THEN** retorna "Fold" (EV negativo, pot committed pero sin equity)

#### Scenario: SPR normal no activa pot commitment
- **GIVEN** heroStack es 200, potSize es 100 (SPR = 2.0)
- **AND** equity es 30%
- **WHEN** HandleFacingBet evalúa fold
- **THEN** no evalúa pot commitment (SPR > 0.5)
- **AND** sigue lógica normal de fold

#### Scenario: Pot committed en HandleLowEquity — Call
- **GIVEN** equity < adjustedFoldBelow, SPR es 0.4, isFacingBet es true
- **AND** EV(call) > 0
- **WHEN** HandleLowEquity llega al fold final
- **THEN** retorna "Call — pot committed" en vez de "Fold"

#### Scenario: Pot committed sin facing bet — no aplica
- **GIVEN** SPR es 0.3, isFacingBet es false
- **WHEN** HandleNoBet se evalúa
- **THEN** pot commitment no se evalúa (sin apuesta → no hay call/fold)

#### Scenario: PotCommitmentSPRThreshold es configurable
- **GIVEN** PokerConstants.PotCommitmentSPRThreshold es 0.5
- **AND** SPR es 0.49
- **WHEN** se evalúa pot commitment
- **THEN** se activa (0.49 < 0.5)
- **GIVEN** SPR es 0.51
- **WHEN** se evalúa pot commitment
- **THEN** NO se activa (0.51 > 0.5)
