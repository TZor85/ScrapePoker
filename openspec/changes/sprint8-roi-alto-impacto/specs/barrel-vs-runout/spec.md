## MODIFIED Requirements

### Requirement: Double barrel debe condicionarse al runout
El double barrel SHALL evaluarse contra el `boardChange` para determinar si el runout es favorable (brick) o desfavorable (overcard, draw completado, board paired).

#### Scenario: Barrel en brick (runout favorable)
- **GIVEN** hero apostó flop como agresor, street es Turn, equity es 40% (entre FoldBelow y ValueAbove)
- **AND** boardChange.NewOvercard es false, FlushCompleted es false, StraightCompleted es false, BoardPaired es false
- **WHEN** HandleNoBet evalúa double barrel
- **THEN** retorna Barrel con razón "brick, consistencia de rango"

#### Scenario: No barrel en overcard (bad runout)
- **GIVEN** hero apostó flop como agresor en K-7-2, turn es A
- **AND** boardChange.NewOvercard es true
- **WHEN** HandleNoBet evalúa double barrel
- **THEN** NO retorna Barrel
- **AND** continúa a check (equity marginal)

#### Scenario: No barrel en flush completado
- **GIVEN** hero apostó flop, turn completa flush draw
- **AND** boardChange.FlushCompleted es true
- **WHEN** HandleNoBet evalúa double barrel
- **THEN** NO retorna Barrel

#### Scenario: No barrel en board paired
- **GIVEN** hero apostó flop, turn parea el board
- **AND** boardChange.BoardPaired es true
- **WHEN** HandleNoBet evalúa double barrel
- **THEN** NO retorna Barrel

#### Scenario: No barrel en straight completado
- **GIVEN** hero apostó flop, turn completa straight draw
- **AND** boardChange.StraightCompleted es true
- **WHEN** HandleNoBet evalúa double barrel
- **THEN** NO retorna Barrel

#### Scenario: Barrel river en brick (2nd barrel)
- **GIVEN** hero apostó flop y turn, street es River, equity marginal
- **AND** boardChange del river es brick (sin overcard, sin draws completados)
- **WHEN** HandleNoBet evalúa double barrel
- **THEN** retorna Barrel (3rd barrel, consistencia de rango)

#### Scenario: NewOvercard detection en BoardTextureAnalyzer
- **GIVEN** board anterior es [K, 7, 2], carta nueva es A
- **WHEN** AnalyzeBoardChange se ejecuta
- **THEN** boardChange.NewOvercard es true (A > K)

#### Scenario: No overcard si carta nueva es menor
- **GIVEN** board anterior es [K, 7, 2], carta nueva es 5
- **WHEN** AnalyzeBoardChange se ejecuta
- **THEN** boardChange.NewOvercard es false (5 < K)
