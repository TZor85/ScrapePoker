# OpenScrape.Features — Decisiones de Diseño

> Registro de decisiones arquitecturales detectadas en la capa de aplicación. Cada decisión cita su evidencia en código y, cuando aplica, su ADR correspondiente en `_reversa_sdd/adrs/` o decisión cruzada del Detective.

---

## DD-01 — Vertical Slice Architecture (en lugar de Clean por carpetas)

**Decisión:** Cada caso de uso vive en un archivo propio dentro de la jerarquía `Feature/Action/UseCase.cs`. No hay capas horizontales `Application/Handlers/`, `Application/Validators/`, `Application/Mappers/`. Cada slice es autocontenido: la entrada (request DTO), la salida (Result/string/lista), y la lógica viven juntas.

**Contexto:** El proyecto tiene 5 features con use cases muy distintos (selección estocástica preflop, CRUD simple, lectura de catálogo). Una organización horizontal generaría carpetas cruzadas (`Handlers/GetTable.cs` + `Handlers/GetActionScenario.cs`) que comparten poco más que el patrón. Con Vertical Slice, todo lo necesario para entender o modificar un caso de uso está en una sola carpeta.

**Alternativas consideradas:**
1. **MediatR + Pipeline Behaviors** (validation/logging/caching como middleware) — descartado: `CLAUDE.md` dice explícitamente "scoped use cases (no MediatR pipeline)". Para 5 use cases, el coste de pipeline > beneficio.
2. **Capa Application clásica** (`Application/Queries/`, `Application/Commands/`) — descartado: jerarquía Query/Command es teatro CQRS sin event sourcing real ni separación de modelos read/write.
3. **CQRS con buses** — descartado: over-engineering masivo para una desktop app.

**Consecuencias positivas:**
- Añadir un nuevo use case requiere exactamente 2 archivos: `Feature/Action/UseCase.cs` + entrada en `Services.cs`.
- Un nuevo dev entiende un slice en <2 minutos sin saltar entre 4-5 archivos.
- Onboarding lineal y trazabilidad inmediata.

**Consecuencias negativas:**
- Si dos slices comparten validación o mapping, la duplicación es real. Mitigación: extraer a `OpenScrape.Domain.Mappers` (ya hecho con `TableDTOMapper`/`CardDTOMapper`).
- No hay extensibilidad transversal vía pipeline (logging cross-cutting, caching, etc.). Si se quisiera, hay que tocar cada use case.

**Evidencia:** Estructura de carpetas `src/OpenScrape.Features/{ActionScenario,Table,Cards,RegionsTableMap,GameRound}/<Action>/`; ausencia de carpetas horizontales. 🟢

**ADR relacionado:** Implícito en ADR-0001 (Clean Architecture cinco capas) — la "Application Layer" del proyecto es Vertical Slice.

---

## DD-02 — Aggregator records como punto de inyección único

**Decisión:** Cada feature expone un `record` agregador (`ActionScenarioUseCases`, `TableUseCases`, `CardUseCases`, `RegionTableMapUseCases`, `GameRoundUseCases`) que toma todos los use cases del slice como parámetros del constructor primario. Los consumidores inyectan el aggregator, no los use cases individuales.

**Contexto:** `FrmMain` y otros consumidores en `App` necesitan ≥3-5 use cases distintos para sus tabs/handlers. Inyectar 12+ use cases planos en un constructor sería frágil. El aggregator agrupa por feature, dejando el constructor del consumidor con N parámetros donde N = número de features que usa.

**Alternativas consideradas:**
1. **Un único aggregator monolítico** (`UseCases` con todos los 9 use cases) — descartado: cualquier cambio en un slice fuerza recompilación de todos los consumidores.
2. **Service locator** (`IServiceProvider` + `GetRequiredService<T>`) — descartado: oculta dependencias, anti-pattern para DI explícito.
3. **No aggregator: inyectar use cases planos** — descartado: explosión de parámetros en `FrmMain` (que ya tiene muchos).

**Consecuencias positivas:**
- 🟢 Constructor de `FrmMain` recibe 4-5 aggregators (`TableUseCases`, `CardUseCases`, …) en lugar de 9 use cases planos.
- El aggregator es record → inmutable, value-equal, conciso (1 línea por feature).
- Añadir un use case dentro de un slice solo modifica el aggregator y `Services.cs`; consumidores intactos.

**Consecuencias negativas:**
- 🟡 Si un consumidor solo necesita 1 de 2 use cases del aggregator, igual recibe ambos (referencia a un objeto inactivo). Coste despreciable (referencia inicializada por DI).
- 🟡 Aggregators con use cases vacíos (`TableUseCases.GetAllTables`, `RegionTableMapUseCases.GetAllRegionTableMap`) propagan dependencias muertas.

**Evidencia:** `ActionScenario/ActionScenarioUseCases.cs:5`, `Table/TableUseCases.cs:6`, `Cards/CardUseCases.cs:5`, `RegionsTableMap/RegionTableMapUseCases.cs:6`, `GameRound/GameRoundUseCases.cs:3`. 🟢

---

## DD-03 — `Ardalis.Result<T>` para errores controlados (parcial: 4 de 5 use cases)

**Decisión:** Los use cases de lectura/escritura sobre Marten retornan `Result<T>` o `Result` (4 de los 5 funcionales). `GetActionScenario` y `GetRecentGameRounds` **no** lo usan: el primero retorna `string` y re-lanza `Exception`; el segundo retorna `List<GameSession>` directo sin try/catch.

**Contexto:** El patrón `Result<T>` permite distinguir éxito, ausencia (NotFound), y fallo (CriticalError) sin lanzar excepciones para flujos esperables. La ausencia de tabla en `GetTable` o de regiones en `UpdateRegionTableMap` no es un error excepcional — es un caso normal del dominio.

**Alternativas consideradas:**
1. **Excepciones para todos los caminos no felices** — descartado: las excepciones son costosas y oscurecen el contrato del método.
2. **`Optional<T>` / `Maybe<T>` custom** — descartado: `Ardalis.Result` ya provee la API y soporta error structurado con mensaje.
3. **Tuple `(bool success, T? value, string? error)`** — descartado: ergonomía pobre, no tiene NotFound semántico.

**Consecuencias positivas:**
- 🟢 Caller (`SetPreflopActionUseCase`, `FrmMain`) puede hacer `if (result.IsSuccess) { ... } else if (result.Status == NotFound) { ... }` sin try/catch.
- Errores envueltos preservan información (`Status`, `Errors`, `ValidationErrors`).
- Patrón uniforme verificado en `GetTable`, `GetAllCards`, `UpdateRegionTableMap` (3 archivos).

**Consecuencias negativas:**
- 🟡 **Inconsistencia**: 2 use cases (`GetActionScenario`, `GetRecentGameRounds`) escapan al patrón.
  - `GetActionScenario.ExecuteAsync`: retorna `Task<string>` y re-lanza `Exception` envuelta — pierde el `Result.NotFound` para "tabla no encontrada" y obliga al caller a try/catch.
  - `GetRecentGameRounds.Execute`: retorna `Task<List<GameSession>>` directa, sin try/catch — propaga cualquier excepción al caller.
- Migrar a `Result<T>` en estos dos casos requiere refactor cross-module (ver DD relacionado y `tasks.md` T-27).

**Evidencia:** `GetTable.cs:17,27,28,32`, `GetAllCards.cs:17,24,26,30`, `UpdateRegionTableMap.cs:15,21,51,55` (uso). `GetActionScenario.cs:16,22,46-48` (no uso). `GetRecentGameRounds.cs:15-22` (no uso). 🟢 (parcial) / 🟡 (inconsistencia)

---

## DD-04 — Sesión Marten por operación, no long-lived

**Decisión:** Cada use case abre y dispone su propia sesión Marten dentro del método (`using var session = _documentStore.QuerySession()` o `LightweightSession()`). No se inyecta `IDocumentSession`/`IQuerySession` directamente; siempre se inyecta `IDocumentStore`.

**Contexto:** WinForms no tiene "request scope" natural. El game loop opera por eventos. Una sesión Marten viva durante toda una sesión de juego (1+ hora) bloquearía conexiones y acumularía objetos en el `IdentityMap` de Marten. CLAUDE.md prescribe "Database sessions use `await using` per operation (no long-lived sessions)".

**Alternativas consideradas:**
1. **Sesión por scope DI** — descartado: ningún scope natural en desktop app; introduciría ceremonia (`CreateScope()` manual).
2. **Sesión singleton compartida** — descartado: las sesiones de Marten **no son thread-safe**; el game loop usa hilos secundarios.
3. **Sesión por hand** (game session = sesión Marten) — descartado: vida útil ≥30 segundos, retiene conexión.

**Consecuencias positivas:**
- 🟢 Patrón uniforme en 5 use cases activos (verificado en `legacy-mapping.md → Llamadores de IDocumentStore`).
- Cada operación libera la conexión inmediatamente al pool gestionado por Npgsql.
- Errores aislados: una operación fallida no contamina la siguiente.
- Simplifica testing: cada test instancia su propio `IDocumentStore` en memoria sin coordinación.

**Consecuencias negativas:**
- Operaciones que requirieran transaccionalidad multi-paso deberían coordinar manualmente. **No hay caso transaccional complejo** en el módulo actual.
- Pequeño overhead por apertura/cierre de sesión (~ms, despreciable comparado con OCR/Monte Carlo en el caller).

**Evidencia:** `GetTable.cs:21`, `GetAllCards.cs:21`, `UpdateRegionTableMap.cs:19`, `GetRecentGameRounds.cs:17`. 🟢

**ADR relacionado:** ADR-0003 (decisión Marten + patrón sesiones cortas).

---

## DD-05 — Inconsistencia `using` síncrono vs `await using`

**Decisión (no decidida, drift acumulado):** 4 archivos usan `using var session = ...` (síncrono); solo `GetRecentGameRounds` usa `await using var session = ...`. CLAUDE.md prescribe `await using`.

**Contexto:** El `await using` asíncrono es preferible porque las sesiones Marten implementan `IAsyncDisposable` y su `DisposeAsync` puede liberar recursos sin bloquear el thread. El `using` síncrono llama `Dispose` que internamente puede sincronizar work asíncrono (sync-over-async).

**Alternativas consideradas (post-hoc):**
1. **Mantener `using` síncrono uniforme** — peor: contra recomendación oficial Marten + CLAUDE.md.
2. **Migrar todos a `await using`** — recomendado, refactor de bajo riesgo.

**Consecuencias positivas (de migrar):**
- Consistencia con `CLAUDE.md` y mejor disposición async (libera al thread durante el cierre).

**Consecuencias negativas:**
- 🟡 **Estado actual:** 4 archivos sub-óptimos. No bloqueante pero inconsistente.

**Evidencia:** `GetTable.cs:21` (sync), `GetAllCards.cs:21` (sync), `UpdateRegionTableMap.cs:19` (sync), `GetRecentGameRounds.cs:17` (async). 🟡

**Recomendación:** abordar en T-30. Refactor mecánico, sin cambio funcional.

---

## DD-06 — Sampling ponderado entero `[1, 100]` sobre `Hand.Percentage`

**Decisión:** `GetRandomAction` muestrea con `Random.Next(1, 101)` (entero en `[1, 100]`) y compara contra acumulado `int` de `Hand.Percentage`. La precisión es ±1 unidad porcentual, no fraccional.

**Contexto:** Las tablas estratégicas se editan a mano en JSON. Trabajar con `int` evita problemas de redondeo flotante (`0.6 + 0.4 ≠ 1.0` en doble precisión). La estrategia GTO se aproxima en pasos del 1 % — granularidad suficiente para un humano.

**Alternativas consideradas:**
1. **`double`/`decimal` con tolerancia** — descartado: complica la validación `Sum==100` y oculta errores de redondeo en el seeder.
2. **Fracciones racionales** — descartado: complejidad gratuita.
3. **Pesos arbitrarios sin restricción de suma** — descartado: pierde la lectura inmediata "X % de las veces hace Y".

**Consecuencias positivas:**
- 🟢 Validación trivial: `actions.Sum(a => a.Percentage) != 100` lanza `ArgumentException` y detecta seeds corruptas en runtime.
- Lectura humana directa: `{"raise": 60, "call": 40}` es auto-explicativo.
- Sampling determinístico bajo seed fijo (testeable).

**Consecuencias negativas:**
- Granularidad del 1 % puede ser insuficiente si una situación requiere 0.5 % (ej: bluff catching marginal). En la práctica, irrelevante para preflop.
- 🟡 La validación se ejecuta en **cada llamada** (no en carga del seed). Costo despreciable, pero podría detectarse antes (al boot).

**Evidencia:** `GetActionScenario.cs:60-77`, `Domain/ValueObjects/Hand.cs:9` (validación `Percentage` en `[0, 100]` como `int`). 🟢

---

## DD-07 — Fold como default seguro

**Decisión:** Cuando `GetActionScenario` no encuentra manos coincidentes en la tabla, retorna `"Fold"` en vez de lanzar excepción o devolver `null`/`string.Empty`.

**Contexto:** En poker, la acción más conservadora ante incertidumbre es foldear (perder solamente la posición ya invertida). Una excepción detendría el game loop; un `null`/empty obligaría al caller a manejar el caso. Devolver `"Fold"` permite que el bot avance sin interrupciones.

**Alternativas consideradas:**
1. **Excepción** — descartado: detiene el game loop ante seed incompleta.
2. **Null/Optional** — descartado: obliga al caller a defensa adicional, comportamiento ambiguo.
3. **Acción configurable como fallback** (`Check` en BB, `Fold` en otras posiciones) — descartado: complejidad sin valor demostrado; `Fold` es seguro globalmente excepto en BB unraised, caso raro.

**Consecuencias positivas:**
- 🟢 Game loop continúa aún con tablas seed incompletas — el bot no se rompe.
- Comportamiento conservador alineado con el principio "primero no perder".
- En testing, `"Fold"` es fácil de detectar como señal de "no había estrategia para este caso".

**Consecuencias negativas:**
- 🟡 Si la seed está mal y debería haber una entrada, `Fold` enmascara el bug — el bot pierde valor sin alerta. Mitigación: instrumentar el path con un log de warning (`"sin entrada para situación X"`) — actualmente **no presente**.
- 🔴 En BB con limpers, `"Fold"` es matemáticamente incorrecto (no se puede foldear si nadie ha apostado más que la BB). El caller (`SetPreflopActionUseCase`) debe traducir `"Fold"` → `"Check"` en ese caso. **Comportamiento no verificado en este módulo**.

**Evidencia:** `GetActionScenario.cs:38, 42`. 🟢 (decisión) / 🟡 (efecto enmascarante).

---

## DD-08 — `UpdateRegionTableMap` non-destructive con flags semánticos

**Decisión:** Al actualizar una región existente, se **eliminan** todas las propiedades configurables del request (`PosX/Y`, `Width/Height`, `Color`, `Umbral`, `InactiveUmbral`) pero se **preservan** los flags semánticos (`IsHash`, `IsColor`, `IsBoard`, `IsOnlyNumber`) tomándolos del original (`regionToRemove?.IsHash`, etc.) — incluso si el request los aporta no-null.

**Contexto:** Los flags semánticos definen *qué tipo de región es* (carta vs número vs hash de imagen). Una región del overlay configurada como "carta" no debería volverse "número" porque el editor de regiones envió un request mal poblado. Los flags se calibran una vez al definir la región y se preservan ad eternum.

**Alternativas consideradas:**
1. **Merge: aplicar flags si el request los aporta no-null** — descartado en el código actual; sería más ergonómico pero permite errores.
2. **Inmutable: rechazar update que cambie flags** — descartado: requeriría endpoint separado.
3. **Status quo (preservar siempre del original)** — implementado.

**Consecuencias positivas:**
- 🟢 Imposible cambiar accidentalmente la semántica de una región editando dimensiones o color.
- Editor de regiones (FrmEditarRegiones en App) puede enviar el request completo sin preocuparse de los flags.

**Consecuencias negativas:**
- 🔴 **Lacuna de diseño:** los campos `IsHash`, `IsColor`, `IsBoard`, `IsOnlyNumber` del request **nunca se usan**. Si la intención original era permitirlos al crear regiones nuevas, el código actual los ignora. Decisión humana pendiente:
  - **A.** Eliminarlos del DTO (el caller no debe poder enviarlos).
  - **B.** Aplicarlos cuando son no-null (merge).
  - **C.** Documentar y mantener.
- Si se necesitase migrar una región a otro tipo, no hay endpoint que lo permita (requeriría delete + create, no soportado).

**Evidencia:** `UpdateRegionTableMap.cs:36-44` (preservación), `UpdateRegionTableMapRequest.cs` (DTO con los flags inertes). 🟢 (decisión) / 🔴 (lacuna).

---

## DD-09 — `GameSituation.GetDescription()` como puente enum→Table.Id

**Decisión:** El identificador de la tabla estratégica en Marten es la `Description` del enum `GameSituation`, no el nombre del enum ni un GUID. `GetActionScenario` invoca `tableUseCases.GetTable.ExecuteAsync(situation.GetDescription())`.

**Contexto:** `GameSituation` tiene `[Description("ColdFourBet")]`, `[Description("OpenRaise")]`, etc. El seeder de `App/Data/*.json` persiste `Table.Id == "OpenRaise"` (esa cadena exacta). Usar el `Description` permite renombrar el enum sin romper datos persistidos (siempre que el atributo se mantenga).

**Alternativas consideradas:**
1. **`enum.ToString()`** — descartado: cambia si renombran el enum.
2. **Mapping explícito** (`switch` o `Dictionary<GameSituation, string>`) — descartado: duplica el `[Description]`.
3. **Marten con índice por enum entero** (`Table.Situation: int`) — descartado: pierde legibilidad del JSON.

**Consecuencias positivas:**
- 🟢 Refactor del enum (rename) sigue funcionando si se preserva `[Description]`.
- Datos persistidos legibles en SQL (`SELECT data->>'Id' FROM mt_doc_table` retorna `"OpenRaise"`).

**Consecuencias negativas:**
- 🔴 **Lacuna crítica detectada:** el archivo `App/Data/Cold4Bet.json` (nombre del archivo) no coincide con `[Description("ColdFourBet")]` del enum. Si el seeder persiste `Table.Id` desde el nombre del archivo, `GetActionScenario` consulta `"ColdFourBet"` y obtiene `NotFound` → la tabla no se encuentra → excepción re-lanzada. Validar urgentemente.
- Los `[Description]` se vuelven contrato implícito que ningún test estático garantiza. Recomendado: test que valide `Enum.GetValues<GameSituation>().All(s => s.GetDescription() != null)` y crece-test que liste cada situación documentada.

**Evidencia:** `GetActionScenario.cs:20`, `Domain/Enums/Positions.cs:69-90` (definición `GameSituation` con `[Description]`), `Domain/Enums/EnumExtensions.cs` (extension method `GetDescription`). 🟢 (decisión) / 🔴 (consistencia con seeder).

**Decisión cruzada:** ver `tasks.md` T-34, `questions.md` Q-FEA-04.

---

## DD-10 — `new Random()` ad-hoc en hot path (en lugar de `Random.Shared`)

**Decisión (legacy, probablemente accidental):** `GetRandomAction` instancia `new Random()` en cada llamada (`GetActionScenario.cs:64`).

**Contexto:** En .NET 6+ existe `Random.Shared` que devuelve un singleton thread-safe. `new Random()` con default seed (sin parámetro) usa el reloj del sistema, en versiones antiguas con resolución coarse podía producir el mismo seed dentro del mismo tick si se invocaba múltiples veces en serie. En .NET 6+ la resolución es por tick más fino, pero igual hay un coste de construcción y se desperdicia entropía.

**Alternativas consideradas:**
1. **`Random.Shared.Next(...)`** — recomendado, sin construcción ni colisión de seed. Cambio mecánico.
2. **Inyectar `IRandom`/`Random` por DI** — descartado: complica testing sin beneficio claro.
3. **Status quo (`new Random()`)** — costo bajo pero peor que la alternativa.

**Consecuencias positivas (de migrar):**
- Sin alocación, sin riesgo histórico de colisiones de seed.
- Trivial: 2 caracteres de cambio (`new Random()` → `Random.Shared`).

**Consecuencias negativas (estado actual):**
- 🟡 Costo despreciable individualmente pero ejecutado N veces por sesión.

**Evidencia:** `GetActionScenario.cs:64-65`. 🟡

**Recomendación:** abordar en T-28.

---

## DD-11 — `throw new Exception(...)` con stack trace perdido

**Decisión (anti-pattern legacy):** `GetActionScenario.ExecuteAsync` envuelve cualquier excepción interna en una nueva `new Exception($"Error executing {situation.GetDescription()} scenario: {ex.Message}")`. El stack trace original se pierde porque el constructor no recibe `ex` como `innerException`.

**Contexto:** El patrón correcto sería `throw;` (re-lanza preservando stack) o `throw new Exception(msg, ex)` (preserva como inner). El código actual hace lo más débil de las opciones.

**Alternativas consideradas:**
1. **`throw;`** — recomendado: preserva stack original sin nueva excepción.
2. **`throw new Exception(msg, ex)`** — preserva inner, añade contexto.
3. **`Result<string>.CriticalError(...)`** — alineamiento con DD-03, requiere refactor de consumidores.
4. **Status quo** — peor opción.

**Consecuencias positivas (de migrar):**
- Diagnóstico de errores en runtime sustancialmente mejor.

**Consecuencias negativas (estado actual):**
- 🟡 En logs solo aparece `"Error executing OpenRaise scenario: Connection refused"`, sin saber si vino de Marten, de la red, o de un mapeo. Diagnóstico ciego.
- 🟡 `Exception` plana es un type incorrecto — viola FxCop CA1031 / CA2200.

**Evidencia:** `GetActionScenario.cs:46-48`. 🟡

**Recomendación:** abordar en T-29 simultáneamente con T-27 (Result uniforme).

---

## DD-12 — Placeholders registrados en DI

**Decisión (cuestionable):** `GetAllTables` y `GetAllRegionTableMap` están **registrados en `Services.AddUseCases()`** y son parte de los aggregators (`TableUseCases`, `RegionTableMapUseCases`), pese a no tener método operativo.

**Contexto:** Probablemente scaffolding inicial que quedó. El registro DI no tiene costo runtime relevante (Scoped instancia bajo demanda) pero contamina el grafo y permite que un consumidor invoque `tableUseCases.GetAllTables.???` y se confunda al no encontrar método.

**Alternativas consideradas:**
1. **Eliminar (recomendado)** — limpia DI, reduce arity de aggregators, evita confusión.
2. **Implementar** — solo si la pestaña Tablas o algún caso de uso lo justifica.
3. **Mantener** — peor, deuda permanente.

**Consecuencias positivas (de eliminar):**
- 🟢 Aggregators más limpios: `TableUseCases(GetTable)`, `RegionTableMapUseCases(UpdateRegionTableMap)`.
- DI sin tipos no-operativos.

**Consecuencias negativas (estado actual):**
- 🔴 Riesgo de uso accidental: alguien podría intentar `tableUseCases.GetAllTables` y no obtener error en compile-time.
- 🟡 Tests de integración (TT-14) deben chequear que ambos resuelven, aún siendo no-funcionales.

**Evidencia:** `Services.cs:23, 29-32`, `GetAllTables.cs`, `GetAllRegionTableMap.cs`. 🔴

**Recomendación:** abordar en T-12, T-19, T-26 (decisión humana sobre los 3 placeholders).

---

## DD-13 — Inconsistencia namespace `Card` (singular) vs carpeta `Cards/` (plural)

**Decisión (legacy, deuda):** La carpeta del slice se llama `Cards/` (plural, sigue convención de "Tables", "Regions", etc.) pero el namespace declarado es `OpenScrape.Features.Card` (singular).

**Contexto:** Probable typo durante el scaffolding. No tiene impacto funcional pero confunde al importar (`using OpenScrape.Features.Card;` cuando uno espera `Cards`).

**Alternativas consideradas:**
1. **Renombrar namespace a `OpenScrape.Features.Cards`** — recomendado, alineamiento con resto del módulo.
2. **Renombrar carpeta a `Card/`** — descartado: rompe convención del resto.
3. **Mantener** — peor opción.

**Consecuencias positivas (de migrar):**
- Coherencia con resto del módulo.
- Removed friction al importar.

**Consecuencias negativas (estado actual):**
- 🟡 `using OpenScrape.Features.Cards.GetAll` (lo que un dev escribiría intuitivamente) **falla**; hay que usar `OpenScrape.Features.Card.GetAll`.

**Evidencia:** `Cards/CardUseCases.cs:3`, `Cards/GetAll/GetAllCards.cs:6`, `Cards/GetFlop/GetFlopCards.cs:8`. 🟡

**Recomendación:** abordar en T-16. Coordinar con consumidores en `App` que usan el namespace.

---

## DD-14 — `AddScoped` uniforme para todos los use cases y aggregators

**Decisión:** Todos los registrations en `Services.AddUseCases()` son `AddScoped<T>`. Sin Singletons ni Transients.

**Contexto:** Scoped es el lifetime correcto para use cases que dependen de `IDocumentStore` (singleton de Marten) y abren sesiones cortas. Singleton sería peligroso si el use case mantuviera estado mutable; Transient instanciaría innecesariamente cada vez. CLAUDE.md prescribe scoped explícitamente.

**Alternativas consideradas:**
1. **Singleton** — descartado: si el use case acumulara estado entre llamadas (cache, contadores), serían leaks. Aunque los use cases actuales son stateless, Scoped es más defensivo.
2. **Transient** — descartado: sin razón para crear nueva instancia por resolución; Scoped reutiliza dentro del scope.
3. **Mixed** (use cases stateless = Singleton, use cases con state = Scoped) — descartado: regla mental adicional sin beneficio.

**Consecuencias positivas:**
- 🟢 Un único patrón en todo el módulo, fácil de auditar.
- Compatible con la lección histórica del proyecto: `SetPreflopActionUseCase` se movió de Singleton a Scoped al detectar que dependía de aggregators Scoped (memoria del proyecto).

**Consecuencias negativas:**
- Microoverhead de instanciación al primer uso por scope. Despreciable.

**Evidencia:** `Services.cs:20-35`. 🟢

**ADR relacionado:** Decisión cruzada con bugfix histórico documentado en memoria del proyecto: "SetPreflopActionUseCase Singleton→Scoped (dependía de scoped ActionScenarioUseCases)".

---

## DD-15 — `GetAction*UseCase` wrappers viviendo fuera del módulo Features

**Decisión (deuda arquitectónica):** Los 9 wrappers `GetActionOpenRaiseUseCase`, `GetActionThreeBetUseCase`, etc. viven en `OpenScrape.App.Aplication.UseCases.Actions/` (módulo App) y son instanciados con `new` desde `SetPreflopActionUseCase` en lugar de inyectarse vía DI.

**Contexto:** El consumidor (`SetPreflopActionUseCase`) construye `new GetActionOpenRaiseUseCase(...)` en lugar de recibirlo como parámetro de constructor. Esto rompe el principio DI y hace los wrappers no-mockeables.

**Alternativas consideradas:**
1. **Mover los 9 wrappers a `OpenScrape.Features.ActionScenario.Get/`** y registrarlos en DI — alineamiento estricto con Vertical Slice. Refactor mayor (9 archivos + Services.cs + SetPreflopActionUseCase). 🟡
2. **Registrar los wrappers en DI sin moverlos** — refactor menor: Services.cs en App registra los 9 + SetPreflopActionUseCase los recibe como params. 🟡
3. **Status quo** — funciona pero deuda.

**Consecuencias positivas (de migrar opción 1):**
- 🟢 Vertical Slice estricto: todo lo relacionado con "selección de acción" en un solo módulo.
- 🟢 Wrappers mockeables → tests unitarios de `SetPreflopActionUseCase` sin Marten real.

**Consecuencias negativas:**
- Refactor con superficie cross-module.
- 🟡 La memoria del proyecto registra que `SetPreflopActionUseCase` ya tuvo una corrección Singleton→Scoped por dependencia con aggregator scoped — los wrappers `new` adentro siguen siendo "Singleton-by-construction" desde el punto de vista del DI.

**Evidencia:** `legacy-mapping.md → Llamadores externos → ActionScenarioUseCases → 9× wrappers`. 🟡

**Recomendación:** abordar en T-35. Decisión humana — coordinar con análisis Fase 2 del módulo App.

---

## Resumen de decisiones por fortaleza/debilidad

| ID | Decisión | Fortaleza | Riesgo / lacuna |
|----|----------|-----------|-----------------|
| DD-01 | Vertical Slice | 🟢 onboarding lineal, fácil de extender | sin pipeline transversal |
| DD-02 | Aggregator records | 🟢 reduce arity de constructores en App | 🟡 placeholders propagan deps muertas |
| DD-03 | `Result<T>` parcial | 🟢 4/5 use cases consistentes | 🟡 `GetActionScenario` y `GetRecentGameRounds` escapan |
| DD-04 | Sesión por operación | 🟢 patrón uniforme y testeable | — |
| DD-05 | `using` vs `await using` | — | 🟡 4 archivos sub-óptimos |
| DD-06 | Sampling entero `[1, 100]` | 🟢 validación trivial, lectura humana | 🟡 validación en runtime no en boot |
| DD-07 | Fold como default seguro | 🟢 game loop continúa con seed incompleta | 🟡 enmascara bugs sin alerta; 🔴 BB unraised incorrecto |
| DD-08 | Update non-destructive con flags | 🟢 imposible cambiar semántica accidentalmente | 🔴 flags del request inertes (lacuna DTO) |
| DD-09 | `GetDescription()` como Table.Id | 🟢 refactor enum-safe, JSON legible | 🔴 mismatch `Cold4Bet.json` ↔ `ColdFourBet` |
| DD-10 | `new Random()` ad-hoc | — | 🟡 desperdicia entropía, refactor trivial pendiente |
| DD-11 | `throw new Exception(...)` | — | 🟡 stack trace perdido, anti-pattern |
| DD-12 | Placeholders en DI | — | 🔴 deuda confusa; T-26 humana |
| DD-13 | namespace `Card` (singular) | — | 🟡 inconsistencia con carpeta `Cards/` |
| DD-14 | `AddScoped` uniforme | 🟢 patrón único, alineado con CLAUDE.md | — |
| DD-15 | Wrappers fuera del módulo | — | 🟡 deuda arquitectónica, `new` rompe DI |

**Patrón de cierre:** las decisiones funcionales son sólidas (DD-01, DD-02, DD-04, DD-06, DD-08, DD-09, DD-14). Las debilidades son consistencia/pulido (DD-03, DD-05, DD-10, DD-11, DD-12, DD-13) y lacunas con datos externos (DD-08 flags, DD-09 Cold4Bet). Ninguna decisión de fondo necesita reverso; todas las debilidades son refactors mecánicos o decisiones humanas pendientes.
