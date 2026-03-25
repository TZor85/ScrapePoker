## MODIFIED Requirements

### Requirement: River value bet sizing reducido en board con flush/straight posible
En river, cuando board tiene flush draw (3+ same suit) o straight completable y hero no bloquea, el sizing SHALL reducirse un nivel.

#### Scenario: River value bet con 3 same suit, hero sin blocker → sizing menor
- **GIVEN** street es River, boardChange.FlushDrawAppeared es true, heroBlocksDangerSuit es false
- **AND** equity > ValueAbove, heroHandRank es OnePair
- **WHEN** HandleNoBet evalúa value bet
- **THEN** sizing se reduce un nivel (ej: 3/4 → 1/2, 1/2 → 1/3)

#### Scenario: River value bet sin flush draw → sizing normal
- **GIVEN** street es River, boardChange sin flush draw
- **WHEN** HandleNoBet evalúa value bet
- **THEN** sizing normal (sin reducción)

#### Scenario: River value bet con flush draw pero hero bloquea → sizing normal
- **GIVEN** boardChange.FlushDrawAppeared true, heroBlocksDangerSuit true
- **WHEN** HandleNoBet evalúa value bet
- **THEN** sizing normal (blocker compensa el peligro)

### Requirement: River sizing merged (OnePair) vs polarizado (TwoPair+)
OnePair en river SHALL usar sizing menor (merged range), TwoPair+ SHALL usar sizing normal o mayor (polarizado).

#### Scenario: River OnePair → sizing reducido (merged)
- **GIVEN** street es River, heroHandRank es OnePair, equity > ValueAbove
- **WHEN** HandleNoBet evalúa value bet
- **THEN** sizing reducido un nivel (merged: extrae valor de peores sin over-commit)

#### Scenario: River ThreeOfAKind+ → sizing normal/mayor (polarizado)
- **GIVEN** street es River, heroHandRank es ThreeOfAKind
- **WHEN** HandleNoBet evalúa value bet
- **THEN** sizing normal o aumentado (polarizado: máximo valor)
