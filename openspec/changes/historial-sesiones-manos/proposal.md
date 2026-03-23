## Why

Las manos jugadas se persisten como documentos `HandRecord` en PostgreSQL vía Marten, pero no hay ninguna pantalla en la aplicación para consultarlas. El usuario solo ve el log en tiempo real (pestaña Logs) sin poder explorar el historial completo de sesiones ni el detalle de cada mano — cartas, decisiones por street, equity, acciones recomendadas vs tomadas, y resultado. Sin esta visibilidad, el análisis post-sesión requiere consultar la base de datos directamente.

## What Changes

- Se añade una nueva pestaña **"Historial"** en el `TabControl` principal de `FrmMain`.
- Se implementa una **grilla de sesiones** (`dgvSessions`) con estadísticas agregadas: mesa, fecha, duración, total de manos, profit y BB/100.
- Se implementa una **grilla de manos** (`dgvSessionHands`) que muestra las manos de la sesión seleccionada: Hand#, cartas, posición, última street, resultado y P/L.
- Se implementa un **panel de detalle** (`rtbHandDetail`) con formato Hand History clásico (estilo PokerStars) con colores diferenciados por sección.
- Se añade una **barra de estado** (`lblSessionStats`) con stats resumidos de la sesión seleccionada.
- Se crea un nuevo `SessionStatsDto` en el dominio y un método de query eficiente `GetRecentSessionsWithStatsAsync` en `GameLoggerService`.

## Capabilities

### New Capabilities

- `session-browser`: Grilla de sesiones recientes con estadísticas calculadas (manos, profit, BB/100, duración). Permite seleccionar una sesión para ver sus manos. Carga lazy al entrar a la pestaña.
- `hand-history-viewer`: Visualización detallada de una mano seleccionada en formato Hand History con colores. Muestra header, cartas del héroe, decisiones por cada street (equity, pot odds, EV, acción recomendada vs tomada) y resultado final.

### Modified Capabilities

- `GameLoggerService`: Se añade el método `GetRecentSessionsWithStatsAsync(int count)` que calcula stats agregados por sesión consultando `HandRecord` agrupados por `GameSessionId`.

## Impact

- **`src/OpenScrape.Domain/Dtos/`**: Se crea `SessionStatsDto.cs` (record con 9 campos).
- **`src/OpenScrape.App/Services/GameLoggerService.cs`**: Se añade método `GetRecentSessionsWithStatsAsync`.
- **`src/OpenScrape.App/Forms/FrmMain.Designer.cs`**: Se añade `TabPage tpHistorial` con 7 controles hijos (2 SplitContainer, 2 DataGridView, 1 RichTextBox, 2 Label).
- **`src/OpenScrape.App/Forms/FrmMain.cs`**: Se añaden métodos de inicialización, carga de datos, handlers de selección y formateo de Hand History.
- **Sin nuevas dependencias externas**.
