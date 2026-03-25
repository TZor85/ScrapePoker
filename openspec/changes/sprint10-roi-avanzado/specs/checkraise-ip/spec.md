## MODIFIED Requirements

### Requirement: Check-raise IP habilitado como trap con mano premium
El check-raise SHALL permitirse en IP con TwoPair+ en flop/turn como jugada de trap, en boards no Wet/Monotone.

#### Scenario: Check-raise IP con TwoPair en flop Dry
- **GIVEN** isInPosition es true, heroHandRank es TwoPair, street es Flop
- **AND** boardTexture es "Dry", isMultiway es false, heroIsAggressor es false
- **AND** thresholds.CanCheckRaise es true, equity > CheckRaiseThreshold
- **WHEN** HandleNoBet se evalúa
- **THEN** retorna "Check (Check-Raise)" con razón "IP trap"
- **AND** IsCheckRaise es true

#### Scenario: Check-raise IP con ThreeOfAKind en turn Coordinated
- **GIVEN** isInPosition es true, heroHandRank es ThreeOfAKind, street es Turn
- **AND** boardTexture es "Coordinated", isMultiway es false
- **WHEN** HandleNoBet se evalúa
- **THEN** retorna "Check (Check-Raise)" con razón "IP trap"

#### Scenario: NO check-raise IP con OnePair
- **GIVEN** isInPosition es true, heroHandRank es OnePair
- **WHEN** HandleNoBet se evalúa
- **THEN** NO retorna check-raise (requiere TwoPair+)

#### Scenario: NO check-raise IP en board Wet
- **GIVEN** isInPosition es true, heroHandRank es TwoPair, boardTexture es "Wet"
- **WHEN** HandleNoBet se evalúa
- **THEN** NO retorna check-raise IP (board Wet demasiado peligroso para trap)

#### Scenario: NO check-raise IP en board Monotone
- **GIVEN** isInPosition es true, heroHandRank es TwoPair, boardTexture es "Monotone"
- **WHEN** HandleNoBet se evalúa
- **THEN** NO retorna check-raise IP

#### Scenario: NO check-raise IP multiway
- **GIVEN** isInPosition es true, heroHandRank es TwoPair, isMultiway es true
- **WHEN** HandleNoBet se evalúa
- **THEN** NO retorna check-raise IP (multiway = demasiados oponentes)

#### Scenario: OOP check-raise sigue teniendo prioridad
- **GIVEN** isInPosition es false, heroHandRank es TwoPair, equity > CheckRaiseThreshold
- **WHEN** HandleNoBet se evalúa
- **THEN** retorna check-raise OOP (comportamiento existente, prioridad sobre IP)

#### Scenario: Check-raise IP no se activa si hero es agresor
- **GIVEN** isInPosition es true, heroHandRank es TwoPair, heroIsAggressor es true
- **WHEN** HandleNoBet se evalúa
- **THEN** NO retorna check-raise (agresor preflop → c-bet en vez de trap)
