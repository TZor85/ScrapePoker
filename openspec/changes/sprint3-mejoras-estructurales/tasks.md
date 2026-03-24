# Sprint 3 — Tareas de Implementación

## Tasks

### 1. S2.3 — Facing bet penalty escalado por street

- **Archivo:** `src/OpenScrape.DecisionMaker/PokerConstants.cs`
- **Acción:** Agregar constantes:
  - `FacingBetTurnMultiplier = 1.15`
  - `FacingBetRiverMultiplier = 1.30`
- **Verificación:** Compila sin errores.

### 2. S2.3 — Aplicar multiplicador en PostflopDecisionService

- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Después de calcular `facingBetPenalty` (línea ~153), multiplicar por factor de street:
  ```csharp
  double streetMultiplier = street switch
  {
      BoardPosition.Turn => PokerConstants.FacingBetTurnMultiplier,
      BoardPosition.River => PokerConstants.FacingBetRiverMultiplier,
      _ => 1.0
  };
  facingBetPenalty *= streetMultiplier;
  ```
- **Verificación:** Tests existentes de facing bet pasan. Nuevos tests verifican que penalty es mayor en river que en turn que en flop.

### 3. S3.1 — Nuevo parámetro BluffCatchTurnEquityMultiplier

- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:**
  - Agregar `public double BluffCatchTurnEquityMultiplier { get; set; } = 0.90;` junto a `BluffCatchFoldBelowMultiplier`.
  - Agregar validación en `Validate()`: `BluffCatchTurnEquityMultiplier` en rango (0, 1].
- **Verificación:** `StrategyProfileTests` pasan. Nuevo test verifica validación.

### 4. S3.1 — Extender bluff catch a turn

- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En `HandleLowEquity`, extender la condición de bluff catch (línea ~704):
  - Turn: solo `Small` bet, `MiddlePair+`, umbral × `BluffCatchTurnEquityMultiplier` (0.90)
  - River: sin cambios (mantener lógica existente)
  - BottomPair en turn: skip bluff catch
- **Verificación:** Nuevos tests: bluff catch turn con MiddlePair+Small bet → Call. BottomPair → no call. Medium bet en turn → no call.

### 5. S3.2 — Nuevo parámetro FloatingIPMinOuts

- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Agregar `public double FloatingIPMinOuts { get; set; } = 6;` junto a `FloatingIPMinEquity`/`FloatingIPMaxEquity`.
- **Verificación:** Compila sin errores.

### 6. S3.2 — Floating IP requiere draw real

- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En `HandleFacingBet`, antes de la condición de floating (línea ~343), agregar variable:
  ```csharp
  bool hasRealDraw = hasFlushDraw || hasComboDraw || totalOuts >= _profile.FloatingIPMinOuts;
  ```
  Añadir `&& hasRealDraw` a la condición existente.
- **Verificación:** Nuevos tests: float con 4 overcards sin draw → no float. Float con flush draw → float. Float con 8 outs OESD → float.

### 7. S3.3 — Fold equity check en bluffs puros

- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En `HandleLowEquity`, dentro del bloque de bluff puro (línea ~679-685):
  - Después de `ShouldBluff()`, calcular `breakevenFoldEquity = betFraction / (1.0 + betFraction)` usando `BetStringToFraction(thresholds.BluffBetSize)`.
  - Solo retornar bluff si `foldEquity / 100.0 >= breakevenFoldEquity`.
  - Actualizar el reason con info de fold equity.
- **Prerequisito:** Verificar que `foldEquity` está disponible como parámetro en `HandleLowEquity`. Si no, agregarlo desde `DetermineAction` (ya disponible como `foldEquity`).
- **Verificación:** Nuevos tests: bluff con fold equity 40% y bet 1/3 (breakeven 25%) → bluff. Bluff con fold equity 15% → check.

### 8. S3.4 — Nuevo campo MonotoneBoardBetSize en StreetThresholds

- **Archivo:** `src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs`
- **Acción:** Agregar:
  ```csharp
  public string MonotoneBoardBetSize { get; init; } = "Bet 1/4";
  ```
- **Verificación:** Compila sin errores.

### 9. S3.4 — Verificar/agregar categoría Monotone en BoardTextureAnalyzer

- **Archivo:** `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`
- **Acción:** Verificar que el analyzer retorna `Category = "Monotone"` cuando 3+ cartas comparten palo. Si no, agregar detección antes de la clasificación por wetness score.
- **Verificación:** Test existente `BoardTextureTests` con board monotone retorna "Monotone".

### 10. S3.4 — Agregar case Monotone en HandleNoBet

- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En el switch de `boardTexture` (línea ~424), agregar:
  ```csharp
  "Monotone" => thresholds.MonotoneBoardBetSize,
  ```
- **Verificación:** Nuevo test: HandleNoBet con board monotone usa sizing "Bet 1/4".

### 11. S3.5 — Preflop equity vs VillainRange

- **Archivo:** `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs`
- **Acción:** En `CalculateEquity`, cuando `communityCards.Count == 0` (preflop), verificar si existe `VillainRange` para la `handSituation`. Si existe, usar Monte Carlo preflop con ese rango en vez del lookup table.
- **Verificación:** Nuevo test: equity de JTs vs 3Bet range < equity de JTs vs random. Test: equity vs OpenRaise sigue usando lookup table (sin rango definido).

### 12. S3.4 — Actualizar appsettings.json con MonotoneBoardBetSize

- **Archivo:** `src/OpenScrape.App/appsettings.json`
- **Acción:** Agregar `"MonotoneBoardBetSize": "Bet 1/4"` en cada `StreetThresholds` que tenga `DryBoardBetSize` configurado.
- **Verificación:** La aplicación carga el perfil sin warnings.

### 13. Tests nuevos

- **Archivo:** `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs`
- **Acción:** Agregar tests para:
  - **S2.3**: Facing bet penalty en flop/turn/river → verificar escala correcta
  - **S3.1**: Bluff catch turn SmallBet+MiddlePair → Call. BottomPair → no Call. MediumBet → no Call
  - **S3.2**: Float con overcards sin draw → no float. Float con flush draw → float
  - **S3.3**: Bluff con fold equity suficiente → bluff. Fold equity insuficiente → check
  - **S3.4**: NoBet en Monotone → "Bet 1/4"
- **Verificación:** `dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj` — todos pasan.

### 14. Build y test final

- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
