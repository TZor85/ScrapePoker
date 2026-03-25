## MODIFIED Requirements

### Requirement: Fold equity calculada con stats reales del OpponentTracker
Cuando `villainFoldToBetPct >= 0` (stats disponibles con 10+ manos), el ajuste de fold equity SHALL basarse en el porcentaje real de fold del villano en vez de multipliers fijos por OpponentType.

#### Scenario: Villain con alto fold-to-bet (fish pasivo)
- **GIVEN** villainFoldToBetPct es 70%, villainType es LP
- **WHEN** DetermineAction ajusta thresholds por oponente
- **THEN** adjustedFoldBelow se reduce en -5 (villain foldea mucho → bluffear más)
- **AND** NO usa el fallback estático de LP (-4)

#### Scenario: Villain con bajo fold-to-bet (calling station)
- **GIVEN** villainFoldToBetPct es 25%, villainType es LAG
- **WHEN** DetermineAction ajusta thresholds por oponente
- **THEN** adjustedFoldBelow sube en +4 (villain no foldea → no bluffear)
- **AND** NO usa el fallback estático de LAG (-5)

#### Scenario: Villain con fold-to-bet medio (neutral)
- **GIVEN** villainFoldToBetPct es 50%
- **WHEN** DetermineAction ajusta thresholds
- **THEN** adjustedFoldBelow se reduce en -2 (ligeramente favorable)

#### Scenario: Stats no disponibles — fallback a multipliers estáticos
- **GIVEN** villainFoldToBetPct es -1 (menos de 10 manos)
- **AND** villainType es LAG
- **WHEN** DetermineAction ajusta thresholds
- **THEN** usa fallback estático: LAG facing bet → (-5, -3)

#### Scenario: Stats y tipo Unknown — sin ajuste
- **GIVEN** villainFoldToBetPct es -1, villainType es Unknown
- **WHEN** DetermineAction ajusta thresholds
- **THEN** adjustedFoldBelow no cambia (0, 0)

#### Scenario: GetFoldToBetPct con manos suficientes
- **GIVEN** villainId "Player1" tiene 15 PostflopFacingBetCount y 10 PostflopFoldCount
- **WHEN** GetFoldToBetPct("Player1") se invoca
- **THEN** retorna 66.67

#### Scenario: GetFoldToBetPct con manos insuficientes
- **GIVEN** villainId "Player2" tiene 5 PostflopFacingBetCount
- **WHEN** GetFoldToBetPct("Player2") se invoca
- **THEN** retorna -1

#### Scenario: GetFoldToBetPct con villano desconocido
- **GIVEN** villainId "Unknown" no existe en OpponentTracker
- **WHEN** GetFoldToBetPct("Unknown") se invoca
- **THEN** retorna -1
