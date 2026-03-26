## ADDED Requirements

### Requirement: Detección de jugador all-in en PostflopGameContext
SHALL existir un flag `IsAnyoneAllIn` en `PostflopGameContext` que indica si algún oponente ya comprometió todo su stack.

#### Scenario: Villain va all-in en flop
- **GIVEN** villain apuesta todo su stack restante en flop
- **WHEN** se detecta que `villainBet >= villainStack` (o `villainStack == 0` post-bet)
- **THEN** `PostflopGameContext.IsAnyoneAllIn = true`

#### Scenario: Nadie all-in — flag false
- **GIVEN** todos los jugadores tienen stack restante > 0
- **WHEN** se evalúa el estado del pot
- **THEN** `PostflopGameContext.IsAnyoneAllIn = false` (por defecto)

### Requirement: All-in desactiva fold equity
Cuando algún jugador está all-in, el sistema SHALL desactivar el cálculo de fold equity para ese jugador.

#### Scenario: Fold equity ignorada contra all-in
- **GIVEN** villain ya está all-in y hero considera semi-bluff
- **WHEN** se evalúa fold equity
- **THEN** `foldEquity = 0` para el jugador all-in (no puede foldear)

### Requirement: All-in desactiva reverse implied odds
Cuando el oponente está all-in, el sistema SHALL desactivar la penalización de reverse implied odds.

#### Scenario: No reverse implied odds contra all-in
- **GIVEN** villain all-in en turn, hero tiene OnePair en board con draws
- **WHEN** se calcula `reverseImpliedPenalty`
- **THEN** penalty = 0 (villain no puede apostar más, no hay implied loss)

### Requirement: All-in ajusta SPR al effective stack
Cuando hay un all-in, el SPR SHALL calcularse sobre el effective stack (mínimo entre hero y villain), no sobre el stack total del hero.

#### Scenario: SPR con villain all-in por 20bb en pot de 30bb
- **GIVEN** hero tiene 100bb, villain all-in por 20bb, pot = 30bb
- **WHEN** se calcula SPR
- **THEN** SPR = 20/30 = 0.67 (effective stack del villain, no 100/30 = 3.3)

### Requirement: Reset del flag al inicio de cada mano
El flag `IsAnyoneAllIn` SHALL resetearse a `false` en `PostflopGameContext.Reset()`.
