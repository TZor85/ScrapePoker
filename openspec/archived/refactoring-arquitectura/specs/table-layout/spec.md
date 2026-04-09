# Spec: TableLayoutService

## Requisito

El sistema DEBE extraer toda la lógica de detección de jugadores, posiciones, dealer y fold a un servicio `TableLayoutService : ITableLayoutService`, independiente de controles UI.

## Conceptos

- **TableLayoutService**: Servicio Scoped que opera sobre `PlayerGameState[]` y screenshots para detectar el layout actual de la mesa.
- **Detección de dealer**: Identificación del botón de dealer via color sampling con LockBits.
- **Posición relativa**: Cálculo de TablePosition (Early, Middle, CutOff, Button, SB, BB) basado en distancia al dealer.

## Interfaz Pública

```csharp
public interface ITableLayoutService
{
    Task InitializePlayersAsync(Bitmap screenshot, PlayerGameState[] players);
    bool DetectDealerPosition(Bitmap screenshot, PlayerGameState[] players);
    void DetectActivePlayers(Bitmap screenshot, PlayerGameState[] players);
    void DetectEmptySeats(Bitmap screenshot, PlayerGameState[] players);
    void DetectFoldedPlayers(Bitmap screenshot, PlayerGameState[] players);
    void DetectSitOutPlayers(Bitmap screenshot, PlayerGameState[] players);
    TablePosition DetermineHeroPosition(PlayerGameState[] players);
    void SetVillainPositions(PlayerGameState[] players);
    bool ValidatePositionAssignments(PlayerGameState[] players);
    void SetIsInPosition(PlayerGameState[] players);
    string GetActiveVillainId(PlayerGameState[] players);
    void RetryEmptyAliases(Bitmap screenshot, PlayerGameState[] players);
    Player CreatePlayerData(PlayerGameState state);
}
```

## Dependencias

```csharp
public TableLayoutService(
    IScreenReaderService screenReader,
    ICoordinateScaler coordinateScaler,
    IOpponentTracker opponentTracker,
    RegionLookupCache regionCache)
```

## Escenarios

### Escenario 1: Determinar posición hero con dealer en seat 3

DADO 6 jugadores activos
Y el dealer está en seat 3
Y hero está en seat 5
CUANDO se llama DetermineHeroPosition
ENTONCES retorna TablePosition.SmallBlind

### Escenario 2: Detectar fold mid-hand

DADO un jugador en seat 2 que tenía IsPlaying = true
Y el screenshot muestra que su región de cartas ya no tiene cartas visibles
CUANDO se llama DetectFoldedPlayers
ENTONCES el jugador en seat 2 tiene IsPlaying = false
Y IsFolded = true

### Escenario 3: Retry de alias vacíos

DADO 3 jugadores con alias = ""
Y el OCR del screenshot ahora puede leer sus nombres
CUANDO se llama RetryEmptyAliases
ENTONCES los 3 jugadores tienen alias != ""
Y se registran en OpponentTracker via RegisterSeatAlias

### Escenario 4: Validar posiciones inconsistentes

DADO 2 jugadores marcados como Button
CUANDO se llama ValidatePositionAssignments
ENTONCES retorna false
Y corrige asignando Button solo al más cercano al dealer

### Escenario 5: IsInPosition correcto para hero BTN vs villain BB

DADO hero en Button
Y villain activo en BigBlind
CUANDO se llama SetIsInPosition
ENTONCES hero.IsInPosition = true (actúa último postflop)

### Escenario 6: GetActiveVillainId prioriza alias sobre seat

DADO un jugador con alias "Player123" en seat 4
Y otro sin alias en seat 7
Y el villain activo es seat 4
CUANDO se llama GetActiveVillainId
ENTONCES retorna "Player123" (no "Seat4")
