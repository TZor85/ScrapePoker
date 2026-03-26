## ADDED Requirements

### Requirement: C-bet con frecuencia propia separada de bluff
Cuando hero es agresor preflop y tiene equity baja (< FoldBelow pero dentro de margen), SHALL usar frecuencia de c-bet en vez de bluff frequency genérico.

#### Scenario: Flop c-bet a 65%
- **GIVEN** hero agresor preflop, flop, equity < adjustedFoldBelow, no facing bet, heads-up
- **WHEN** random < CbetFrequencyFlop (0.65)
- **THEN** retorna "Bet (C-Bet)" con sizing de bluff ajustado por SPR

#### Scenario: Turn c-bet a 45%
- **GIVEN** hero agresor, turn, equity baja, no facing bet
- **WHEN** random < CbetFrequencyTurn (0.45)
- **THEN** retorna c-bet con IsBarrel=true si previousStreetBet

#### Scenario: C-bet no aplica si facing bet
- **GIVEN** hero agresor pero villain ya apostó
- **WHEN** isFacingBet == true
- **THEN** c-bet no se evalúa (va directo a HandleFacingBet)

#### Scenario: C-bet no aplica multiway
- **GIVEN** hero agresor pero pot multiway
- **WHEN** isMultiway == true
- **THEN** c-bet no se evalúa (demasiado arriesgado multi-way)
