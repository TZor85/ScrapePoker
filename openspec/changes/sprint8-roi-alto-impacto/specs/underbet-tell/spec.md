## NEW Requirements

### Requirement: Detectar y explotar underbet del villano
Bets < 15% del pot SHALL clasificarse como `BetSizeCategory.Underbet` con tratamiento diferenciado: penalty reducida y raise oportunístico.

#### Scenario: Clasificación de underbet
- **GIVEN** pot es 100, villano apuesta 10 (10% del pot)
- **WHEN** se clasifica bet size
- **THEN** retorna `BetSizeCategory.Underbet`

#### Scenario: Bet 16% del pot no es underbet
- **GIVEN** pot es 100, villano apuesta 16
- **WHEN** se clasifica bet size
- **THEN** retorna `BetSizeCategory.Small` (no Underbet)

#### Scenario: Facing bet penalty con underbet
- **GIVEN** villainBetSize es Underbet
- **WHEN** se calcula facingBetPenalty
- **THEN** penalty es 0 (sin ajuste al FoldBelow)
- **AND** adjustedFoldBelow no sube por la bet

#### Scenario: Raise vs underbet con equity buena
- **GIVEN** villainBetSize es Underbet, equity es 50%, ThinValueAbove es 45%
- **WHEN** HandleFacingBet se evalúa
- **THEN** retorna "Raise 3x (Value)" con razón "villano muestra debilidad"

#### Scenario: No raise vs underbet con equity baja
- **GIVEN** villainBetSize es Underbet, equity es 30%, ThinValueAbove es 45%
- **WHEN** HandleFacingBet se evalúa
- **THEN** NO retorna Raise
- **AND** sigue lógica normal (Call o Fold según equity)

#### Scenario: Underbet en sizing tell (no escalación)
- **GIVEN** street es Turn, villainBetSizeFlop es Small, villainBetSize (Turn) es Underbet
- **WHEN** se evalúa sizing tell escalation
- **THEN** Underbet < Small → no es escalación → no penalty extra
