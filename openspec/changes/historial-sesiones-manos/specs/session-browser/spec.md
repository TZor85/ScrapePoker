## ADDED Requirements

### Requirement: Crear DTO SessionStatsDto para datos agregados de sesión
El sistema SHALL exponer un record `SessionStatsDto` en `OpenScrape.Domain.Dtos` con los campos: Id, SessionId, TableName, StartTime, EndTime, BigBlind, TotalHands, TotalProfit y BBPer100.

#### Scenario: DTO contiene todos los campos necesarios
- **WHEN** se instancia un `SessionStatsDto`
- **THEN** contiene las 9 propiedades requeridas para representar una sesión con sus estadísticas agregadas

---

### Requirement: Consultar sesiones recientes con estadísticas
El sistema SHALL proveer un método `GetRecentSessionsWithStatsAsync(int count = 50)` en `GameLoggerService` que consulte las sesiones más recientes ordenadas por `EndTime` descendente y calcule para cada una: TotalHands, TotalProfit y BBPer100 a partir de sus `HandRecord` asociados.

#### Scenario: Sesiones con manos persistidas
- **WHEN** se llama a `GetRecentSessionsWithStatsAsync(50)`
- **THEN** retorna hasta 50 `SessionStatsDto` ordenados por fecha descendente, con TotalHands = count de HandRecords, TotalProfit = sum(HeroStackEnd - HeroStackStart) de manos con Result != Unknown, y BBPer100 calculado correctamente

#### Scenario: Sesión sin manos
- **WHEN** existe una sesión en BD sin HandRecords asociados
- **THEN** se incluye en los resultados con TotalHands = 0, TotalProfit = 0 y BBPer100 = 0

#### Scenario: No hay sesiones
- **WHEN** no hay sesiones en la BD
- **THEN** retorna una lista vacía

---

### Requirement: Mostrar grilla de sesiones en pestaña Historial
El sistema SHALL mostrar una pestaña "Historial" en el `TabControl` de `FrmMain` con una grilla `dgvSessions` que contenga 6 columnas: Mesa (string), Inicio (DateTime "dd/MM HH:mm"), Duración (string "Xh Ym"), Manos (int), Profit (decimal con color verde/rojo) y BB/100 (double con color verde/rojo).

#### Scenario: Entrar a la pestaña carga sesiones
- **WHEN** el usuario navega a la pestaña Historial por primera vez
- **THEN** se ejecuta `LoadSessionsWithStatsAsync()` y la grilla `dgvSessions` se llena con las sesiones recientes

#### Scenario: Colores condicionales en Profit y BB/100
- **WHEN** una sesión tiene Profit > 0 o BB/100 > 0
- **THEN** la celda correspondiente se muestra en verde (`AppThemeHelper.Success`)
- **WHEN** una sesión tiene Profit < 0 o BB/100 < 0
- **THEN** la celda correspondiente se muestra en rojo (`AppThemeHelper.Danger`)

#### Scenario: Duración formateada
- **WHEN** una sesión tiene StartTime = 14:00 y EndTime = 16:30
- **THEN** la columna Duración muestra "2h 30m"

---

### Requirement: Cargar manos al seleccionar sesión
El sistema SHALL cargar las manos de la sesión seleccionada en `dgvSessionHands` al hacer click en una fila de `dgvSessions`. La grilla de manos SHALL contener 6 columnas: Hand# (long), Cartas (string "HeroCard1 HeroCard2"), Pos (abreviado: BTN/CO/MP/EP/SB/BB), Street (BoardPosition), Resultado (HandResult con color) y P/L (decimal con color).

#### Scenario: Seleccionar sesión carga sus manos
- **WHEN** el usuario selecciona una fila en `dgvSessions`
- **THEN** se llama `GetHandsForSessionAsync(gameSessionId)` y `dgvSessionHands` se llena con las manos de esa sesión

#### Scenario: Actualizar barra de estado
- **WHEN** el usuario selecciona una sesión
- **THEN** `lblSessionStats` muestra: "Sesión: {Mesa} | {Manos} manos | {Profit} | {BB/100} BB/100"

#### Scenario: Colores en resultado y P/L
- **WHEN** una mano tiene Result = Won
- **THEN** la celda Resultado se muestra en verde y P/L muestra valor positivo en verde
- **WHEN** una mano tiene Result = Lost
- **THEN** la celda Resultado se muestra en rojo y P/L muestra valor negativo en rojo

---

### Requirement: Estilo visual consistente con la aplicación
La pestaña Historial SHALL seguir el patrón de estilo de `ApplyTablesTabStyle()`: headers con fondo `PrimaryDark` y texto blanco (alto 35px), celdas con fondo `BackgroundCard` y alternado `BackgroundMain`, selección con `PrimaryLight`, fuente "Segoe UI" 9pt en grillas, labels título en "Segoe UI" 10pt Bold.

#### Scenario: Estilos aplicados al inicializar
- **WHEN** se llama a `InitializeHistorialTab()`
- **THEN** ambas grillas (`dgvSessions` y `dgvSessionHands`) tienen headers con fondo oscuro, filas alternas, selección con color de acento, y sin bordes exteriores
