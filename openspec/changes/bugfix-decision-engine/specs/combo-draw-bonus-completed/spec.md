## MODIFIED Requirements

### Requirement: Combo draw bonus no debe aplicarse cuando hero completó el draw
El bonus de combo draw (`ComboDrawEquityBonus`) SHALL aplicarse solo cuando hero tiene draws pendientes de completar. Si hero ya completó el draw (HandRank >= Straight), el bonus es redundante y no debe sumarse a la equity efectiva.

#### Scenario: Combo draw con draws pendientes en flop
- **GIVEN** hasComboDraw es true, street es Flop, heroHandRank es OnePair
- **WHEN** se calcula effectiveEquity
- **THEN** se suma ComboDrawEquityBonus (+6.0) a effectiveEquity

#### Scenario: Combo draw con draws pendientes en turn
- **GIVEN** hasComboDraw es true, street es Turn, heroHandRank es TwoPair
- **WHEN** se calcula effectiveEquity
- **THEN** se suma ComboDrawEquityBonus (+6.0) a effectiveEquity

#### Scenario: Hero completó straight (draw ya no pendiente)
- **GIVEN** hasComboDraw es true, street es Turn, heroHandRank es Straight
- **WHEN** se calcula effectiveEquity
- **THEN** NO se suma ComboDrawEquityBonus
- **AND** effectiveEquity se mantiene sin bonus

#### Scenario: Hero completó flush
- **GIVEN** hasComboDraw es true, street es Turn, heroHandRank es Flush
- **WHEN** se calcula effectiveEquity
- **THEN** NO se suma ComboDrawEquityBonus

#### Scenario: River nunca aplica combo draw bonus
- **GIVEN** hasComboDraw es true, street es River, heroHandRank es OnePair
- **WHEN** se calcula effectiveEquity
- **THEN** NO se suma ComboDrawEquityBonus (river = no más cartas)

#### Scenario: Sin combo draw
- **GIVEN** hasComboDraw es false, street es Flop
- **WHEN** se calcula effectiveEquity
- **THEN** NO se suma ComboDrawEquityBonus independientemente de heroHandRank
