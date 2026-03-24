# Integración OpponentTracker + Optimización MC — Tareas

## Tasks

### Parte 1: OpponentTracker en Game Loop

#### 1. Inyectar OpponentTracker en FrmMain
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Agregar `private readonly OpponentTracker _opponentTracker;` como campo. Agregar parámetro al constructor e inyectar. OpponentTracker ya está registrado como Singleton en `Program.cs`.
- **Verificación:** Build OK. OpponentTracker accesible en FrmMain.

#### 2. Helper GetActiveVillainId
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Agregar método privado que retorna el nombre del villano activo con mayor bet. Fallback a "Unknown".
- **Verificación:** Compila.

#### 3. RecordHandPlayed en DetectNewHand
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En el punto donde se detecta nueva mano (`DetectNewHand` o inicio de game loop iteration), llamar `RecordHandPlayed(player.Name)` para cada villano activo.
- **Verificación:** Después de 5 manos, `GetProfile(villain).HandsPlayed >= 5`.

#### 4. RecordVPIP y RecordPFR en preflop
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Después de detectar la situación preflop, iterar villanos con bet > BigBlind → `RecordVPIP`. Si bet > 2×BB → `RecordPFR`. Si se detecta 3Bet → `RecordThreeBet`.
- **Verificación:** VPIP y PFR se acumulan correctamente.

#### 5. RecordPostflopAction en flop/turn/river
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En `DetermineFlopActionUnified`, `ProcessTurnAsync`, `ProcessRiverAsync`: si `maxBet > 0` → `RecordPostflopAction(villainId, PostflopAction.Bet)`. También `RecordCBetOpportunity` cuando el agresor preflop apuesta/checkea flop.
- **Verificación:** AggressionFactor se calcula correctamente después de 20+ manos.

#### 6. Pasar villainType real a DetermineAction
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En las 3 llamadas a `DetermineAction` (flop/turn/river), obtener `villainProfile.Type` del tracker y pasarlo como `villainType:`. Solo si `IsReliable` (20+ manos); sino mantener `Unknown`.
- **Verificación:** Log muestra tipo de villano cuando hay 20+ manos.

#### 7. Fold equity ajustada por oponente
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Antes de pasar `foldEquity` a `DetermineAction`, ajustar con `_opponentTracker.GetAdjustedFoldEquity(villainId, baseFoldEquity)`.
- **Verificación:** LAG reduce fold equity, LP la aumenta.

### Parte 2: Optimización Monte Carlo

#### 8. Iteraciones adaptativas
- **Archivo:** `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs`
- **Acción:** Agregar método `GetAdaptiveIterations()`. Si equity previa cacheada >75% o <25% → 500 iteraciones. Si >65% o <35% → 750. Default → 1000. Usar en llamada a `_monteCarloSimulator.CalculateEquity()`.
- **Verificación:** Test: equity 90% usa 500 iters. Equity 50% usa 1000.

#### 9. Cache preflop MC con VillainRange
- **Archivo:** `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs`
- **Acción:** En `CalculateEquity()`, cuando preflop con VillainRange, cachear resultado con key `"preflop|hand|situation|numOpp"`. Reutilizar en llamadas posteriores con misma key.
- **Verificación:** Segunda llamada con misma hand+situation no ejecuta MC.

#### 10. Aumentar cache a 512
- **Archivo:** `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs`
- **Acción:** Cambiar `EquityCacheMaxSize` de 256 a 512.
- **Verificación:** Compila.

### Parte 3: Tests

#### 11. Tests de integración OpponentTracker
- **Archivo:** `OpenScrape.App.Tests/`
- **Acción:**
  - Test: después de 20 RecordHandPlayed + 15 RecordVPIP → VPIP = 75%, IsReliable = true
  - Test: tipo LAG con VPIP>30 + AF>1.5
  - Test: GetAdjustedFoldEquity LAG × 0.70, LP × 1.25
- **Verificación:** Tests pasan.

#### 12. Tests de rendimiento MC
- **Archivo:** `OpenScrape.App.Tests/`
- **Acción:**
  - Test: GetAdaptiveIterations con equity cacheada 90% → retorna 500
  - Test: preflop MC con VillainRange cacheado → segunda llamada no recalcula
- **Verificación:** Tests pasan.

#### 13. Build y test final
- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
