# Sprint 7 — Tareas de Implementación

## Tasks

### 1. S7.1 — Interpolación cuadrática en ImpliedOddsCalculator
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/ImpliedOddsCalculator.cs`
- **Acción:** Reemplazar `position` por `Math.Sqrt(position)` en la interpolación entre SPR shallow y deep (línea ~42).
- **Verificación:** Test: SPR 3.0 factor con sqrt < factor lineal. SPR 2.0 y 4.0 sin cambio.

### 2. S7.2 — Nuevos parámetros tainted outs
- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Agregar `TaintedOutsDiscountHeroStrong = 0.7` y `TaintedOutsDiscountHeroWeak = 0.3` junto a `TaintedOutsDiscount`.
- **Verificación:** Compila.

### 3. S7.2 — Tainted outs descuento variable en OutsCalculator
- **Archivo:** `src/OpenScrape.DecisionMaker/Algorithms/OutsCalculator.cs`
- **Acción:** En `CalculateOuts`, reemplazar `result.TaintedOuts * _profile.TaintedOutsDiscount` por descuento variable: si hero tiene flush draw → `TaintedOutsDiscountHeroStrong`; sino → `TaintedOutsDiscountHeroWeak`.
- **Verificación:** Test: con flush draw, effective outs usa descuento 0.7. Sin flush draw, descuento 0.3.

### 4. S7.3 — Nuevos parámetros bluff SPR
- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Agregar `BluffSPRShortThreshold = 2.0`, `BluffSPRShortMultiplier = 0.5`, `BluffSPRDeepThreshold = 4.0`, `BluffSPRDeepMultiplier = 1.2`.
- **Verificación:** Compila.

### 5. S7.3 — Bluff frequency × SPR en PostflopDecisionService
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En HandleLowEquity, dentro del bloque de bluff puro, calcular `sprBluffMultiplier` basado en SPR (heroStack/potSize). Ajustar el breakeven fold equity: `adjustedBreakevenFE = breakevenFoldEquity / sprBluffMultiplier`.
- **Prerequisito:** `heroStack` y `potSize` no están disponibles en `HandleLowEquity`. Agregar como parámetros opcionales y propagarlos desde `DetermineAction`.
- **Verificación:** Test: SPR corto (<2) → bluff requiere más fold equity. SPR profundo (>4) → menos.

### 6. S7.4 — Corregir FoldBelow en RaiseOverLimper
- **Archivo:** `src/OpenScrape.App/appsettings.json`
- **Acción:** Cambiar `"FoldBelow": 0` a `"FoldBelow": 25` en `Turn_RaiseOverLimper` y a `"FoldBelow": 30` en `River_RaiseOverLimper`.
- **Verificación:** App carga sin warnings. Bot foldea con equity <25/30 vs limpers en turn/river.

### 7. Tests nuevos
- **Archivo:** `OpenScrape.App.Tests/ImpliedOddsCalculatorTests.cs`
  - **S7.1**: SPR 3.0 factor sqrt < factor lineal. SPR 2.0 y 4.0 iguales.
- **Archivo:** `OpenScrape.App.Tests/OutsCalculatorTests.cs`
  - **S7.2**: Hero con flush draw → tainted discount 0.7. Sin flush draw → 0.3.
- **Archivo:** `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs`
  - **S7.3**: Bluff SPR corto requiere más fold equity que SPR profundo.
- **Verificación:** `dotnet test OpenScrape.sln` — todos pasan.

### 8. Build y test final
- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
