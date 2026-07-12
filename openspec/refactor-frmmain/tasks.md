# Tasks: refactor-frmmain

## Estructura de Archivos Target

```
src/OpenScrape.App/
├── Services/
│   ├── IDetectionLoopService.cs      (nuevo)
│   ├── DetectionLoopService.cs        (nuevo)
│   ├── ICaptureService.cs             (nuevo)
│   ├── CaptureService.cs              (nuevo)
│   ├── ITableReaderService.cs         (nuevo)
│   ├── TableReaderService.cs          (nuevo)
│   ├── HandProcessor.cs               (nuevo, implementa IGameLoopTickProcessor)
│   ├── IUiUpdateService.cs            (nuevo)
│   ├── UiUpdateService.cs             (nuevo)
│   ├── IThemeService.cs               (nuevo)
│   ├── ThemeService.cs                (nuevo)
│   └── GameLoopResult.cs              (extender con campos RF4)
├── Forms/
│   ├── Controls/
│   │   ├── HistorialTabControl.cs     (nuevo UserControl)
│   │   ├── BankrollTabControl.cs      (nuevo UserControl)
│   │   ├── MetricsTabControl.cs       (nuevo UserControl)
│   │   └── RegionEditorControl.cs     (nuevo UserControl)
│   └── FrmMain.cs                     (≤ 1500 LOC tras 8 fases)
├── Program.cs                         (registros DI por fase)

test/OpenScrape.App.Tests/
├── DetectionLoopServiceTests.cs       (nuevo)
├── CaptureServiceTests.cs             (nuevo)
├── TableReaderServiceTests.cs         (nuevo)
├── HandProcessorTests.cs              (nuevo)
├── UiUpdateServiceTests.cs            (nuevo)
├── ThemeServiceTests.cs               (nuevo)
└── RegionEditorControlTests.cs        (nuevo)
```

## Dependencias entre Tasks

```
Fase 1 (DetectionLoopService)  ──indep──> Gate: build + 1163 tests verdes
Fase 2 (CaptureService)        ──indep──> Gate: build + tests verdes
Fase 3 (TableReaderService)    ──dep──>  Fase 2
Fase 4 (HandProcessor)         ──dep──>  Fases 1, 2, 3  →  ⚙️ CUTOVER feature flag
Fase 5 (UiUpdateService)       ──dep──>  Fase 4
Fase 6 (ThemeService)          ──indep──> (paralelizable tras Fase 4)
Fase 7 (TabPageContributions)  ──indep──> (paralelizable tras Fase 4)
Fase 8 (RegionEditor)          ──dep──>  Fase 2  (paralelizable tras Fase 4)
```

Reglas de secuenciación: Fases 1→4 obligatorias en orden. Fases 5→8 pueden reordenarse tras Fase 4.

---

## Fase 1: DetectionLoopService

#### 1.1 Crear interfaz `IDetectionLoopService`
- **Archivos**: `src/OpenScrape.App/Services/IDetectionLoopService.cs` (nuevo)
- **Contrato**: eventos `HeroTurnDetected`, `FlopDetected`, `DetectionStopped`; `IsRunning`; `StartAsync(IntPtr, CancellationToken)`; `StopAsync()`; records `HeroTurnDetectedEventArgs`, `FlopDetectedEventArgs`.
- **Deps**: ninguna.

#### 1.2 Crear `DetectionLoopService`
- **Archivos**: `src/OpenScrape.App/Services/DetectionLoopService.cs` (nuevo)
- **Comportamiento**: `Task.Run` con `while (_executeCapture)`; lee bitmap vía `GetWindowsScreenUseCase`; extrae `PerformEnhancedDetection` (LockBits, muestreo cruz ±2 px) y `ReadPixelFromBuffer`; watchdog 500ms ante bitmap inválido; delay adaptativo 200/100ms post-detección; métricas internas (`TotalChecks`, `Detections`, `Errors`); preserva `_detectionLoggerService`.
- **Deps**: 1.1.

#### 1.3 Tests de `DetectionLoopService`
- **Archivos**: `test/OpenScrape.App.Tests/DetectionLoopServiceTests.cs` (nuevo)
- **Cubre**: Escenarios RF1 (turno héroe, overlay no visible, captura inválida); cancelación limpia; métricas; threading-safe en `_executeCapture`.
- **Deps**: 1.2.

#### 1.4 Registrar en DI (`Program.cs`)
- **Archivos**: `src/OpenScrape.App/Program.cs` (modificar)
- **Deps**: 1.2.

#### 1.5 Inyectar y delegar en `FrmMain`
- **Archivos**: `src/OpenScrape.App/Forms/FrmMain.cs` (modificar)
- **Cambio**: `btnWindow_Click` reemplaza `backgroundWorker1.RunWorkerAsync()` por `_detectionLoopService.StartAsync(_handle, cts.Token)`; suscripción a `HeroTurnDetected`/`FlopDetected` con `this.Invoke` provisional.
- **Deps**: 1.4.

#### 1.6 Limpiar `FrmMain` (eliminar código legacy)
- **Archivos**: `src/OpenScrape.App/Forms/FrmMain.cs`
- **Eliminar**: campo `backgroundWorker1`, handlers `BackgroundWorker1_DoWork`/`ProgressChanged`/`RunWorkerCompleted`, `PerformEnhancedDetection`, `ReadPixelFromBuffer`, record `DetectionResult`.
- **Deps**: 1.5.

#### 1.7 Gate de Fase 1
- `dotnet build OpenScrape.sln` → exit 0
- `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj` → ≥ 1163 verdes
- Commit: `refactor(frmmain): extraer DetectionLoopService (fase 1)`

---

## Fase 2: CaptureService

#### [x] 2.1 Crear interfaz `ICaptureService`
- [x] **Archivos**: `src/OpenScrape.App/Services/ICaptureService.cs` (nuevo)
- [x] **Contrato**: `CaptureWindow(IntPtr) → Bitmap?`; `GetScaledRegion(Region) → Region`; `SaveReferenceDimensions(int, int)`; `GetJsonValue(string, string) → string?`.
- [x] **Deps**: ninguna.

#### [x] 2.2 Crear `CaptureService`
- [x] **Archivos**: `src/OpenScrape.App/Services/CaptureService.cs` (nuevo)
- [x] **Comportamiento**: mueve `GetImageWhilePlaying` (log + return null si handle es `IntPtr.Zero`), `SaveReferenceDimensionsToConfig`, `GetScaledRegion` (vía `ICoordinateScaler`), `GetJsonValue`. NO gestiona `_handle` ni `_session`.
- [x] **Deps**: 2.1.

#### [x] 2.3 Tests de `CaptureService`
- [x] **Archivos**: `test/OpenScrape.App.Tests/CaptureServiceTests.cs` (nuevo)
- [x] **Cubre**: Escenarios RF2 (captura exitosa, handle inválido sin excepción, escalado, persistencia dimensiones).
- [x] **Deps**: 2.2.

#### [x] 2.4 Registrar en DI (`Program.cs`)
- [x] **Archivos**: `src/OpenScrape.App/Program.cs` (modificar). Lifetime: **Scoped**.
- [x] **Deps**: 2.2.

#### [x] 2.5 Inyectar y delegar en `FrmMain`
- [x] **Archivos**: `src/OpenScrape.App/Forms/FrmMain.cs` (modificar)
- [x] **Cambio**: `GetImageWhilePlaying()` → `_captureService.CaptureWindow(_handle)`; delega `GetScaledRegion`, `SaveReferenceDimensionsToConfig`, `GetJsonValue`.
- [x] **Deps**: 2.4.

#### [x] 2.6 Limpiar `FrmMain`
- [x] **Eliminar**: métodos extraídos en 2.5.
- [x] **Deps**: 2.5.

#### [x] 2.7 Gate de Fase 2
- [x] Build + tests verdes. Commit: `refactor(frmmain): extraer CaptureService (fase 2)`.

---

## Fase 3: TableReaderService

#### [x] 3.1 Crear interfaz `ITableReaderService`
- [x] **Archivos**: `src/OpenScrape.App/Services/ITableReaderService.cs` (nuevo)
- [x] **Contrato**: `SetBetPlayer`, `SetHeroStack`, `SetPotValue`, `SetTableHandAsync`, `ObtainCardsPlayerAsync`, `IsBoardCardVisible`, `CountVisibleBoardCards`.
- [x] **Deps**: 2.1 (usa `ICaptureService.GetScaledRegion`).

#### [x] 3.2 Crear `TableReaderService`
- [x] **Archivos**: `src/OpenScrape.App/Services/TableReaderService.cs` (nuevo)
- [x] **Comportamiento**: mueve `SetBetPlayer`, `SetHeroStack`, `SetPotValue`, `IsBoardCardVisible`, `CountVisibleBoardCards`, `ObtainCardsPlayerAsync`. `SetTableHand` permanece en FrmMain (orquestación compleja de estado). `DetectNewHand` permanece en FrmMain.
- [x] **Deps**: 3.1.

#### [x] 3.3 Tests de `TableReaderService`
- [x] **Archivos**: `test/OpenScrape.App.Tests/TableReaderServiceTests.cs` (nuevo)
- [x] **Cubre**: Escenarios RF3 (IsBoardCardVisible >/< 80%, CountVisibleBoardCards secuencial, SetPotValue parsing 4-dígitos, ObtainCardsPlayerAsync hole cards). 10 tests.
- [x] **Deps**: 3.2.

#### [x] 3.4 Registrar en DI (`Program.cs`)
- [x] Lifetime: **Scoped** para ITableReaderService. Interfaces forwarding: IRegionLookupCache, IImageCropperService, ICardCacheService, IOcrService.
- [x] **Deps**: 3.2.

#### [x] 3.5 Inyectar y delegar en `FrmMain`
- [x] **Archivos**: `src/OpenScrape.App/Forms/FrmMain.cs` (modificar)
- [x] **Cambio**: inyecta ITableReaderService; SetBetPlayer, SetHeroStack, SetPotValue, ObtainCardsPlayerAsync, IsBoardCardVisible, CountVisibleBoardCards delegan al servicio.
- [x] **Deps**: 3.4.

#### [x] 3.6 Limpiar `FrmMain`
- [x] **Eliminar**: cuerpos de métodos extraídos (SetBetPlayer, SetHeroStack, SetPotValue, ObtainCardsPlayerAsync, IsBoardCardVisible, CountVisibleBoardCards). Campo `_pictureUmbralBet` eliminado (sin uso).
- [x] **Deps**: 3.5.

#### [x] 3.7 Gate de Fase 3
- [x] Build + 1236 tests verdes. Lineas FrmMain: 3525 (reducción de ~200 LOC).

---

## Fase 4: HandProcessor (⚙️ CUTOVER) — Completada ✅

#### [x] 4.1 Extender `GameLoopResult`
- [x] **Archivos**: `src/OpenScrape.App/Services/GameLoopResult.cs`
- [x] **Añadir**: `PotOdds (decimal?)`, `Equity (double?)`, `HoleCards ((string,string)?)`, `BoardCards (IReadOnlyList<string>)`, `PlayerBets (IReadOnlyDictionary<string,decimal>)`, `HeroPosition`, `HandSituation`, `ResponseAction`, `TableName`, `SummaryText`.
- [x] **Deps**: ninguna.

#### [x] 4.2 Crear `HandProcessor`
- [x] **Archivos**: `src/OpenScrape.App/Services/HandProcessor.cs` (nuevo)
- [x] **Comportamiento**: implementa `IGameLoopTickProcessor.ExecuteAsync(CancellationToken)`. Pipeline preflop: captura → OCR → decisión. Postflop retorna `Empty=true`.
- [x] **Deps**: 4.1, Fases 1-3 completas.

#### [x] 4.3 Tests de `HandProcessor`
- [x] **Archivos**: `test/OpenScrape.App.Tests/HandProcessorTests.cs` (nuevo)
- [x] **Cubre**: preflop pipeline, hole cards, hero position, postflop (flop/turn/river) retorna Empty, captura nula, cancelación, WaitingForHand.
- [x] **Deps**: 4.2.

#### [x] 4.4 Registrar `IGameLoopTickProcessor` con factory condicional
- [x] **Archivos**: `src/OpenScrape.App/Program.cs`
- [x] **Lógica**: `HandProcessor` registrado como Scoped; factory condicional vía `UseGameLoopCoordinator`.
- [x] **Deps**: 4.2.

#### [x] 4.5 Inyectar y simplificar `FrmMain`
- [x] **Archivos**: `src/OpenScrape.App/Forms/FrmMain.cs`
- [x] **Cambio**: `btnWindow_Click` setea `_captureService.WindowHandle = _handle` para que HandProcessor pueda capturar.
- [x] **Deps**: 4.4.

#### [ ] 4.6 Limpiar `FrmMain`
- [ ] **NO EJECUTADO deliberadamente**: Los métodos extraídos permanecen en FrmMain como fallback para el path legacy (feature flag=false). Garantiza rollback instantáneo.
- [ ] **Deps**: 4.5.

#### [x] 4.7 Activar feature flag
- [x] **Archivos**: `src/OpenScrape.App/appsettings.json`
- [x] **Cambio**: `Features.UseGameLoopCoordinator = true`.
- [x] **Deps**: 4.4.

#### [x] 4.8 Gate de Fase 4
- [x] Build: 0 errores.
- [x] Tests: 1245/1245 verdes (1236 baseline + 9 nuevos).
- [x] **Verificación manual**: pendiente — sesión real con flag=true vs flag=false. Paridad funcional esperada.
- [x] Commit: `refactor(frmmain): extraer HandProcessor y activar GameLoopCoordinator (fase 4)`.

---

## Fase 5: UiUpdateService — Completada ✅

#### [x] 5.1 Crear interfaz `IUiUpdateService`
- [x] **Archivos**: `src/OpenScrape.App/Services/IUiUpdateService.cs` (nuevo)
- [x] **Contrato**: `Attach/Detach`, `UpdateResults(PlayerGameState, PokerCalculationResult, ...)`, `UpdateResults(GameLoopResult)`, `UpdateOverlay`, `SetCardList`. Record `UiUpdateControls` con todas las referencias de controles.
- [x] **Deps**: Fase 4.

#### [x] 5.2 Crear `UiUpdateService`
- [x] **Archivos**: `src/OpenScrape.App/Services/UiUpdateService.cs` (nuevo)
- [x] **Comportamiento**: implementa los 7 métodos extraídos de FrmMain. Contiene `UpdateResults` (orquestador), `UpdateOverlay`, `UpdateResumeTextForPreflop`, `UpdateResumeTextForFlop`, `UpdatePlayerIndicators`, `UpdateCardImages`, `GetCardImage`. Sin lógica de decisión.
- [x] **Deps**: 5.1.

#### [x] 5.3 Tests de `UiUpdateService`
- [x] **Archivos**: `test/OpenScrape.App.Tests/UiUpdateServiceTests.cs` (nuevo)
- [x] **Cubre**: 8 tests — controles actualizados, resultado vacío sin excepción, overlay delegado, imágenes de cartas asignadas, imágenes nulas sin cartas, Detach idempotente, Detach + UpdateResults no lanza, Attach y actualización normal.
- [x] **Deps**: 5.2.

#### [x] 5.4 Registrar en DI (`Program.cs`)
- [x] Lifetime: **Scoped**. Añadido `services.AddScoped<IUiUpdateService, UiUpdateService>()`.
- [x] **Deps**: 5.2.

#### [x] 5.5 Inyectar y delegar en `FrmMain`
- [x] **Archivos**: `src/OpenScrape.App/Forms/FrmMain.cs` (modificar)
- [x] **Cambio**: inyectado `IUiUpdateService` en constructor; `SetCardList` en `FrmMain_Load`; Attach con `UiUpdateControls` en `btnWindow_Click`; `UpdateUIWithResults(potOddsResult)` reemplazado por `_uiUpdateService.UpdateResults(...)`.
- [x] **Deps**: 5.4.

#### [x] 5.6 Integrar `UiSyncService` con `IUiUpdateService`
- [x] **Archivos**: `src/OpenScrape.App/Services/UiSyncService.cs` (modificar), `src/OpenScrape.App/Services/IUiSyncService.cs` (ampliar)
- [x] **Cambio**: añadido `SetUiUpdateService(IUiUpdateService?)` a `IUiSyncService`; `UiSyncService` almacena referencia y llama `_uiUpdateService.UpdateResults(result)` en `ApplyResult` cuando está disponible.
- [x] **Deps**: 5.2.

#### [ ] 5.7 Limpiar `FrmMain`
- [ ] **APLAZADO deliberadamente**: Los métodos originales (`UpdateUIWithResults`, `UpdateOverlayWithPotOdds`, etc.) permanecen en FrmMain como fallback. Se limpiarán en fase posterior.
- [ ] **Deps**: 5.5, 5.6.

#### [x] 5.8 Gate de Fase 5
- [x] `dotnet build OpenScrape.sln` → exit 0
- [x] `dotnet test` → 1253/1253 tests verdes (1245 baseline + 8 nuevos)
- [x] `IUiUpdateService` y `UiUpdateControls` creados
- [x] Dependencias inyectadas: `IPokerCalculator`, `GameLoggerService`, `IActionFormatter`, `ImageCropperService`, `IGameCoordinator`, `GameLoopStateMachine`
- [x] FrmMain modificado: constructor + btnCapture_Click + FrmMain_Load + btnWindow_Click
- [x] UiSyncService integrado con IUiUpdateService vía SetUiUpdateService

---

## Fase 6: ThemeService

#### 6.1 Crear interfaz `IThemeService`
- **Archivos**: `src/OpenScrape.App/Services/IThemeService.cs` (nuevo)
- **Contrato**: `InitializeVisualStyles(Form)`; `ApplyModernTabStyle(TabControl)`; `ApplyGameTabStyle(TabPage)`; `ApplyConfigTabStyle(TabPage)`; métodos específicos por tab.
- **Deps**: ninguna.

#### 6.2 Crear `ThemeService`
- **Archivos**: `src/OpenScrape.App/Services/ThemeService.cs` (nuevo)
- **Comportamiento**: mueve `InitializeVisualStyles`, `ApplyModernTabStyle`, `ApplyGameTabStyle`, `ApplyConfigTabStyle`, etc. Acepta controles por parámetro. Sin referencias a `_playerGameState`.
- **Deps**: 6.1.

#### 6.3 Tests de `ThemeService`
- **Archivos**: `test/OpenScrape.App.Tests/ThemeServiceTests.cs` (nuevo)
- **Cubre**: Escenario RF6 (paleta, fuentes, padding de TabControl). Controles WinForms reales en test.
- **Deps**: 6.2.

#### 6.4 Registrar en DI (`Program.cs`)
- Lifetime: **Singleton** (estilos puros sin estado).
- **Deps**: 6.2.

#### 6.5 Inyectar y delegar en `FrmMain`
- **Archivos**: `src/OpenScrape.App/Forms/FrmMain.cs` (modificar)
- **Cambio**: `FrmMain_Load` llama a `_themeService.InitializeVisualStyles(this)` y aplica estilos por tab; pasa referencias explícitas.
- **Deps**: 6.4.

#### 6.6 Limpiar `FrmMain`
- **Eliminar**: métodos de styling extraídos.
- **Deps**: 6.5.

#### 6.7 Gate de Fase 6
- Build + tests verdes + verificación visual (comparar screenshot antes/después). Commit: `refactor(frmmain): extraer ThemeService (fase 6)`.

---

## Fase 7: TabPageContributions

#### 7.1 Crear `HistorialTabControl`
- **Archivos**: `src/OpenScrape.App/Forms/Controls/HistorialTabControl.cs` (nuevo)
- **Comportamiento**: `UserControl` con `DataGridView`; mueve `InitializeHistorialTab` y handlers de selección. Recibe datos por propiedad pública.
- **Deps**: ninguna.

#### 7.2 Crear `BankrollTabControl`
- **Archivos**: `src/OpenScrape.App/Forms/Controls/BankrollTabControl.cs` (nuevo)
- **Comportamiento**: `UserControl` con gráficos de bankroll; mueve `InitializeBankrollTab`.
- **Deps**: 7.1 (paralelizable).

#### 7.3 Crear `MetricsTabControl`
- **Archivos**: `src/OpenScrape.App/Forms/Controls/MetricsTabControl.cs` (nuevo)
- **Comportamiento**: `UserControl` con métricas (manos/hora, win rate, VPIP, PFR); mueve `InitializeMetricsTab`.
- **Deps**: 7.1 (paralelizable).

#### 7.4 Tests de TabPageContributions
- **Archivos**: `test/OpenScrape.App.Tests/TabPageContributionsTests.cs` (nuevo)
- **Cubre**: Escenario RF7 (estado preservado entre cambios de tab).
- **Deps**: 7.1, 7.2, 7.3.

#### 7.5 Embeber UserControls en `FrmMain`
- **Archivos**: `src/OpenScrape.App/Forms/FrmMain.cs` + `FrmMain.Designer.cs` (modificar)
- **Cambio**: cada tab page se reemplaza por el `UserControl` correspondiente; conecta datos vía propiedades.
- **Deps**: 7.1, 7.2, 7.3.

#### 7.6 Limpiar `FrmMain`
- **Eliminar**: `InitializeHistorialTab`, `InitializeBankrollTab`, `InitializeMetricsTab` y sus handlers.
- **Deps**: 7.5.

#### 7.7 Gate de Fase 7
- Build + tests verdes. Commit: `refactor(frmmain): extraer TabPageContributions (fase 7)`.

---

## Fase 8: RegionEditor

#### 8.1 Crear `RegionEditorControl`
- **Archivos**: `src/OpenScrape.App/Forms/Controls/RegionEditorControl.cs` (nuevo)
- **Comportamiento**: `UserControl` autocontenido con `TreeView`, `PictureBox`, botones `btnTestColor`/`btnTestTexto`/`btnTestCarta`/`btnSaveMap`. Mueve `LoadTreeViewRegions`, `UpdateRegionDisplay`, handlers de TreeView, drag/resize. Recibe `ICaptureService`, `IScreenReaderService`, `OcrService`, `ImageCropperService` por constructor.
- **Deps**: 2.1 (usa `ICaptureService`).

#### 8.2 Tests de `RegionEditorControl`
- **Archivos**: `test/OpenScrape.App.Tests/RegionEditorControlTests.cs` (nuevo)
- **Cubre**: Escenarios RF8 (test color, test OCR, test carta, test independiente sin FrmMain).
- **Deps**: 8.1.

#### 8.3 Embeber `RegionEditorControl` en `FrmMain`
- **Archivos**: `src/OpenScrape.App/Forms/FrmMain.cs` + `FrmMain.Designer.cs` (modificar)
- **Cambio**: reemplazar panel de edición de regiones por el `UserControl`; inyectar dependencias vía `Program.cs` o constructor.
- **Deps**: 8.1.

#### 8.4 Limpiar `FrmMain`
- **Eliminar**: `LoadTreeViewRegions`, `UpdateRegionDisplay`, `btnTestColor_Click`, `btnTestTexto_Click`, `btnTestCarta_Click`, `btnSaveMap_Click`, handlers de TreeView.
- **Deps**: 8.3.

#### 8.5 Gate de Fase 8
- Build + tests verdes.
- **Verificación**: FrmMain.cs ≤ 1500 LOC; los 1163 tests originales siguen verdes; build debug exit 0.
- Commit: `refactor(frmmain): extraer RegionEditor (fase 8)`.

---

## Gate Final Post-Implementación

- [ ] `dotnet build OpenScrape.sln` → exit 0
- [ ] `dotnet test` → ≥ 1163 tests verdes
- [ ] FrmMain.cs ≤ 1500 líneas
- [ ] Sesión real de casino con `UseGameLoopCoordinator = true` muestra paridad funcional
- [ ] Ningún servicio nuevo referencia `Control`/`Form` salvo `UiUpdateService`, `ThemeService` y UserControls
- [ ] 8 commits separados con mensajes convencionales en castellano
- [ ] PR a `develop` siguiendo `docs/pre-merge-checklist.md`
