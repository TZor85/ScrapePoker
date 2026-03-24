# Sprint 6 — Tareas de Implementación

## Tasks

### 1. S6.1 — VillainCheckedMiddleStreet en PostflopGameContext
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs`
- **Acción:** Agregar `public bool VillainCheckedMiddleStreet { get; set; }`. En `UpdateTurnState`, detectar patrón bet-check: `VillainCheckedMiddleStreet = VillainBetFlop && !villainBet;`. En `Reset()`: `VillainCheckedMiddleStreet = false;`.
- **Verificación:** Compila.

### 2. S6.1 — Nuevo parámetro VillainBetCheckBetPenalty
- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Agregar `public double VillainBetCheckBetPenalty { get; set; } = 2.0;` junto a `VillainBarrelFoldIncrease`.
- **Verificación:** Compila.

### 3. S6.1 — Barrel vs bet-check-bet en PostflopDecisionService
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Agregar parámetro `bool villainCheckedMiddleStreet = false` a `DetermineAction`. En el bloque de `villainBarreling`, distinguir: si `villainCheckedMiddleStreet` → usar `VillainBetCheckBetPenalty` (solo FoldBelow, no ThinValue); sino → barrel normal.
- **Verificación:** Test: bet-check-bet penalty < barrel penalty.

### 4. S6.1 — Propagar VillainCheckedMiddleStreet desde FrmMain
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En la llamada a `DetermineAction` del river, agregar `villainCheckedMiddleStreet: _postflopContext.VillainCheckedMiddleStreet`.
- **Verificación:** Build OK.

### 5. S6.2 — Nuevo parámetro ReverseImpliedBlockerReduction
- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Agregar `public double ReverseImpliedBlockerReduction { get; set; } = 0.5;` junto a `ReverseImpliedOnePairMultiplier`.
- **Verificación:** Compila.

### 6. S6.2 — Blocker en CalculateReverseImpliedOdds
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/ImpliedOddsCalculator.cs`
- **Acción:** Agregar `bool heroBlocksDangerSuit = false` como último parámetro de `CalculateReverseImpliedOdds`. Antes del return, si `heroBlocksDangerSuit && penalty > 0` → `penalty *= profile.ReverseImpliedBlockerReduction`.
- **Verificación:** Test: con blocker penalty < sin blocker.

### 7. S6.2 — Propagar heroBlocksDangerSuit a CalculateReverseImpliedOdds
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En `CalculateReverseImpliedOdds` (wrapper y la llamada interna), propagar `heroBlocksDangerSuit`. El valor ya está disponible en `DetermineAction`.
- **Verificación:** Build OK.

### 8. S6.3 — Nuevos parámetros blocker granular
- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Agregar `DangerNutBlockerReduction = 0.35`, `DangerNonNutBlockerReduction = 0.55`, `DangerBlockerBoard4FlushReduction = 0.7` junto a `DangerHeroBlocksReduction`.
- **Verificación:** Compila.

### 9. S6.3 — Blocker granular en DangerPenaltyCalculator
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs`
- **Acción:** Agregar `bool heroHasNutBlocker = false` como parámetro. Si `heroBlocksDangerSuit`, usar el parámetro granular: nut blocker → `DangerNutBlockerReduction`, non-nut → `DangerNonNutBlockerReduction`. Si `CompletedFlushSuit` en board tiene 4+ cartas → `DangerBlockerBoard4FlushReduction` (siempre). Mantener `DangerHeroBlocksReduction` como fallback.
- **Verificación:** Test: nut blocker penalty < non-nut < sin blocker.

### 10. S6.3 — Calcular heroHasNutBlocker en FrmMain
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Donde se calcula `heroBlocks`, agregar: `bool heroHasNutBlocker = heroBlocks && (heroCard1Rank == 14 && heroCard1Suit == completedSuit || heroCard2Rank == 14 && heroCard2Suit == completedSuit)`. Propagar a `CalculateDangerPenalty`.
- **Verificación:** Build OK.

### 11. S6.4 — Nuevo campo ProbeBetIPSize en StreetThresholds
- **Archivo:** `src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs`
- **Acción:** Agregar `public string ProbeBetIPSize { get; init; } = "Bet 1/2";` junto a `ProbeBetSize`.
- **Verificación:** Compila.

### 12. S6.4 — Probe bet IP en PostflopDecisionService
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Eliminar `!isInPosition` de la condición de probe bet. Usar `ProbeBetIPSize` cuando IP, `ProbeBetSize` cuando OOP.
- **Verificación:** Test: probe bet IP con sizing "Bet 1/2". Probe bet OOP con sizing "Bet 1/3".

### 13. S6.5 — Slowplay turn en PostflopDecisionService
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Agregar `OpponentType villainType = OpponentType.Unknown` a `HandleNoBet`. Extender condición de slowplay: permitir turn con villainType LAG o Unknown. Propagar `villainType` desde `DetermineAction` al call de `HandleNoBet`.
- **Verificación:** Test: slowplay turn con LAG → Check. Slowplay turn con TP → no slowplay.

### 14. S6.4 — ProbeBetIPSize en appsettings.json
- **Archivo:** `src/OpenScrape.App/appsettings.json`
- **Acción:** Agregar `"ProbeBetIPSize": "Bet 1/2"` en thresholds que tienen `CanProbeBet: true`.
- **Verificación:** App carga sin warnings.

### 15. Tests nuevos
- **Archivo:** `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs`
  - **S6.1**: Barrel (bet-bet) → +5 FoldBelow. Bet-check-bet → +2 FoldBelow.
  - **S6.4**: Probe bet IP → "Bet 1/2". Probe bet OOP → "Bet 1/3".
  - **S6.5**: Slowplay turn Dry ThreeOfAKind+ LAG → Check. Turn con TP → no slowplay.
- **Archivo:** `OpenScrape.App.Tests/DangerPenaltyCalculatorTests.cs`
  - **S6.3**: Nut blocker penalty < non-nut < sin blocker.
- **Archivo:** `OpenScrape.App.Tests/ImpliedOddsCalculatorTests.cs`
  - **S6.2**: Reverse implied con blocker < sin blocker.
- **Verificación:** `dotnet test OpenScrape.sln` — todos pasan.

### 16. Build y test final
- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
