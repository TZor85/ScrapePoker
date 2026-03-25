## MODIFIED Requirements

### Requirement: Bluff catch en river debe ajustarse por runout
El bluff catch threshold en river SHALL modularse por el tipo de carta que cayó: brick (villain falló draw) o scare card (draw completado).

#### Scenario: Bluff catch river brick — call más amplio
- **GIVEN** street es River, isRiverBluffCatch es true
- **AND** boardChange: FlushCompleted=false, StraightCompleted=false, BoardPaired=false, OvercardAppeared=false
- **WHEN** se calcula bluffCatchThreshold
- **THEN** threshold se multiplica por 0.85 (BluffCatchBrickRunoutMultiplier)
- **AND** hero call con equity más baja que sin ajuste

#### Scenario: Bluff catch river flush completado — fold más
- **GIVEN** street es River, isRiverBluffCatch es true
- **AND** boardChange.FlushCompleted es true
- **WHEN** se calcula bluffCatchThreshold
- **THEN** threshold se multiplica por 1.15 (BluffCatchScareRunoutMultiplier)
- **AND** hero necesita más equity para call

#### Scenario: Bluff catch river straight completado — fold más
- **GIVEN** boardChange.StraightCompleted es true
- **WHEN** se calcula bluffCatchThreshold
- **THEN** threshold se multiplica por 1.15

#### Scenario: Bluff catch turn — no afectado por runout
- **GIVEN** street es Turn, isTurnBluffCatch es true
- **WHEN** se calcula bluffCatchThreshold
- **THEN** runout multiplier NO se aplica (solo river)

#### Scenario: Bluff catch river overcard — no es brick
- **GIVEN** boardChange: OvercardAppeared=true, otros false
- **WHEN** se evalúa si es brick
- **THEN** NO es brick (overcard appeared) → sin bonus de brick
