## MODIFIED Requirements

### Requirement: Thin value en river bloqueado cuando draws completados
En river, el thin value bet SHALL bloquearse cuando el board tiene flush o straight completado y hero no posee el draw completado.

#### Scenario: No thin value river con flush completado y hero sin flush
- **GIVEN** street es River, equity es 48% (> ThinValueAbove 45)
- **AND** boardChange.FlushCompleted es true
- **AND** heroHandRank es OnePair (no tiene flush)
- **WHEN** HandleNoBet evalúa thin value
- **THEN** retorna "Check — thin value peligroso, draw completado en river"
- **AND** NO apuesta

#### Scenario: Thin value river con flush completado pero hero TIENE flush
- **GIVEN** street es River, equity es 48%
- **AND** boardChange.FlushCompleted es true
- **AND** heroHandRank es Flush
- **WHEN** HandleNoBet evalúa thin value
- **THEN** retorna bet thin value normalmente (hero tiene el draw)

#### Scenario: No thin value river con straight completado y hero sin straight
- **GIVEN** street es River, equity es 47%
- **AND** boardChange.StraightCompleted es true
- **AND** heroHandRank es TwoPair (no tiene straight)
- **WHEN** HandleNoBet evalúa thin value
- **THEN** retorna "Check" (draw completado peligroso)

#### Scenario: Thin value river con straight completado y hero TIENE straight
- **GIVEN** street es River, equity es 47%
- **AND** boardChange.StraightCompleted es true
- **AND** heroHandRank es Straight
- **WHEN** HandleNoBet evalúa thin value
- **THEN** retorna bet thin value normalmente

#### Scenario: Thin value river sin draws completados — comportamiento normal
- **GIVEN** street es River, equity es 48%, boardChange sin draws completados
- **WHEN** HandleNoBet evalúa thin value
- **THEN** retorna bet thin value IP o check OOP según ThinValueIPOnly

#### Scenario: Thin value flop/turn no afectado (solo river)
- **GIVEN** street es Turn, equity es 48%, boardChange.FlushCompleted es true
- **WHEN** HandleNoBet evalúa thin value
- **THEN** retorna bet thin value normalmente (guard solo aplica en river)
