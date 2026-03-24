# Sprint 4 — Tareas de Implementación

## Tasks

### 1. S4.1 — Agregar campos BetSizeCategory a PostflopGameContext

- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs`
- **Acción:**
  - Agregar `public BetSizeCategory VillainBetSizeFlop { get; set; } = BetSizeCategory.NoBet;`
  - Agregar `public BetSizeCategory VillainBetSizeTurn { get; set; } = BetSizeCategory.NoBet;`
  - Agregar `BetSizeCategory villainBetSize = BetSizeCategory.NoBet` como parámetro a `UpdateFlopState()` y `UpdateTurnState()`. Asignar al campo correspondiente.
  - Agregar `VillainBetSizeFlop = BetSizeCategory.NoBet;` y `VillainBetSizeTurn = BetSizeCategory.NoBet;` en `Reset()`.
- **Verificación:** Compila sin errores.

### 2. S4.1 — Guardar betSize en FrmMain al actualizar contexto

- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:**
  - En flop (línea ~1368): agregar `_postflopContext.VillainBetSizeFlop = betSize;` después de `_postflopContext.VillainBetFlop = maxBet > 0;`.
  - En turn (línea ~1451): agregar `_postflopContext.VillainBetSizeTurn = betSize;` después de `_postflopContext.VillainBetTurn = maxBet > 0;`.
- **Verificación:** Build OK. Contexto guarda betSize correctamente.

### 3. S4.1 — Nuevo parámetro VillainSizingEscalationPenalty

- **Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- **Acción:** Agregar `public double VillainSizingEscalationPenalty { get; set; } = 4.0;` junto a `VillainBarrelFoldIncrease`.
- **Verificación:** Compila sin errores.

### 4. S4.1 — Sizing tell detection en PostflopDecisionService

- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:**
  - Agregar parámetros opcionales a `DetermineAction`: `BetSizeCategory villainBetSizeFlop = BetSizeCategory.NoBet, BetSizeCategory villainBetSizeTurn = BetSizeCategory.NoBet`.
  - Después del bloque de `villainBarreling` (línea ~185), agregar detección de sizing tell:
    - Turn: `villainBetSizeFlop != NoBet && villainBetSize > villainBetSizeFlop` → `adjustedFoldBelow += VillainSizingEscalationPenalty`.
    - River: `villainBetSizeTurn != NoBet && villainBetSize > villainBetSizeTurn` → `adjustedFoldBelow += VillainSizingEscalationPenalty`.
- **Verificación:** Build OK. Tests existentes pasan (parámetros tienen valor default).

### 5. S4.1 — Pasar sizing al DetermineAction desde FrmMain

- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:**
  - En la llamada a `DetermineAction` del turn (línea ~1404): agregar `villainBetSizeFlop: _postflopContext.VillainBetSizeFlop`.
  - En la llamada a `DetermineAction` del river (línea ~1005): agregar `villainBetSizeTurn: _postflopContext.VillainBetSizeTurn`.
- **Verificación:** Build OK.

### 6. S4.2 — Nuevo campo InitialBoardDanger en PostflopGameContext

- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs`
- **Acción:**
  - Agregar `public BoardChangeResult InitialBoardDanger { get; set; } = BoardChangeResult.Safe;`.
  - Agregar `InitialBoardDanger = BoardChangeResult.Safe;` en `Reset()`.
- **Verificación:** Compila sin errores.

### 7. S4.2 — Nuevo método AnalyzeInitialBoard en BoardTextureAnalyzer

- **Archivo:** `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`
- **Acción:** Agregar método público:
  ```csharp
  public BoardChangeResult AnalyzeInitialBoard(List<int> ranks, List<int> suits)
  ```
  Detecta: flush draw presente (2+ same suit), flush posible (3+ same suit), board paired, straight draw presente. Retorna `BoardChangeResult` con estado base del flop (sin `FlushCompleted`/`StraightCompleted` porque en flop no se "completa" nada).
- **Prerequisito:** `HasStraightDraw()` es `private static`. Ya accesible desde el nuevo método.
- **Verificación:** Nuevo test con flop 2-tone retorna `FlushDrawAppeared=true`.

### 8. S4.2 — Agregar overload AnalyzeInitialBoard con CardDataOuts

- **Archivo:** `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`
- **Acción:** Agregar overload:
  ```csharp
  public BoardChangeResult AnalyzeInitialBoard(List<CardDataOuts> communityCards)
  ```
  Convierte a ranks/suits y delega al overload principal.
- **Verificación:** Compila sin errores.

### 9. S4.2 — Agregar a IBoardTextureAnalyzer

- **Archivo:** Verificar si `IBoardTextureAnalyzer` existe y necesita el nuevo método.
- **Acción:** Si existe, agregar `BoardChangeResult AnalyzeInitialBoard(List<int> ranks, List<int> suits);` y overload.
- **Verificación:** Compila sin errores.

### 10. S4.2 — Analizar flop como estado base en FrmMain

- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En `DetermineFlopActionUnified()` (línea ~1318), reemplazar:
  ```csharp
  var boardChange = DecisionMaker.Algorithms.BoardChangeResult.Safe;
  ```
  Por:
  ```csharp
  var boardChange = _boardTextureAnalyzer.AnalyzeInitialBoard(flopRanks, flopSuits);
  _postflopContext.InitialBoardDanger = boardChange;
  ```
  `flopRanks` y `flopSuits` ya se calculan en líneas ~1299-1302.
- **Verificación:** Board 2-tone genera `FlushDrawAppeared=true` en el flop. Logs muestran danger info.

### 11. S4.2 — Combinar InitialBoardDanger en turn

- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En `ProcessTurnAsync()` (línea ~1394), cambiar:
  ```csharp
  var boardChange = AnalyzeBoardChange(_playerGameState.BoardCards, 3);
  ```
  A:
  ```csharp
  var turnChange = AnalyzeBoardChange(_playerGameState.BoardCards, 3);
  var boardChange = PostflopGameContext.CombineBoardChanges(
      _postflopContext.InitialBoardDanger, turnChange);
  ```
  Asegurar que `_postflopContext.LastBoardChange = boardChange;` se mantiene (ya existe más abajo).
- **Verificación:** Board 2-tone en flop + overcard en turn → danger acumulado.

### 12. S4.3 — Helper GetVillainStack en FrmMain

- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Agregar método privado:
  ```csharp
  private decimal GetVillainStack()
  {
      var activeVillains = _playerGameState.Players
          .Where(p => p.Active && !string.IsNullOrEmpty(p.Name));
      return activeVillains.Any() ? activeVillains.Max(p => p.Stack) : 0;
  }
  ```
- **Verificación:** Compila sin errores.

### 13. S4.3 — Reemplazar villainStack: 0 en 4 puntos

- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Reemplazar `villainStack: 0` por `villainStack: GetVillainStack()` en:
  - Línea ~1265 (preflop Calculate)
  - Línea ~1580 (turn Calculate)
  - Línea ~1658 (river Calculate)
  - Línea ~1935 (GetPotOddsCalculator)
- **Verificación:** Build OK. SPR usa effective stack real.

### 14. Tests nuevos

- **Archivo:** `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs`
- **Acción:**
  - **S4.1**: Test sizing tell — villain escala Small→Large en turn → adjustedFoldBelow sube +4.
  - **S4.1**: Test sin escalado — villain mantiene Large→Large → sin penalty extra.
  - **S4.1**: Test NoBet en flop — sin referencia, no aplica penalty.

- **Archivo:** `OpenScrape.App.Tests/BoardTextureAnalyzerTests.cs`
- **Acción:**
  - **S4.2**: Test flop 2-tone → `FlushDrawAppeared=true`, `DangerLevel >= 1`.
  - **S4.2**: Test flop monotone → `FlushDrawAppeared=true`, `DangerLevel >= 3`.
  - **S4.2**: Test flop rainbow → `FlushDrawAppeared=false`, `DangerLevel=0`.
  - **S4.2**: Test flop paired → `BoardPaired=true`.
  - **S4.2**: Test flop connected → `DangerLevel >= 1`.

- **Verificación:** `dotnet test OpenScrape.sln` — todos pasan.

### 15. Build y test final

- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
