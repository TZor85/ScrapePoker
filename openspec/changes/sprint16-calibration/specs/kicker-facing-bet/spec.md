## ADDED Requirements

### Requirement: Kicker quality ajusta threshold de call con OnePair
Cuando hero tiene OnePair y está facing bet, el kicker strength SHALL ajustar el threshold de fold.

#### Scenario: TPTK → más fácil de call
- **GIVEN** hero con OnePair, heroKickerStrength=Strong, facing bet
- **WHEN** se calcula adjustedFoldBelow
- **THEN** adjustedFoldBelow -= KickerStrongEquityBonus (3.0) → hero calla más fácil

#### Scenario: TPWK OOP → más difícil de call
- **GIVEN** hero con OnePair, heroKickerStrength=Weak, OOP, facing bet
- **WHEN** se calcula adjustedFoldBelow
- **THEN** adjustedFoldBelow += KickerWeakEquityPenalty (2.0) → hero más cauto

#### Scenario: TPWK IP → sin penalización
- **GIVEN** hero con OnePair, heroKickerStrength=Weak, IP, facing bet
- **WHEN** se calcula adjustedFoldBelow
- **THEN** sin ajuste (posición compensa kicker débil)

#### Scenario: TwoPair+ → no aplica
- **GIVEN** hero con TwoPair, facing bet
- **WHEN** se evalúa kicker adjustment
- **THEN** sin ajuste (kicker irrelevante con manos fuertes)
