# Sprint 9 — Tareas de Implementación

## Tasks

### 1. S9.1 — OpponentTracker: GetFoldToBetPct + tracking
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs`
- **Acción:**
  - Agregar campos a `OpponentProfile`: `PostflopFacingBetCount`, `PostflopFoldCount`
  - En `RecordPostflopAction`: incrementar `PostflopFacingBetCount` cuando villano enfrenta bet. Incrementar `PostflopFoldCount` cuando acción es Fold.
  - Crear método `GetFoldToBetPct(string villainId) → double` (retorna -1 si < 10 manos)
- **Verificación:** Test: villain con 15 facing bets, 10 folds → 66.7%. Test: villain con 5 manos → -1.

### 2. S9.1 — Fold equity con stats reales en PostflopDecisionService
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - Agregar parámetro `double villainFoldToBetPct = -1` a `DetermineAction`
  - En bloque ajuste por oponente (línea ~232-250): si `villainFoldToBetPct >= 0` → usar ajuste basado en stats reales (>60% → -5, <30% → +4). Else → fallback a multipliers estáticos existentes.
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Antes de cada llamada a `DetermineAction`, obtener `_opponentTracker.GetFoldToBetPct(villainId)` y pasarlo.
- **Verificación:** Test: villain con 70% fold → adjustedFoldBelow baja. Test: villain con 25% fold → adjustedFoldBelow sube. Test: stats no disponibles (-1) → usa fallback.

### 3. S9.2 — Semi-bluff verifica fold equity
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - En `HandleLowEquity`, bloque semi-bluff (línea ~717-731): antes de retornar, calcular `breakevenFE` ajustado por draw equity.
  - Si `actualFE >= adjustedBreakevenFE` → semi-bluff. Si no → seguir al siguiente path (call/fold).
  - Propagar `foldEquity` a `HandleLowEquity` (ya existe como parámetro).
- **Verificación:** Test: 9 outs + fold equity 40% → semi-bluff. Test: 9 outs + fold equity 5% → no semi-bluff (call/fold).

### 4. S9.3 — Nuevo parámetro CheckRaiseDrawMinEquity
- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Agregar `public double CheckRaiseDrawMinEquity { get; init; } = 40.0;`. Validación en `Validate()`: 20-60.
- **Archivo:** `src/OpenScrape.App/appsettings.json`
- **Acción:** Agregar `"CheckRaiseDrawMinEquity": 40.0` en StrategyProfile.
- **Verificación:** Compila. Validation test pasa.

### 5. S9.3 — Check-raise con draws fuertes
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - Propagar `hasComboDraw`, `hasFlushDraw`, `totalOuts` a `HandleNoBet` (agregar parámetros)
  - En check-raise (línea ~441-449): cambiar condición de `heroHandRank >= TwoPair` a `hasStrongMade || hasStrongDraw`
  - `hasStrongDraw = street == Flop && (hasComboDraw || (hasFlushDraw && totalOuts >= 9)) && equity >= CheckRaiseDrawMinEquity`
- **Verificación:** Test: flush draw flop OOP, 9 outs, equity 45% → Check (Check-Raise). Test: flush draw flop IP → no check-raise (requiere OOP). Test: flush draw turn → no check-raise draw (solo flop).

### 6. S9.4 — Pot commitment detection
- **Archivo:** `src/OpenScrape.DecisionMaker/PokerConstants.cs`
- **Acción:** Agregar `PotCommitmentSPRThreshold = 0.5`.
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - En `HandleFacingBet`, antes del fold final (línea ~402): si SPR < PotCommitmentSPRThreshold → calcular EV(call) → si > 0 → Call.
  - En `HandleLowEquity`, antes del fold facing bet final (línea ~834): misma lógica pot commitment.
- **Verificación:** Test: SPR 0.3, equity 30%, pot 100 → EV(call) > 0 → Call. Test: SPR 0.3, equity 10% → EV(call) < 0 → Fold. Test: SPR 2.0 → no aplica (threshold 0.5).

### 7. Actualizar tests existentes afectados
- **Acción:**
  - Tests de HandleLowEquity semi-bluff: pueden fallar si fold equity es 0 (ahora requiere FE check)
  - Tests de check-raise: actualizar para incluir nuevos parámetros hasComboDraw/hasFlushDraw/totalOuts
  - Tests de HandleFacingBet fold: verificar que pot commitment no interfiere en SPR normal
- **Verificación:** 0 tests fallidos.

### 8. Tests nuevos Sprint 9
- **Archivo:** `OpenScrape.App.Tests/`
- **Acción:** Crear ~15-20 tests:
  - S9.1: GetFoldToBetPct con stats suficientes. GetFoldToBetPct sin stats. DetermineAction con villainFoldToBetPct alto → fold threshold baja. Con bajo → sube.
  - S9.2: Semi-bluff con fold equity alta → bet. Semi-bluff sin fold equity → no bet. Semi-bluff con draw equity reduce breakeven FE.
  - S9.3: Check-raise combo draw flop OOP. Check-raise flush draw flop OOP. No check-raise draw turn. No check-raise draw IP.
  - S9.4: Pot committed SPR 0.3 equity 30% → Call. Pot committed SPR 0.3 equity 10% → Fold. SPR normal → no interfiere.
- **Verificación:** Todos pasan.

### 9. Build y test final
- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
