# Flop DonkBet Configurations

## ADDED Requirements

### Requirement: appsettings.json SHALL include Flop_DonkBet configuration

A new `Flop_DonkBet` threshold configuration shall be added for situations where a non-aggressor villain leads with a bet on the flop. This situation currently falls back to generic thresholds (FoldBelow=40), which is suboptimal.

#### Scenario: Flop DonkBet uses specific config (not fallback)
- **GIVEN** street = Flop
- **AND** situation = DonkBet
- **WHEN** GetThresholds("Flop_DonkBet") is called
- **THEN** it SHALL return the configured thresholds (not fallback)
- **AND** FoldBelow SHALL be 32 (donk bets are often weak → call more)

#### Scenario: Flop DonkBet allows check-raise
- **GIVEN** the Flop_DonkBet configuration
- **WHEN** CanCheckRaise is checked
- **THEN** it SHALL be true
- **AND** CheckRaiseThreshold SHALL be 75
- **AND** because donk bets are often weak hands that fold to raises

#### Scenario: Flop DonkBet LowEquityAction is Call
- **GIVEN** the Flop_DonkBet configuration
- **WHEN** LowEquityAction is checked
- **THEN** it SHALL be "Call" (not "Fold")
- **AND** because donk bets often represent draws or weak pairs worth floating

#### Scenario: Flop DonkBet allows bluffs
- **GIVEN** the Flop_DonkBet configuration
- **WHEN** CanBluff is checked
- **THEN** it SHALL be true
- **AND** BluffCondition SHALL be "Always"
- **AND** BluffFrequencyMultiplier SHALL be 1.2 (bluff slightly more vs weak range)

### Requirement: appsettings.json SHALL include Flop_DonkBetVsOpenRaise configuration

A new `Flop_DonkBetVsOpenRaise` threshold configuration shall be added for situations where hero was the preflop aggressor (open raiser) and villain leads with a donk bet on the flop.

#### Scenario: Flop DonkBetVsOpenRaise uses specific config
- **GIVEN** street = Flop
- **AND** situation = DonkBetVsOpenRaise
- **WHEN** GetThresholds("Flop_DonkBetVsOpenRaise") is called
- **THEN** it SHALL return the configured thresholds (not fallback)
- **AND** FoldBelow SHALL be 38 (hero has range advantage → more selective)

#### Scenario: DonkBetVsOpenRaise FoldBelow higher than DonkBet
- **GIVEN** both Flop_DonkBet and Flop_DonkBetVsOpenRaise configs exist
- **WHEN** comparing FoldBelow values
- **THEN** DonkBetVsOpenRaise.FoldBelow (38) SHALL be > DonkBet.FoldBelow (32)
- **AND** because hero as preflop aggressor has stronger range and can fold more marginals

#### Scenario: DonkBetVsOpenRaise LowEquityAction is Fold
- **GIVEN** the Flop_DonkBetVsOpenRaise configuration
- **WHEN** LowEquityAction is checked
- **THEN** it SHALL be "Fold"
- **AND** because hero's range is strong; low equity hands should fold vs donk from weaker range

#### Scenario: Both configs have WetBoardBetSize
- **GIVEN** both new Flop configs
- **WHEN** WetBoardBetSize is checked
- **THEN** both SHALL have WetBoardBetSize = "Bet 1/3"
- **AND** MonotoneBoardBetSize = "Bet 1/4"

### Requirement: No fallback warning for DonkBet flop situations

#### Scenario: Console does not show fallback warning for Flop_DonkBet
- **GIVEN** a hand where villain donk bets on the flop
- **AND** situation resolves to DonkBet or DonkBetVsOpenRaise
- **WHEN** GetThresholds is called
- **THEN** it SHALL NOT print "[WARNING] Threshold no encontrado"
- **AND** because the configuration now exists in appsettings.json
