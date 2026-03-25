## NEW Requirements

### Requirement: Turn call con flush danger → river check si draw completa
Cuando hero call turn con board peligroso (3+ same suit), si river completa el flush, hero SHALL check en vez de value bet.

#### Scenario: Turn call con 3 hearts → river 4th heart → check
- **GIVEN** hero calleó turn con TurnCalledWithFlushDanger = true
- **AND** river boardChange.FlushCompleted es true
- **WHEN** HandleNoBet evalúa river
- **THEN** retorna "Check — draw completó en river, hero calleó turn con peligro"

#### Scenario: Turn call con 3 hearts → river brick → bet normal
- **GIVEN** hero calleó turn con TurnCalledWithFlushDanger = true
- **AND** river boardChange.FlushCompleted es false
- **WHEN** HandleNoBet evalúa river
- **THEN** bet value normal (draw no completó)

#### Scenario: Turn raise (no call) → river flush completado → no afecta
- **GIVEN** TurnCalledWithFlushDanger es false (hero raiseó turn)
- **AND** river flush completado
- **WHEN** HandleNoBet evalúa river
- **THEN** usa lógica normal (no aplica el plan de turn-call)
