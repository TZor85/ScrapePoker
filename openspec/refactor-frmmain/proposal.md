# Propuesta: refactor-frmmain

## Resumen Ejecutivo

`FrmMain.cs` (3867 LOC) es una God Class que concentra BackgroundWorker, detección de píxeles, captura de pantalla, orquestación OCR, procesamiento de manos, actualización de UI, estilos visuales y tab pages. Refactors previos (`refactor-frmmain-coordinators`, `extract-frmmain-testable-logic`) ya extrajeron GameLoopCoordinator (esqueleto), UiSyncService, ActionFormatter y OverlayPositioner, dejando la infraestructura lista para la extracción del resto de responsabilidades.

Este cambio completa la descomposición en **8 fases secuenciales**, extrayendo cada responsabilidad restante a servicios especializados y reduciendo FrmMain a presentación pura (~900-1500 LOC). La Fase 4 activa la feature flag `UseGameLoopCoordinator = true`, cortando al game loop real que hoy usa EmptyGameLoopTickProcessor.

## Alcance

### Incluye
- Extraer DetectionLoopService (BackgroundWorker + detección de píxeles)
- Extraer CaptureService (captura de pantalla + window management)
- Extraer TableReaderService (orquestación OCR + lectura de datos de mesa)
- Extraer HandProcessor (pipeline de procesamiento de manos, reemplaza EmptyGameLoopTickProcessor)
- Extraer UiUpdateService (actualización de controles y overlay)
- Extraer ThemeService (estilos visuales)
- Extraer TabPageContributions (UserControls para tabs de historial/bankroll/métricas)
- Extraer RegionEditor (UserControl de edición de regiones)
- Activar `UseGameLoopCoordinator = true` en Fase 4

### No incluye
- Cambios de comportamiento en el bot (paridad funcional estricta)
- Refactor de DecisionMaker, Infrastructure ni Domain
- Extracción de UseCases de FrmMain (GetWindowsScreenUseCase ya está fuera)
- Migración de WinForms a otro framework UI

## Enfoque

Cada fase sigue el patrón: **analizar dependencias → crear servicio/interfaz → migrar métodos → inyectar → verificar paridad**. Los servicios se extraen sin alterar comportamiento: el BackgroundWorker se reemplaza por un `Task.Run` con `while (_executeCapture)` idéntico, preservando los `this.Invoke()` para UI hasta que UiUpdateService los encapsule. El cutover de Fase 4 es el punto de inflexión: el detector de píxeles y el procesamiento de manos pasan a vivir dentro de `GameLoopCoordinator` vía `IGameLoopTickProcessor`.

Principios: paridad funcional estricta (ningún cambio visible al usuario), tests de regresión al final de cada fase, commits separados por fase.

## Capabilities

### New Capabilities
- `detection-loop-service`: Loop de detección de píxeles independiente de UI, con notificación por eventos
- `capture-service`: Captura de screenshot de ventana de póker con escalado de coordenadas
- `table-reader-service`: Orquestación de lecturas OCR (bets, stacks, pot, cartas, hand number)
- `hand-processor`: Pipeline de procesamiento de mano (preflop, flop, turn, river, decisión)
- `ui-update-service`: Actualización de controles WinForms y overlay
- `theme-service`: Aplicación de estilos visuales consistentes
- `tab-page-contributions`: UserControls para tabs de historial, bankroll y métricas
- `region-editor`: UserControl de edición y testing de regiones

### Modified Capabilities
- `game-loop-coordinator`: El `IGameLoopTickProcessor` actual (EmptyGameLoopTickProcessor) se reemplaza por HandProcessor con la lógica real del pipeline de decisión. La interfaz `IGameLoopTickProcessor` no cambia.

## Fases Detalladas

### Fase 1: DetectionLoopService
- **Líneas a extraer**: ~280
- **Métodos**: `BackgroundWorker1_DoWork`, `PerformEnhancedDetection`, `ReadPixelFromBuffer`
- **Dependencias**: `_handle` (IntPtr), `_frmOverlay`, `_regionLookupCache`, `_gameLoopStateMachine`, `GetWindowsScreenUseCase`, `_detectionLoggerService`
- **Estrategia**: Reemplazar BackgroundWorker por `Task.Run` con `while (_executeCapture)` y `this.Invoke()` (provisional hasta Fase 5)

### Fase 2: CaptureService
- **Líneas**: ~160
- **Métodos**: `GetImageWhilePlaying`, `SaveReferenceDimensionsToConfig`, `GetScaledRegion`, `GetJsonValue`
- **Dependencias**: `_handle`, `_session`, `_folderPath`, `GetWindowsScreenUseCase`, `_coordinateScaler`

### Fase 3: TableReaderService
- **Líneas**: ~380
- **Métodos**: `SetBetPlayer`, `SetHeroStack`, `SetPotValue`, `SetTableHand`, `ObtainCardsPlayerAsync`, `IsBoardCardVisible`, `CountVisibleBoardCards`, `DetectNewHand`
- **Dependencias**: `_regionLookupCache`, `_screenReader`, `_ocrService`, `_cardCacheService`, `_imageCropperService`, `_playerGameState`, `_tableLayout`, `_gameLoopStateMachine`, `_coordinateScaler`

### Fase 4: HandProcessor
- **Líneas**: ~480
- **Métodos**: `ProcessTableInfoAsync`, `ProcessPreflopAsync`, `SetPreflopAggressors`, `ProcessPostFlopAsync`, `ProcessFlopAsync`, `ProcessTurnAsync`, `ProcessRiverAsync`, `HandleNewHandAsync`, `GetPreflopHeroPosition`
- **Dependencias**: `_playerGameState`, `_gameLoopStateMachine`, `_pokerCalculator`, `_coordinator`, `_tableLayout`, `_contextHolder`, `_postflopDecisionService`, etc.
- **Implementa**: `IGameLoopTickProcessor.ExecuteAsync`, reemplazando `EmptyGameLoopTickProcessor`
- **Feature flag**: Activar `UseGameLoopCoordinator = true` (appsettings o environment)

### Fase 5: UiUpdateService
- **Líneas**: ~230
- **Métodos**: `UpdateUIWithResults`, `UpdateOverlayWithPotOdds`, `UpdateResumeTextForPreflop/Flop`, `UpdatePlayerIndicators`, `UpdateCardImages`, `GetCardImage`
- **Amplía**: `IUiSyncService` existente (hoy solo sincroniza GameLoopResult)

### Fase 6: ThemeService
- **Líneas**: ~350
- **Métodos**: `InitializeVisualStyles`, `ApplyModernTabStyle`, `ApplyGameTabStyle`, `ApplyConfigTabStyle`, etc.
- **Riesgo bajo**: código de styling puro, sin lógica de negocio

### Fase 7: TabPageContributions
- **Líneas**: ~360
- **Métodos**: `InitializeHistorialTab`, `InitializeBankrollTab`, `InitializeMetricsTab` + handlers
- **Estrategia**: Cada tab se convierte en un `UserControl` independiente

### Fase 8: RegionEditor
- **Líneas**: ~580
- **Métodos**: `LoadTreeViewRegions`, `UpdateRegionDisplay`, `btnTestColor_Click`, `btnTestTexto_Click`, `btnTestCarta_Click`
- **Estrategia**: Extraer a `RegionEditorControl : UserControl`

## Riesgos

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| `this.Invoke()` en DetectionLoopService rompe sincronización | Media | Mantener Invoke provisional, migrar a eventos en Fase 5 |
| Orden de inicialización de servicios difiere | Baja | Verificar `btnWindow_Click` preserva secuencia original |
| Regresión en OCR (timing, reintentos) | Baja | Tests de regresión manual tras Fase 3 |
| Cutover HandProcessor (Fase 4) con race conditions | Media | Feature flag con rollback instantáneo; test A/B manual |
| UserControl pierde estado entre tabs | Baja | Tests de integración por tab tras Fase 7 |

## Criterios de Éxito

- [ ] FrmMain.cs ≤ 1500 líneas
- [ ] 1163 tests existentes siguen verdes
- [ ] Build debug exit code 0 en cada fase
- [ ] Feature flag `UseGameLoopCoordinator = true` funciona en sesión real de casino
- [ ] Cada nuevo servicio tiene interfaz explícita y está registrado en DI
- [ ] Ningún servicio nuevo referencia `Control`, `Form` ni WinForms (salvo UiUpdateService y UserControls)
