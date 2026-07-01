# Specs: refactor-frmmain

## Visión General

`FrmMain.cs` (3867 LOC) concentra BackgroundWorker, detección de píxeles, captura de pantalla, orquestación OCR, procesamiento de manos, actualización de UI, estilos visuales y tab pages. Este cambio descompone la God Class en 8 servicios especializados + 1 UserControl, reduciendo FrmMain a presentación pura (~900–1500 LOC). La Fase 4 activa la feature flag `UseGameLoopCoordinator = true`, cortando al game loop real que reemplaza `EmptyGameLoopTickProcessor` por `HandProcessor`.

**Principio rector**: paridad funcional estricta. Ningún cambio observable por el usuario. Cada fase extrae sin alterar comportamiento.

---

## Requerimientos Funcionales

### RF1: DetectionLoopService

**Descripción**: Extraer el loop de detección de píxeles (`BackgroundWorker1_DoWork`, `PerformEnhancedDetection`, `ReadPixelFromBuffer`) a un servicio independiente que se ejecuta en segundo plano y notifica detecciones vía eventos.

**Criterios de Aceptación**:
- [ ] El servicio ejecuta el loop de detección en un `Task.Run` con `while (_executeCapture)`, reemplazando `BackgroundWorker`
- [ ] El servicio DEBE (`MUST`) detectar turnos del héroe comparando el color del píxel de `uAction` contra `HeroActionColor` con tolerancia configurable
- [ ] El servicio DEBE (`MUST`) detectar cambios de calle (flop) comparando el color del píxel de `isFlop` contra `FlopDetectedColor`
- [ ] El muestreo de píxeles usa patrón de cruz (±2 px) y acceso directo a buffer vía `LockBits` (mismo algoritmo que `PerformEnhancedDetection`)
- [ ] Las detecciones se notifican a la UI mediante eventos `HeroTurnDetected` y `FlopDetectedEvent`, sin dependencia directa de `this.Invoke()`
- [ ] El watchdog de timeout y las métricas de detección (`TotalChecks`, `Detections`, `Errors`) se mantienen sin cambios
- [ ] Los logs de detección (`_detectionLoggerService`) se preservan con idéntica verbosidad
- [ ] El delay adaptativo post-detección (200ms / 100ms) se preserva

**Escenarios**:

#### Escenario: Detección de turno del héroe
- **GIVEN** el loop de detección está activo y el overlay visible
- **AND** `GetWindowsScreenUseCase` retorna un bitmap válido de la ventana de póker
- **WHEN** el color del píxel `uAction` coincide con `HeroActionColor` dentro de la tolerancia
- **THEN** el servicio emite el evento `HeroTurnDetected`
- **AND** `_gameLoopStateMachine` transiciona al estado correspondiente

#### Escenario: Overlay no visible detiene el loop
- **GIVEN** el loop de detección está activo
- **WHEN** `_frmOverlay` es null o no está visible
- **THEN** el servicio DEBE (`MUST`) cancelar el loop y notificar `DetectionStopped`

#### Escenario: Captura inválida no detiene el loop
- **GIVEN** el loop de detección está activo
- **WHEN** `GetWindowsScreenUseCase` retorna un bitmap nulo o de dimensiones ≤ 1x1
- **THEN** el servicio DEBE (`MUST`) esperar 500ms y continuar (sin cancelar)

**Requerimientos No Funcionales**:
- **Thread safety**: El acceso a `_executeCapture` DEBE (`MUST`) estar sincronizado
- **Performance**: `LockBits` y lectura directa de buffer se preservan sin overhead adicional
- **Resiliencia**: Errores en el loop interno no deben propagarse; se loguean y se continúa

---

### RF2: CaptureService

**Descripción**: Extraer la captura de screenshot de la ventana de póker y el escalado de coordenadas (`GetImageWhilePlaying`, `SaveReferenceDimensionsToConfig`, `GetScaledRegion`, `GetJsonValue`) a un servicio dedicado.

**Criterios de Aceptación**:
- [ ] `GetImageWhilePlaying` DEBE (`MUST`) capturar la ventana de póker vía `GetWindowsScreenUseCase.Execute(_handle)` y devolver un `Bitmap` válido
- [ ] Si `_handle` es `IntPtr.Zero`, el servicio DEBE (`MUST`) loguear error y retornar sin lanzar excepción
- [ ] `SaveReferenceDimensionsToConfig` DEBE (`MUST`) persistir dimensiones de referencia en `appsettings.json`
- [ ] `GetScaledRegion` DEBE (`MUST`) aplicar `_coordinateScaler` para mapear coordenadas de región a píxeles reales según dimensiones de referencia
- [ ] `GetJsonValue` DEBE (`MUST`) extraer valores de configuración con el mismo comportamiento actual
- [ ] El servicio NO DEBE (`MUST NOT`) gestionar el ciclo de vida de `_handle` ni de `_session`

**Escenarios**:

#### Escenario: Captura exitosa de ventana
- **GIVEN** `_handle` apunta a una ventana de póker válida
- **WHEN** se invoca `GetImageWhilePlaying()`
- **THEN** retorna un `Bitmap` con dimensiones > 1x1
- **AND** las dimensiones de referencia se persisten si es la primera captura

#### Escenario: Handle inválido no lanza excepción
- **GIVEN** `_handle` es `IntPtr.Zero`
- **WHEN** se invoca `GetImageWhilePlaying()`
- **THEN** se loguea error y se retorna sin lanzar excepción

---

### RF3: TableReaderService

**Descripción**: Extraer la orquestación de lecturas OCR de la mesa (`SetBetPlayer`, `SetHeroStack`, `SetPotValue`, `SetTableHand`, `ObtainCardsPlayerAsync`, `IsBoardCardVisible`, `CountVisibleBoardCards`, `DetectNewHand`) a un servicio dedicado.

**Criterios de Aceptación**:
- [ ] `SetBetPlayer` DEBE (`MUST`) leer regiones de apuesta vía OCR y actualizar `_playerGameState` con los valores parseados
- [ ] `SetHeroStack` DEBE (`MUST`) leer el stack del héroe y actualizar `_playerGameState`
- [ ] `SetPotValue` DEBE (`MUST`) leer el valor del bote y actualizar `_playerGameState`
- [ ] `SetTableHand` DEBE (`MUST`) leer el número de mano de la mesa y detectar cambios de mano (`DetectNewHand`)
- [ ] `ObtainCardsPlayerAsync` DEBE (`MUST`) leer cartas del héroe y cartas comunitarias usando `_imageCropperService` y `_cardCacheService`
- [ ] `IsBoardCardVisible` y `CountVisibleBoardCards` DEBEN (`MUST`) preservar el algoritmo actual de detección de cartas comunitarias visibles
- [ ] Todas las lecturas OCR usan `GetScaledRegion` del `CaptureService` para escalado de coordenadas
- [ ] Los reintentos y timeouts de OCR se preservan sin cambios

**Escenarios**:

#### Escenario: Detección de nueva mano
- **GIVEN** el número de mano actual en `_playerGameState` es "123"
- **WHEN** `SetTableHand` lee "124" vía OCR
- **THEN** `DetectNewHand` retorna `true` y se dispara el flujo `HandleNewHandAsync`

#### Escenario: Lectura de cartas del jugador
- **GIVEN** la región de cartas del héroe contiene texto visible
- **WHEN** se invoca `ObtainCardsPlayerAsync`
- **THEN** las cartas se recortan vía `_imageCropperService`, se comparan con `_cardCacheService` y se actualiza `_playerGameState`

**Requerimientos No Funcionales**:
- **Thread safety**: `_playerGameState` DEBE (`MUST`) ser thread-safe o accedido secuencialmente
- **Dependencia**: RF3 depende de RF2 (`CaptureService.GetScaledRegion`)

---

### RF4: HandProcessor

**Descripción**: Extraer el pipeline de procesamiento de manos (`ProcessTableInfoAsync`, `ProcessPreflopAsync`, `SetPreflopAggressors`, `ProcessPostFlopAsync`, `ProcessFlopAsync`, `ProcessTurnAsync`, `ProcessRiverAsync`, `HandleNewHandAsync`, `GetPreflopHeroPosition`) a un servicio que implementa `IGameLoopTickProcessor`, reemplazando `EmptyGameLoopTickProcessor`. Activar feature flag `UseGameLoopCoordinator = true`.

**Criterios de Aceptación**:
- [ ] `HandProcessor` implementa `IGameLoopTickProcessor.ExecuteAsync(CancellationToken)` y retorna `Task<GameLoopResult>`
- [ ] `ExecuteAsync` DEBE (`MUST`) orquestar: captura → lectura de mesa → detección de fase → procesamiento → cálculo → resultado
- [ ] `ProcessTableInfoAsync` DEBE (`MUST`) invocar `SetPotValue`, `ObtainCardsPlayerAsync`, y decidir si procesar preflop o postflop
- [ ] `ProcessPreflopAsync` DEBE (`MUST`) determinar agresores, posición del héroe, y calcular odds preflop
- [ ] `ProcessFlopAsync`, `ProcessTurnAsync`, `ProcessRiverAsync` DEBEN (`MUST`) calcular odds postflop con `_pokerCalculator` y `_postflopDecisionService`
- [ ] `HandleNewHandAsync` DEBE (`MUST`) resetear estado de mano e inicializar tracking
- [ ] El feature flag `UseGameLoopCoordinator = true` DEBE (`MUST`) activar el flujo real; `false` DEBE (`MUST`) mantener `EmptyGameLoopTickProcessor`
- [ ] El `GameLoopResult` retornado DEBE (`MUST`) contener todos los datos necesarios para `UiUpdateService` (pot odds, cartas, estado de calle, decisión)

**Escenarios**:

#### Escenario: Procesamiento de mano preflop
- **GIVEN** el estado del game loop es `Preflop`
- **AND** `CaptureService` retorna un screenshot válido
- **AND** `TableReaderService` leyó bets, stacks y cartas del héroe
- **WHEN** `ExecuteAsync` es invocado
- **THEN** `ProcessPreflopAsync` determina posición del héroe y agresores
- **AND** `_pokerCalculator` calcula odds preflop
- **AND** se retorna `GameLoopResult` con los datos de la decisión

#### Escenario: Feature flag desactivado mantiene EmptyGameLoopTickProcessor
- **GIVEN** `UseGameLoopCoordinator` es `false`
- **WHEN** `GameLoopCoordinator` inicia el loop
- **THEN** `EmptyGameLoopTickProcessor` procesa los ticks (sin lógica real)
- **AND** el resultado es `GameLoopResult { Empty = true }`

#### Escenario: Rollback instantáneo vía feature flag
- **GIVEN** `UseGameLoopCoordinator` es `true` y el bot está corriendo
- **WHEN** se cambia a `false` (sin reiniciar la app)
- **THEN** el siguiente tick usa `EmptyGameLoopTickProcessor`

**Requerimientos No Funcionales**:
- **Thread safety**: `HandProcessor` DEBE (`MUST`) soportar cancelación vía `CancellationToken`
- **Dependencia**: RF4 depende de RF1, RF2, RF3
- **Feature flag**: DEBE (`MUST`) leerse de `appsettings.json` o variable de entorno, con recarga en caliente

---

### RF5: UiUpdateService

**Descripción**: Ampliar `IUiSyncService` existente para encapsular toda la actualización de controles WinForms y overlay (`UpdateUIWithResults`, `UpdateOverlayWithPotOdds`, `UpdateResumeTextForPreflop/Flop`, `UpdatePlayerIndicators`, `UpdateCardImages`, `GetCardImage`). Hoy `IUiSyncService` solo sincroniza `GameLoopResult`.

**Criterios de Aceptación**:
- [ ] `UpdateUIWithResults` DEBE (`MUST`) actualizar labels de odds, outs, equity y recomendación en el formulario
- [ ] `UpdateOverlayWithPotOdds` DEBE (`MUST`) proyectar la decisión (fold/check/call/raise) en `_frmOverlay` usando `OverlayPositioner`
- [ ] `UpdateResumeTextForPreflop` y `UpdateResumeTextForFlop` DEBEN (`MUST`) generar texto de resumen en `txtResumen`
- [ ] `UpdatePlayerIndicators` DEBE (`MUST`) actualizar indicadores visuales de posición y acción de cada jugador
- [ ] `UpdateCardImages` DEBE (`MUST`) renderizar imágenes de cartas del héroe y comunitarias
- [ ] El marshaling al UI thread DEBE (`MUST`) usar `ISynchronizeInvoke.Invoke` (mismo mecanismo actual de `IUiSyncService`)
- [ ] El servicio NO DEBE (`MUST NOT`) contener lógica de negocio ni de decisión

**Escenarios**:

#### Escenario: Actualización de overlay con odds
- **GIVEN** `GameLoopResult` contiene `PokerCalculationResult` con decisión "Call" y pot odds 2.5:1
- **WHEN** `UiUpdateService` procesa el resultado
- **THEN** el overlay muestra "CALL" con las odds calculadas
- **AND** el label de odds en FrmMain se actualiza

#### Escenario: Sin resultado no lanza excepción
- **GIVEN** `GameLoopResult` tiene `Empty = true` (sin datos de decisión)
- **WHEN** `UiUpdateService` procesa el resultado
- **THEN** no se lanza excepción y los controles mantienen su estado actual

---

### RF6: ThemeService

**Descripción**: Extraer la aplicación de estilos visuales (`InitializeVisualStyles`, `ApplyModernTabStyle`, `ApplyGameTabStyle`, `ApplyConfigTabStyle`, etc.) a un servicio dedicado.

**Criterios de Aceptación**:
- [ ] `InitializeVisualStyles` DEBE (`MUST`) aplicar la paleta de colores, fuentes y estilos de botones al formulario
- [ ] `ApplyModernTabStyle` DEBE (`MUST`) configurar el `TabControl` principal con tabs modernos (colores, padding, bordes)
- [ ] `ApplyGameTabStyle`, `ApplyConfigTabStyle`, etc. DEBEN (`MUST`) aplicar estilos específicos a cada tab page
- [ ] Los estilos DEBEN (`MUST`) ser visualmente idénticos a los actuales (mismos colores, fuentes, tamaños)
- [ ] El servicio NO DEBE (`MUST NOT`) contener lógica de negocio ni referencias a `_playerGameState`
- [ ] El servicio DEBE (`MUST`) aceptar los controles a estilizar por parámetro (sin acoplarse a FrmMain)

**Escenarios**:

#### Escenario: Aplicación de tema oscuro al TabControl
- **GIVEN** `ThemeService` recibe un `TabControl` con 4 tabs
- **WHEN** se invoca `ApplyModernTabStyle(tabControl)`
- **THEN** todos los tabs tienen fondo oscuro, texto claro, y bordes redondeados
- **AND** el tab seleccionado tiene color de acento distintivo

---

### RF7: TabPageContributions

**Descripción**: Extraer la inicialización de tabs de historial, bankroll y métricas (`InitializeHistorialTab`, `InitializeBankrollTab`, `InitializeMetricsTab`) a `UserControl` independientes.

**Criterios de Aceptación**:
- [ ] Cada tab (Historial, Bankroll, Métricas) DEBE (`MUST`) ser un `UserControl` independiente con su propia lógica de inicialización
- [ ] `HistorialTabControl` DEBE (`MUST`) mostrar el historial de manos con `DataGridView` y handlers de selección
- [ ] `BankrollTabControl` DEBE (`MUST`) mostrar gráficos de bankroll y estadísticas acumuladas
- [ ] `MetricsTabControl` DEBE (`MUST`) mostrar métricas de sesión (manos por hora, win rate, VPIP, PFR)
- [ ] Los `UserControl` NO DEBEN (`MUST NOT`) acceder directamente a `FrmMain`; reciben datos por propiedades o métodos públicos
- [ ] Los handlers de eventos de cada tab DEBEN (`MUST`) preservarse con idéntico comportamiento
- [ ] El estado de cada tab DEBE (`MUST`) mantenerse al cambiar entre tabs

**Escenarios**:

#### Escenario: Navegación entre tabs preserva estado
- **GIVEN** el tab de Bankroll está mostrando un gráfico con datos de sesión
- **WHEN** el usuario cambia al tab de Historial y luego vuelve a Bankroll
- **THEN** el gráfico de Bankroll muestra los mismos datos sin recalcular

---

### RF8: RegionEditor

**Descripción**: Extraer el editor de regiones (`LoadTreeViewRegions`, `UpdateRegionDisplay`, `btnTestColor_Click`, `btnTestTexto_Click`, `btnTestCarta_Click` y handlers relacionados) a un `UserControl` independiente (`RegionEditorControl`).

**Criterios de Aceptación**:
- [ ] `RegionEditorControl` DEBE (`MUST`) ser un `UserControl` autocontenido con `TreeView`, `PictureBox`, controles de test y panel de propiedades
- [ ] `LoadTreeViewRegions` DEBE (`MUST`) cargar las regiones desde la configuración y poblarlas en el `TreeView` categorizado
- [ ] `UpdateRegionDisplay` DEBE (`MUST`) mostrar la región seleccionada con rectángulo de selección en el `PictureBox`
- [ ] `btnTestColor_Click` DEBE (`MUST`) capturar y mostrar el color del píxel en la posición de la región testeada
- [ ] `btnTestTexto_Click` DEBE (`MUST`) ejecutar OCR sobre la región y mostrar el texto reconocido
- [ ] `btnTestCarta_Click` DEBE (`MUST`) ejecutar reconocimiento de carta sobre la región y mostrar resultado
- [ ] El control NO DEBE (`MUST NOT`) acceder directamente a `FrmMain`; recibe dependencias (`_screenReader`, `_ocrService`, `_imageCropperService`) por constructor o inyección
- [ ] Las operaciones de arrastrar y redimensionar regiones en el `PictureBox` DEBEN (`MUST`) preservarse

**Escenarios**:

#### Escenario: Test de color en región
- **GIVEN** `RegionEditorControl` tiene una región seleccionada y una captura de pantalla cargada
- **WHEN** el usuario hace clic en "Test Color"
- **THEN** se muestra el color RGB del píxel en la posición (PosX, PosY) de la región
- **AND** se compara visualmente con los colores configurados para esa región

#### Escenario: Test de OCR en región
- **GIVEN** `RegionEditorControl` tiene una región de texto seleccionada y una captura cargada
- **WHEN** el usuario hace clic en "Test Texto"
- **THEN** se ejecuta OCR sobre la región y se muestra el texto reconocido en un `TextBox` de resultado

**Requerimientos No Funcionales**:
- `RegionEditorControl` DEBE (`MUST`) poder usarse en un formulario independiente para testing sin FrmMain

---

## Requerimiento Modificado: GameLoopCoordinator

**Descripción**: `IGameLoopTickProcessor` actualmente usa `EmptyGameLoopTickProcessor` (retorna `GameLoopResult` vacío). Se reemplaza por `HandProcessor` (RF4) que contiene la lógica real del pipeline de decisión. La interfaz `IGameLoopTickProcessor` NO cambia.

**Criterios de Aceptación**:
- [ ] `HandProcessor` se registra en DI como implementación de `IGameLoopTickProcessor`
- [ ] `GameLoopCoordinator` recibe `HandProcessor` vía constructor cuando `UseGameLoopCoordinator = true`
- [ ] La interfaz `IGameLoopTickProcessor` permanece sin cambios (mismo contrato: `Task<GameLoopResult> ExecuteAsync(CancellationToken)`)
- [ ] `EmptyGameLoopTickProcessor` se conserva como fallback (no se elimina)
- [ ] `GameLoopCoordinator.ResultReady` sigue emitiendo `GameLoopResult`; `IUiSyncService` sigue suscribiéndose sin cambios

**Escenario**: Cutover de EmptyGameLoopTickProcessor a HandProcessor
- **GIVEN** `UseGameLoopCoordinator = true` en configuración
- **WHEN** `GameLoopCoordinator.StartAsync` es invocado
- **THEN** `IGameLoopTickProcessor` resuelto es `HandProcessor`
- **AND** cada tick ejecuta el pipeline completo (captura → OCR → decisión)
- **AND** `GameLoopResult` contiene `PokerCalculationResult` con odds y decisión

---

## Dependencias entre Fases

| Fase | RF | Depende de | Notas |
|------|-----|-----------|-------|
| 1 | RF1: DetectionLoopService | — | Sin dependencias; reemplaza BackgroundWorker |
| 2 | RF2: CaptureService | — | Sin dependencias |
| 3 | RF3: TableReaderService | RF2 | Usa `GetScaledRegion` de CaptureService |
| 4 | RF4: HandProcessor | RF1, RF2, RF3 | Activa `UseGameLoopCoordinator`; punto de inflexión |
| 5 | RF5: UiUpdateService | RF4 | Procesa `GameLoopResult` de HandProcessor |
| 6 | RF6: ThemeService | — | Riesgo bajo, sin dependencias de negocio |
| 7 | RF7: TabPageContributions | — | Sin dependencias de negocio |
| 8 | RF8: RegionEditor | RF2 | Usa `CaptureService` para capturas de test |

**Regla de secuenciación**: Las fases 1–4 DEBEN (`MUST`) ejecutarse en orden. Las fases 5–8 PUEDEN (`MAY`) ejecutarse en cualquier orden tras la Fase 4.

---

## Requerimientos No Funcionales Generales

| Categoría | Requerimiento |
|-----------|--------------|
| **Paridad funcional** | Ningún cambio observable por el usuario. El bot DEBE (`MUST`) comportarse idénticamente en sesión real de casino. |
| **Tests** | Los 1163 tests existentes DEBEN (`MUST`) permanecer verdes tras cada fase. |
| **Build** | `dotnet build OpenScrape.sln` DEBE (`MUST`) tener exit code 0 en cada fase. |
| **DI** | Cada servicio nuevo DEBE (`MUST`) tener interfaz explícita y estar registrado en `Program.cs`. |
| **Acoplamiento UI** | Ningún servicio nuevo PUEDE (`MAY`) referenciar `Control`, `Form` ni WinForms, salvo RF5 (UiUpdateService) y RF7/RF8 (UserControls). |
| **Commits** | Cada fase DEBE (`MUST`) ser un commit separado con mensaje convencional en castellano. |
| **Rollback** | La feature flag de Fase 4 DEBE (`MUST`) permitir rollback instantáneo sin reiniciar la app. |
| **FrmMain final** | Al completar las 8 fases, FrmMain.cs DEBE (`MUST`) tener ≤ 1500 líneas. |
