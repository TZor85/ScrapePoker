# Adaptive Monte Carlo Iterations

## MODIFIED Requirements

### Requirement: Monte Carlo iterations SHALL adapt based on decision clarity

When equity is clearly high or low (based on cached results or preflop lookup), fewer iterations shall be used to reduce computation time. Marginal decisions keep full iterations for precision.

#### Scenario: Cached equity > 75% — use 500 iterations
- **GIVEN** equity cache contains an entry with equity = 82% for the current hand
- **WHEN** GetAdaptiveIterations is called
- **THEN** iterations SHALL be 500
- **AND** because the decision is clearly a value bet, precision is less critical

#### Scenario: Cached equity < 25% — use 500 iterations
- **GIVEN** equity cache contains an entry with equity = 18% for the current hand
- **WHEN** GetAdaptiveIterations is called
- **THEN** iterations SHALL be 500

#### Scenario: Cached equity 65-75% — use 750 iterations
- **GIVEN** equity cache contains an entry with equity = 68% for the current hand
- **WHEN** GetAdaptiveIterations is called
- **THEN** iterations SHALL be 750

#### Scenario: Cached equity 35-65% (marginal) — use 1000 iterations
- **GIVEN** equity cache contains an entry with equity = 50% for the current hand
- **WHEN** GetAdaptiveIterations is called
- **THEN** iterations SHALL be 1000 (DefaultMonteCarloIterations)
- **AND** because marginal decisions require maximum precision

#### Scenario: No cached equity — use 1000 iterations (default)
- **GIVEN** equity cache has no entry for the current hand
- **AND** hand is postflop (no preflop lookup available)
- **WHEN** GetAdaptiveIterations is called
- **THEN** iterations SHALL be 1000

#### Scenario: Preflop with no cache — use lookup for estimation
- **GIVEN** equity cache has no entry for the current hand
- **AND** hand is preflop (communityCards.Count == 0)
- **WHEN** GetAdaptiveIterations is called
- **THEN** it SHALL use PreflopEquityCalculator.GetEquity as rough estimate
- **AND** if rough equity > 75% or < 25%, use 500 iterations
