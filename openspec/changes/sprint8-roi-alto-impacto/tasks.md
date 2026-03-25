# Sprint 8 — Tareas de Implementación

## Tasks

### 1. S8.1 — Método CalculateAllinEV + integración push/fold
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - Crear método `CalculateAllinEV(double equity, decimal heroStack, decimal potSize) → double`
  - Reemplazar las 2 líneas de push/fold (facing bet línea ~268 y no facing bet línea ~279) con: si `isPushFold` → calcular EV → si EV > 0 → All-In
  - Eliminar condición `heroHandRank >= HandRank.OnePair` del push/fold
- **Verificación:** Test: SPR 1.2, equity 42%, pot 100 → EV > 0 → All-In. Test: SPR 1.2, equity 15% → EV < 0 → no All-In.

### 2. S8.2 — Bluff catch multiplicador por villainType
- **Archivo:** `src/OpenScrape.DecisionMaker/PokerConstants.cs`
- **Acción:** Agregar 4 constantes: `BluffCatchLAGMultiplier = 0.80`, `BluffCatchLPMultiplier = 0.85`, `BluffCatchTAGMultiplier = 1.00`, `BluffCatchTPMultiplier = 1.20`.
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - Propagar `villainType` a `HandleLowEquity()` (agregar parámetro)
  - En bluff catch (línea ~795), después de calcular `bluffCatchThreshold`, multiplicar por `opponentBluffMultiplier` según `villainType`
  - Actualizar llamada a `HandleLowEquity` en `DetermineAction` para pasar `villainType`
- **Verificación:** Test: bluff catch vs LAG → threshold más bajo. Test: bluff catch vs TP → threshold más alto → fold.

### 3. S8.3 — BetSizeCategory.Underbet
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` (enum)
- **Acción:** Agregar `Underbet` al enum `BetSizeCategory` entre `NoBet` y `Small`.
- **Archivo:** `src/OpenScrape.DecisionMaker/PokerConstants.cs`
- **Acción:** Agregar `FacingBetPenaltyUnderbet = 0.0`.
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - En facing bet penalty switch: agregar case `Underbet => FacingBetPenaltyUnderbet`
  - En `HandleFacingBet`, antes del fold final: si `villainBetSize == Underbet && equity > ThinValueAbove` → Raise 3x
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Donde se clasifica bet size del villano: insertar categoría Underbet para bets < 15% pot (entre NoBet y Small).
- **Verificación:** Test: villain bet 10% pot → Underbet. Test: facing Underbet con equity 50% → Raise. Test: facing Underbet con equity 25% → usa penalty 0.

### 4. S8.4 — Double barrel condicionado al runout
- **Archivo:** `src/OpenScrape.Domain/ValueObjects/BoardChangeResult.cs`
- **Acción:** Agregar `public bool NewOvercard { get; init; }` si no existe.
- **Archivo:** `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`
- **Acción:** En `AnalyzeBoardChange()`, detectar si la carta nueva es mayor que la carta más alta del board anterior → `NewOvercard = true`.
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En double barrel (línea ~567-581): evaluar `boardChange` → si `NewOvercard || FlushCompleted || StraightCompleted || BoardPaired` → `badRunout = true` → no barrelear (check).
- **Verificación:** Test: flop K-7-2 bet, turn A (overcard) → check (no barrel). Test: flop K-7-2 bet, turn 3 (brick) → barrel.

### 5. S8.5 — Thin value river conservador
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En `HandleNoBet`, bloque thin value (línea ~554): agregar guard para river con draw completado.
  - Si `street == River && boardChange tiene FlushCompleted/StraightCompleted && !heroHasCompletedDraw` → Check en vez de bet.
  - Propagar `boardChange` que ya llega a `HandleNoBet` y computar `heroHasCompletedDraw` (misma lógica que en `DetermineAction` línea ~130-131).
- **Verificación:** Test: river con flush completado, hero tiene OnePair → Check. Test: river con flush completado, hero tiene Flush → Bet thin value.

### 6. S8.6 — Float IP exit strategy
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - Agregar parámetro `bool heroFloatedFlop = false` a `DetermineAction`
  - Propagar a `HandleNoBet` (nuevo parámetro)
  - En `HandleNoBet`, después de check-raise y antes de probe bet: si `heroFloatedFlop && street == Turn && !isMultiway` → Bet 1/2 (Float Exit)
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs` (o `PostflopGameContext`)
- **Acción:**
  - Agregar `bool HeroFloatedFlop` a `PostflopGameContext`
  - Después de decisión flop: si `decision.IsFloating` → `_postflopContext.HeroFloatedFlop = true`
  - En llamada turn a `DetermineAction`: pasar `heroFloatedFlop: _postflopContext.HeroFloatedFlop`
- **Verificación:** Test: hero floateó flop, turn villain check → Bet 1/2 (Float Exit). Test: hero NO floateó, turn villain check → lógica normal.

### 7. Actualizar tests existentes afectados
- **Acción:**
  - Tests que verifican push/fold con HandRank < OnePair: ahora pueden retornar All-In si EV > 0.
  - Tests que verifican BetSizeCategory: actualizar enum order (Underbet insertado).
  - Tests de bluff catch: verificar que sin villainType pasan igual (multiplier 1.0).
  - Tests de double barrel: actualizar para pasar boardChange.
- **Verificación:** 0 tests fallidos.

### 8. Tests nuevos Sprint 8
- **Archivo:** `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs`
- **Acción:** Crear ~20-25 tests:
  - S8.1: AllinEV positivo → All-In. AllinEV negativo → no All-In. SPR 1.2, equity 42% → All-In. SPR 1.2, equity 15% → Fold.
  - S8.2: BluffCatch vs LAG → Call. BluffCatch vs TP → Fold. BluffCatch vs Unknown → comportamiento base.
  - S8.3: Underbet classification (10% pot → Underbet). Facing Underbet + equity buena → Raise. Facing Underbet + equity baja → penalty 0.
  - S8.4: Barrel en brick → barrel. Barrel en overcard → check. Barrel en flush completado → check.
  - S8.5: ThinValue river draw completado hero no tiene → Check. ThinValue river draw completado hero tiene → Bet.
  - S8.6: Float exit turn → Bet 1/2. No float turn → normal. Float exit multiway → no (check).
- **Verificación:** Todos pasan.

### 9. Build y test final
- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
