# OpenScrape.App — Decisiones de Diseño

> Registro de decisiones arquitecturales específicas del composition root, la UI y la captura/OCR. Cada decisión cita su evidencia en código y, cuando aplica, su ADR correspondiente en `_reversa_sdd/adrs/`. Se incluyen también **anomalías que actúan como decisiones de facto** (no documentadas formalmente, pero asentadas en la base de código). Las decisiones globales del proyecto (Clean Architecture, Marten, OCR over API, etc.) viven en `_reversa_sdd/adrs/`; aquí se documentan **solo las decisiones que viven en esta capa**.

---

## DD-01 — `FrmMain` resuelto desde `CreateAsyncScope`, no del root

**Decisión:** El formulario principal se registra como `Transient` y se resuelve desde un `IServiceScope` creado explícitamente en `Program.Main`. El scope vive todo el ciclo de la ventana y se dispone con `await scope.DisposeAsync().AsTask().GetAwaiter().GetResult()` en `finally`.

**Contexto:** `FrmMain` consume 13 servicios scoped (`GameCoordinator`, `TableLayoutService`, `GameLoggerService`, `PostflopContextHolder`, `SetPreflopActionUseCase`, `GameLoopCoordinator`, `UiSyncService`, `PokerDecisionFacade`). Si `FrmMain` se resolviera del root (`host.Services`), todos los scoped se promoverían implícitamente a singleton: el `_currentSession` del logger, el `_currentHand`, el `Current` del context holder, todos vivirían cross-mesa y cross-mano. Además, `GameLoopCoordinator` implementa **`IAsyncDisposable`** — sin scope async no se podría cerrar limpiamente al `Application.Run` retornar.

**Alternativas consideradas:**
1. **`FrmMain` resuelto desde `host.Services` (root)** — descartado: rompería el lifecycle scoped y forzaría a auto-gestionar `IDisposable` manualmente.
2. **`FrmMain` registrado como `Singleton`** — descartado: imposibilita re-instanciar la ventana sin reiniciar el proceso (caso de cambio de configuración crítico).
3. **`CreateScope()` síncrono** — descartado: `GameLoopCoordinator.IAsyncDisposable.DisposeAsync` no se ejecuta correctamente si el scope no es async.

**Consecuencias positivas:**
- 🟢 Imposible que dos sesiones contiguas compartan estado postflop.
- 🟢 `Application.Run` retorna y el `finally` dispone TODOS los scoped (incluyendo `IAsyncDisposable`).
- 🟢 Tests pueden crear scopes ad-hoc para validar comportamiento sin instanciar `FrmMain`.

**Consecuencias negativas:**
- 🟡 Patrón sutil — un developer junior puede invocar `host.Services.GetService<FrmMain>()` y romper el lifecycle silenciosamente. Mitigación: comentario explicativo en `Program.cs:210-212` y test de DI que verifica resolución desde scope.

**Evidencia:** `src/OpenScrape.App/Program.cs:178,213-222`. ADR-0014 § FrmMain Scoped No Root. 🟢

---

## DD-02 — Forwarding pattern (clase concreta + interface comparten instancia)

**Decisión:** Cada algoritmo y servicio del `OpenScrape.DecisionMaker` se registra DOS veces en el contenedor: primero como clase concreta (`AddSingleton<MonteCarloSimulator>()`) y después como interfaz mediante factoría que reusa la concreta (`AddSingleton<IMonteCarloSimulator>(sp => sp.GetRequiredService<MonteCarloSimulator>())`). Aplica a 18 pares (5 algoritmos + 13 servicios DM).

**Contexto:** `OpenScrape.App.Tests` y algunos consumidores internos (telemetría) necesitan acceso a la **clase concreta** (para resetear caches, leer estado interno, ejecutar overloads no expuestos en la interfaz). Los consumidores de producción consumen la **interfaz** (para mockeo y desacoplamiento). Si se registraran por separado, dos `GetService` consecutivos podrían retornar instancias distintas, rompiendo el invariante de caches singleton (`MonteCarloSimulator` mantiene `ConcurrentDictionary<string, double>` de equity).

**Alternativas consideradas:**
1. **Solo registrar la concreta y resolver `interface` por reflection** — descartado: no es idiomático en `Microsoft.Extensions.DependencyInjection` y rompe `IServiceCollection` semantics.
2. **Solo registrar la interfaz y down-castear cuando se necesita la concreta** — descartado: explota encapsulación y rompe LSP.
3. **Dos `AddSingleton` separados (`<Concrete>` y `<IFace, Concrete>`)** — descartado: produce DOS instancias singleton distintas (verificable: `sp.GetService<MonteCarloSimulator>() != sp.GetService<IMonteCarloSimulator>()`).

**Consecuencias positivas:**
- 🟢 Una sola instancia compartida — el cache de equity NO se duplica.
- 🟢 Interfaz para mockeo en tests; concreta para acceso interno.
- 🟢 Cero allocations adicionales: el lambda factory `sp => sp.GetRequiredService<Concrete>()` es resuelto una sola vez al construir el grafo singleton.

**Consecuencias negativas:**
- 🟡 ~30 líneas redundantes en `Program.cs:71-113`. Mitigación: extender con `services.AddSingletonForwarded<IFace, Concrete>()` extension method (no implementado actualmente).
- 🟡 Si se olvida el forwarding y se hace `AddSingleton<IFace, Concrete>()` directo, el bug es silencioso (dos instancias). Tests de DI de regresión recomendados.

**Evidencia:** `src/OpenScrape.App/Program.cs:71-113`. ADR-0006 § Pipeline Unificado. 🟢

---

## DD-03 — Fail-fast del `StrategyProfile` antes de mostrar UI

**Decisión:** Tras `host.Build()` y antes de `Application.Run(form)`, `Program.Main` resuelve `IOptions<StrategyProfile>` y llama a `StrategyProfileValidator.Validate(profile)`. Si lanza `StrategyProfileValidationException` (lista acumulada de errores), se muestra `MessageBox` con el mensaje completo y se invoca `Environment.Exit(1)` SIN levantar `FrmMain`.

**Contexto:** `StrategyProfile` tiene ~150 parámetros distribuidos en `Dictionary<string, StreetThresholds>` (clave `"{BoardPosition}_{HandSituation}"`, ej. `"Flop_OpenRaise"`). Un error tipográfico en una clave (`"Flop_OpenRaise"` vs `"flop_openraise"`) o un orden incoherente de tiers (`FoldBelow=80, ThinValueAbove=70`) NO produce excepción al hidratar `IOptions` — produce un `StreetThresholds` vacío que aplica defaults. Si el bot arranca con thresholds defaults silenciosamente, el motor decide con criterios distintos a los que el usuario pretendía. **Es preferible no arrancar.**

**Alternativas consideradas:**
1. **Validación lazy al primer uso** — descartado: el primer error se vería tras 5 minutos de juego, con dinero apostado bajo decisiones erradas.
2. **Logging warning + continuar con defaults** — descartado: defecto silencioso, viola el principio de fail-fast.
3. **Validación en build-time con generador de código** — descartado: complejidad excesiva; el JSON aún se podría editar mal en producción.

**Consecuencias positivas:**
- 🟢 La app NO ejecuta nunca con thresholds incoherentes.
- 🟢 El usuario ve el listado completo de errores en una sola pantalla — facilita corrección.
- 🟢 La validación incluye órdenes de tiers, rangos `[0,100]`, presencia de las ~30 combinaciones obligatorias.

**Consecuencias negativas:**
- 🟡 `Environment.Exit(1)` no permite recovery interactivo. Una versión más usuario-amigable abriría un editor de profile o `MessageBox` con botones "Reparar"/"Cerrar". Mitigación documentada en `questions.md`.

**Evidencia:** `src/OpenScrape.App/Program.cs:187-197`, `src/OpenScrape.App/Services/StrategyProfileValidator.cs:1-97`. ADR-0008 § Thresholds Tipados Startup Validation. 🟢

---

## DD-04 — Game loop ejecutado por `BackgroundWorker` (vs `PeriodicTimer`) — feature flag opt-in

**Decisión:** El ciclo de captura periódico vive en `BackgroundWorker1_DoWork` (`Forms/FrmMain.cs:2737-2897`) que invoca `btnCapture_Click` cada `CaptureIntervalMs=100`. Una alternativa más moderna basada en `PeriodicTimer + IAsyncDisposable` existe en `Services/GameLoopCoordinator.cs` pero está **detrás de `FeatureFlags.UseGameLoopCoordinator=false`** y retorna `GameLoopResult { Empty = true }` por tick. La migración Fase 4 está pendiente.

**Contexto:** WinForms diseñado en .NET Framework usaba `BackgroundWorker` como primitiva de concurrencia. Migrar a `PeriodicTimer` requiere reescribir el dispatch UI (`Invoke`/`BeginInvoke` cross-thread), el cleanup (`CancellationTokenSource` + `await StopAsync`), y sobre todo, validar exhaustivamente que el comportamiento de captura/decisión es idéntico. El equipo eligió mantener `BackgroundWorker` operacional y construir `GameLoopCoordinator` en paralelo, activable por flag, para permitir cutover gradual sin riesgo.

**Alternativas consideradas:**
1. **Migración big-bang a `PeriodicTimer`** — descartado: regresiones difíciles de detectar (timing, threading, ordering de eventos).
2. **Eliminar el flag y mantener solo `BackgroundWorker`** — descartado: bloquearía mejoras futuras (cancelación cooperativa, eventos `IAsyncDisposable`, async/await first-class).
3. **`System.Timers.Timer` o `System.Threading.Timer`** — descartado: el primero captura excepciones silenciosamente; el segundo dispatcha en `ThreadPool` lo que multiplica `Invoke` cross-thread.

**Consecuencias positivas:**
- 🟢 Riesgo cero en runtime actual — `BackgroundWorker` está battle-tested.
- 🟢 `GameLoopCoordinator` permite migrar gradualmente con `if (flag) UseNew() else UseOld()`.
- 🟢 Ambas implementaciones respetan el mismo `GameLoopOptions.CaptureIntervalMs`.

**Consecuencias negativas:**
- 🟡 Código muerto bajo el flag — `GameLoopCoordinator` (213 LOC) no aporta valor en producción hoy. Riesgo de bit-rot. Mitigación: tests unitarios cubren su contrato.
- 🟡 Decisión pendiente (Fase 4 cutover). Documentado en `questions.md`.

**Evidencia:** `src/OpenScrape.App/Forms/FrmMain.cs:2737-2897`, `src/OpenScrape.App/Services/GameLoopCoordinator.cs:1-213`, `src/OpenScrape.App/Configuration/FeatureFlags.cs`, `src/OpenScrape.App/Configuration/GameLoopOptions.cs`. 🟡

---

## DD-05 — `OcrService` con `lock` global (Tesseract single-threaded)

**Decisión:** Todas las llamadas a `TesseractEngine` están protegidas por un `static readonly object _lock = new()` global del módulo. El acceso al engine es 100 % serializado. Esto se asume incluso para el cache LRU (`_bitmapCache`, `_ocrCache`) que también se protege con `lock` interno via `LruCache<TK,TV>`.

**Contexto:** `Tesseract 5.x` para .NET no es thread-safe — el engine mantiene estado interno mutable (configuración de PSM, PageIterator, RIL_PARA buffers). Concurrencia no protegida produce `AccessViolationException` en una fracción de calls (no determinista). El único modo seguro es serializar.

**Alternativas consideradas:**
1. **Pool de `TesseractEngine` con `ObjectPool<TesseractEngine>`** — descartado: cada engine carga ~30 MB de traineddata, un pool de 4 engines duplica la memoria sin ganancia neta (el bottleneck real es CPU del OCR, no el contention del lock).
2. **Engine `[ThreadStatic]` con un engine por thread** — descartado: imposible con `BackgroundWorker` que reusa threads de `ThreadPool`.
3. **`SemaphoreSlim(1, 1)` async** — descartado: las primitivas async no aportan valor en este path (el OCR mismo es síncrono y CPU-bound).

**Consecuencias positivas:**
- 🟢 Cero crashes por concurrencia.
- 🟢 Memoria predecible: una sola instancia de engine + traineddata cargada.
- 🟢 Cache hit ratio alto compensa el contention del lock.

**Consecuencias negativas:**
- 🟡 Si el cache miss es alto (sesión con cliente nuevo, regiones desconocidas), el lock se vuelve cuello de botella. Mitigación: el dHash 64-bit perceptual del `ImageCropperService` resuelve la mayoría de matches sin tocar el engine.
- 🟡 Documentación dispersa — el `lock` está justo en una línea sin comentario explicando el porqué. Se sugiere comentario en `Services/OcrService.cs:19`.

**Evidencia:** `src/OpenScrape.App/Services/OcrService.cs:19,33`. ADR-0010 § Tesseract single-threaded. 🟢

---

## DD-06 — Auto-rebuy detection con `_heroStackPreRebuy` snapshot (no flag)

**Decisión:** `FrmMain` mantiene un `private decimal _heroStackPreRebuy` que se actualiza durante la mano activa. Al detectar nueva mano, `EndHand` recibe `prevHeroStack = _heroStackPreRebuy > 0 ? _heroStackPreRebuy : _playerGameState.HeroStack` (line 690). El snapshot NO se sobrescribe cuando el stack sube bruscamente — esto distingue el auto-rebuy del juego normal.

**Contexto:** Los clientes de poker auto-rebuy al stack predefinido (≈100 BB) cuando el hero cae bajo un threshold (≈5 BB) y termina la mano. Si el bot lee el stack al inicio de la siguiente mano, ve 100 BB y registraría `Profit = 100 - heroStackStart` (ej. `100 - 87 = +13 BB`) cuando en realidad el hero perdió 12 BB. El snapshot pre-rebuy preserva el valor justo antes del rebuy y permite calcular `Profit = stackPreRebuy - heroStackStart` correctamente.

**Alternativas consideradas:**
1. **Bandera booleana `_isRebuying`** — descartado: requiere distinguir "subida real por ganancia" vs "subida por rebuy", lo que necesita inspeccionar el game state — más complejo que el snapshot.
2. **Leer rebuy del log del cliente** — descartado: parsear logs del cliente es frágil y depende de la sala.
3. **Confiar en `HandRecord.HeroStackEnd` antes del cierre** — descartado: el closing de la mano puede coincidir con el rebuy automático (race condition en algunos clientes).

**Consecuencias positivas:**
- 🟢 Profit por mano correcto incluso con auto-rebuy frecuente (sesiones cash de stake bajo).
- 🟢 Mecanismo simple de 5 líneas en `FrmMain.cs:99-105,690,713`.
- 🟢 No requiere inferir intención del cliente — solo observa el stack.

**Consecuencias negativas:**
- 🟡 Threshold de "subida brusca" no está parametrizado — vive implícito en el orden de operaciones. Mitigación: documentar que el snapshot se debe actualizar SOLO cuando el stack baja o se mantiene, no cuando sube. Test de regresión recomendado.
- 🟡 No detecta correctamente "ganancia + rebuy en la misma mano" (escenario raro pero posible en heads-up con rebuys cooperativos).

**Evidencia:** `src/OpenScrape.App/Forms/FrmMain.cs:99-105,690,713`. ADR-0013 § Auto-Rebuy Detection. 🟢

---

## DD-07 — `GameLoopStateMachine` con validación cruzada `visibleBoardCards`

**Decisión:** El método `TryTransition(GameState newState)` tiene un overload `TryTransition(GameState newState, int visibleBoardCards)` que rechaza la transición si las cartas visibles del board no coinciden con el mínimo esperado por el estado destino: `≥3` para `FlopDetected`/`FlopAction`, `≥4` para `Turn*`, `≥5` para `River*`. La transición rechazada NO mueve el estado; el siguiente ciclo del game loop reintenta.

**Contexto:** OCR puede leer "4 cartas visibles" en un flop si el filtrado no es perfecto (artefacto, ruido, animation overlay). Sin validación, el state machine avanzaría a `TurnDetected` y el motor postflop calcularía equity con un board que NO está completo, produciendo decisiones erradas. La validación cruzada actúa como **invariante estructural** que evita estados incoherentes.

**Alternativas consideradas:**
1. **Validar antes de llamar a `TryTransition`** — descartado: deja la responsabilidad al caller, multiplica el código cliente y permite olvidos.
2. **`Debug.Assert`** — descartado: solo activo en debug builds, no protege producción.
3. **Bloquear con excepción en lugar de retornar `false`** — descartado: el reintento natural del game loop es el mecanismo correcto; una excepción mata el ciclo.

**Consecuencias positivas:**
- 🟢 Imposible decidir en street equivocada por error de OCR de cartas.
- 🟢 La validación se concentra en un solo punto (`Services/GameLoopStateMachine.cs:73-110`).
- 🟢 `LogWarning` con `CurrentState`, `NewState`, `VisibleCards`, `ExpectedMin` — fácil debugging.

**Consecuencias negativas:**
- 🟡 Si el OCR pierde una carta y solo lee 4 cuando hay 5, no transita a River — el bot decide en Turn con board incompleto. Mitigación: `MaxOcrRetries=2` y el ciclo siguiente reintenta.

**Evidencia:** `src/OpenScrape.App/Services/GameLoopStateMachine.cs:73-110`. ADR-0012 § Game Loop State Machine + Board Card Validation. 🟢

---

## DD-08 — `FrmOverlay` con `Color.Magenta TransparencyKey` (no WPF, no transparency real)

**Decisión:** `FrmOverlay` es un `System.Windows.Forms.Form` topmost sin bordes con `TransparencyKey = Color.Magenta`. Cualquier región pintada de magenta es **transparente al cliente OS**: el click pasa al cliente debajo, no al overlay. Las áreas no-magenta (texto, indicadores, action panel) son opacas. Drag manual con P-Invoke `WM_NCLBUTTONDOWN`.

**Contexto:** El overlay debe (a) mostrar la decisión recomendada visible al usuario, (b) no robar foco al cliente de poker (clicks deben ir al cliente), (c) mantenerse encima del cliente. WinForms no soporta transparencia real (alpha channel) sin perder click-through. La técnica magenta-key es la solución estándar en aplicaciones desktop pre-WPF que conserva click-through y opacidad selectiva.

**Alternativas consideradas:**
1. **`Form.Opacity = 0.7`** — descartado: opacidad uniforme, no permite áreas opacas + áreas click-through.
2. **WPF con `WindowStyle="None"` + `AllowsTransparency="True"`** — descartado: requeriría introducir WPF en el proyecto solo para el overlay; complica el deploy y duplica frameworks.
3. **DirectX overlay con `D3DKMT`** — descartado: complejidad enorme para un beneficio visual marginal.

**Consecuencias positivas:**
- 🟢 Click-through real: el cliente de poker recibe los clicks normalmente.
- 🟢 Soporte nativo de WinForms — sin paquetes adicionales.
- 🟢 Drag manual con `WM_NCLBUTTONDOWN` permite reposicionar sin barra de título.

**Consecuencias negativas:**
- 🟡 Si el cliente de poker pinta magenta en alguna parte (raro pero posible), aparece "agujero" en el overlay sobre esa región. Mitigación: usar magenta saturado puro (`#FF00FF`) que ningún cliente serio usa.
- 🟡 Renderizado lento si el overlay tiene muchas capas — límite práctico ~9 filas + action panel (suficiente para mostrar 9 jugadores + acción).

**Evidencia:** `src/OpenScrape.App/Forms/FrmOverlay.cs:?`, `src/OpenScrape.App/Forms/FormAction.cs:?`. 🟢

---

## DD-09 — Equity cache con `ConcurrentDictionary` FIFO (no LRU)

**Decisión:** `UnifiedPokerCalculator._equityCache` es un `ConcurrentDictionary<string, double>` con `EquityCacheMaxSize=2048`. Al llegar al tope, se descarta el primer key insertado (FIFO). NO es un LRU real — un key consultado 100 veces puede ser desalojado por un key insertado 1 vez si está antes en orden.

**Contexto:** `MonteCarloSimulator.CalculateEquity` es CPU-bound (50 K iteraciones en flop, ~42 K evals exactos en turn). Cachear evita re-calcular cuando las cartas no cambian — escenario muy común en el polling cada 100 ms del `BackgroundWorker`. La clave incluye `playerHand|communityCards|numOpp|situation` y captura el ~95 % de los re-cálculos. La diferencia entre LRU "perfecto" y FIFO "bueno" en este caso es marginal.

**Alternativas consideradas:**
1. **`LruCache<string, double>` thread-safe** — descartado: contention en el `LinkedList` interno bajo concurrencia high-frequency. La ventaja sobre FIFO es <5 % en hit ratio para este patrón de uso.
2. **`MemoryCache` con expiración temporal** — descartado: el equity NO expira (cartas no cambian). Una expiración temporal es semánticamente errada.
3. **Sin tope** — descartado: en sesiones largas (>10K manos), el cache crece indefinidamente con keys únicos por mano.

**Consecuencias positivas:**
- 🟢 Hit ratio empírico >90 % en sesiones nominales (validado por logs internos).
- 🟢 Implementación trivial — `ConcurrentDictionary` thread-safe out-of-the-box.
- 🟢 Cero allocations en hit path.

**Consecuencias negativas:**
- 🟡 FIFO mata el cache de equity preflop heavily reusado (AKs, QQ, etc.) si llegan más de 2048 entries. Mitigación: 2048 es suficiente para cubrir 169 manos × 12 situaciones × 5-10 boards = 10-20K combinations únicas a lo largo de una sesión, pero la mayoría de hits en ventana corta.
- 🟡 No hay mecanismo de invalidación si `StrategyProfile` cambia (nuevos thresholds → equity bajo profile distinto). Decisión: el profile NO se cambia en runtime; cambiarlo requiere reinicio. Documentado en `questions.md`.

**Evidencia:** `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs:59-60`. 🟢

---

## DD-10 — OCR con cache bicapa indexado por `dHash` 64-bit

**Decisión:** `OcrService` mantiene DOS caches en serie:
1. `_bitmapCache: LruCache<string, SKBitmap>` (200 entradas) — cachea el bitmap pre-procesado por `imagePath + region key`.
2. `_ocrCache: LruCache<ulong, string>` (500 entradas) — cachea el resultado OCR indexado por `dHash` 64-bit del bitmap pre-procesado.

El flujo es: `imagePath + region → bitmap (cache 1) → dHash → text (cache 2)`. Hamming distance ≤15 considera match en `ImageCropperService`.

**Contexto:** El polling cada 100 ms produce muchas capturas idénticas o casi-idénticas (cartas no cambian, bets no cambian). El dHash perceptual permite que dos capturas con diferencias mínimas (1-2 píxeles por compresión JPEG, antialiasing del cliente) hit el mismo cache entry. El Hamming threshold ≤15 fue calibrado empíricamente para tolerar variaciones legítimas sin matches falsos.

**Alternativas consideradas:**
1. **Hash exacto (SHA256, MD5)** — descartado: cualquier diferencia de píxel produce miss; hit ratio efectivo cae <30 %.
2. **Cache solo de OCR text (sin bitmap)** — descartado: el preprocesamiento (resize+grayscale+contraste+deskew) es caro (~10 ms) y se repetiría cada call.
3. **Single cache mezclado** — descartado: imposible separar invalidation independiente de bitmap vs text.

**Consecuencias positivas:**
- 🟢 Hit ratio empírico >70 % en sesiones largas.
- 🟢 Latencia de cache hit <1 µs (vs OCR fresh ~30-100 ms).
- 🟢 Hamming distance tolerante a artefactos visuales menores.

**Consecuencias negativas:**
- 🟡 dHash 64-bit tiene colisiones — dos imágenes distintas pueden mapear al mismo hash si el patrón es similar. Mitigación: el dominio (cartas, bets, stacks) tiene texto bien diferenciado; colisiones reportadas en log <0.1 % de cases.
- 🟡 Hamming ≤15 puede confundir "5,93 BB" con "5,98 BB" si la diferencia visual es solo el último dígito. Mitigación: validación post-OCR contra el pot context (`NormalizeBetValue` rechaza valores incoherentes).

**Evidencia:** `src/OpenScrape.App/Services/OcrService.cs:13-18`, `src/OpenScrape.App/Services/ImageCropperService.cs:1-395`. 🟢

---

## DD-11 — Telemetría con 17 categorías como contrato estable + Histogram log buckets

**Decisión:** `TelemetryCategories` define 17 categorías como `public const string`. **Las cadenas forman parte del esquema persistido en `HandRecord.Telemetry`** y NO se renombran sin migración. Los histogramas usan 30 buckets logarítmicos `bound[i] = 1e-5 × 10^(i × 0.2)` segundos (10 µs → 6.3 s) que sobrestiman ~37 % nunca subestiman.

**Contexto:** La telemetría debe servir tanto para debugging en runtime (pestaña Métricas) como para análisis histórico (queries sobre `HandRecord.Telemetry`). Renombrar `"OCR.Cards"` a `"Ocr.Cards"` rompería todos los reports históricos. Los buckets log permiten cubrir un rango de 5+ órdenes de magnitud (µs → segundos) con solo 30 buckets, suficientes para detectar regresiones (típicamente 2-3× en latencia).

**Alternativas consideradas:**
1. **Categorías como `enum`** — descartado: el persisted format en Marten necesita strings; un `enum` requeriría serializer custom y reverse map.
2. **Buckets lineales** — descartado: 100 buckets de 1 ms cubrirían solo 0-100 ms; saturarían en la cola larga.
3. **Buckets adaptativos (histogram balanceado)** — descartado: complejidad excesiva para el ROI; la sobrestimación del 37 % es aceptable para detectar regresiones.

**Consecuencias positivas:**
- 🟢 Esquema persistido estable — queries históricas funcionan tras meses.
- 🟢 30 buckets cubren el rango completo con baja memoria (240 bytes/categoría/histograma).
- 🟢 `DisplayOrder` y `SessionOnly` son centralizados en una clase static — UI y logger los consumen.

**Consecuencias negativas:**
- 🟡 Sobrestimación 37 % es aceptable para regresiones pero NO para SLOs absolutos. Mitigación: usar percentiles relativos a baselines, no absolutos.
- 🟡 Renombrar requiere migration — costo conocido. Mitigación: documentado en `Telemetry/TelemetryCategories.cs:5-7` y comentario `<!-- contrato estable -->` en código.

**Evidencia:** `src/OpenScrape.App/Telemetry/TelemetryCategories.cs:1-65`, `src/OpenScrape.App/Telemetry/Histogram.cs:1-88`. ADR-0017 § Telemetría Metrics Collector. 🟢

---

## DD-12 — `UnifiedPokerCalculator` ES el facade real (no `PokerDecisionFacade`)

**Decisión:** `Program.cs:115-116` registra `IPokerCalculator → UnifiedPokerCalculator` como singleton. **Este es el camino real de producción.** `IPokerDecisionFacade → PokerDecisionFacade` también está registrado (scoped, `Program.cs:118-119`) pero **ningún consumidor lo inyecta** — `FrmMain` consume directamente `IPokerCalculator` y `IPostflopDecisionService`. La telemetría de las "5 fases medidas" del facade NO se ejecuta en producción.

**Contexto:** El facade fue planeado como cutover Fase 2 para centralizar la pipeline en una clase con telemetría granular (equity → texture → profile → decision → sizing). Pero la migración nunca se completó — `FrmMain` todavía orquesta directamente desde `btnCapture_Click → ProcessPostFlopAsync → IPokerCalculator + IPostflopDecisionService`. El facade quedó como código muerto bajo registro DI.

**Alternativas consideradas:**
1. **Eliminar `PokerDecisionFacade` del scope** — pendiente de decisión (`questions.md`).
2. **Completar el cutover** — pendiente de decisión (Fase 2 nunca se cerró).
3. **Mantener ambos** — estado actual; la incoherencia genera confusión a developers nuevos.

**Consecuencias positivas:**
- 🟢 La pipeline real funciona y está optimizada.
- 🟢 Cero impacto en runtime de tener el facade registrado pero no usado (DI no instancia hasta resolver).

**Consecuencias negativas:**
- 🟡 Telemetría engañosa — `Decision.Equity`, `Decision.Texture`, etc. parecen sugerir que el facade los emite, pero en realidad los emite `GameCoordinator` directamente.
- 🟡 Documentación incoherente con código — CLAUDE.md y otros docs mencionan el facade como punto único cuando NO lo es. Mitigación: corregir docs en revisión.

**Evidencia:** `src/OpenScrape.App/Program.cs:115-119`, `src/OpenScrape.App/Services/PokerDecisionFacade.cs:1-231` (sin consumidores). ADR-0006 § Pipeline Unificado. 🟡

---

## DD-13 — `PostflopContextHolder` thread-safe scoped con `Volatile.Read` + `lock`

**Decisión:** El estado cross-street se accede a través de un `PostflopContextHolder` scoped. Lecturas usan `Volatile.Read(ref _current)` (memory-barrier de lectura, sin lock). Mutaciones usan `Update(Func<T,T> mutator)` bajo `lock(_gate)` produciendo nuevo `record`. `StartNewHand()` resetea bajo lock con `_current = new PostflopGameContext()`.

**Contexto:** El context holder es scoped — vive una instancia por scope de `FrmMain`. Sin embargo, la captura ocurre en `BackgroundWorker` (thread distinto del UI), y el render del overlay ocurre en UI thread. Si dos threads acceden simultáneamente al holder, una lectura podría observar un context "a medio mutar" si la mutación no fuera atómica. La inmutabilidad del `record` + `Volatile.Read` garantiza consistencia.

**Alternativas consideradas:**
1. **Mutable class con `lock` para todas las operaciones** — descartado: lectura bajo lock es contention innecesaria; los lectores son frequency >>> mutadores.
2. **`Interlocked.Exchange` directamente sobre `_current`** — descartado: no protege la operación compuesta `read-modify-write` que `Update(Func<T,T>)` necesita.
3. **`ImmutableInterlocked.Update`** — equivalente al `lock` actual, pero el `lock` simple es suficiente y más legible.

**Consecuencias positivas:**
- 🟢 Lecturas zero-contention con `Volatile.Read`.
- 🟢 Mutaciones atómicas garantizadas — siempre hay un `_current` válido visible a lectores.
- 🟢 `PostflopGameContext` inmutable estructuralmente — imposible mutar accidentalmente.

**Consecuencias negativas:**
- 🟡 `Update(mutator)` no es lock-free — bajo contention alta (no aplica aquí), `Interlocked.CompareExchange` con retry sería más performante.
- 🟡 Documentación implícita — si un developer agrega un mutador sin usar `Update`, rompe el invariante. Mitigación: hacer `_current` private y exponer solo los métodos del holder.

**Evidencia:** `src/OpenScrape.App/Services/PostflopContextHolder.cs:1-35`. ADR-0007 § PostflopContext Inmutable Holder Scoped. 🟢

---

## DD-14 — `RegionLookupCache` y `CardCacheService` como singletons con prelectura lazy

**Decisión:**
- `RegionLookupCache` (singleton) precarga `Dictionary<mapId, Dictionary<regionName, Region>>` con `OrdinalIgnoreCase` al primer `Initialize(maps)`. Lookup O(1).
- `CardCacheService` (singleton) hace UNA query Marten por vida de la app (52 cartas) protegida por `SemaphoreSlim` double-check.

**Contexto:** Los lookups de regiones eran originalmente `regions.FirstOrDefault(r => r.Name == name)` — O(n) sobre listas de 50-100 entries, ejecutado 17 veces por ciclo. El cache de cartas tenía 3 use cases (`GetCardsFlop/Turn/RiverUseCase`) consultando cada uno la BD por separado. La consolidación a singletons elimina ambos overheads.

**Alternativas consideradas:**
1. **Mantener `FirstOrDefault` y aceptar el costo** — descartado: BenchmarkDotNet midió 35 % del CPU del ciclo era lookup linear; el cache redujo a <2 %.
2. **`MemoryCache` con expiración** — descartado: las regions y cards NO cambian en runtime; expiración es semánticamente errada.
3. **Carga eager en arranque** — equivalente al lazy actual, pero el lazy permite arrancar la app sin DB conectada (Marten errors aparecen al primer use, no en startup).

**Consecuencias positivas:**
- 🟢 Lookups O(1) verificable con BenchmarkDotNet.
- 🟢 1 query Marten por vida de app (vs N por scope con 3 use cases).
- 🟢 `CardCacheService.SemaphoreSlim` double-check evita race condition en el primer call concurrente.

**Consecuencias negativas:**
- 🟡 Si la BD se actualiza en caliente con nuevas cards o regions, la app NO ve los cambios sin reinicio. Mitigación: documentar como invariante operacional; las cards son los 52 estándar (no cambian) y las regions se cargan vía `LoadTableMapUseCase` que invalida `RegionLookupCache.Initialize(maps)`.

**Evidencia:** `src/OpenScrape.App/Services/RegionLookupCache.cs:1-64`, `src/OpenScrape.App/Services/CardCacheService.cs:1-44`. ADR-0016 § Perf Pixel Sampling Region Card Cache. 🟢

---

## DD-15 — Truncado `MaxHandsInMemory=20` con acumuladores `Interlocked` como fuente de verdad

**Decisión:** `GameLoggerService` mantiene en memoria solo las últimas 20 manos en `_currentSession.Hands`; las anteriores ya están persistidas como `HandRecord` documents independientes. Los stats de sesión (`TotalHands`, `TotalProfit`, `BBPer100`) NO se calculan iterando `Hands` — se mantienen en `_sessionTotalHands: int` y `_sessionTotalProfit: decimal` actualizados con `Interlocked`.

**Contexto:** Sesiones largas (4-6 h) producen 200-400 manos. Mantener todas en memoria sería ~50-100 KB por sesión × N sesiones — manejable, pero `GameSession.Hands` se serializa a Marten como un campo del documento. Persistir el array completo cada vez que cambia es O(n²) por sesión. El truncado + acumuladores resuelve ambos problemas: memoria predecible + persistencia O(1) por hand.

**Alternativas consideradas:**
1. **Sin truncado, con persist incremental** — descartado: Marten `Patch` es más complejo y no soporta append-to-array trivialmente.
2. **Truncado sin acumuladores (recalcular bajo demanda con query)** — descartado: queries Marten para 400 docs por sesión son lentas vs `Interlocked` con O(1) overhead.
3. **EventStore con eventos por mano** — descartado: complejidad arquitectural enorme para una métrica derivada.

**Consecuencias positivas:**
- 🟢 Memoria O(1) por sesión (20 manos ~5 KB).
- 🟢 Persistencia O(1) por mano completada.
- 🟢 Queries (`GetRecentSessionsWithStatsAsync`) consultan `_sessionTotalHands` directo del documento.

**Consecuencias negativas:**
- 🟡 Si la app crashea entre `_sessionTotalHands++` y `await SaveSessionAsync()`, los acumuladores divergen del recuento real de `HandRecord` en disco. Mitigación: `await SaveSessionAsync()` se llama tras cada `EndHand` y al cambiar de mesa.
- 🟡 La pestaña Historial muestra solo las 20 últimas — para ver más, el usuario abre el detalle de sesión que sí queries `GetHandsForSessionAsync`.

**Evidencia:** `src/OpenScrape.App/Services/GameLoggerService.cs:15,27-28`. ADR-0017. 🟢

---

## DD-16 — `SetPreflopActionUseCase` scoped (no singleton, anti-patrón corregido)

**Decisión:** `SetPreflopActionUseCase` se registra como `Scoped` (`Program.cs:144`), no singleton, **a pesar de que su clase implementación no tiene estado mutable**. La razón: depende de `ActionScenarioUseCases` del módulo `OpenScrape.Features` que SÍ es scoped (consume `IDocumentSession` per-request).

**Contexto:** Originalmente `SetPreflopActionUseCase` se registró como Singleton — coincidía con su naturaleza stateless. Pero al inyectar `ActionScenarioUseCases` (scoped), DI detectaba el conflicto en debug builds (`InvalidOperationException: Cannot consume scoped service from singleton`). En release builds el detector estaba desactivado y la app instanciaba `ActionScenarioUseCases` con `IDocumentSession` capturado del scope inicial — leak entre scopes con efectos sutiles.

**Alternativas consideradas:**
1. **Hacer `ActionScenarioUseCases` singleton también** — descartado: rompería el lifecycle de `IDocumentSession` (Marten exige session per scope).
2. **Inyectar `IDocumentStore` y crear `IDocumentSession` por uso** — descartado: requiere `using` cada call y pierde el benefit del scope correlation.
3. **Mantener Singleton con factory** — descartado: factory pattern oculta el lifecycle real y mantiene el riesgo en release.

**Consecuencias positivas:**
- 🟢 Lifecycle correcto: una `IDocumentSession` por scope, sin leak entre manos.
- 🟢 Detector DI en debug builds no falla.
- 🟢 Tests unitarios crean scope ad-hoc con mocks.

**Consecuencias negativas:**
- 🟡 Documentación dispersa — el bugfix vive en commit history. Comentario en código recomendado.

**Evidencia:** `src/OpenScrape.App/Program.cs:144`. Bugfix documentado en `MEMORY.md § Refactoring Arquitectónico`. 🟢

---

## DD-17 — `TextBoxLoggerProvider` con buffer one-shot hasta `SetTextBoxTarget`

**Decisión:** `TextBoxLoggerProvider` bufferiza todos los logs emitidos antes de `SetTextBoxTarget(textBox)` en una `ConcurrentQueue<string>` con capacidad `BufferCapacity`. El primer (y único) `SetTextBoxTarget` flushea el buffer al `TextBox` en orden FIFO. Un segundo `SetTextBoxTarget` se ignora silenciosamente. Cross-thread con `BeginInvoke`. Rotación FIFO por `MaxLines` cuando el TextBox está lleno.

**Contexto:** Durante la construcción del DI graph (`Host.Build`), los servicios emiten logs antes de que `FrmMain` exista — y antes que el `TextBox` (control hijo de `FrmMain`) esté disponible. Sin buffer, los logs de arranque se pierden. Sin one-shot, un segundo `Set` accidental rompería el correlation con el TextBox actual.

**Alternativas consideradas:**
1. **Sin buffer; perder logs de arranque** — descartado: los logs de DI son críticos para diagnosticar problemas de configuración.
2. **`MemoryStream` con flush periódico** — descartado: complejidad innecesaria; `ConcurrentQueue` cubre el caso.
3. **Permitir múltiples `SetTextBoxTarget`** — descartado: confusión sobre quién recibe qué; el caso real de uso es 1 sola vez al construir `FrmMain`.

**Consecuencias positivas:**
- 🟢 Cero pérdida de logs durante arranque.
- 🟢 Cross-thread safety con `BeginInvoke`.
- 🟢 Control de tamaño con `BufferCapacity` y `MaxLines`.

**Consecuencias negativas:**
- 🟡 Si la app crashea antes de `SetTextBoxTarget`, los logs solo viven en consola — no quedan en TextBox. Mitigación: el sink de consola sigue activo en paralelo.
- 🟡 El buffer puede saturar `BufferCapacity` antes del `Set` si la fase de arranque emite muchos logs. Decisión: `BufferCapacity` configurable en `appsettings.json`, default 1000.

**Evidencia:** `src/OpenScrape.App/Services/Logging/TextBoxLoggerProvider.cs:1-138`. ADR-0009 § ILogger TextBox Sink Scopes. 🟢

---

## DD-18 — `appsettings.json` versionado con credenciales reales (anomalía 🔴)

**Decisión:** El `appsettings.json` legacy contiene credenciales reales (PostgreSQL connection string completa con usuario/password, `EncryptionKey` real). Está versionado en Git y entra al control de versiones. `appsettings.Development.json` (gitignored) se generó posteriormente para mover credenciales pero **el legacy nunca se purgó del histórico**.

**Contexto:** Anomalía detectada por el Scout (`_reversa_sdd/inventory.md § Anomalías`). Cualquiera con acceso al repo (incluido el histórico Git) puede consultar la BD personal del usuario con privilegios de su rol Marten. ADR-0018 mandata `appsettings.Development.json` como única fuente de secretos, contradicho por la realidad del repo.

**Decisión histórica de facto:** Mantener el archivo en el repo "porque ya está ahí". No es una decisión consciente — es deuda técnica.

**Alternativas consideradas (para resolución):**
1. **Rotar credenciales + purgar histórico Git con `git filter-repo`** — opción correcta pero invasiva (reescribe SHAs).
2. **Rotar credenciales + dejar histórico** — atacante con acceso al repo viejo aún ve credenciales antiguas, pero ya no funcionan.
3. **Mover a User Secrets / Azure Key Vault / etc.** — opción definitiva, requiere refactor de `Program.cs:43-50`.

**Consecuencias positivas (de la decisión histórica):**
- Ninguna. Es exclusivamente deuda técnica.

**Consecuencias negativas:**
- 🔴 Compromiso de seguridad — la BD del usuario está expuesta a cualquiera con acceso al repo.
- 🔴 Bloquea open-source-ización del repo sin previa rotación.
- 🔴 ADR-0018 contradicho por la realidad — documentación incoherente.

**Resolución pendiente:** Decisión humana en `questions.md`. Recomendación del Reviewer (Reversa): rotar credenciales urgentemente + considerar purga del histórico.

**Evidencia:** `src/OpenScrape.App/appsettings.json` (~31 KB, versionado), `_reversa_sdd/inventory.md § Anomalías`. ADR-0018 § Config Environment Development Secrets Gitignored (contradicho). 🔴

---

## DD-19 — `EncrypterHelper` con AES-CBC y IV fija de 16 ceros (anomalía 🔴)

**Decisión histórica de facto:** `EncrypterHelper.Encrypt(plaintext, secret)` usa AES-CBC con `Key = SHA256(secret)` y `IV = new byte[16]` (16 ceros). El IV se reusa entre todos los cifrados con la misma clave.

**Contexto:** Patrón clásico de cifrado naïve detectado en code review. Reusar IV con AES-CBC es vulnerabilidad conocida: dos plaintexts con prefijo común producen ciphertexts con prefijo común; un atacante con acceso a 2+ ciphertexts puede deducir información del plaintext (ataque de prefijo conocido). El uso actual es limitado (cifrado de datos persistidos pre-Marten) pero el patrón debería corregirse.

**Alternativas consideradas (para resolución):**
1. **IV aleatoria por cifrado, prefijar IV en ciphertext** — solución estándar; ciphertext crece 16 bytes; round-trip requiere extraer los primeros 16 bytes como IV antes del decrypt.
2. **AES-GCM (autenticado)** — más seguro (autentica el ciphertext); requiere `System.Security.Cryptography.AesGcm` (.NET 5+, disponible).
3. **Deprecar `EncrypterHelper` completo** — si los datos cifrados son code muerto post-Marten, eliminar el helper.

**Consecuencias negativas:**
- 🔴 Confidencialidad comprometida con prefijos repetidos.
- 🔴 No hay autenticación — un atacante puede modificar ciphertext sin detectar.
- 🟡 Posible que `EncrypterHelper` sea código muerto — decisión de validación pendiente.

**Resolución pendiente:** Decisión humana en `questions.md`. Recomendación: reescribir con IV aleatoria si está vivo; eliminar si está muerto.

**Evidencia:** `src/OpenScrape.App/Helpers/EncrypterHelper.cs:1-145`. 🔴

---

## Resumen de decisiones

| Decisión | Estado | Confianza | ADR relacionado |
|----------|--------|-----------|-----------------|
| DD-01 — `FrmMain` desde scope async | Activa | 🟢 | ADR-0014 |
| DD-02 — Forwarding pattern (concrete + interface) | Activa | 🟢 | ADR-0006 |
| DD-03 — Fail-fast `StrategyProfile` | Activa | 🟢 | ADR-0008 |
| DD-04 — `BackgroundWorker` (vs `PeriodicTimer` con flag) | Activa con cutover pendiente | 🟡 | — |
| DD-05 — Tesseract con `lock` global | Activa | 🟢 | ADR-0010 |
| DD-06 — Auto-rebuy con `_heroStackPreRebuy` | Activa | 🟢 | ADR-0013 |
| DD-07 — State machine con `visibleBoardCards` | Activa | 🟢 | ADR-0012 |
| DD-08 — `FrmOverlay` con magenta-key | Activa | 🟢 | — |
| DD-09 — Equity cache `ConcurrentDictionary` FIFO | Activa | 🟢 | — |
| DD-10 — OCR cache bicapa con dHash 64-bit | Activa | 🟢 | ADR-0016 |
| DD-11 — Telemetría 17 categorías como contrato | Activa | 🟢 | ADR-0017 |
| DD-12 — `UnifiedPokerCalculator` ES el facade real | Activa con incoherencia documental | 🟡 | ADR-0006 (parcialmente) |
| DD-13 — `PostflopContextHolder` thread-safe | Activa | 🟢 | ADR-0007 |
| DD-14 — Caches singleton lazy (`RegionLookup`/`CardCache`) | Activa | 🟢 | ADR-0016 |
| DD-15 — Truncado `MaxHandsInMemory=20` + acumuladores | Activa | 🟢 | ADR-0017 |
| DD-16 — `SetPreflopActionUseCase` scoped | Activa (bugfix) | 🟢 | — |
| DD-17 — `TextBoxLoggerProvider` buffer one-shot | Activa | 🟢 | ADR-0009 |
| DD-18 — `appsettings.json` con credenciales (anomalía) | Resolución pendiente | 🔴 | ADR-0018 (contradicho) |
| DD-19 — `EncrypterHelper` con IV fija (anomalía) | Resolución pendiente | 🔴 | — |
