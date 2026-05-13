# OpenScrape.App — Casos Extremos

> Casos límite de la capa App detectados en el código actual del composition root, captura, OCR, game loop, persistencia, telemetría y UI. Cada EC documenta disparador, comportamiento real (con confianza), comportamiento esperado y consecuencias si se ignora. `doc_level = detalhado` exige al menos 2 EC por unit; este archivo cubre **22**.

---

## EC-01 — Cliente de poker cerrado durante captura activa

**Disparador:** El usuario cierra la ventana del cliente mientras el `BackgroundWorker` está iterando `btnCapture_Click`. El `_handle: IntPtr` apunta a una ventana ya destruida.

**Comportamiento real** (🟡 captura silenciosa):
- `User32.GetWindowRect(_handle, ref _locWindowRect)` retorna `false` o un rect inválido (`Right - Left < 0`).
- `User32.PrintWindow(_handle, hdcBlt, PW_RENDERFULLCONTENT)` retorna `false`.
- `CaptureWindowsHelper.GetWindowsScreenAsync` cae al fallback `BitBlt` que también falla → bitmap negro de tamaño 0×0 o `null`.
- El pipeline continúa: OCR sobre bitmap inválido produce strings vacíos; `TableLayoutService` retorna `Players=[]`; `ProcessTableInfoAsync` no encuentra hero → branch `WaitingForHand`.
- No hay excepción ni log de error explícito que indique "ventana cerrada".

**Comportamiento esperado:**
- Validación post-captura: `if (image == null || image.Width == 0 || image.Height == 0) { _logger.LogWarning("Captura inválida — ventana cerrada?"); return; }`
- Detección proactiva: `User32.IsWindow(_handle)` antes de `GetWindowRect` → si retorna false, abrir `FormListApps` automáticamente.

**Consecuencias si se ignora:**
- 🟡 Ciclos vacíos consumiendo CPU sin valor (60 fps × CPU del OCR fallido).
- 🟡 El usuario no sabe que tiene que reseleccionar la ventana — la app parece colgada.

**Cobertura test:** Ninguna directa. TT-23 (integración del pipeline) cubre el camino feliz.

---

## EC-02 — Tesseract `eng.traineddata` corrupta o incompatible

**Disparador:** Una versión vieja de `eng.traineddata` queda en `tessdata/` (build action `PreserveNewest` no la sobrescribe si tiene fecha posterior). Tesseract intenta cargar pero falla con `TesseractException: Unsupported Tesseract data version`.

**Comportamiento real** (🔴 fallo en arranque sin mensaje claro):
- `OcrService` constructor en `Program.cs` indirectamente: `_engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default)` lanza excepción.
- El catch en `OcrService:55-58` solo escribe a `Debug.WriteLine` y vuelve a lanzar.
- `Host.Build()` propaga la excepción al `Main()`. El usuario ve `MessageBox` genérico de WinForms con stack trace en lugar de mensaje contextualizado.

**Comportamiento esperado:**
- Validar versión de la traineddata antes de construir el engine, o catch específico que indica "actualizar `eng.traineddata` desde Resources/tessdata/".
- Auto-recovery: si la traineddata en disco está corrupta, sobrescribir con la embebida del recurso.

**Consecuencias si se ignora:**
- 🔴 La app no arranca en máquina con artefactos viejos.
- 🟡 Diagnóstico difícil para usuarios — el mensaje de WinForms no es accionable.

**Cobertura test:** Ninguna.

---

## EC-03 — `appsettings.Development.json` ausente en máquina nueva

**Disparador:** Un developer clona el repo en una máquina nueva. `DOTNET_ENVIRONMENT=Development` está activo pero `appsettings.Development.json` (gitignored) no existe.

**Comportamiento real** (🟡 fallback inesperado):
- `IConfiguration` carga `appsettings.json` (con credenciales **hardcoded** del usuario original — anomalía DD-18).
- La conexión Marten apunta a la BD del usuario original, NO la BD del developer.
- Si la BD del usuario original está accesible, el developer escribe en datos de producción accidentalmente.
- Si NO está accesible (`Connection refused`), Marten falla en el primer query y la app crashea.

**Comportamiento esperado:**
- Detectar `appsettings.Development.json` faltante en arranque y advertir.
- O bien, usar `appsettings.json` con placeholders puros (`CHANGE_ME`) que falle el `StrategyProfileValidator` por valores inválidos hasta que el developer rellene `appsettings.Development.json`.

**Consecuencias si se ignora:**
- 🔴 Riesgo de escritura accidental en BD de producción.
- 🟡 Onboarding lento — developer pierde tiempo diagnosticando.

**Cobertura test:** Ninguna. Decisión humana en `questions.md` para la anomalía DD-18.

---

## EC-04 — `StrategyProfile` válido en arranque pero corrupto en runtime (hot reload)

**Disparador:** El usuario edita `appsettings.json` mientras la app está corriendo. `IConfiguration` con hot reload activado (default en `Host.CreateDefaultBuilder`) propaga el cambio.

**Comportamiento real** (🔴 sin re-validación):
- `StrategyProfileValidator.Validate` solo se ejecuta UNA vez en `Program.Main` antes de `Application.Run`.
- Si el usuario introduce un tier inválido en runtime (`FoldBelow=80, ThinValueAbove=70` para alguna combinación), `IOptions<StrategyProfile>` propaga el nuevo valor en el siguiente `IOptionsMonitor` callback.
- `ThresholdsRegistry` (singleton, instanciado en arranque) NO observa cambios — su `Dictionary<ThresholdKey, StreetThresholds>` es immutable post-construcción.
- En la práctica: los thresholds nuevos NO se aplican (porque `ThresholdsRegistry` no los lee de nuevo) — cambio silenciosamente ignorado.

**Comportamiento esperado:**
- Documentar que cambios en `StrategyProfile` requieren reinicio de la app.
- O bien, suscribirse a `IOptionsMonitor<StrategyProfile>.OnChange` y re-validar + reconstruir `ThresholdsRegistry` (complejo, alto riesgo).

**Consecuencias si se ignora:**
- 🟡 El usuario edita el profile esperando aplicación inmediata; cree que aplicó pero el motor sigue con los viejos thresholds.

**Cobertura test:** Ninguna. Decisión documentada.

---

## EC-05 — OCR baja confianza encadenado (3 reads consecutivos < 0.70)

**Disparador:** El cliente de poker está animando un cambio de turno con efectos visuales (fade, blur). Las 3 lecturas consecutivas del `ScreenReaderService` retornan `IsHighConfidence = false`.

**Comportamiento real** (🟡 consenso degradado):
- `ScreenReaderService.ReadXxx` ejecuta 3 lecturas con preprocesamientos variados.
- Si las 3 retornan strings distintas (sin mayoría), el resultado es ambiguo.
- El servicio retorna la primera lectura no-vacía con flag `IsHighConfidence = false`.
- El game loop continúa con el valor "best effort" — si ese valor es `0` para un bet o stack, el motor decide con datos incoherentes.

**Comportamiento esperado:**
- Si confianza promedio de las 3 reads < 0.70, retornar **valor anterior** (cache last-known) en lugar de continuar con datos malos.
- Incrementar contador `_failedOcrCount`; tras `MaxOcrRetries=2` consecutivas, abortar el ciclo y esperar al siguiente.

**Consecuencias si se ignora:**
- 🟡 Decisiones tomadas con `bet=0` cuando villain en realidad apostó 5BB → motor cree que es "no bet" → recomienda Bet en spot que era Call/Fold.
- 🟡 Detectable en logs (decisión "Bet" en spot post-bet ajeno) pero solo en review manual.

**Cobertura test:** TT-20 cubre 3 reads con consenso, NO el caso de 3 reads divergentes.

---

## EC-06 — `_heroStackPreRebuy` no se reinicia entre sesiones (cross-mesa leak)

**Disparador:** El usuario cierra mesa A con `_heroStackPreRebuy = 87`. Abre mesa B sin reiniciar la app. `DetectNewHand` detecta nueva mano en mesa B.

**Comportamiento real** (🟢 reset correcto):
- `btnCapture_Click:713` ejecuta `_heroStackPreRebuy = 0` cuando `_newHand && !isTestPostflop`.
- En la primera mano de mesa B, `_heroStackPreRebuy = 0` → `prevHeroStack = _playerGameState.HeroStack` (línea 690).
- El profit de la primera mano de mesa B se calcula con stack inicial real, no con leak de mesa A.

**Comportamiento esperado:** = real. Validado.

**Consecuencias si se ignora:** N/A (caso correcto).

**Cobertura test:** TT-24 (auto-rebuy) cubre el camino. Falta test específico de cross-mesa.

**Validación adicional:** El `GameLoggerService.StartSessionAsync(newSessionId, ...)` también resetea `_sessionTotalHands` y `_sessionTotalProfit` (`Services/GameLoggerService.cs:65-66`). Doble protección.

---

## EC-07 — `BackgroundWorker` excepción no capturada → ciclo muerto

**Disparador:** Una excepción inesperada (ej. `OutOfMemoryException` por bitmap leak) escapa de `btnCapture_Click`.

**Comportamiento real** (🟡 catch defensivo):
- `BackgroundWorker1_DoWork:2737-2897` envuelve cada iteración en try/catch.
- En catch: `_detectionLoggerService.LogDetectionError(...)` + `_logger.LogError(ex, ...)` + `continue` con el siguiente tick.
- El loop NO muere — sigue iterando aunque cada iteración falle.

**Comportamiento esperado:** = real, pero **agregar contador**: si fallan 10 iteraciones consecutivas, abortar el `BackgroundWorker` y notificar al usuario con `MessageBox`.

**Consecuencias si se ignora:**
- 🟡 Loop "zombie" que consume CPU sin producir resultados — el usuario no sabe que algo está mal.
- 🟡 Logs llenos de errores idénticos sin escalación.

**Cobertura test:** Ninguna.

---

## EC-08 — App cerrada con mano activa (no `EndHand` llamado)

**Disparador:** El usuario hace `Alt+F4` o cierra la X durante el flop. `_currentHand != null` y `_currentSession != null`.

**Comportamiento real** (🟢 cleanup explícito):
- `FrmMain_FormClosing:304` se invoca antes de cerrar.
- Si `_gameLoopCoordinator.IsRunning`: `_uiSyncService.Detach() + _gameLoopCts.Cancel() + await StopAsync()`.
- `await _gameLoggerService.SaveSessionAsync()` persiste la sesión actual con la mano parcial — `_currentHand` se persiste con `Result = Unknown` (no se cerró con `EndHand`).
- `host.DisposeAsync()` limpia DI graph.

**Comportamiento esperado:** = real. Validado.

**Consecuencias si se ignora:** N/A. Pero si `SaveSessionAsync` falla (BD desconectada), la mano se pierde.

**Cobertura test:** TT-30 (manual end-to-end). Falta test automatizado.

---

## EC-09 — Marten desconectado durante persistencia de mano

**Disparador:** PostgreSQL crashea durante `await _gameLoggerService.FinalizeAndPersistHandAsync()`.

**Comportamiento real** (🟡 captura excepción + log):
- `FinalizeAndPersistHandAsync` obtiene `await using var session = _store.LightweightSession()`.
- `await session.SaveChangesAsync()` lanza `NpgsqlException` o `MartenException`.
- El catch en `GameLoggerService` loggea error pero NO propaga la excepción.
- La mano se pierde — `_currentHand` se pone a `null` para la siguiente.
- El `_dbWriteLock: SemaphoreSlim` se libera correctamente vía `try/finally`.

**Comportamiento esperado:**
- Cola de manos pendientes en disco (file-based fallback) que se reintenta cuando la BD vuelva.
- Notificar al usuario con `MessageBox` o indicador en UI.

**Consecuencias si se ignora:**
- 🟡 Pérdida silenciosa de manos en outage transitorio de PostgreSQL.
- 🟡 Stats de sesión (`_sessionTotalHands`) divergen del recuento real persistido.

**Cobertura test:** Ninguna directa.

---

## EC-10 — Multi-monitor con DPI distinto en cada monitor

**Disparador:** Cliente de poker en monitor A (DPI 100 %), Visual Studio en monitor B (DPI 150 %). El usuario mueve el cliente de A a B.

**Comportamiento real** (🟡 escalado parcial):
- `User32.SetProcessDPIAware()` se invoca al inicio del proceso, NO por monitor.
- `PrintWindow` captura el bitmap a la resolución física del monitor donde está el cliente.
- `CoordinateScaler` (si fue inicializado con dimensiones de monitor A) NO sabe que ahora estamos en monitor B con escalado distinto.
- Las coordenadas de regiones (calibradas en monitor A) NO matchean el bitmap del monitor B.

**Comportamiento esperado:**
- `SetProcessDPIAwareV2` (per-monitor DPI awareness) que detecta cambio de DPI y reinicia `CoordinateScaler.Initialize` con nuevas dimensiones.
- O bien, recalibrar regiones cada vez que el cliente cambia de monitor (UI prompt).

**Consecuencias si se ignora:**
- 🟡 OCR falla silenciosamente (lee áreas equivocadas de la imagen) en monitor secundario.

**Cobertura test:** Ninguna.

---

## EC-11 — `BoardCards` con duplicado tras OCR ruidoso

**Disparador:** El OCR de cartas del flop lee `[As, Kd, As]` (As duplicado). Probabilidad pequeña pero no-cero con dHash collision o región mal calibrada.

**Comportamiento real** (🟡 sin validación de unicidad):
- `GetCardsFlopUseCase` retorna `BoardData[3]` con dos cartas iguales.
- `_pokerCalculator.Calculate(playerHand, communityCards)` recibe el board duplicado.
- `BitHandEvaluator` calcula con cartas repetidas → ranking incoherente (FullHouse falso).
- `MonteCarloSimulator` puede fallar en `TryDrawFromRange` con `BlockedComboUnreliableThreshold` superado, marcando `IsReliable = false`.

**Comportamiento esperado:**
- `_pokerCalculator.Calculate` valida `communityCards.Distinct().Count() == communityCards.Count` y lanza `ArgumentException` si hay duplicados.
- O bien, `GetCardsFlopUseCase` valida unicidad antes de retornar.

**Consecuencias si se ignora:**
- 🔴 Decisión basada en mano evaluada incorrectamente (FullHouse falso = bet pot en vez de check).

**Cobertura test:** Ninguna.

**Pregunta abierta:** ¿se valida en App o se delega a DecisionMaker?

---

## EC-12 — `PostflopGameContext` leak entre dos manos consecutivas (state bleed)

**Disparador:** Mano A llega a river con `IsAnyoneAllIn = true`. Termina en showdown. La mano B arranca y `DetectNewHand` falla (no detecta el cambio porque `handNumber` no cambió por OCR).

**Comportamiento real** (🟢 reset incondicional):
- `btnCapture_Click:730` ejecuta `_contextHolder.StartNewHand()` **incondicionalmente** cuando se detecta nueva mano.
- Pero si `DetectNewHand` falla, NO entra al if y el `_contextHolder.Current` mantiene `IsAnyoneAllIn = true`.
- El motor decide en flop de mano B con `foldEquity = 0` (porque cree que hay all-in pendiente).

**Comportamiento esperado:**
- `DetectNewHand` debe ser robusto — usar consenso de 7 indicadores (R5 § Detección de nueva mano por consenso).
- Adicionalmente: si `_contextHolder.Current.IsAnyoneAllIn` y `boardCards.Count == 0` (preflop), forzar `StartNewHand` defensivamente.

**Consecuencias si se ignora:**
- 🟡 Decisiones erradas en la primera calle de mano B con motor creyendo que estamos post-allin.
- 🟡 Difícil de detectar — el log no marca el leak.

**Cobertura test:** TT-16 (DetectNewHand) cubre los 7 indicadores. Falta test del fallback defensivo.

---

## EC-13 — Equity cache con tope `EquityCacheMaxSize=2048` saturado

**Disparador:** Sesión muy larga (>10 K manos) con muchos boards únicos. El `ConcurrentDictionary` llega a 2048 entradas.

**Comportamiento real** (🟡 FIFO simple):
- `UnifiedPokerCalculator._equityCache` no tiene política de eviction LRU implementada en el código actual visible — al saturarse, la próxima `TryAdd` retorna `false` y NO añade.
- Las nuevas equities NO se cachean — cada call recalcula con MC/enumeración.
- El cache mantiene los primeros 2048 keys insertados (FIFO de inserción).

**Comportamiento esperado:**
- Implementar política real (LRU o cleanup periódico al alcanzar tope).
- O bien, dimensionar correctamente: 2048 cubre 169 manos × 12 situaciones × 1-2 boards típicos = suficiente para sesión normal.

**Consecuencias si se ignora:**
- 🟡 Performance degradada en sesiones largas (cache miss en 100 % de los nuevos boards).
- 🟡 No bug, solo penalty de latencia.

**Cobertura test:** Ninguna.

---

## EC-14 — `RegionLookupCache` sin `Initialize` antes del primer lookup

**Disparador:** El usuario lanza la app sin haber cargado `tableMap.json` previamente (instalación nueva).

**Comportamiento real** (🟡 retorna null):
- `RegionLookupCache.GetRegion(mapId, regionName)` consulta el `Dictionary` interno.
- Si nunca se llamó `Initialize(maps)`, el diccionario está vacío.
- Retorna `null`.
- El caller (`btnCapture_Click` indirectamente) verifica `if (region == null) return;` y aborta el ciclo.

**Comportamiento esperado:** = real, pero loggear warning en el primer call: "RegionLookupCache no inicializado — cargar tableMap primero".

**Consecuencias si se ignora:**
- 🟡 La app arranca pero el ciclo de captura es no-op silencioso. Usuario no sabe qué falta.

**Cobertura test:** Ninguna.

---

## EC-15 — `GameLoopStateMachine.ForceState` con estado inválido (test/debug pollution)

**Disparador:** Un developer en debug ejecuta `_gameLoopStateMachine.ForceState((GameState)999)` por error de cast.

**Comportamiento real** (🟢 validación):
- `ForceState(state)` valida `_validTransitions.ContainsKey(state)` antes de aplicar.
- Si no existe, loggea `LogError` con "ForceState rechazado: estado inválido {State}" y NO modifica el estado.
- Caller continúa con el `CurrentState` actual.

**Comportamiento esperado:** = real. Validado.

**Consecuencias si se ignora:** N/A.

**Cobertura test:** TT-07 cubre. Validado.

---

## EC-16 — `TextBoxLoggerProvider` buffer saturado antes de `SetTextBoxTarget`

**Disparador:** `Host.Build()` y la fase pre-`FrmMain` emiten más de `BufferCapacity=1000` logs (escenario poco probable pero posible con `LogLevel.Trace` activado en startup).

**Comportamiento real** (🟡 drop FIFO):
- `TextBoxLoggerProvider` mantiene `ConcurrentQueue<string>` con tope `BufferCapacity`.
- Al saturarse, los nuevos logs se descartan o se aplican rotación FIFO (depende de impl).
- Cuando `SetTextBoxTarget` flushea, solo se ve el subset reciente.

**Comportamiento esperado:**
- Documentar que `MinimumLevel=Trace` en startup puede saturar el buffer.
- Sink de consola en paralelo siempre activo — los logs tracelevel viven en consola aunque no en TextBox.

**Consecuencias si se ignora:**
- 🟡 Diagnóstico incompleto si los logs perdidos son los relevantes.

**Cobertura test:** Ninguna.

---

## EC-17 — Tesseract crash con bitmap inválido (out-of-bounds region)

**Disparador:** `CoordinateScaler` produce un region con `(x, y, w, h) = (0, 0, 99999, 99999)` por bug de cálculo. `OcrService` intenta crop+OCR de área fuera del bitmap.

**Comportamiento real** (🟡 crash o región vacía):
- `Bitmap.Clone(rect, format)` lanza `OutOfMemoryException` si `rect` excede el bitmap (paradójicamente — la excepción de WinForms es engañosa).
- O bien, retorna un bitmap parcial (depende de Bitmap impl).
- Tesseract intenta procesar y puede crashear con `AccessViolationException` (proceso muere).

**Comportamiento esperado:**
- `OcrService.ExtractTextFromRegionAsync` valida que `(x + w) <= image.Width && (y + h) <= image.Height` y retorna string vacío si no.

**Consecuencias si se ignora:**
- 🔴 Crash del proceso entero — game loop muere, sesión se pierde.

**Cobertura test:** Ninguna.

---

## EC-18 — Heads-up con `_previousSBPlayerName == _previousBBPlayerName` (mismo player)

**Disparador:** En heads-up, el dealer es SB y el otro jugador es BB. Si ambos cambian asientos por SitOut → return, los nombres pueden alinearse incorrectamente.

**Comportamiento real** (🟡 indicadores ambiguos):
- `DetectNewHand` evalúa `currentSBPlayerName != previousSB` para `indicator6` y similar para `indicator7`.
- Si la mano anterior tenía `previousSB = "Bob"` y la nueva mano `currentSB = "Alice"` (SB cambió), pero `currentBB = "Bob"` (BB ahora es el viejo SB), ambos indicadores pueden activarse correctamente.
- En heads-up donde solo hay 2 jugadores rotando, la lógica funciona si dealer detection es correcto.

**Comportamiento esperado:** = real para heads-up nominal. Pero si `_previousSBPlayerName == _previousBBPlayerName` (caso edge raro de OCR fallido), la lógica se rompe.

**Consecuencias si se ignora:**
- 🟡 Falsos positivos / negativos en `DetectNewHand` en heads-up con OCR errático.

**Cobertura test:** TT-26 (sesión heads-up 50 manos, manual).

---

## EC-19 — `MaxHandsInMemory=20` con sesión de 1000+ manos: stats divergen

**Disparador:** Sesión cash de 1000 manos. Tras la primera mano, `_sessionTotalHands` empieza a contar. Tras la mano 21, `_currentSession.Hands` se trunca al primer elemento (FIFO).

**Comportamiento real** (🟢 acumuladores como fuente de verdad):
- `_sessionTotalHands` (Interlocked) refleja 1000.
- `_sessionTotalProfit` (Interlocked) refleja la suma real.
- `_currentSession.Hands.Count == 20` — muestra solo las últimas 20.
- `BBPer100 = (_sessionTotalProfit / _sessionTotalHands * 100) / bigBlind` correctamente computado desde acumuladores.
- Pestaña Historial muestra solo las últimas 20 (suficiente UX); detalle abre `GetHandsForSessionAsync` que queries todas.

**Comportamiento esperado:** = real. Validado.

**Consecuencias si se ignora:** N/A. Diseño correcto.

**Cobertura test:** Falta test de regresión específico — TT-21 cubre 3 manos.

---

## EC-20 — `FrmOverlay` con magenta-key sobre cliente que pinta magenta

**Disparador:** Hipotéticamente, una sala de poker que use `#FF00FF` (magenta saturado) en algún elemento de UI. El overlay tiene un "agujero" sobre esa región.

**Comportamiento real** (🟡 inevitable con magenta-key):
- Las regiones del cliente con color magenta exacto se ven a través del overlay.
- No es un bug — es la limitación inherente de magenta-key transparency.

**Comportamiento esperado:**
- Documentar la limitación.
- O migrar a WPF con alpha channel real (ver DD-08 alternativas).

**Consecuencias si se ignora:**
- 🟡 Visualmente extraño en clientes hipotéticos. Ningún cliente actual conocido pinta magenta saturado.

**Cobertura test:** N/A.

---

## EC-21 — Reload de mapa de mesa durante captura activa

**Disparador:** El usuario abre la pestaña Tablas y aplica un nuevo `tableMap.json` mientras el `BackgroundWorker` está iterando.

**Comportamiento real** (🟡 race condition leve):
- `LoadTableMapUseCase` invoca `RegionLookupCache.Initialize(newMaps)` que reconstruye los diccionarios.
- Mientras tanto, `btnCapture_Click` puede estar en medio del lookup `regionLookupCache.GetRegion(...)` — en otro thread.
- `Dictionary` no es thread-safe — el reader puede ver un estado inconsistente y lanzar `InvalidOperationException`.

**Comportamiento esperado:**
- `RegionLookupCache.Initialize` debe ser thread-safe — usar `ConcurrentDictionary` o `Volatile.Write` en una nueva instancia y reasignar atómicamente.
- Pausar el `BackgroundWorker` durante reload (`_executeCapture = false; await; reload; _executeCapture = true`).

**Consecuencias si se ignora:**
- 🟡 Excepción transitoria en un ciclo (capturada por `BackgroundWorker` try/catch — EC-07).
- 🟡 Recuperación automática en el siguiente ciclo.

**Cobertura test:** Ninguna.

---

## EC-22 — `IDocumentStore` resuelto antes de `services.AddDataBase` registrar Marten

**Disparador:** Durante refactor del `Program.cs`, alguien mueve la línea `services.AddDataBase(...)` después de un consumidor de `IDocumentStore` que se construye en `BankrollTrackerService` (lambda factory — `Program.cs:107-113`).

**Comportamiento real** (🔴 InvalidOperationException):
- `services.AddSingleton(sp => { var store = sp.GetRequiredService<IDocumentStore>(); ... })` con `IDocumentStore` no registrado lanza al primer resolve.
- En el caso actual, `services.AddDataBase` está PRIMERO (línea 52), antes de `BankrollTrackerService` (línea 107). Funciona.

**Comportamiento esperado:** = real para el orden actual. Si se cambia el orden, se rompe.

**Consecuencias si se ignora:**
- 🔴 La app no arranca. Mensaje de error claro: `Unable to resolve service for type 'Marten.IDocumentStore'`.

**Cobertura test:** Ninguna específica. El crash en arranque es notable.

**Mitigación:** Comentario en `Program.cs:52` indicando que `AddDataBase` debe ir antes que cualquier servicio que consuma `IDocumentStore` en lambda factory.

---

## Resumen por categoría

| Categoría | EC | Severidad max |
|-----------|-----|---------------|
| Captura y OCR | EC-01, EC-02, EC-05, EC-10, EC-11, EC-17 | 🔴 EC-17 (crash proceso) |
| Configuración y arranque | EC-03, EC-04, EC-22 | 🔴 EC-03 (riesgo BD producción) |
| Game loop y state machine | EC-12, EC-15 | 🟡 |
| Persistencia y lifecycle | EC-06, EC-08, EC-09, EC-19 | 🟡 EC-09 (pérdida de manos) |
| Threading y race conditions | EC-07, EC-21 | 🟡 |
| Telemetría y caches | EC-13, EC-14, EC-16 | 🟡 |
| UI y overlay | EC-18, EC-20 | 🟡 |

**Distribución de severidad:**
- 🔴 Críticos: 5 (EC-02, EC-03, EC-11, EC-17, EC-22) — requieren fix prioritario
- 🟡 Importantes: 17 — mejoras de robustez
- Total: 22 ECs

**Cobertura test actual:** ~25 % (5 ECs cubiertos parcialmente por TTs). Reforzar con tests específicos de regresión.
