## MODIFIED Requirements

### Requirement: Propiedades stat-specific de fiabilidad
OpponentProfile SHALL exponer propiedades de fiabilidad por stat que permitan usar datos antes del threshold global de 20 manos.

#### Scenario: C-bet data fiable con 5 muestras
- **GIVEN** oponente con 8 manos jugadas, 5 oportunidades de c-bet, 5 veces faced c-bet
- **WHEN** se evalúa HasReliableCBetData
- **THEN** retorna true (>= 5 de cada) aunque IsReliable == false (< 20 manos)

#### Scenario: AF data fiable con 10 acciones postflop
- **GIVEN** oponente con 12 manos, 4 bets + 3 raises + 5 calls = 12 acciones
- **WHEN** se evalúa HasReliableAFData
- **THEN** retorna true (>= 10 acciones) aunque IsReliable == false

#### Scenario: Fold data fiable con 8 situaciones
- **GIVEN** oponente con 15 manos, 3 folds + 4 calls + 2 raises = 9 acciones facing
- **WHEN** se evalúa HasReliableFoldData
- **THEN** retorna true (9 >= 8)

### Requirement: OpponentTracker usa thresholds granulares
GetAdjustedFoldEquity SHALL usar HasReliableAFData en vez de IsReliable. GetFoldToBetPct SHALL usar HasReliableFoldData.

#### Scenario: Fold equity ajustada antes de 20 manos
- **GIVEN** oponente con 12 manos, 10+ acciones postflop, classified como LAG
- **WHEN** se llama GetAdjustedFoldEquity
- **THEN** aplica ajuste LAG ×0.70 (antes ignoraba por IsReliable == false)

#### Scenario: FoldToBetPct disponible antes
- **GIVEN** oponente con 8 acciones facing bet (3 folds + 3 calls + 2 raises)
- **WHEN** se llama GetFoldToBetPct
- **THEN** retorna 37.5% (antes retornaba -1 por < 10 hardcoded)
