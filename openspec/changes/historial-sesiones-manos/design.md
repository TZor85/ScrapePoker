## Layout de la pestaña

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

## Árbol de controles

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

## Columnas de dgvSessions

| Columna | Header | Tipo | Width | Formato |
|---|---|---|---|---|
| TableName | Mesa | string | 120 | — |
| StartTime | Inicio | DateTime | 130 | "dd/MM HH:mm" |
| Duration | Duración | string | 80 | "Xh Ym" calculado |
| TotalHands | Manos | int | 60 | — |
| TotalProfit | Profit | decimal | 80 | "+0.00;-0.00" con color verde/rojo |
| BBPer100 | BB/100 | double | 70 | "+0.0;-0.0" con color verde/rojo |

## Columnas de dgvSessionHands

| Columna | Header | Tipo | Width | Formato |
|---|---|---|---|---|
| HandNumber | Hand# | long | 70 | — |
| Cards | Cartas | string | 80 | "HeroCard1 HeroCard2" concatenado |
| HeroPosition | Pos | TablePosition | 60 | Abreviado (BTN, CO, MP, EP, SB, BB) |
| LastStreetPlayed | Street | BoardPosition | 60 | — |
| Result | Resultado | HandResult | 70 | Won/Lost/Push con color |
| ProfitLoss | P/L | decimal | 70 | "+0.00;-0.00" con color |

## Formato Hand History (RichTextBox)

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

## Colores RichTextBox

| Elemento | Color | Referencia |
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

## Flujo de datos

1. **Al entrar a la pestaña** → `LoadSessionsWithStatsAsync()`:
   - Llama `GameLoggerService.GetRecentSessionsWithStatsAsync(50)`
   - Llena `dgvSessions` con los `SessionStatsDto`

2. **Click en sesión** → `dgvSessions_SelectionChanged`:
   - Llama `GameLoggerService.GetHandsForSessionAsync(sessionId)`
   - Llena `dgvSessionHands`
   - Actualiza `lblSessionStats` con stats de la sesión

3. **Click en mano** → `dgvSessionHands_SelectionChanged`:
   - Llama `FormatHandHistory(HandRecord)` → genera RichText con colores
   - Muestra en `rtbHandDetail`

## Nuevo DTO: SessionStatsDto

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

## Nuevo método: GameLoggerService.GetRecentSessionsWithStatsAsync

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

## Estilo visual (consistente con AppThemeHelper)

Seguir el patrón de `ApplyTablesTabStyle()` existente en FrmMain:
- **Headers grillas**: Fondo `PrimaryDark`, texto blanco, alto 35px
- **Celdas**: Fondo `BackgroundCard`, alternado `BackgroundMain`
- **Selección**: `PrimaryLight` con texto blanco
- **Fuente grillas**: "Segoe UI" 9pt
- **Fuente RichTextBox**: "Consolas" 10pt sobre fondo oscuro
- **Labels título**: "Segoe UI" 10pt Bold, fondo `PrimaryDark`, texto blanco
- **Label stats**: "Segoe UI" 9pt, fondo `BackgroundMain`
- **Sin bordes exteriores** en grillas, borde horizontal simple entre filas

## Riesgos y mitigaciones

- **Rendimiento N+1 queries**: `GetRecentSessionsWithStatsAsync` hace 1 query por sesión para obtener sus manos. Mitigación: límite de 50 sesiones y lazy-load al entrar a la pestaña.
- **Designer.cs manual**: Editar el Designer a mano es frágil. Alternativa viable: crear los controles programáticamente en `InitializeHistorialTab()`.
