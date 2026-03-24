## Tasks

### 1. Fix ShouldBluff: eliminar dependencia de betSize
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - Eliminar parámetro `BetSizeCategory betSize` de `ShouldBluff()`
  - En `IPCoordinatedSmallOnly`: quitar condición `betSize == BetSizeCategory.Small`, dejar solo `isInPosition && boardTexture == "Coordinated"`
  - Actualizar llamada en `HandleLowEquity` (línea ~679): quitar `villainBetSize` de los argumentos
- **Verificación:** Build limpio. Test nuevo: bluff IP en Coordinated board con condición IPCoordinatedSmallOnly retorna true.

### 2. Fix DangerPenaltyCalculator: Math.Max en vez de suma
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs`
- **Acción:**
  - Separar flush completed y straight completed en variables locales
  - Aplicar `Math.Max(flushCompletePenalty, straightCompletePenalty)` en vez de sumarlas
  - Mantener flush draw flat separado (solo si flush NO completó)
  - Mantener board paired y overcard flat sumándose normalmente
- **Verificación:** Build limpio. Actualizar `DangerPenaltyCalculatorTests`: test `FlushCompletedYBoardPaired` debe reflejar nueva lógica. Nuevo test: flush + straight simultáneo usa Max.

### 3. Fix combo draw bonus: no aplicar con draw completado
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Cambiar línea ~118:
  ```
  if (hasComboDraw && street != BoardPosition.River)
  ```
  a:
  ```
  if (hasComboDraw && street != BoardPosition.River && heroHandRank < HandRank.Straight)
  ```
- **Verificación:** Build limpio. Test nuevo: combo draw con heroHandRank Straight → no bonus.

### 4. Fix river probe bet: agregar parámetro faltante
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En la llamada a `_postflopDecisionService.DetermineAction` en river (~línea 1005-1022), agregar:
  ```
  villainAggressorCheckedPreviousStreet: !_postflopContext.VillainBetTurn && !riverIsAggressor,
  ```
- **Verificación:** Build limpio. Verificar que probe bet se evalúa en river cuando villano agresor checkeó turn.

### 5. Calibración: actualizar defaults en StrategyProfile
- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Cambiar defaults:
  - `DangerFlushCompletePct`: 25.0 → 35.0
  - `ReverseImpliedFlushDrawPenalty`: 4.0 → 7.0
  - `ReverseImpliedCoordinatedPenalty`: 2.0 → 4.0
  - `BluffCatchFoldBelowMultiplier`: 0.85 → 0.75
  - `FloatingIPMinEquity`: 20.0 → 25.0
  - `SlowPlayMinEquity`: 80.0 → 72.0
- **Verificación:** Build limpio.

### 6. Calibración: actualizar appsettings.json
- **Archivo:** `src/OpenScrape.App/appsettings.json`
- **Acción:** Actualizar valores en sección StrategyProfile para reflejar los nuevos defaults:
  - `DangerFlushCompletePct`: 35.0
  - `ReverseImpliedFlushDrawPenalty`: 7.0
  - `ReverseImpliedCoordinatedPenalty`: 4.0
  - `BluffCatchFoldBelowMultiplier`: 0.75
  - `FloatingIPMinEquity`: 25.0
  - `SlowPlayMinEquity`: 72.0
- **Verificación:** Build limpio.

### 7. Actualizar tests existentes
- **Archivo:** `OpenScrape.App.Tests/DangerPenaltyCalculatorTests.cs`
- **Acción:**
  - Actualizar test `FlushCompletedYBoardPaired` para Math.Max en vez de suma
  - Actualizar tests que dependan de los valores default cambiados
- **Archivo:** `OpenScrape.App.Tests/ImpliedOddsCalculatorTests.cs`
- **Acción:** Actualizar tests que usan default profile (reverse implied penalties cambiaron)
- **Verificación:** Todos los tests existentes pasan.

### 8. Crear tests nuevos para los fixes
- **Archivo:** `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs` (o nuevo archivo)
- **Acción:**
  - Test: ShouldBluff con IPCoordinatedSmallOnly retorna true en board Coordinated IP
  - Test: Flush + Straight completados usa Math.Max (no suma)
  - Test: Combo draw bonus no se aplica con HandRank >= Straight
  - Test: Calibración FloatingIPMinEquity 25% bloquea floats con 22% equity
- **Verificación:** Todos los tests pasan.

### 9. Build y test final
- **Acción:** `dotnet build OpenScrape.sln` → 0 errores. `dotnet test OpenScrape.sln` → todos los tests pasando.
- **Verificación manual:** Revisar logs de decisión en sesión de prueba para confirmar que bluffs se activan, danger penalties son razonables, y probe bets aparecen en river.
