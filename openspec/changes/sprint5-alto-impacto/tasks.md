# Sprint 5 — Tareas de Implementación

## Tasks

### 1. S5.1 — Overcards con draws en OutsCalculator
- **Archivo:** `src/OpenScrape.DecisionMaker/Algorithms/OutsCalculator.cs`
- **Acción:** Eliminar la condición `!hasMainDraw` del bloque de overcards. Contar overcards siempre, pero excluir ranks que ya son straight completing ranks (evitar doble-conteo).
- **Verificación:** Test con AK en 9-8-7: overcards A,K contadas (6 outs extra). Test con AK en 9-8-7 donde A es también straight out: no doble-contar.

### 2. S5.2 — Nuevos parámetros DangerPenalty por street
- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Agregar `DangerPenaltyFlopMultiplier = 1.3` y `DangerPenaltyRiverMultiplier = 0.8`. Validación en `Validate()`: ambos > 0.
- **Verificación:** Compila. Tests de validación pasan.

### 3. S5.2 — Escalar danger penalty por street
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs`
- **Acción:** Agregar `BoardPosition street` como parámetro a `Calculate()`. Aplicar multiplicador por street a las penalizaciones porcentuales (FlushComplete, StraightComplete). Las flat (BoardPaired, Overcard, FlushDraw) no se escalan.
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Propagar `street` a `CalculateDangerPenalty()` y al call interno de `DangerPenaltyCalculator.Calculate()`.
- **Verificación:** Test: flush complete en flop penalty > flush complete en river con misma equity.

### 4. S5.3 — Nuevas constantes multiway IP/OOP
- **Archivo:** `src/OpenScrape.DecisionMaker/PokerConstants.cs`
- **Acción:** Reemplazar `MultiwayFoldBelowPerOpponent` y `MultiwayThinValuePerOpponent` por 4 constantes: `MultiwayFoldBelowIP = 2.0`, `MultiwayFoldBelowOOP = 6.0`, `MultiwayThinValueIP = 2.0`, `MultiwayThinValueOOP = 4.0`.
- **Verificación:** Compila.

### 5. S5.3 — Multiway penalty diferenciada en PostflopDecisionService
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En el bloque multiway (~línea 173), usar `isInPosition` para seleccionar constante IP vs OOP. En `HandleLowEquity`, bloquear bluff cuando `isMultiway && !isInPosition`.
- **Verificación:** Test: multiway OOP penalty > multiway IP. Test: no bluff multiway OOP.

### 6. S5.4 — Separar Wet en SimplifiedTexture
- **Archivo:** `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`
- **Acción:** En `SimplifiedTexture`, separar `Wet` → "Wet" y dejar `SemiWet` → "Coordinated".
- **Verificación:** Test: board Wet (60+ wetness) retorna "Wet". Board SemiWet retorna "Coordinated".

### 7. S5.4 — Nuevo campo WetBoardBetSize en StreetThresholds
- **Archivo:** `src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs`
- **Acción:** Agregar `public string WetBoardBetSize { get; init; } = "Bet 1/3";`
- **Verificación:** Compila.

### 8. S5.4 — Case "Wet" en HandleNoBet
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Agregar `"Wet" => thresholds.WetBoardBetSize` al switch de boardTexture en HandleNoBet.
- **Verificación:** Test: NoBet en Wet board usa sizing "Bet 1/3".

### 9. S5.4 — WetBoardBetSize en appsettings.json
- **Archivo:** `src/OpenScrape.App/appsettings.json`
- **Acción:** Agregar `"WetBoardBetSize": "Bet 1/3"` en cada threshold que tenga `CoordinatedBoardBetSize`.
- **Verificación:** App carga sin warnings.

### 10. S5.5 — Flop_DonkBet y Flop_DonkBetVsOpenRaise en appsettings.json
- **Archivo:** `src/OpenScrape.App/appsettings.json`
- **Acción:** Agregar ambas configuraciones dentro de `Thresholds` después de `Flop_VsSqueeze`. Valores según design.md.
- **Verificación:** App carga sin warnings. DonkBet en flop ya no usa fallback genérico.

### 11. Tests nuevos
- **Archivo:** `OpenScrape.App.Tests/`
- **Acción:**
  - **S5.1**: AK en 9-8-7 → outs incluyen overcards. AK en T-9-8 → A no doble-contada (straight out).
  - **S5.2**: Flush complete flop penalty > turn > river con misma equity.
  - **S5.3**: Multiway OOP adjustedFoldBelow > multiway IP. No bluff multiway OOP.
  - **S5.4**: Board Wet → "Wet" simplifiedTexture. NoBet Wet → "Bet 1/3".
  - **S5.5**: DetermineAction con Flop+DonkBet situation → no usa fallback.
- **Verificación:** `dotnet test OpenScrape.sln` — todos pasan.

### 12. Actualizar tests existentes afectados
- **Acción:** Tests de BoardTexture que esperan "Coordinated" para boards Wet ahora deben esperar "Wet". Tests de multiway que asumen penalty fija deben reflejar IP/OOP.
- **Verificación:** 0 tests fallidos.

### 13. Build y test final
- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
