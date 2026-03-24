## MODIFIED Requirements

### Requirement: Ajustar DangerFlushCompletePct a 35%
El valor default de `StrategyProfile.DangerFlushCompletePct` SHALL ser 35.0 (antes 25.0) porque cuando 4+ cartas del mismo palo están en board, el villano tiene flush aproximadamente 35-40% del tiempo en rangos normales de juego.

#### Scenario: Penalty con flush completado y equity 70%
- **GIVEN** DangerFlushCompletePct es 35.0, rawEquity es 70
- **WHEN** flush completed en board
- **THEN** penalty porcentual = 70 × 0.35 = 24.5 puntos (antes era 70 × 0.25 = 17.5)

---

### Requirement: Ajustar ReverseImpliedFlushDrawPenalty a 7.0
El valor default de `StrategyProfile.ReverseImpliedFlushDrawPenalty` SHALL ser 7.0 (antes 4.0) porque con OnePair/MiddlePair facing turn bet en board con flush draw aparecido, hero paga rivers caros con frecuencia.

#### Scenario: Reverse implied odds con OnePair/MiddlePair en turn
- **GIVEN** ReverseImpliedFlushDrawPenalty es 7.0, heroHandRank es OnePair, pairClassification es MiddlePair (×1.5)
- **AND** street es Turn, isFacingBet, boardChange.FlushDrawAppeared
- **WHEN** se calcula reverse implied odds
- **THEN** base penalty = 7.0, con multiplier = 7.0 × 1.5 = 10.5 puntos (antes era 4.0 × 1.5 = 6.0)

---

### Requirement: Ajustar ReverseImpliedCoordinatedPenalty a 4.0
El valor default de `StrategyProfile.ReverseImpliedCoordinatedPenalty` SHALL ser 4.0 (antes 2.0) porque boards con DangerLevel >= 2 representan riesgo significativo para manos vulnerables.

#### Scenario: Board coordinated con DangerLevel 2 y TwoPair
- **GIVEN** ReverseImpliedCoordinatedPenalty es 4.0, heroHandRank es TwoPair
- **AND** boardChange.DangerLevel >= 2, !FlushCompleted
- **WHEN** se calcula reverse implied odds
- **THEN** penalty incluye 4.0 puntos (antes 2.0)

---

### Requirement: Ajustar BluffCatchFoldBelowMultiplier a 0.75
El valor default de `StrategyProfile.BluffCatchFoldBelowMultiplier` SHALL ser 0.75 (antes 0.85) para ser más selectivo en bluff catching y evitar calls marginales -EV.

#### Scenario: Bluff catch con FoldBelow 30%
- **GIVEN** BluffCatchFoldBelowMultiplier es 0.75, thresholds.FoldBelow es 30
- **WHEN** se calcula bluffCatchThreshold
- **THEN** threshold = 30 × 0.75 = 22.5% (antes 30 × 0.85 = 25.5%)
- **AND** hero solo llama bluff catch con equity >= 22.5% (más estricto)

---

### Requirement: Ajustar FloatingIPMinEquity a 25%
El valor default de `StrategyProfile.FloatingIPMinEquity` SHALL ser 25.0 (antes 20.0) porque con 20% equity hero pierde contra cualquier par; 25% requiere outs reales para justificar el float.

#### Scenario: Floating IP con 22% equity
- **GIVEN** FloatingIPMinEquity es 25.0, equity es 22%, hero IP, flop, bet small/medium
- **WHEN** se evalúa floating
- **THEN** floating NO se activa (22% < 25%)
- **AND** hero foldea en vez de float-call con equity insuficiente

#### Scenario: Floating IP con 27% equity
- **GIVEN** FloatingIPMinEquity es 25.0, equity es 27%, hero IP, flop, bet small/medium
- **AND** heroHandRank <= OnePair, totalOuts >= 4, equity <= FloatingIPMaxEquity
- **WHEN** se evalúa floating
- **THEN** floating se activa normalmente

---

### Requirement: Ajustar SlowPlayMinEquity a 72%
El valor default de `StrategyProfile.SlowPlayMinEquity` SHALL ser 72.0 (antes 80.0) para permitir slow play con sets en boards secos con mayor frecuencia.

#### Scenario: Slow play con set (75% equity) en flop seco
- **GIVEN** SlowPlayMinEquity es 72.0, equity es 75%, street es Flop
- **AND** boardTexture es "Dry", !isMultiway, !heroIsAggressor, heroHandRank es ThreeOfAKind
- **WHEN** se evalúa slow play
- **THEN** slow play se activa y retorna Check (antes requería 80% → no activaba)

---

### Requirement: Actualizar appsettings.json con nuevos valores
Los valores de calibración en la sección `StrategyProfile` de `appsettings.json` SHALL reflejar los nuevos defaults.

#### Scenario: Valores en appsettings.json
- **WHEN** se lee la sección StrategyProfile de appsettings.json
- **THEN** DangerFlushCompletePct es 35.0
- **AND** ReverseImpliedFlushDrawPenalty es 7.0
- **AND** ReverseImpliedCoordinatedPenalty es 4.0
- **AND** BluffCatchFoldBelowMultiplier es 0.75
- **AND** FloatingIPMinEquity es 25.0
- **AND** SlowPlayMinEquity es 72.0
