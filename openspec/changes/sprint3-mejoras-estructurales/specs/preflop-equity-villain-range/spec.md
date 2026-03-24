# Preflop Equity vs VillainRange

## MODIFIED Requirements

### Requirement: Preflop equity SHALL use VillainRange when available for the HandSituation

When calculating preflop equity, if a VillainRange exists for the current HandSituation (e.g., ThreeBet, FourBet, Squeeze), the system shall use Monte Carlo simulation against that range instead of the lookup table. For situations without a defined VillainRange (e.g., OpenRaise), the lookup table shall continue to be used.

#### Scenario: OpenRaise uses lookup table (no VillainRange)
- **GIVEN** handSituation = "OpenRaise"
- **AND** communityCards is empty (preflop)
- **AND** VillainRange.GetForSituation(OpenRaise) returns null
- **WHEN** CalculateEquity is called
- **THEN** equity SHALL be calculated using PreflopEquityCalculator.GetEquity (lookup table)
- **AND** MonteCarloSimulator SHALL NOT be invoked

#### Scenario: ThreeBet uses Monte Carlo vs VillainRange
- **GIVEN** handSituation = "ThreeBet"
- **AND** communityCards is empty (preflop)
- **AND** VillainRange.GetForSituation(ThreeBet) returns a non-null range (~5-8%)
- **WHEN** CalculateEquity is called
- **THEN** equity SHALL be calculated using MonteCarloSimulator with the VillainRange
- **AND** PreflopEquityCalculator SHALL NOT be invoked

#### Scenario: FourBet uses Monte Carlo vs narrow VillainRange
- **GIVEN** handSituation = "FourBet"
- **AND** communityCards is empty (preflop)
- **AND** VillainRange.GetForSituation(FourBet) returns a narrow range (~2-3%)
- **WHEN** CalculateEquity is called
- **THEN** equity SHALL be calculated using MonteCarloSimulator with the VillainRange

#### Scenario: Equity vs 3Bet range is lower than vs random for speculative hands
- **GIVEN** hero hand is JTs (jack-ten suited)
- **AND** handSituation = "ThreeBet"
- **WHEN** equity is calculated vs VillainRange (ThreeBet)
- **AND** equity is also calculated vs random (OpenRaise)
- **THEN** equity vs ThreeBet range SHALL be lower than equity vs random
- **AND** because the 3Bet range contains stronger hands (AA, KK, QQ, AK, etc.)

#### Scenario: Equity vs 3Bet range is similar for premium hands
- **GIVEN** hero hand is AA (pocket aces)
- **AND** handSituation = "ThreeBet"
- **WHEN** equity is calculated vs VillainRange (ThreeBet)
- **AND** equity is also calculated vs random (OpenRaise)
- **THEN** both equities SHALL be high (>75%)
- **AND** the difference SHALL be less than 10 percentage points

#### Scenario: No handSituation defaults to lookup table
- **GIVEN** handSituation is null
- **AND** communityCards is empty (preflop)
- **WHEN** CalculateEquity is called
- **THEN** equity SHALL be calculated using PreflopEquityCalculator.GetEquity (lookup table)

#### Scenario: Postflop equity unchanged (existing VillainRange logic)
- **GIVEN** communityCards has 3+ cards (postflop)
- **AND** any handSituation
- **WHEN** CalculateEquity is called
- **THEN** the existing postflop equity logic SHALL remain unchanged
- **AND** Monte Carlo with VillainRange SHALL continue to work as before for postflop
