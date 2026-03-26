## ADDED Requirements

### Requirement: Validación de board cards en transición de estado
El overload TryTransition(GameState, int visibleBoardCards) SHALL bloquear transiciones cuando el nº de cartas visibles es menor al mínimo requerido por el state destino.

#### Scenario: FlopDetected con 3 cartas → permitido
- **GIVEN** state actual = PreflopAction, visibleBoardCards = 3
- **WHEN** se intenta TryTransition(FlopDetected, 3)
- **THEN** transición permitida (3 >= 3)

#### Scenario: FlopDetected con 2 cartas → bloqueado
- **GIVEN** state actual = PreflopAction, visibleBoardCards = 2
- **WHEN** se intenta TryTransition(FlopDetected, 2)
- **THEN** transición bloqueada, retorna false, log warning

#### Scenario: TurnDetected con 3 cartas → bloqueado
- **GIVEN** state actual = FlopAction, visibleBoardCards = 3
- **WHEN** se intenta TryTransition(TurnDetected, 3)
- **THEN** transición bloqueada (3 < 4 requeridas para turn)

#### Scenario: TurnDetected con 4+ cartas → warning si > 4
- **GIVEN** state actual = FlopAction, visibleBoardCards = 5
- **WHEN** se intenta TryTransition(TurnDetected, 5)
- **THEN** transición permitida pero log warning "Posible desfase de street"

#### Scenario: HandDetected → sin validación de cards
- **GIVEN** state actual = WaitingForHand, visibleBoardCards = 0
- **WHEN** se intenta TryTransition(HandDetected, 0)
- **THEN** transición permitida sin validación de board cards
