# Bugfix Live Session — Tareas de Implementación

## Tasks

### 1. Bug 2 — HasRangeAdvantageOnBoard para boards mixtos
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PreflopAnalyzer.cs`
- **Acción:** Reescribir la lógica de 3Bet pot: si `hasAceOrKing` → true. Si `isLowBoard` → false. Si board mixto (0 highCards + min rank ≤ 6) → false. Si 2+ highCards → true.
- **Verificación:** Test: T-5-2 en 3Bet → false. A-K-3 en 3Bet → true. Q-J-T en 3Bet → true. 7-5-3 en 3Bet → false.

### 2. Bug 3 — SetPotValue antes de re-procesar streets
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En `ProcessPostFlopAsync`, antes de cada `await ProcessTurnAsync()` o `await ProcessRiverAsync()` en reprocessing (cuando no hay nueva carta), agregar `SetPotValue(); SetBetValues();`.
- **Verificación:** Pot size > 0 al re-procesar turn. Log no muestra `Pot: 0` en re-procesamiento.

### 3. Bug 4 — numOpponents Math.Max en turn y river
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Buscar todas las líneas con `_playerGameState.Players.Count(p => p.Active) - 1` que NO tengan Math.Max. Agregar `Math.Max(1, ...)` en turn y river.
- **Verificación:** numOpponents nunca es 0 o negativo.

### 4. Bug 5 — DetermineSimplifiedAction con boardTexture
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Agregar `string boardTexture = "Dry"` como parámetro a `DetermineSimplifiedAction`. En NoBet paths, adaptar sizing: Monotone → "Bet 1/4", Wet → "Bet 1/3", otros → usar SimplifiedBetSize existente. Propagar `boardTexture` desde la llamada en `DetermineAction`.
- **Verificación:** Test: RaiseOverLimper en Monotone → sizing "Bet 1/4". En Dry → sizing "Bet 1/2" (existente).

### 5. Tests
- **Archivo:** `OpenScrape.App.Tests/PreflopAnalyzerTests.cs`
  - Test: T-5-2 3Bet → HasRangeAdvantageOnBoard = false
  - Test: A-K-3 3Bet → true
  - Test: 7-5-3 3Bet → false
  - Test: Q-J-T 3Bet → true
- **Archivo:** `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs`
  - Test: Simplified + Monotone → sizing "Bet 1/4"
  - Test: Simplified + Dry → sizing existente
- **Verificación:** `dotnet test OpenScrape.sln` — todos pasan.

### 6. Build y test final
- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
