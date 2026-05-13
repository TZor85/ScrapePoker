# OpenScrape.DecisionMaker — Decisiones de Diseño

> Registro de decisiones arquitecturales detectadas en el motor de decisión. Cada decisión cita su evidencia en código y, cuando aplica, su ADR correspondiente en `_reversa_sdd/adrs/`. Se incluyen también **anomalías que actúan como decisiones de facto** (no documentadas formalmente, pero asentadas en la base de código).

---

## DD-01 — Monte Carlo híbrido (enumeración exacta + MC adaptativo) en lugar de MC universal

**Decisión:** El cálculo de equity selecciona método según `community.Count`:
- River (5 cartas) → enumeración exacta C(45,2)=990 manos.
- Turn (4) → enumeración exacta 45×C(44,2)≈42 K.
- Flop (3) → MC paralelizado 50 K iteraciones.
- Preflop (0) → MC paralelizado 30 K iteraciones.

**Contexto:** Un Monte Carlo universal de 50 K iteraciones tiene varianza ±0.5 % y un coste comparable a una enumeración exacta C(45,2)=990. En river la enumeración es ~50× más rápida y elimina la varianza completamente. En turn, los ~42 K casos siguen siendo enumerables en CPU consumer en pocos ms y producen equity exacta — un beneficio enorme para la matriz integration test (216 casos), donde la varianza ±0.5 % de un MC haría inestable la verificación.

**Alternativas consideradas:**
1. **MC universal con 100 K iteraciones para todos los streets** — descartado: varianza inferida ±0.35 % todavía hace inestable la matriz integration test; coste 2× sin ganancia en flop/preflop.
2. **Enumeración exacta también en flop** — descartado: 3 cartas implican C(45,4)≈148 K hands × C(43,2) opp hands = ~134 M evaluaciones por equity request; inviable en tiempo real.
3. **Aproximación analítica por preflop equity table + adjustments** — descartado para flop/turn: dependencia de board texture demasiado alta para una tabla; el legado mantiene la tabla solo para `community = []`.

**Consecuencias positivas:**
- 🟢 River y turn determinísticos: la matriz integration test (216 casos) verifica con asserts exactos.
- 🟢 Errores de equity acotados a flop/preflop (~±0.5 %), donde el ruido de la decisión absorbe la varianza.
- 🟢 Optimizaciones específicas por método: `BitHandEvaluator.EvaluateHandScore` zero-alloc absorbe los ~42 K evals/turn sin GC pressure.

**Consecuencias negativas:**
- 🟡 Cuatro paths distintos en `MonteCarloSimulator.cs` (`CalculateEquity` switch en `community.Count`) → más superficie de bug. Mitigación: tests parametrizados cubren los 4 caminos.
- 🟡 La varianza del MC en flop/preflop puede generar decisiones inconsistentes entre dos calls idénticos (mismo input, distinto output). Mitigación: 50 K iter es suficiente para que la decisión sea estable salvo en spots ±0.3 % del threshold; ahí la randomización adaptativa (DD-09) absorbe la inconsistencia.

**Evidencia:** `Algorithms/MonteCarloSimulator.cs:50,127-135,142-237,244-362,369-424`. `flowcharts/OpenScrape.DecisionMaker-MonteCarloSimulator.md`. 🟢

**ADR relacionado:** ADR-0010 (Monte Carlo híbrido enumeración exacta).

---

## DD-02 — Inversión completa de dependencias (13 interfaces) sin DI propio del módulo

**Decisión:** Cada servicio expone su contrato vía `Interfaces/I{Servicio}.cs`. La composición (registro DI) **vive enteramente en `OpenScrape.App.Program.cs`** — este módulo NO declara `Services.AddDecisionMaker()` ni equivalente. Las implementaciones se registran como **singletons** desde App.

**Contexto:** El motor de decisión es consumido por:
- `UnifiedPokerCalculator` (facade) en App.
- `GameCoordinator` en App.
- `OpenScrape.App.Tests` (con `InternalsVisibleTo`).

Mantener la composición en App permite que el dueño del lifecycle (la app desktop) controle scopes, mocks de tests, y orden de construcción. El módulo se queda como un "library puro".

**Alternativas consideradas:**
1. **`Services.AddDecisionMaker(IServiceCollection)`** dentro del módulo — descartado: forzaría a App a depender del orden y forma de registro elegidos por DM. Tests no podrían sustituir un servicio sin tocar el módulo.
2. **Servicios estáticos sin DI** — descartado: imposibilita mocks; estado mutable (`OpponentTracker`, `ExploitabilityCalculator`) requeriría singletons globales tipo `Singleton.Instance`.
3. **`InternalsVisibleTo` para todos los consumidores** en lugar de interfaces — descartado: rompe encapsulación y compromete contratos públicos.

**Consecuencias positivas:**
- 🟢 13 puntos de mock para tests (uno por interface).
- 🟢 App decide singleton vs scoped según necesidad (todos singleton actualmente — el motor es stateless o thread-safe).
- 🟢 El módulo `dotnet list reference` solo apunta a `OpenScrape.Domain`; cero dependencias circulares.

**Consecuencias negativas:**
- 🟡 Documentación de DI dispersa: el "qué se registra" vive en App, no aquí. Mitigación: `design.md` lista las 13 interfaces explícitamente.
- 🟡 Tres interfaces exponen tipos `nested` (`IMonteCarloSimulator.EquityResult`, `IOutsCalculator.OutsResult`, `IEquityCalculatorService.FullEquityAnalysis`) — acopla la interfaz a la implementación. Ver DD-09.

**Evidencia:** `Interfaces/` (13 archivos) + `code-analysis.md § OpenScrape.DecisionMaker / Inversión de dependencias`. `OpenScrape.DecisionMaker.csproj` solo referencia `OpenScrape.Domain`. 🟢

**ADR relacionado:** Implícito en ADR-0001 (Clean Architecture cinco capas) — la separación de interfaces corresponde a la regla de inversión.

---

## DD-03 — `PostflopGameContext` como `sealed record` inmutable con helpers `With*`

**Decisión:** El estado cross-street se representa con un `sealed record` inmutable. Cada transición (flop→turn, turn→river, hero stack tracking) retorna una nueva instancia vía `WithFlopState(...)`, `WithTurnState(...)`, `TrackHeroStack(...)`, `CombineBoardChanges(...)`. La instancia previa nunca muta.

**Contexto:** Un solo bug clásico en bots de poker es leak de estado entre manos: una flag (`FloatedFlop`, `TurnCalledWithFlushDanger`, `IsAnyoneAllIn`) que sobrevive una mano y contamina la siguiente. La inmutabilidad + reasignación incondicional al detectar nueva mano (`HandReset()` en `App`) lo hace estructuralmente imposible.

**Alternativas consideradas:**
1. **Clase mutable con método `Reset()`** — descartado: olvido de llamada a `Reset()` (o reset incompleto) reproduce exactamente el bug que se quiere evitar.
2. **`Dictionary<string, object>` flexible** — descartado: pierde tipo, multiplica errores en ortografía y casts.
3. **Inmutabilidad con clases `init-only`** — equivalente al record en este contexto, pero record agrega value equality y `with { }` syntax.

**Consecuencias positivas:**
- 🟢 Imposible mutar el contexto de una mano por accidente.
- 🟢 `with { Field = value }` syntax conciso para evolución del estado.
- 🟢 Value equality útil en tests (`Assert.That(context, Is.EqualTo(expected))`).

**Consecuencias negativas:**
- 🟡 Cada transición de calle aloca un nuevo record (record es heap allocation). Mitigación: 3 transiciones por mano × ~100 manos/h = 300 alloc/h, despreciable.
- 🟡 Auto-rebuy detection (`TrackHeroStack`) requiere mantener un campo `_heroStackPreRebuy` interno; la lógica de "stack < 50 BB → 100 BB súbito" es no obvia, requiere tests cubriendo el caso.

**Evidencia:** `Services/PostflopGameContext.cs:1-171, 94, 106-124, 133-147`. Reseteo incondicional en `App` documentado en `domain.md § 3.5`. 🟢

**ADR relacionado:** ADR-0007 (PostflopContext inmutable holder scoped).

---

## DD-04 — Constantes algorítmicas en `PokerConstants`, no en `StrategyProfile`

**Decisión:** ~25 valores derivados de **teoría del poker** (no de estrategia subjetiva) viven como `public const` o `public static readonly` en `PokerConstants.cs`. NO se exponen como `IOptions<>`. Ejemplos: `MaxOutsPossible=15`, `RuleOf2Multiplier=2.0`, `BackdoorFlushImpliedOuts=1.5`, `OvercardOutsBase=3`, `WetnessDryMax=15`, `BlockedComboUnreliableThreshold=20.0`, `DangerCompletedDrawNoBetCap=45`.

`StrategyProfile` (~150 parámetros) sí es configurable y vive en `Domain/Entities/StrategyProfile.cs`, leído vía `IOptions<StrategyProfile>` desde `appsettings.json`.

**Contexto:** Hay dos clases de "magic numbers" en un motor de poker:
1. **Leyes del juego / convenciones probabilísticas** — el "regla del 2" no es una decisión de estrategia, es un atajo aritmético. `BackdoorFlush ≈ 1.5 outs` es un consenso pokeriano. Cambiarlo no calibra el bot, lo rompe.
2. **Tunables de estrategia** — `FoldBelow`, `ThinValueAbove`, `MultiwayPenalty`, `CbetFrequency` son ajustables según estilo del jugador o stake.

Mezclar ambos en `StrategyProfile` invitaría a "jugar" con valores de la primera categoría sin justificación, lo que produciría regresiones difíciles de detectar.

**Alternativas consideradas:**
1. **Todo en `StrategyProfile`** — descartado: 175 parámetros configurables aumentan el riesgo de configuración corrupta y degradan UX (UI de configuración inviable).
2. **Todo en `PokerConstants`** — descartado: el bot no podría calibrarse para LAG vs TAG, micro vs low stakes, cash vs MTT.
3. **Configuración en dos archivos JSON separados** — descartado: el segundo JSON se confundiría con el primero; mejor mantener constantes en código (más visibles para desarrolladores).

**Consecuencias positivas:**
- 🟢 Calibración del bot solo toca `StrategyProfile`. Las leyes del poker son inviolables sin un commit explícito.
- 🟢 Compilador detecta cualquier intento de modificar una constante (no se puede asignar `const`).
- 🟢 Constantes documentables con XML doc comments (no posible en JSON).

**Consecuencias negativas:**
- 🟡 Si el equipo decide en el futuro hacer experimentos sobre, p.ej., `OvercardOutsBase` (3 vs 4), debe modificar el código y recompilar. Trade-off aceptado: experimentos con constantes algorítmicas son raros y deben ser deliberados.

**Evidencia:** `PokerConstants.cs:1-111` (~25 constantes). `Domain/Entities/StrategyProfile.cs` (~150 propiedades). `appsettings.json` solo declara estrategia, no constantes. 🟢

**ADR relacionado:** ADR-0008 (Thresholds tipados con startup validation) — alinea con la separación: thresholds (configurables) en profile, constantes en código.

---

## DD-05 — `OpponentTracker` thread-safe vía `ConcurrentDictionary`, sin locks externos

**Decisión:** `OpponentTracker` mantiene `ConcurrentDictionary<string, OpponentProfile>` para profiles + `ConcurrentDictionary<int, string>` para seat→alias. Los counters (17 por profile) se incrementan con `Interlocked.Increment` o LINQ `GetOrAdd`. **No hay `lock` externo**.

**Contexto:** El game loop de la app es multithread:
- Captura de pantalla en thread de UI.
- OCR de regiones en threads de pool.
- Decisión postflop puede consultar `GetTypeForPosition` desde varios puntos.
- UI de stats lee profiles para rendering.

Usar `lock(_dict)` en todas las lecturas/escrituras serializaría todo el tracker, anulando el beneficio del paralelismo del game loop.

**Alternativas consideradas:**
1. **`Dictionary<>` + `lock`** — descartado: contention masiva en lecturas de stats UI durante el game loop.
2. **`ImmutableDictionary` con CAS** — descartado: cada escritura crea una nueva colección; coste alto para 17 counters por player.
3. **Particionado por playerId** — descartado: complejidad innecesaria; `ConcurrentDictionary` ya particiona internamente.
4. **Eventual consistency con queue de updates** — descartado: lecturas verían datos stale; UI mostraría stats con lag observable.

**Consecuencias positivas:**
- 🟢 100 writers + 100 readers concurrentes 5s sin race conditions (test de stress).
- 🟢 Lecturas O(1) sin bloqueo, escrituras O(1) amortizado.
- 🟢 No requiere disposal explícito de locks.

**Consecuencias negativas:**
- 🟡 Inicialización lazy con `GetOrAdd(playerId, new OpponentProfile(...))` puede crear instancias descartadas si dos threads inicializan simultáneamente. Mitigación: factory de `OpponentProfile` es barata.
- 🟡 Increments no atómicos cross-counter (cambiar 3 counters relacionados ≠ una transacción). Mitigación: cada counter es independiente; la consistencia "global" del profile no se exige (es estimación estadística).
- 🔴 No persiste cross-sesión (Q-FSM-02). Decisión deferida.

**Evidencia:** `Services/OpponentTracker.cs:16, 191-208, 214-221, 234-248`. Tests: 21 casos en `OpenScrape.App.Tests/OpponentTracker*`. 🟢

**ADR relacionado:** ADR-0011 (OpponentTracker Laplace + reliability).

---

## DD-06 — `ThresholdsRegistry` con fail-fast en arranque (no fallback en runtime)

**Decisión:** `ThresholdsRegistry` valida en su constructor que existen las 30+ combinaciones `(BoardPosition × HandSituation)` requeridas. Si falta cualquiera, lanza excepción con mensaje descriptivo (`"Missing threshold: Flop_OpenRaise"`) y la app **aborta el arranque**. NO se aplican defaults silenciosos en runtime.

**Contexto:** El `StrategyProfile` se carga de `appsettings.json` y `appsettings.Development.json`. Un typo en una clave (`"Flop_OpenRise"` en lugar de `"Flop_OpenRaise"`) degradaría el motor a usar un fallback "razonable" para esa situación. El usuario operaría durante semanas con un bug silencioso que solo aparece en spots específicos.

Detectar el problema al arrancar — antes de ver una sola mano — es estructuralmente más seguro: el bot no abre, el log lo dice claro, el usuario corrige.

**Alternativas consideradas:**
1. **Default fallback (`StreetThresholds.Default`) si la clave falta** — descartado: el usuario nunca sabe que está usando defaults; bug silencioso.
2. **Warning log + default** — descartado: el log se pierde si la app inicia headless; el bug pasa desapercibido.
3. **Lazy validation por clave (validar en cada `Get(...)`)** — descartado: la decisión de equity puede ocurrir 30 minutos después del arranque; demasiado tarde.

**Consecuencias positivas:**
- 🟢 Configuración corrupta detectada en < 1s después de iniciar la app.
- 🟢 Mensaje de error apunta a la clave faltante exacta.
- 🟢 Lookup en runtime es O(1) sin validación adicional.

**Consecuencias negativas:**
- 🟡 No hay forma de "modo degradado" si una situación esotérica tiene threshold mal configurado. Trade-off aceptado: el dueño del bot prefiere abortar a operar con configuración rota.

**Evidencia:** `Services/ThresholdsRegistry.cs:23-32`. Test `OpenScrape.App.Tests/ThresholdsRegistry*`. 🟢

**ADR relacionado:** ADR-0008 (Thresholds tipados con startup validation).

---

## DD-07 — `BitHandEvaluator` zero-alloc con `stackalloc Span<int>` y `readonly struct HandScore`

**Decisión:** El evaluador de manos usa exclusivamente memoria en stack (`stackalloc Span<int> rankCount = stackalloc int[15]`, etc.) y devuelve un `readonly struct HandScore` con `long CompositeScore` para comparación O(1) sin GC pressure.

**Contexto:** Una equity de turn enumera ~42 K hands del villano. Cada hand requiere evaluar la mejor combinación de 5 cartas de 7 (hero 2 + board 4 + community 1 hipotético). Una alocación de heap por evaluación dispararía GC presión que degradaría 10× el throughput.

**Alternativas consideradas:**
1. **Brute-force `C(7,5)=21` con `int[]` allocados en heap** — implementación legacy en `HandEvaluator.cs`; ~50× más lento por GC pressure.
2. **`HandScore` como `class`** — perdería el beneficio del struct (allocation por evaluación).
3. **`(int rank, int k1, int k2, int k3, int k4, int k5)` tuple** — equivalente, pero comparación requiere comparar 6 fields; con `long CompositeScore` es 1 instrucción CPU.

**Consecuencias positivas:**
- 🟢 Benchmark `EvaluateHandScore(7 cartas)` < 1 µs y 0 allocations en heap.
- 🟢 Turn equity completa (~42 K evals) en milisegundos en CPU consumer.
- 🟢 `HandScore.CompositeScore.CompareTo(other)` es 1 op CPU (long compare).

**Consecuencias negativas:**
- 🟡 Lógica bitwise en `EvaluateHandScore` (fases 1-6 con bitmasks) es densa y poco mantenible para devs sin background en bit-tricks. Mitigación: 15 tests cubren cada `HandRank`; comentarios densos en código.
- 🟡 Wheel (A-2-3-4-5) requiere case especial en `FindStraightHigh` con `WheelMask`. Bug latente si se cambia la lógica sin entender el caso.
- 🟡 Coexiste con `HandEvaluator.cs` legacy. Ver DD-12.

**Evidencia:** `Algorithms/BitHandEvaluator.cs:24, 253-408, 415-429`. `Algorithms/IHandEvaluator.cs` (struct `HandScore`). 15 tests en `OpenScrape.App.Tests/HandEvaluator*` y `HandScore*`. 🟢

**ADR relacionado:** Implícito en ADR-0010 (Monte Carlo híbrido) — el zero-alloc del evaluator es prerequisito del exact enumeration en turn.

---

## DD-08 — `Marten.IDocumentStore` consumido SOLO por `BankrollTrackerService`, no en otros servicios

**Decisión:** De los 15 servicios, solo `BankrollTrackerService` recibe `IDocumentStore` en su constructor. Los otros 14 (incluido `PostflopDecisionService`, `MonteCarloSimulator`, `OpponentTracker`, `EquityCalculatorService`) son **stateless o memory-only** y no tocan BD.

**Contexto:** Permitir que cualquier servicio del motor consulte Marten libremente convertiría el módulo en una capa "todo-en-uno" sin separación de concerns. Además:
- El motor de decisión debe ejecutarse en milisegundos (decisión postflop). Una query Marten añade 10-50 ms de latencia inaceptable.
- Tests unitarios del motor no deben requerir Postgres.
- Backtesting (`StrategyBacktester`) recibe sesiones ya cargadas como parámetro, no las consulta él mismo.

`BankrollTrackerService` es la excepción legítima porque su input es histórico (sesiones cerradas en Marten) y no participa del game loop de tiempo real.

**Alternativas consideradas:**
1. **`OpponentTracker` consume Marten para persistencia cross-sesión** (Q-FSM-02) — deferido. Si se aprueba, se añadirá `IDocumentStore` con cuidado para no contaminar lecturas en runtime.
2. **`StrategyBacktester` consulta sesiones desde Marten** — descartado: hace el servicio dependiente de DB y no testeable sin Postgres. Hoy recibe `List<GameSession>` como parámetro.
3. **`EquityCalculatorService` cachea resultados en BD** — descartado: lookup MC es ms; cache añadiría complejidad sin beneficio.

**Consecuencias positivas:**
- 🟢 13 servicios testeable con NUnit puro (sin docker postgres).
- 🟢 Latencia de decisión postflop < 200 ms total (incluyendo MC en flop).
- 🟢 Separation of concerns: motor de juego (in-memory) vs reporting/bankroll (BD).

**Consecuencias negativas:**
- 🟡 `BankrollTrackerService` requiere infraestructura para tests integrados.
- 🟡 N+1 queries en `BankrollTrackerService.CalculateRiskOfRuin` (anomalía documentada en `tasks.md` T-70).
- 🟡 Exceptions silenciadas en `BankrollTrackerService` (anomalía documentada en `tasks.md` T-71).

**Evidencia:** `Services/BankrollTrackerService.cs:177-213`. Restantes 14 servicios sin referencia a Marten. `dotnet list reference` solo declara Marten 8.24.0 a nivel de csproj (consumido en un solo punto). 🟢

**ADR relacionado:** ADR-0003 (Marten Postgres document DB).

---

## DD-09 — Interfaces que exponen tipos `nested` (anomalía aceptada como decisión de facto)

**Decisión:** Tres interfaces (`IMonteCarloSimulator`, `IOutsCalculator`, `IEquityCalculatorService`) referencian tipos definidos como `nested` en sus implementaciones (`MonteCarloSimulator.EquityResult`, `OutsCalculator.OutsResult`, `EquityCalculatorService.FullEquityAnalysis`).

**Contexto:** Esta NO es una decisión deliberada — es una asentación del legado. Funcionalmente compila, pero acopla la interfaz a la implementación: cualquier mock de `IMonteCarloSimulator` debe construir un `MonteCarloSimulator.EquityResult`, lo que requiere referenciar la implementación.

Idiomáticamente C# debería ubicar estos tipos en `DTOs/` o `ValueObjects/`. La razón histórica probable: los tipos nacieron como tipos auxiliares "internos" del cálculo y se promocionaron a salida del método sin refactor de namespace.

**Alternativas consideradas (a aplicar en refactor futuro):**
1. **Mover `EquityResult`, `OutsResult`, `FullEquityAnalysis` a `DTOs/`** — recomendado por `tasks.md` T-83. Permite mocks limpios y desacopla la interfaz.
2. **Aceptar el acoplamiento** (estado actual) — pragmático: tests unitarios usan implementaciones reales, no mocks.
3. **Reescribir interfaces con genéricos** (`IEquityResult`) — descartado: over-engineering para 3 tipos.

**Consecuencias positivas (del estado actual):**
- 🟢 Cero refactor pendiente; el código compila y los tests pasan.

**Consecuencias negativas:**
- 🟡 Mocks de las 3 interfaces requieren conocer implementación → tests con `Moq` o `NSubstitute` son más verbosos.
- 🟡 Si en el futuro se quiere reemplazar la implementación de MC por una versión GPU, la interfaz no protege la API.

**Evidencia:** `Interfaces/IMonteCarloSimulator.cs:1-16` (referencia a `EquityResult` nested). `Interfaces/IOutsCalculator.cs:1-12` (`OutsResult`). `Interfaces/IEquityCalculatorService.cs:1-18` (`FullEquityAnalysis`). 🟡

**ADR relacionado:** ninguno (anomalía no documentada formalmente; capturada aquí para visibilidad).

---

## DD-10 — `PostflopDecisionInput` como `record` con 6 required + 36 propiedades con defaults

**Decisión:** El DTO de entrada al motor postflop tiene **42 campos**: 6 obligatorios sin default (`Equity`, `BoardPosition`, `Situation`, `BoardTexture`, `Position`, `Pot`) y 36 con defaults razonables (`HeroIsAggressor=false`, `VillainBetSizeBB=0`, `KickerStrength=KickerStrength.None`, `IsAnyoneAllIn=false`, etc.).

**Contexto:** El motor postflop evalúa 10+ paths en `DetermineAction`. Cada path consulta un subconjunto distinto de información (cross-street flags, opponent profile, kicker strength, etc.). Un DTO con 42 parámetros de constructor sería inviable para los consumidores; un DTO con 42 setters mutables introduciría riesgo de mutación.

Records con `init`-only properties ofrecen el balance: inmutabilidad + sintaxis fluida (`new PostflopDecisionInput { Equity = 65.0, ... }`).

**Alternativas consideradas:**
1. **42 parámetros de constructor** — descartado: imposible de mantener.
2. **DTO con setters mutables** — descartado: rompe inmutabilidad.
3. **Record con `with { }` desde un default** — equivalente al estado actual; los defaults vienen de la propia declaración.
4. **Builder pattern** — descartado: over-engineering en C# moderno donde records ya cubren el caso.

**Consecuencias positivas:**
- 🟢 Consumidores construyen un input con sintaxis declarativa: solo especifican lo que se aparta del default.
- 🟢 Tests pueden iterar matrices con `with { Equity = newEq }` → 1 línea por variación.
- 🟢 Compilador exige los 6 required, evita typos.

**Consecuencias negativas:**
- 🟡 Si un default es incorrecto (p.ej. `HeroIsAggressor=false` en un caller que olvidó setearlo), el motor decide como caller no agresivo silenciosamente. Mitigación: la matriz integration test (216 casos) cubre todas las combinaciones críticas.
- 🟡 Difícil saber qué propiedades son "siempre setear" vs "default ok" sin leer XML doc.

**Evidencia:** `DTOs/PostflopDecisionInput.cs:1-66`. 🟢

**ADR relacionado:** ninguno explícito; consistente con la regla del proyecto de records inmutables (`CLAUDE.md § Code Style`).

---

## DD-11 — `Random.Shared.NextDouble()` directo en producción (sin `IRandomProvider`)

**Decisión:** `PostflopDecisionService` usa `Random.Shared.NextDouble()` directamente en 11 puntos para randomización adaptativa, c-bet mixing, check-raise mixing, etc.

**Contexto:** La randomización del bot es **anti-exploit deliberado** — un humano que observa al bot durante 1000 manos puede detectar patrones determinísticos (siempre c-bet en flop dry, siempre check turn medium); la mezcla anti-exploit lo dificulta.

`Random.Shared` es thread-safe en .NET 6+ y produce calidad estadística adecuada. Un `IRandomProvider` inyectable haría tests reproducibles, pero introduciría dependencia adicional en cada uno de los 11 callsites.

**Alternativas consideradas:**
1. **`IRandomProvider` inyectable** — recomendado por `tasks.md` T-56. Beneficio: tests reproducibles. Coste: refactor de 11 callsites + decisión de scope (singleton? scoped?).
2. **`new Random()` por llamada** — descartado: no thread-safe + degrada calidad estadística (si se llama dos veces en el mismo tick).
3. **`Random.Shared`** (estado actual) — pragmático: simple, thread-safe, calidad ok.

**Consecuencias positivas:**
- 🟢 Cero overhead de DI para randomización.
- 🟢 Anti-exploit funcional desde día 1.

**Consecuencias negativas:**
- 🔴 Tests no pueden controlar el rng → tests sobre randomización (T-56) son **estadísticos** (1000 ejecuciones, ratio ±5%), no asserts directos. Lentos y flakey.
- 🟡 No hay forma de "fijar la semilla" para reproducir una sesión específica para debugging.

**Evidencia:** `Services/PostflopDecisionService.cs:473, 539, 597, 608, 634, 842, 861, 871, 968, 989, 1231`. 🟡

**ADR relacionado:** ninguno explícito; capturado aquí como decisión consciente de pragmatismo.

---

## DD-12 — Coexistencia de `HandEvaluator` legacy y `BitHandEvaluator` (anomalía pendiente)

**Decisión:** El módulo mantiene `HandEvaluator.cs` (206 LOC, brute-force `C(7,5)=21`) **junto a** `BitHandEvaluator.cs` (515 LOC, zero-alloc). El legacy `EvaluateHandScore` delega a `new BitHandEvaluator()` por llamada, pero el resto de su superficie (`EvaluateBestHand`) sigue siendo brute-force.

**Contexto:** No hay justificación documentada para mantener el legacy. Posibles razones (todas inferidas):
- Migración incompleta: `BitHandEvaluator` se introdujo después y solo se reemplazó el path crítico (`EvaluateHandScore` usado en MC ~42 K veces/turn).
- Tests legacy referencian `HandEvaluator.EvaluateBestHand` directamente y eliminar el legacy rompería un corpus que nadie ha querido tocar.
- Cautela: el legacy es "conocido bueno" en outputs no críticos.

**Alternativas (a aplicar en refactor futuro):**
1. **Eliminar `HandEvaluator.cs`** y redirigir consumers a `BitHandEvaluator` — recomendado por `tasks.md` T-29 si los tests legacy se pueden migrar.
2. **Mantener** (estado actual) — pragmático.
3. **Mover `HandEvaluator` a `OpenScrape.App.Tests` como código de test legacy** — descartado: rompe la regla de no tocar código legacy en módulos productivos.

**Consecuencias positivas (del estado actual):**
- 🟢 Riesgo cero de regresión por eliminación.

**Consecuencias negativas:**
- 🟡 ~206 LOC muerto a la vista de cualquier dev nuevo.
- 🟡 `EvaluateHandScore` legacy aloca un nuevo `BitHandEvaluator()` por llamada (no es zero-alloc para ese call path).

**Evidencia:** `Algorithms/HandEvaluator.cs:1-206`. `code-analysis.md § Anomalías / coexistencia legacy + bit`. 🟡

**ADR relacionado:** ninguno; capturado aquí para visibilidad.

---

## DD-13 — `AutoCalibrationService.PreviewAndApply` con `OldValue` hardcoded (BUG conocido)

**Decisión (de facto, NO deliberada):** `AutoCalibrationService.PreviewAndApply` calcula los `OldValue` que muestra al usuario con literales hardcoded (`45`, `40`) en lugar de leer del `StrategyProfile` activo.

**Contexto:** El servicio propone calibraciones del tipo "tu FoldBelow está en 45, sugiero ajustar a 50" basadas en `TopLeaks` del `ExploitabilityCalculator`. Si el `StrategyProfile` real tiene `FoldBelow=50` (no `45`), la sugerencia es incorrecta porque parte de un baseline equivocado.

Esta NO es una decisión deliberada — es un **bug latente** documentado en `code-analysis.md § Anomalías`. Se mantiene en este registro como "decisión de facto" porque la implementación actual descansa sobre estos literales, y arreglarla requiere acceso al profile activo en el constructor (refactor).

**Alternativas (a aplicar):**
1. **Inyectar `IOptionsMonitor<StrategyProfile>`** y leer `OldValue` en cada llamada → `tasks.md` T-75.
2. **Pasar el profile como parámetro de `PreviewAndApply`** — equivalente.
3. **Mantener hardcoded** (estado actual) — bug que confunde al usuario.

**Consecuencias negativas:**
- 🔴 La UI muestra "tu valor actual: 45 → sugerido: 48" cuando el usuario tiene 50. Erosión de confianza.
- 🔴 Si el usuario aplica la sugerencia, `StrategyProfile.FoldBelow = 48` (no 53 como esperaría tras "incrementar 3").

**Evidencia:** `Services/AutoCalibrationService.cs:174-208`. 🔴 Documentado en `code-analysis.md § Anomalías`.

**ADR relacionado:** ninguno; este es un bug, no una decisión.

---

## DD-14 — `ExploitabilityCalculator` con `BigBlind=1.0` hardcoded (anomalía aceptada)

**Decisión (de facto):** `ExploitabilityCalculator` produce reports en BB/100 asumiendo `BigBlind=1.0`. Si el usuario opera en NL5 (BigBlind = 0.05) o NL25 (BigBlind = 0.25), los números reportados están escalados incorrectamente.

**Contexto:** El servicio consume historiales de decisiones y calcula leaks (overfold, underfold, overbluff, etc.) en términos de "BB perdidos por 100 manos". El factor de conversión `chips → BB` requiere conocer el BigBlind del usuario, que vive en `StrategyProfile`.

Posibles razones de la anomalía:
- Implementación inicial asumía un solo stake.
- Se pospuso la parametrización mientras se validaba el algoritmo de leak detection.

**Alternativas (a aplicar):**
1. **Inyectar `IOptions<StrategyProfile>` y leer `BigBlind`** → `tasks.md` T-74.
2. **Pasar `bigBlind` como parámetro de `RecordDecision`** — equivalente, pero acopla cada caller.
3. **Mantener hardcoded** (estado actual) — válido solo si el usuario opera en NL1 (BigBlind = 1.0).

**Consecuencias negativas:**
- 🟡 Reportes en BB/100 incorrectos por factor `1/realBB`. P.ej., en NL10 con BigBlind=0.10, los reportes muestran números 10× lo real.
- 🟡 La pestaña Estadísticas (`StrategyAnalyzerService`) usa los mismos números → cascade de números incorrectos.

**Evidencia:** `Services/ExploitabilityCalculator.cs:93, 322-348`. 🟡 Documentado en `code-analysis.md § Anomalías`.

**ADR relacionado:** ninguno.

---

## DD-15 — `obj/` versionado con artefactos de migraciones de target framework

**Decisión (de facto):** El repo versiona `src/OpenScrape.DecisionMaker/obj/Debug/net8.0/`, `obj/Debug/net9.0/`, `obj/Debug/net10.0/` (y mismo en Release). El csproj solo declara `<TargetFramework>net10.0</TargetFramework>`.

**Contexto:** La migración de target framework de net8 → net9 → net10 dejó artefactos de build viejos. `obj/` no está en `.gitignore` (o lo estaba antes y se versionaron por error). 

**Alternativas:**
1. **Limpiar `obj/Debug/net8.0/` y `obj/Debug/net9.0/`** y añadir `obj/` al `.gitignore` → `tasks.md` T-82.
2. **Mantener** (estado actual) — solo penaliza tamaño de repo, no funcionamiento.

**Consecuencias negativas:**
- 🟡 Repo más grande de lo necesario.
- 🟡 Confusión para devs nuevos al ver carpetas `net8.0/` cuando el csproj declara solo `net10.0`.

**Evidencia:** `legacy-mapping.md § obj/`. 🟡

**ADR relacionado:** ninguno.

---

## Resumen de ADRs ligados a este módulo

| ADR | Título | DDs ligadas |
|-----|--------|-------------|
| ADR-0001 | Clean Architecture cinco capas | DD-02 |
| ADR-0006 | Pipeline unificado equity-decisión | (cross-module) |
| ADR-0007 | PostflopContext inmutable holder scoped | DD-03 |
| ADR-0008 | Thresholds tipados con startup validation | DD-04, DD-06 |
| ADR-0010 | Monte Carlo híbrido enumeración exacta | DD-01, DD-07 |
| ADR-0011 | OpponentTracker Laplace + reliability | DD-05 |
| ADR-0015 | Decision matrix 216 casos integration test | DD-01, DD-10 |

Anomalías documentadas como DDs:
- **DD-09** — interfaces con tipos `nested`
- **DD-11** — `Random.Shared` directo
- **DD-12** — `HandEvaluator` legacy
- **DD-13** — `AutoCalibration` `OldValue` hardcoded (🔴 BUG)
- **DD-14** — `ExploitabilityCalculator` `BigBlind=1.0` hardcoded
- **DD-15** — `obj/` versionado

Las anomalías 🔴 (DD-13) y 🟡 críticas (DD-14) están priorizadas para corrección en `tasks.md`.
