# Preflop Monte Carlo Cache

## MODIFIED Requirements

### Requirement: Preflop MC equity with VillainRange SHALL be cached

When preflop equity is calculated via Monte Carlo (because a VillainRange exists for the HandSituation), the result shall be cached by `(hand, situation, numOpponents)`. Subsequent calls with the same key shall return the cached value without re-running Monte Carlo.

#### Scenario: First preflop MC calculation — cache miss
- **GIVEN** hero has AhKs, situation = ThreeBet, numOpponents = 1
- **AND** VillainRange exists for ThreeBet
- **AND** no cached equity exists for this key
- **WHEN** CalculateEquity is called
- **THEN** MonteCarloSimulator SHALL execute with VillainRange
- **AND** the result SHALL be stored in _equityCache with key "preflop|AK|ThreeBet|1"

#### Scenario: Second preflop MC calculation — cache hit
- **GIVEN** hero has AhKs, situation = ThreeBet, numOpponents = 1
- **AND** cached equity exists for key "preflop|AK|ThreeBet|1" = 42.5%
- **WHEN** CalculateEquity is called
- **THEN** MonteCarloSimulator SHALL NOT execute
- **AND** the result SHALL be 42.5% (from cache)

#### Scenario: Different hand same situation — cache miss
- **GIVEN** hero has QsJd, situation = ThreeBet, numOpponents = 1
- **AND** cached equity exists for "preflop|AK|ThreeBet|1" but NOT for "preflop|QJ|ThreeBet|1"
- **WHEN** CalculateEquity is called
- **THEN** MonteCarloSimulator SHALL execute for QJ
- **AND** result SHALL be cached separately

#### Scenario: OpenRaise without VillainRange — uses lookup table (no MC)
- **GIVEN** hero has AhKs, situation = OpenRaise
- **AND** VillainRange does NOT exist for OpenRaise
- **WHEN** CalculateEquity is called
- **THEN** PreflopEquityCalculator.GetEquity SHALL be used (lookup table)
- **AND** MonteCarloSimulator SHALL NOT execute

#### Scenario: Cache eviction at max size
- **GIVEN** _equityCache has 512 entries (EquityCacheMaxSize)
- **WHEN** a new entry needs to be cached
- **THEN** the cache SHALL be cleared before adding the new entry
- **AND** EquityCacheMaxSize SHALL be 512 (increased from 256)
