## MODIFIED Requirements

### Requirement: Semi-bluff debe verificar fold equity mínima
Los semi-bluffs con draws SHALL verificar que la fold equity sea suficiente para que el play sea +EV, considerando la draw equity como backup.

#### Scenario: Semi-bluff con fold equity alta — ejecutar
- **GIVEN** totalOuts es 9 (flush draw), street es Flop, !isFacingBet, !isMultiway
- **AND** foldEquity es 40%, bluffBetSize es "Bet 1/2"
- **AND** breakevenFE = 0.5/1.5 = 33%, drawEquity = 9 × 4 / 100 = 36%
- **AND** adjustedBreakevenFE = max(0, 33% - 36%) = 0%
- **WHEN** HandleLowEquity evalúa semi-bluff
- **THEN** 40% >= 0% → retorna Semi-Bluff
- **AND** razón incluye "FE=" y outs

#### Scenario: Semi-bluff sin fold equity contra calling station — no ejecutar
- **GIVEN** totalOuts es 9, street es Flop, !isFacingBet, !isMultiway
- **AND** foldEquity es 5%, bluffBetSize es "Bet 2/3"
- **AND** breakevenFE = 0.67/1.67 = 40%, drawEquity = 36%
- **AND** adjustedBreakevenFE = max(0, 40% - 36%) = 4%
- **WHEN** HandleLowEquity evalúa semi-bluff
- **THEN** 5% >= 4% → retorna Semi-Bluff (draw equity compensa)

#### Scenario: Semi-bluff con pocos outs y sin fold equity — no ejecutar
- **GIVEN** totalOuts es 4 (gutshot), street es Flop, !isFacingBet
- **AND** foldEquity es 10%, bluffBetSize es "Bet 1/2"
- **AND** breakevenFE = 33%, drawEquity = 4 × 4 / 100 = 16%
- **AND** adjustedBreakevenFE = max(0, 33% - 16%) = 17%
- **WHEN** HandleLowEquity evalúa semi-bluff
- **THEN** 10% < 17% → NO retorna Semi-Bluff
- **AND** continúa al siguiente path (call/fold)

#### Scenario: Semi-bluff combo draw con alta draw equity
- **GIVEN** totalOuts es 15 (combo draw), street es Flop
- **AND** foldEquity es 20%, bluffBetSize es "Bet 3/4"
- **AND** breakevenFE = 0.75/1.75 = 43%, drawEquity = 15 × 4 / 100 = 60%
- **AND** adjustedBreakevenFE = max(0, 43% - 60%) = 0%
- **WHEN** HandleLowEquity evalúa semi-bluff
- **THEN** 20% >= 0% → retorna Semi-Bluff (combo draw siempre justifica)

#### Scenario: Semi-bluff en turn — multiplicador de outs diferente
- **GIVEN** totalOuts es 9, street es Turn
- **AND** drawEquity = 9 × 2.17 / 100 ≈ 19.5% (solo 1 carta por venir)
- **AND** breakevenFE = 33%
- **AND** adjustedBreakevenFE = max(0, 33% - 19.5%) = 13.5%
- **WHEN** HandleLowEquity evalúa semi-bluff
- **THEN** requiere foldEquity >= 13.5% para ejecutar
