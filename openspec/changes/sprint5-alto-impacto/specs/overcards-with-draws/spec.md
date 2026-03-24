# Overcards with Draws

## MODIFIED Requirements

### Requirement: OutsCalculator SHALL count overcards even when draws are present

Overcards shall be counted as outs regardless of whether the hero has a flush draw, OESD, or other main draw. However, overcard ranks that are already counted as straight completing ranks shall NOT be double-counted.

#### Scenario: AK on 9-8-7 — overcards added to OESD
- **GIVEN** hero has Ace and King (ranks 14, 13)
- **AND** board is 9-8-7 (ranks 9, 8, 7)
- **AND** hero has OESD (straight completing ranks: 6, Q → 8 outs)
- **WHEN** OutsCalculator calculates outs
- **THEN** CleanOuts SHALL include 6 overcard outs (A=3, K=3)
- **AND** because A and K are NOT straight completing ranks (6 and Q are)
- **AND** total outs SHALL be >= 14 (8 straight + 6 overcards)

#### Scenario: AK on T-9-8 — Ace not double-counted
- **GIVEN** hero has Ace and King (ranks 14, 13)
- **AND** board is T-9-8 (ranks 10, 9, 8)
- **AND** hero has OESD (straight completing ranks include 7 and J)
- **WHEN** OutsCalculator calculates outs
- **THEN** A and K SHALL be counted as overcards (neither is a straight completing rank)
- **AND** total overcard outs SHALL be 6 (A=3, K=3)

#### Scenario: KQ on A-J-T — K is straight completing
- **GIVEN** hero has King and Queen (ranks 13, 12)
- **AND** board is A-J-T (ranks 14, 11, 10)
- **AND** hero has gutshot (K completes A-K-Q-J-T straight)
- **WHEN** OutsCalculator calculates outs
- **THEN** K SHALL NOT be counted as overcard (already a straight completing rank)
- **AND** Q SHALL NOT be counted as overcard (Q < A, not an overcard)

#### Scenario: AQ on 8-5-3 rainbow — no draw, overcards only
- **GIVEN** hero has Ace and Queen (ranks 14, 12)
- **AND** board is 8-5-3 (ranks 8, 5, 3) rainbow
- **AND** no flush draw, no straight draw (hasMainDraw = false)
- **WHEN** OutsCalculator calculates outs
- **THEN** CleanOuts SHALL include 6 overcard outs (A=3, Q=3)
- **AND** this is the existing behavior, preserved

#### Scenario: Hero with made hand (flush) — no overcards
- **GIVEN** hero has Ah Kh
- **AND** board is Qh 7h 2h (hero has made flush)
- **AND** hasMadeHand = true
- **WHEN** OutsCalculator calculates outs
- **THEN** overcards SHALL NOT be counted
- **AND** because hero already has a made hand
