## NEW Requirements

### Requirement: Float IP debe tener exit strategy en turn
Cuando hero floatea en flop (call con aire + posición), el motor SHALL reconocer en turn que fue un float y apostar automáticamente si villain chequea.

#### Scenario: Float exit — villain chequea turn después de float
- **GIVEN** hero floateó en flop (IsFloating = true), street es Turn
- **AND** isFacingBet es false (villain checkeó), isMultiway es false
- **AND** heroFloatedFlop es true
- **WHEN** HandleNoBet se evalúa
- **THEN** retorna "Bet 1/2 (Float Exit)" con razón "exit strategy del float IP"

#### Scenario: No float exit si villain apuesta turn
- **GIVEN** hero floateó en flop, street es Turn
- **AND** isFacingBet es true (villain apostó)
- **WHEN** DetermineAction se evalúa
- **THEN** NO activa float exit
- **AND** procesa como HandleFacingBet normal

#### Scenario: No float exit si multiway
- **GIVEN** hero floateó en flop, street es Turn
- **AND** isFacingBet es false, isMultiway es true
- **WHEN** HandleNoBet se evalúa
- **THEN** NO activa float exit (demasiado arriesgado multiway)

#### Scenario: No float exit si no floateó
- **GIVEN** hero NO floateó en flop (heroFloatedFlop = false), street es Turn
- **AND** isFacingBet es false
- **WHEN** HandleNoBet se evalúa
- **THEN** NO activa float exit
- **AND** sigue lógica normal

#### Scenario: Float exit respeta ajuste SPR
- **GIVEN** heroFloatedFlop es true, street es Turn, SPR es 1.5
- **AND** isFacingBet es false, isMultiway es false
- **WHEN** HandleNoBet se evalúa
- **THEN** retorna float exit con sizing ajustado por SPR (IncreaseBetSize si SPR < 2)

#### Scenario: Propagación IsFloating de flop a turn
- **GIVEN** decisión flop retorna IsFloating = true
- **WHEN** se procesa resultado flop en game loop
- **THEN** _postflopContext.HeroFloatedFlop se marca como true
- **AND** en llamada turn DetermineAction, heroFloatedFlop = true

#### Scenario: Float exit tiene prioridad sobre probe bet pero no sobre check-raise
- **GIVEN** heroFloatedFlop es true, street es Turn, villain checkeó
- **AND** condiciones de check-raise se cumplen (OOP, TwoPair+, etc.)
- **WHEN** HandleNoBet se evalúa
- **THEN** check-raise tiene prioridad sobre float exit
- **GIVEN** heroFloatedFlop es true, condiciones de probe bet se cumplen
- **WHEN** HandleNoBet se evalúa
- **THEN** float exit tiene prioridad sobre probe bet
