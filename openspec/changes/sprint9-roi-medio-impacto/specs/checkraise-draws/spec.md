## MODIFIED Requirements

### Requirement: Check-raise habilitado para draws fuertes en flop
El check-raise SHALL aceptar combo draws (12+ outs) y flush draws (9+ outs) en flop como alternativa a TwoPair+, siempre que la equity supere `CheckRaiseDrawMinEquity`.

#### Scenario: Check-raise con flush draw en flop OOP
- **GIVEN** street es Flop, isInPosition es false, isMultiway es false
- **AND** heroHandRank es HighCard, hasFlushDraw es true, totalOuts es 9
- **AND** equity es 45%, CheckRaiseThreshold es 40%, CheckRaiseDrawMinEquity es 40%
- **AND** heroIsAggressor es false, thresholds.CanCheckRaise es true
- **WHEN** HandleNoBet evalúa check-raise
- **THEN** hasStrongDraw es true (flop + flushDraw + 9 outs + 45% >= 40%)
- **AND** retorna "Check (Check-Raise)" con razón "semi-bluff — 9 outs OOP"

#### Scenario: Check-raise con combo draw en flop OOP
- **GIVEN** street es Flop, isInPosition es false, hasComboDraw es true, totalOuts es 15
- **AND** equity es 42%, CheckRaiseDrawMinEquity es 40%
- **WHEN** HandleNoBet evalúa check-raise
- **THEN** hasStrongDraw es true (flop + comboDraw)
- **AND** retorna "Check (Check-Raise)"

#### Scenario: NO check-raise con draw en turn
- **GIVEN** street es Turn, hasFlushDraw es true, totalOuts es 9
- **AND** equity es 45%, isInPosition es false
- **WHEN** HandleNoBet evalúa check-raise
- **THEN** hasStrongDraw es false (solo flop permite check-raise draw)
- **AND** requiere TwoPair+ para check-raise en turn

#### Scenario: NO check-raise con draw IP
- **GIVEN** street es Flop, isInPosition es true, hasFlushDraw es true
- **WHEN** HandleNoBet evalúa check-raise
- **THEN** no check-raise (requiere OOP)

#### Scenario: NO check-raise con draw si equity insuficiente
- **GIVEN** street es Flop, isInPosition es false, hasFlushDraw es true, totalOuts es 9
- **AND** equity es 35%, CheckRaiseDrawMinEquity es 40%
- **WHEN** HandleNoBet evalúa check-raise
- **THEN** hasStrongDraw es false (35% < 40%)
- **AND** no check-raise (sin TwoPair+ tampoco)

#### Scenario: Check-raise con TwoPair+ sigue funcionando igual
- **GIVEN** street es Flop, heroHandRank es TwoPair, equity es 65%
- **AND** isInPosition es false, isMultiway es false, heroIsAggressor es false
- **WHEN** HandleNoBet evalúa check-raise
- **THEN** hasStrongMade es true
- **AND** retorna "Check (Check-Raise)" con razón "trap — TwoPair OOP"

#### Scenario: Check-raise draw multiway bloqueado
- **GIVEN** street es Flop, hasFlushDraw es true, isMultiway es true
- **WHEN** HandleNoBet evalúa check-raise
- **THEN** no check-raise (isMultiway bloquea)

#### Scenario: Parámetro CheckRaiseDrawMinEquity configurable
- **GIVEN** StrategyProfile con CheckRaiseDrawMinEquity es 50%
- **AND** equity es 45%, hasFlushDraw es true, street es Flop
- **WHEN** HandleNoBet evalúa check-raise
- **THEN** hasStrongDraw es false (45% < 50%)
