# SDD — Fase 5: Extraer GameCoordinator

## 1. Propósito

Extraer la lógica de decisión y orquestación del game loop de FrmMain a un servicio `GameCoordinator : IGameCoordinator` testeable. FrmMain mantiene responsabilidad de OCR y UI; GameCoordinator maneja la pipeline de decisión.

## 2. Estrategia de Extracción

### Criterio de split: OCR/UI queda en FrmMain, decisión va a GameCoordinator

**GameCoordinator posee:**
- `_postflopContext` (estado cross-street)
- `_flopResult`, `_turnResult`, `_riverResult` (resultados de cálculos)
- `_turnBoardTexture`, `_riverBoardTexture`
- Los 3 métodos DetermineXxxAction (~470 LOC)
- Helpers de decisión: GetOpponentBetSize, GetVillainType, TrackVillainPostflopAction, GetVillainStack, AdjustBetSize, HeroBlocksTopBoardCard, EnrichActionWithBBAmount, FormatCardsForLog (~200 LOC)
- Board analysis: AnalyzeBoardChange, AnalyzeTurnBoardTexture, AnalyzeRiverBoardTexture (~80 LOC)
- GetPotOddsCalculator (~30 LOC)
- DetectNewHand (~50 LOC)

**FrmMain mantiene:**
- `_playerGameState` (llenado por OCR, pasado por referencia a GameCoordinator)
- `_formImage`, `_useCase` (captura de pantalla)
- `_responseAction` (feedback UI — actualizado por resultado de GameCoordinator)
- Process*Async (orquesta OCR → llama a GameCoordinator → actualiza UI)
- Todas las métodos UpdateXxx (UI pura)
- HandleNewHandAsync (file I/O + BD + UI — delega lógica de tracking a GameCoordinator)

### Patrón de comunicación

FrmMain llama a GameCoordinator pasando datos (PlayerGameState, cards, bet info):
```
FrmMain.DetermineFlopActionUnified()
  → _coordinator.DetermineFlopAction(playerGameState)
  → retorna (string action, string logText)
  → FrmMain actualiza _responseAction y UI
```

## 3. Decisiones de Diseño

### 3.1 GameCoordinator como Scoped (no Singleton)

GameCoordinator mantiene estado mutable por sesión (_postflopContext, resultados). Scoped asegura que cada scope tiene su propia instancia.

### 3.2 PlayerGameState se pasa por referencia

FrmMain posee PlayerGameState y lo pasa al GameCoordinator. No se duplica estado.

### 3.3 Logging via Action<string> delegate

GameCoordinator no accede a `tbResume` ni controles UI. Los mensajes de log se retornan como string en el resultado o se emiten via un delegate `Action<string>` inyectado.

### 3.4 GetActiveVillainId se mueve a GameCoordinator

Es lógica pura que depende de PlayerGameState y OpponentTracker.

## 4. Interfaz

```csharp
public interface IGameCoordinator
{
    PostflopGameContext PostflopContext { get; }
    void ResetContext();
    
    // Decisiones por calle
    GameDecisionResult DetermineFlopAction(PlayerGameState state);
    GameDecisionResult DetermineTurnAction(PlayerGameState state);
    GameDecisionResult DetermineRiverAction(PlayerGameState state);
    
    // Helpers
    BetSizeCategory GetOpponentBetSize(decimal maxBet, decimal potSize);
    OpponentType GetVillainType(PlayerGameState state, bool? heroIsInPosition = null);
    void TrackVillainPostflopAction(PlayerGameState state, decimal maxBet, bool isPreflopAggressor, bool? heroIsInPosition = null);
    decimal GetVillainStack(PlayerGameState state);
    string GetActiveVillainId(PlayerGameState state);
    string AdjustBetSize(string action, decimal heroStack, decimal potSize, int numOpponents, bool isPaired, bool isCoordinated, bool isDry, bool isInPosition);
    bool HeroBlocksTopBoardCard(PlayerGameState state);
    string EnrichActionWithBBAmount(string action, decimal potSize, decimal bigBlind = 0.10m);
    
    // Board analysis
    BoardChangeResult AnalyzeBoardChange(List<BoardData> boardCards, int previousCardCount);
    TurnBoardTexture AnalyzeTurnBoardTexture(List<BoardData> boardCards);
    RiverBoardTexture AnalyzeRiverBoardTexture(List<BoardData> boardCards);
    
    // Detection
    bool DetectNewHand(PlayerGameState state, bool handNumberChanged, string currentHand);
    
    // Cálculo
    PokerCalculationResult CalculatePotOdds(PlayerGameState state);
}
```

## 5. Impacto

### Nuevos
- `src/OpenScrape.App/Services/IGameCoordinator.cs`
- `src/OpenScrape.App/Services/GameCoordinator.cs`
- `src/OpenScrape.App/DTOs/GameDecisionResult.cs`

### Modificados
- `FrmMain.cs` — elimina ~800 LOC de métodos, delega a GameCoordinator
- `Program.cs` — registra GameCoordinator como Scoped

## 6. Criterios de Verificación

- `dotnet build` — 0 errores
- `dotnet test` — 592 tests pasan
- FrmMain no contiene lógica de DetermineAction
- GameCoordinator es testeable sin UI
