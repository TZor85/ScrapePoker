# Spec: Pestaña Historial de Sesiones y Manos

## Proposal

### Why

Las manos jugadas se persisten como documentos `HandRecord` en PostgreSQL pero no hay ninguna pantalla en la aplicación para consultarlas. El usuario solo ve sesiones sin poder explorar el detalle de cada mano — cartas, decisiones por street, equity, acciones recomendadas vs tomadas, y resultado. Sin esta visibilidad, el análisis post-sesión requiere consultar la base de datos directamente.

### What Changes

- Nueva pestaña **"Historial"** en el `TabControl` principal de `FrmMain`
- **Grilla de sesiones** con estadísticas: mesa, fecha, duración, total de manos, profit, BB/100
- **Grilla de manos** de la sesión seleccionada: Hand#, cartas, posición, última street, resultado, P/L
- **Panel de detalle** en formato Hand History clásico (estilo PokerStars) con colores
- **Barra de estado** con stats resumidos
- Nuevo `SessionStatsDto` y método de query eficiente en `GameLoggerService`

### Capabilities

- `session-browser`: Grilla de sesiones con stats calculados
- `hand-history-viewer`: Visualización de manos en formato Hand History con colores

### Impact

- **UI**: `FrmMain.cs` + `FrmMain.Designer.cs` — nueva pestaña con controles
- **Servicios**: `GameLoggerService.cs` — nuevo método de stats
- **Dominio**: Nuevo `SessionStatsDto` en `OpenScrape.Domain/Dtos/`
- Sin nuevas dependencias externas

---

## Design

### Layout de la pestaña

```
┌─────────────────────────────────────────────────────────────┐
│  [Juego] [Config] [Tablas] [Logs] [Historial]              │
├──────────────────┬──────────────────────────────────────────┤
│  SESIONES        │  DETALLE DE MANO                        │
│  ┌────────────┐  │  ┌────────────────────────────────────┐  │
│  │ dgvSessions│  │  │ rtbHandDetail                      │  │
│  │            │  │  │ (RichTextBox, ReadOnly, Consolas)   │  │
│  │ Mesa       │  │  │                                    │  │
│  │ Fecha      │  │  │ Hand #12345 — 2026-03-22 14:30     │  │
│  │ Manos      │  │  │ Mesa: NL Holdem ($0.25/$0.50)      │  │
│  │ Profit     │  │  │ Hero [Ah Kd] — Button — $52.30     │  │
│  │ BB/100     │  │  │                                    │  │
│  ├────────────┤  │  │ *** FLOP *** [Qs Jc 3h]            │  │
│  │ MANOS      │  │  │   Equity: 45.2% | PotOdds: 33%     │  │
│  ├────────────┤  │  │   Recomendado: Bet 1/2 (Value)     │  │
│  │dgvHands    │  │  │   Acción: Bet $2.50                │  │
│  │            │  │  │                                    │  │
│  │ Hand#      │  │  │ *** TURN *** [Qs Jc 3h] [Td]       │  │
│  │ Cartas     │  │  │   ...                              │  │
│  │ Pos        │  │  │                                    │  │
│  │ Resultado  │  │  │ RESULTADO: Won (+$8.20)            │  │
│  │ P/L        │  │  │                                    │  │
│  └────────────┘  │  └────────────────────────────────────┘  │
├──────────────────┴──────────────────────────────────────────┤
│  Sesión: Mesa1 | 45 manos | +$12.50 | +5.0 BB/100         │
└─────────────────────────────────────────────────────────────┘
```

### Árbol de controles

```
tpHistorial (TabPage)
└── splitHistorialMain (SplitContainer, Vertical, FixedPanel=Panel1, SplitterDistance=350)
    ├── Panel1 (izquierda)
    │   └── splitHistorialLeft (SplitContainer, Horizontal, SplitterDistance=55%)
    │       ├── Panel1 (arriba)
    │       │   ├── lblSesionesTitle (Label, "Sesiones", Dock=Top, h=30)
    │       │   └── dgvSessions (DataGridView, Dock=Fill)
    │       └── Panel2 (abajo)
    │           ├── lblManosTitle (Label, "Manos", Dock=Top, h=30)
    │           └── dgvSessionHands (DataGridView, Dock=Fill)
    └── Panel2 (derecha)
        ├── rtbHandDetail (RichTextBox, Dock=Fill, ReadOnly, Consolas 10pt)
        └── lblSessionStats (Label, Dock=Bottom, h=30)
```

### Columnas de dgvSessions

| Columna | Header | Tipo | Width | Formato |
|---|---|---|---|---|
| TableName | Mesa | string | 120 | — |
| StartTime | Inicio | DateTime | 130 | "dd/MM HH:mm" |
| Duration | Duración | string | 80 | "Xh Ym" calculado |
| TotalHands | Manos | int | 60 | — |
| TotalProfit | Profit | decimal | 80 | "+0.00;-0.00" con color verde/rojo |
| BBPer100 | BB/100 | double | 70 | "+0.0;-0.0" con color verde/rojo |

### Columnas de dgvSessionHands

| Columna | Header | Tipo | Width | Formato |
|---|---|---|---|---|
| HandNumber | Hand# | long | 70 | — |
| Cards | Cartas | string | 80 | "HeroCard1 HeroCard2" concatenado |
| HeroPosition | Pos | TablePosition | 60 | Abreviado (BTN, CO, MP, EP, SB, BB) |
| LastStreetPlayed | Street | BoardPosition | 60 | — |
| Result | Resultado | HandResult | 70 | Won/Lost/Push con color |
| ProfitLoss | P/L | decimal | 70 | "+0.00;-0.00" con color |

### Formato Hand History (texto RichTextBox)

```
═══════════════════════════════════════════
Hand #12345 — 2026-03-22 14:30:05 UTC
Mesa: NL Holdem ($0.25/$0.50)
Situación: OpenRaise | Oponentes: 3
═══════════════════════════════════════════

Hero [Ah Kd] — Button — Stack: $52.30

*** FLOP *** [Qs Jc 3h]
  Equity: 45.2% | Pot Odds: 33.0% | EV: +1.25
  Recomendado: Bet 1/2 (Value)
  Acción: Bet $2.50 | Pot: $6.75

*** TURN *** [Qs Jc 3h] [Td]
  Equity: 78.5% | Pot Odds: 28.0% | EV: +4.80
  Recomendado: Bet 3/4 (Value)
  Acción: Bet $5.00 | Pot: $16.75

*** RIVER *** [Qs Jc 3h Td] [2s]
  Equity: 95.0% | Pot Odds: 25.0% | EV: +8.20
  Recomendado: Bet Pot (Strong Value)
  Acción: Bet $12.00 | Pot: $28.75

───────────────────────────────────────────
RESULTADO: Won (+$8.20)
Stack final: $60.50
═══════════════════════════════════════════
```

#### Colores RichTextBox

| Elemento | Color | RGB |
|---|---|---|
| Headers (`═══`, `Hand #`, `Mesa:`) | Azul acento | AppThemeHelper.Accent (0,123,191) |
| Street headers (`*** FLOP ***`) | Azul claro | Color.DodgerBlue |
| Equity/Stats líneas | Gris medio | AppThemeHelper.PrimaryLight |
| Recomendado | Blanco/Default | — |
| Acción tomada | Amarillo | Color.DarkGoldenrod |
| Won / Profit positivo | Verde | AppThemeHelper.Success |
| Lost / Profit negativo | Rojo | AppThemeHelper.Danger |
| Push | Gris | AppThemeHelper.PrimaryLight |
| Cartas hero `[Ah Kd]` | Blanco bold | — |
| Fondo RichTextBox | Oscuro | Color.FromArgb(30, 33, 45) |

### Flujo de datos

1. **Al entrar a la pestaña** → `LoadSessionsWithStatsAsync()`:
   - Llama `GameLoggerService.GetRecentSessionsAsync(50)`
   - Para cada sesión, llama `GameLoggerService.GetSessionStatsAsync(sessionId)` (query eficiente)
   - Llena `dgvSessions` con los `SessionStatsDto`

2. **Click en sesión** → `dgvSessions_SelectionChanged`:
   - Llama `GameLoggerService.GetHandsForSessionAsync(sessionId)`
   - Llena `dgvSessionHands`
   - Actualiza `lblSessionStats` con stats de la sesión

3. **Click en mano** → `dgvSessionHands_SelectionChanged`:
   - Llama `FormatHandHistory(HandRecord)` → genera RichText con colores
   - Muestra en `rtbHandDetail`

### Nuevo DTO: SessionStatsDto

```csharp
// src/OpenScrape.Domain/Dtos/SessionStatsDto.cs
namespace OpenScrape.Domain.Dtos;

public record SessionStatsDto(
    string Id,
    string SessionId,
    string TableName,
    DateTime StartTime,
    DateTime EndTime,
    decimal BigBlind,
    int TotalHands,
    decimal TotalProfit,
    double BBPer100);
```

### Nuevo método: GameLoggerService.GetSessionStatsAsync

```csharp
public async Task<List<SessionStatsDto>> GetRecentSessionsWithStatsAsync(int count = 50)
{
    await using var session = _store.QuerySession();
    var sessions = await session.Query<GameSession>()
        .OrderByDescending(s => s.EndTime)
        .Take(count)
        .ToListAsync();

    var result = new List<SessionStatsDto>();
    foreach (var gs in sessions)
    {
        var hands = await session.Query<HandRecord>()
            .Where(h => h.GameSessionId == gs.Id)
            .ToListAsync();

        int totalHands = hands.Count;
        decimal totalProfit = hands
            .Where(h => h.Result != HandResult.Unknown)
            .Sum(h => h.HeroStackEnd - h.HeroStackStart);
        double bbPer100 = totalHands > 0 && gs.BigBlind > 0
            ? (double)(totalProfit / gs.BigBlind) / totalHands * 100
            : 0;

        result.Add(new SessionStatsDto(
            gs.Id, gs.SessionId, gs.TableName,
            gs.StartTime, gs.EndTime, gs.BigBlind,
            totalHands, totalProfit, bbPer100));
    }
    return result;
}
```

### Estilo visual (consistente con AppThemeHelper)

Seguir el patrón de `ApplyTablesTabStyle()` existente en FrmMain:
- **Headers grillas**: Fondo `PrimaryDark`, texto blanco, alto 35px
- **Celdas**: Fondo `BackgroundCard`, alternado `BackgroundMain`
- **Selección**: `PrimaryLight` con texto blanco
- **Fuente grillas**: "Segoe UI" 9pt
- **Fuente RichTextBox**: "Consolas" 10pt sobre fondo oscuro
- **Labels título**: "Segoe UI" 10pt Bold, fondo `PrimaryDark`, texto blanco
- **Label stats**: "Segoe UI" 9pt, fondo `BackgroundMain`
- **Sin bordes exteriores** en grillas, borde horizontal simple entre filas

---

## Tasks

### 1. Crear SessionStatsDto
- **Archivo**: `src/OpenScrape.Domain/Dtos/SessionStatsDto.cs`
- **Acción**: Crear record `SessionStatsDto` con campos: Id, SessionId, TableName, StartTime, EndTime, BigBlind, TotalHands, TotalProfit, BBPer100
- **Verificación**: Build limpio

### 2. Agregar GetRecentSessionsWithStatsAsync en GameLoggerService
- **Archivo**: `src/OpenScrape.App/Services/GameLoggerService.cs`
- **Acción**: Nuevo método `GetRecentSessionsWithStatsAsync(int count = 50)` que consulta sesiones y calcula stats agregados por cada una
- **Import**: `using OpenScrape.Domain.Dtos;`
- **Verificación**: Build limpio

### 3. Agregar controles de la pestaña Historial en el Designer
- **Archivo**: `src/OpenScrape.App/Forms/FrmMain.Designer.cs`
- **Acción**: Agregar `TabPage tpHistorial` al `TabControl` existente, con árbol de controles: `splitHistorialMain` → `splitHistorialLeft` → `dgvSessions` + `dgvSessionHands` + `rtbHandDetail` + `lblSessionStats` + `lblSesionesTitle` + `lblManosTitle`
- **Detalle**: Declarar todos los controles como campos privados, configurar propiedades básicas (Name, Dock, Text, ReadOnly, etc). Ver sección "Árbol de controles" del diseño.
- **Verificación**: Build limpio (UI aún sin lógica)

### 4. Implementar InitializeHistorialTab()
- **Archivo**: `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción**: Nuevo método `InitializeHistorialTab()` llamado desde el constructor (después de `InitializeComponent()`):
  - Configurar columnas de `dgvSessions` (6 columnas según diseño)
  - Configurar columnas de `dgvSessionHands` (6 columnas según diseño)
  - Aplicar estilos con `AppThemeHelper` (seguir patrón de `ApplyTablesTabStyle()`)
  - Configurar `rtbHandDetail`: fondo oscuro `Color.FromArgb(30, 33, 45)`, fuente "Consolas" 10pt, ReadOnly, ForeColor blanco
  - Suscribir eventos: `dgvSessions.SelectionChanged`, `dgvSessionHands.SelectionChanged`, `tpHistorial.Enter` (para lazy load)
- **Verificación**: Build limpio, pestaña visible con grillas vacías y estilos aplicados

### 5. Implementar carga de sesiones (LoadSessionsWithStatsAsync)
- **Archivo**: `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción**: Nuevo método `async Task LoadSessionsWithStatsAsync()`:
  - Llamar a `_gameLoggerService.GetRecentSessionsWithStatsAsync()`
  - Crear lista anónima/DTO con columnas formateadas (duración calculada como `EndTime - StartTime`)
  - Asignar a `dgvSessions.DataSource`
  - Aplicar colores condicionales en `CellFormatting` (verde/rojo para Profit y BB/100)
  - Trigger: evento `tpHistorial.Enter` (lazy load, solo la primera vez o con botón refresh)
- **Verificación**: Build limpio, grilla de sesiones se llena con datos reales

### 6. Implementar carga de manos al seleccionar sesión
- **Archivo**: `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción**: Handler `dgvSessions_SelectionChanged`:
  - Obtener `SessionId` de la fila seleccionada
  - Llamar `_gameLoggerService.GetHandsForSessionAsync(gameSessionId)`
  - Mapear a lista con columnas formateadas (Cards concatenado, P/L calculado, posición abreviada)
  - Asignar a `dgvSessionHands.DataSource`
  - Actualizar `lblSessionStats` con stats de la sesión
  - Aplicar colores condicionales (Result y P/L)
- **Verificación**: Build limpio, seleccionar sesión carga sus manos

### 7. Implementar FormatHandHistory y visualización de mano
- **Archivo**: `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción**: 
  - Método `FormatHandHistory(HandRecord hand, decimal bigBlind)`:
    - Limpiar `rtbHandDetail`
    - Construir texto progresivamente con `AppendColoredText(text, color)` helper
    - Header: Hand#, fecha, mesa, situación, oponentes
    - Hero: cartas, posición, stack
    - Por cada `StreetDecision` en `hand.Decisions`: header de street con board progresivo, equity/pot odds/EV, acción recomendada, acción tomada, pot
    - Footer: resultado con color, stack final, P/L
    - Colores según tabla del diseño
  - Helper `AppendColoredText(RichTextBox rtb, string text, Color color, bool bold = false)`
  - Handler `dgvSessionHands_SelectionChanged`: obtener `HandRecord` seleccionado, llamar `FormatHandHistory`
- **Verificación**: Build limpio, seleccionar mano muestra hand history formateado con colores

### 8. Build y test final
- **Acción**: `dotnet build OpenScrape.sln` → 0 errores nuevos. `dotnet test OpenScrape.sln` → 322+ tests pasando.
- **Verificación manual**: Abrir la app, ir a pestaña Historial, verificar que las sesiones cargan, que al seleccionar una sesión cargan las manos, y que al seleccionar una mano se muestra el hand history formateado.

---

## Notas de implementación

### Archivos a crear
- `src/OpenScrape.Domain/Dtos/SessionStatsDto.cs`

### Archivos a modificar
- `src/OpenScrape.App/Services/GameLoggerService.cs` — nuevo método
- `src/OpenScrape.App/Forms/FrmMain.Designer.cs` — controles de la pestaña
- `src/OpenScrape.App/Forms/FrmMain.cs` — lógica de la pestaña (constructor, handlers, formateo)

### Patrones a seguir
- `ApplyTablesTabStyle()` en FrmMain.cs — para estilizar las grillas
- `dgvHands` en pestaña Tablas — referencia de DataGridView existente
- `FrmOverlay` — referencia de tema oscuro para el RichTextBox
- `AppThemeHelper` — paleta de colores consistente

### Riesgos
- **Rendimiento**: `GetRecentSessionsWithStatsAsync` hace N+1 queries (1 por sesión). Si hay muchas sesiones, podría ser lento. Mitigación: limitar a 50 sesiones y lazy-load.
- **Designer.cs manual**: Editar el Designer a mano es frágil. Alternativa: crear los controles programáticamente en `InitializeHistorialTab()` en vez del Designer.
