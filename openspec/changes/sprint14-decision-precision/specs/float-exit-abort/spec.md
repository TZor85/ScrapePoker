## MODIFIED Requirements

### Requirement: Float exit verifica runout antes de apostar
Cuando hero floateó en flop y el villano chequea turn, el sistema SHALL verificar si hero mejoró o si el board empeoró ANTES de ejecutar la apuesta de float exit.

#### Scenario: Float exit normal — hero no mejoró pero board es neutro
- **GIVEN** hero floateó flop con AK high, turn es un 2 (brick neutro), villano chequea
- **WHEN** se evalúa float exit
- **THEN** se ejecuta "Bet 1/2 (Float Exit)" como antes (board no empeoró)

#### Scenario: Float exit abortado — bad runout (overcard)
- **GIVEN** hero floateó flop con 87s en board Q53, turn es un A (overcard), villano chequea
- **WHEN** se evalúa float exit y `boardChange.OvercardAppeared == true`
- **THEN** retorna "Check" con razón "Float exit abortado — bad runout" en vez de Bet

#### Scenario: Float exit abortado — draw completado
- **GIVEN** hero floateó flop con JTs en board K72 (dos hearts), turn completa flush (tercer heart), villano chequea
- **WHEN** se evalúa float exit y `boardChange.FlushCompleted == true` y hero no tiene flush
- **THEN** retorna "Check" con razón "Float exit abortado — draw completado en board"

#### Scenario: Float exit con hero que mejoró — bet normal
- **GIVEN** hero floateó flop con AK en board Q73, turn es un A (hero tiene top pair)
- **WHEN** se evalúa float exit y `heroHandRank >= HandRank.OnePair`
- **THEN** retorna "Bet 1/2 (Value)" en vez de "Float Exit" (ya es value bet, no bluff)

#### Scenario: Float exit respeta multiway check
- **GIVEN** hero floateó flop pero ahora hay 2+ oponentes (alguien calleó detrás)
- **WHEN** se evalúa float exit y `isMultiway == true`
- **THEN** NO ejecuta float exit (ya bloqueado por condición existente `!isMultiway`)
