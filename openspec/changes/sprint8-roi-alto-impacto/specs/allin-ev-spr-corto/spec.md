## MODIFIED Requirements

### Requirement: Push/fold debe usar cálculo EV explícito, no threshold binario
Cuando SPR < 2.0 (push/fold mode), la decisión de all-in SHALL basarse en `EV(allin) > 0` en vez de `equity > ValueAbove && HandRank >= OnePair`.

#### Scenario: All-in +EV con equity marginal sin par
- **GIVEN** street es Turn, SPR es 1.2, equity es 42%, heroHandRank es HighCard, pot es 100
- **AND** isPushFold es true (SPR < 2.0)
- **WHEN** DetermineAction se evalúa
- **THEN** calcula EV(allin) = 0.42 × (100 + 2×120) - 0.58 × 120 = 73.2 > 0
- **AND** retorna "All-In (Value)" con razón que incluye EV calculado

#### Scenario: All-in -EV con equity muy baja
- **GIVEN** street es Turn, SPR es 1.2, equity es 15%, heroHandRank es HighCard, pot es 100
- **AND** isPushFold es true
- **WHEN** DetermineAction se evalúa
- **THEN** calcula EV(allin) = 0.15 × (100 + 240) - 0.85 × 120 = -51 < 0
- **AND** NO retorna All-In
- **AND** continúa al siguiente path de decisión

#### Scenario: All-in +EV facing bet con SPR corto
- **GIVEN** street es River, SPR es 0.8, equity es 38%, isFacingBet es true, pot es 200
- **AND** isPushFold es true
- **WHEN** DetermineAction se evalúa
- **THEN** calcula EV(allin) con stack efectivo
- **AND** si EV > 0 → retorna "All-In (Value)"

#### Scenario: SPR >= 2.0 no activa push/fold EV
- **GIVEN** street es Turn, SPR es 3.0, equity es 42%
- **AND** isPushFold es false
- **WHEN** DetermineAction se evalúa
- **THEN** NO calcula EV(allin)
- **AND** usa lógica normal de thresholds

#### Scenario: All-in +EV sin facing bet (polarizado)
- **GIVEN** street es Turn, SPR es 1.0, equity es 50%, isFacingBet es false, pot es 150
- **AND** isPushFold es true
- **WHEN** DetermineAction se evalúa
- **THEN** calcula EV(allin) = 0.50 × (150 + 300) - 0.50 × 150 = 150 > 0
- **AND** retorna "All-In (Value)" con razón "Push +EV"
