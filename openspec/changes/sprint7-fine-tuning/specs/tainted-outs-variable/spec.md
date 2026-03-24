# Tainted Outs Variable Discount

## MODIFIED Requirements

### Requirement: Tainted outs discount SHALL vary based on hero's draw strength

When hero has a strong draw (flush draw), tainted outs are discounted at a higher rate (0.7) because hero's improvement is likely stronger than villain's. When hero has weaker draws (pair/trips potential), tainted outs are discounted at a lower rate (0.3) because villain's improvement may dominate.

#### Scenario: Hero with flush draw — strong discount (0.7)
- **GIVEN** hero has a flush draw (hasFlushDraw = true)
- **AND** result.TaintedOuts = 4
- **AND** result.CleanOuts = 9
- **WHEN** EffectiveOuts is calculated
- **THEN** EffectiveOuts SHALL be 9 + (4 × 0.7) = 11.8
- **AND** because flush beats most villain improvements (trips, two pair)

#### Scenario: Hero without flush draw — weak discount (0.3)
- **GIVEN** hero does NOT have a flush draw (hasFlushDraw = false)
- **AND** result.TaintedOuts = 4
- **AND** result.CleanOuts = 6
- **WHEN** EffectiveOuts is calculated
- **THEN** EffectiveOuts SHALL be 6 + (4 × 0.3) = 7.2
- **AND** because hero's pair/trips may lose to villain's flush/straight completion

#### Scenario: Strong discount yields more EffectiveOuts than weak
- **GIVEN** same TaintedOuts and CleanOuts
- **WHEN** comparing flush draw (0.7) vs no flush draw (0.3)
- **THEN** flush draw EffectiveOuts SHALL be higher
- **AND** TaintedOutsDiscountHeroStrong (0.7) > TaintedOutsDiscountHeroWeak (0.3)

#### Scenario: Zero tainted outs — no difference
- **GIVEN** result.TaintedOuts = 0
- **WHEN** EffectiveOuts is calculated
- **THEN** EffectiveOuts SHALL equal CleanOuts regardless of draw type

#### Scenario: Original TaintedOutsDiscount preserved as property
- **GIVEN** StrategyProfile with TaintedOutsDiscount = 0.5
- **WHEN** the profile is loaded
- **THEN** TaintedOutsDiscount SHALL still exist (backward compatibility)
- **AND** TaintedOutsDiscountHeroStrong SHALL default to 0.7
- **AND** TaintedOutsDiscountHeroWeak SHALL default to 0.3
