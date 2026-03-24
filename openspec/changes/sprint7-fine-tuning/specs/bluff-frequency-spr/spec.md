# Bluff Frequency SPR Modulation

## MODIFIED Requirements

### Requirement: Bluff breakeven fold equity SHALL be modulated by SPR

When hero attempts a pure bluff, the breakeven fold equity threshold shall be adjusted by an SPR-dependent multiplier. Short stacks (SPR < 2) reduce bluff profitability (multiplier 0.5 → higher FE required). Deep stacks (SPR > 4) increase bluff profitability (multiplier 1.2 → lower FE required).

#### Scenario: SPR short (< 2) — bluff requires more fold equity
- **GIVEN** SPR = 1.5 (heroStack=300, potSize=200)
- **AND** thresholds.BluffBetSize = "Bet 1/3" (breakeven FE = 25%)
- **AND** BluffSPRShortMultiplier = 0.5
- **WHEN** HandleLowEquity evaluates a bluff
- **THEN** adjusted breakeven FE SHALL be 25% / 0.5 = 50%
- **AND** bluff SHALL only execute if actualFoldEquity >= 50%
- **AND** because short stacks make bluffs less profitable

#### Scenario: SPR deep (> 4) — bluff requires less fold equity
- **GIVEN** SPR = 5.0 (heroStack=1000, potSize=200)
- **AND** thresholds.BluffBetSize = "Bet 1/3" (breakeven FE = 25%)
- **AND** BluffSPRDeepMultiplier = 1.2
- **WHEN** HandleLowEquity evaluates a bluff
- **THEN** adjusted breakeven FE SHALL be 25% / 1.2 ≈ 20.8%
- **AND** bluff SHALL execute if actualFoldEquity >= 20.8%

#### Scenario: SPR normal (2-4) — no adjustment
- **GIVEN** SPR = 3.0
- **WHEN** HandleLowEquity evaluates a bluff
- **THEN** breakeven FE SHALL NOT be adjusted (multiplier = 1.0)
- **AND** standard fold equity check applies

#### Scenario: No stack info (heroStack = 0) — no adjustment
- **GIVEN** heroStack = 0 or potSize = 0
- **WHEN** HandleLowEquity evaluates a bluff
- **THEN** sprBluffMultiplier SHALL be 1.0
- **AND** no SPR adjustment applied

#### Scenario: SPR short blocks marginal bluffs
- **GIVEN** SPR = 1.5 (short)
- **AND** actualFoldEquity = 30%
- **AND** base breakeven = 25%, adjusted = 50%
- **WHEN** HandleLowEquity evaluates a bluff
- **THEN** bluff SHALL NOT execute (30% < 50%)
- **AND** result SHALL be "Check"
