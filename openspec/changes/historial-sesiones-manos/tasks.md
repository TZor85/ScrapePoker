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
  - Crear lista con columnas formateadas (duración calculada como `EndTime - StartTime`)
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
- **Acción**: `dotnet build OpenScrape.sln` → 0 errores nuevos. `dotnet test OpenScrape.sln` → tests pasando.
- **Verificación manual**: Abrir la app, ir a pestaña Historial, verificar que las sesiones cargan, que al seleccionar una sesión cargan las manos, y que al seleccionar una mano se muestra el hand history formateado.
