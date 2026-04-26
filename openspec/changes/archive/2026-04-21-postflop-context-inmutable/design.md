## Context

`PostflopGameContext` nació como un contenedor de estado cross-street con 15 setters públicos para que los consumidores lo fueran rellenando a medida que avanzaba la mano. Esa ergonomía tuvo dos efectos no deseados al escalar:

1. **Duplicación silenciosa**. `FrmMain` y `GameCoordinator` mantienen cada uno un `PostflopGameContext` propio (`_postflopContext = new()` en `FrmMain.cs:168`, `PostflopContext { get; } = new()` en `GameCoordinator.cs:29`). Nada obliga a sincronizarlos. En la práctica, cada flujo muta sólo uno y cualquier lectura que mezcle fuentes (por ejemplo, overlay leyendo `_postflopContext` mientras la decisión usa el del coordinator) devuelve estado divergente.

2. **Atomicidad perdida en transiciones de street**. `UpdateFlopState` y `UpdateTurnState` son wrappers que asignan 3-4 campos secuencialmente. En el game loop multi-hilo (captura, estado, decisión) un lector puede observar el contexto en un estado intermedio: `HeroBetFlop = true` pero `PreviousStreetWasBet` todavía con el valor anterior. No se han detectado bugs atribuibles a esto, pero es un pozo de bugs futuros.

Stakeholders: el desarrollador único (Alberto). El cambio no afecta ninguna decisión ni UI — es un refactor arquitectónico con impacto directo en robustez y base para futuras optimizaciones (por ejemplo, snapshots persistidos por mano en el `HandRecord`).

## Goals / Non-Goals

**Goals:**

- Convertir `PostflopGameContext` en un `record` inmutable con propiedades `init`-only.
- Sustituir las operaciones mutadoras (`Reset`, `UpdateFlopState`, `UpdateTurnState`, `TrackHeroStackForRebuy`) por equivalentes no-mutadores que devuelven un contexto nuevo.
- Introducir `IPostflopContextHolder` scoped como único poseedor del contexto por mano, eliminando la duplicación entre `FrmMain` y `GameCoordinator`.
- Migrar los 44 call-sites existentes sin alterar ninguna decisión del `PostflopDecisionService`.
- Mantener los tests preexistentes verdes y añadir cobertura nueva para la API inmutable (`With*`, `NewHand`, `TrackHeroStack`) y el holder.

**Non-Goals:**

- Rediseñar los campos del contexto. Salir de este change con el mismo conjunto de 15 piezas de estado; cualquier campo nuevo o eliminado pertenece a otro cambio.
- Añadir persistencia del contexto en `HandRecord` o similar. El record inmutable lo habilita, pero no es parte de este scope.
- Cambiar el comportamiento de `PostflopDecisionService` o de cualquier regla de decisión.
- Hacer el contexto thread-safe sin lock (por ejemplo, reemplazar el holder por un `ImmutableInterlocked` con CAS). Este change opta por `lock` dentro del holder — la ganancia CAS no compensa la complejidad para la carga actual.

## Decisions

### 1. `PostflopGameContext` como `sealed record`

**Decisión:** convertir la clase en `public sealed record` con todas las propiedades `{ get; init; }`. Mantener `HeroCheckedAllStreets` e `IsVillainBarreling` como propiedades computadas.

**Alternativas consideradas:**

- *`readonly struct`*: tamaño del payload (~80 bytes con los campos actuales) cruza el umbral en el que los structs se pasan mejor como `in ref` que por valor. Además, los `BoardChangeResult` internos son ya clases/records — meterlos en un struct sería semánticamente torpe. Descartado.
- *Mantener `class` con `{ get; init; }`*: funciona pero pierde igualdad por valor y el operador `with` nativo. Record es ideomático y documenta intención.
- *`record struct`*: copia en cada paso podría ser rápida, pero el uso real exige pasar el contexto por referencia a muchos métodos — un `class-based record` evita defensive copies implícitas.

**Rationale:** record referencial + `init` = inmutabilidad sin fricción, igualdad por valor gratuita, `with` expressions para construir transiciones atómicas.

### 2. Métodos `With*` compuestos vs `with` crudo

**Decisión:** añadir `WithFlopState(...)`, `WithTurnState(...)` y `TrackHeroStack(...)` como métodos de instancia que componen varias asignaciones en una sola operación. Para mutaciones de un solo campo, los call-sites pueden usar `ctx with { Campo = valor }` directamente.

**Alternativas consideradas:**

- *Todo vía `with` crudo en call-sites*: obliga a cada call-site a replicar la lógica derivada (`VillainCheckedMiddleStreet = VillainBetFlop && !villainBet` en turn). Es precisamente lo que provocó bugs antes. Descartado.
- *Builder pattern*: sobra — los cambios son locales y pequeños; un builder mutable en medio de un refactor hacia inmutabilidad es incoherente.

**Rationale:** centralizar las derivaciones en métodos `With*` mantiene los invariantes (`VillainCheckedMiddleStreet` siempre consistente con `VillainBetFlop`/`VillainBetTurn`) y hace que los 44 call-sites lean mejor. `with { }` queda para casos aislados.

### 3. `IPostflopContextHolder` scoped

**Decisión:** introducir el holder como servicio scoped registrado en `Program.cs`. `GameCoordinator` (scoped) y `FrmMain` (resuelto desde el scope async) comparten la misma instancia durante la vida de la sesión. El holder expone `Current`, `Update` (con función transformadora) y `StartNewHand` (equivalente a `Reset` antiguo).

**Alternativas consideradas:**

- *Singleton*: rompería el modelo de "una instancia viva por sesión de juego" si en algún momento se abre una segunda ventana/escopo. Scoped es más correcto.
- *Pasar `PostflopGameContext` como parámetro a cada método*: funcional puro ideal, pero obligaría a propagar el tipo por todo el pipeline del game loop (miles de LOC). Exceso de scope.
- *Mantener la propiedad `PostflopContext` en `IGameCoordinator`*: perpetúa la duplicación. La migración debe ser completa.

**Rationale:** el holder es el mínimo imprescindible para tener un único poseedor sin destruir la arquitectura actual. Thread-safety mediante `lock` interno — simple, suficiente, probado.

### 4. Migración por fases intra-change

**Decisión:** el change se ejecuta en cuatro fases dentro del mismo PR:

1. Añadir el record inmutable + helpers `With*` + `NewHand` + `TrackHeroStack` (sin eliminar aún los setters mutables — coexisten temporalmente para permitir migración gradual).
2. Añadir `IPostflopContextHolder` y registrarlo en DI.
3. Migrar los 44 call-sites al holder + `with` / `With*` expressions.
4. Eliminar los setters mutables, `Reset`, `UpdateFlopState`, `UpdateTurnState`, y la propiedad `PostflopContext` de `IGameCoordinator`. Confirmar build y test suite verde.

**Alternativas consideradas:**

- *Big-bang en un solo commit*: compilación rota intermedia impide iteración. Descartado.
- *Change separado por cada fase*: crea overhead de OpenSpec y PRs pequeños que no aportan valor aislado. Descartado.

**Rationale:** fases lógicas dentro del mismo PR permiten commits intermedios si se quiere, y preservan la compilabilidad continua.

### 5. Thread-safety con `lock`

**Decisión:** `PostflopContextHolder.Update` toma un `lock` interno antes de leer `Current`, aplicar `updater`, y reemplazar la referencia. `Current` lee la referencia con `Volatile.Read` para barrera de memoria sin lock.

**Alternativas consideradas:**

- *`Interlocked.Exchange` en loop*: más rápido para contención alta, pero el update ejecuta una función arbitraria del usuario — meterla en loop CAS tiene pegas (efectos secundarios múltiples si hay retry). Descartado.
- *Sin sincronización*: el game loop actual es mayoritariamente un hilo, pero hay captura async y timers que tocan el contexto. No asumir el modelo de un hilo.

**Rationale:** `lock` es suficientemente rápido para ~5 updates por mano. Correctitud > microoptimización.

## Risks / Trade-offs

- **Riesgo:** el mapeo 1:1 de los 44 call-sites puede introducir bugs sutiles si se olvida actualizar un campo derivado. → **Mitigación:** los métodos `With*` centralizan las derivaciones. El plan de tasks incluye un grep exhaustivo final buscando patrones `PostflopContext\.\w+\s*=` para garantizar que no quede ninguno.
- **Riesgo:** `FrmMain._postflopContext` y `GameCoordinator.PostflopContext` hoy contienen estado posiblemente divergente. Unificarlos podría exponer un comportamiento ligeramente distinto (el holder elegirá una "fuente de verdad"). → **Mitigación:** auditoría en el paso 1 de tasks; verificar manualmente que las mutaciones de `FrmMain` se corresponden con las del coordinator antes de migrar.
- **Trade-off:** `record` + `with` produce una asignación por update. Para ~5 updates por mano es despreciable; si en el futuro el contexto crece a decenas de campos o se actualiza en bucle tight, habría que revisar. Aceptable para el perfil de carga actual.
- **Trade-off:** `IPostflopContextHolder` añade una pequeña indirection a cada lectura (`_holder.Current.X` vs `PostflopContext.X`). El cost es nulo (property access) pero sintácticamente más largo. Aceptable por la garantía de poseedor único.

## Migration Plan

1. **Paso 1 — Record inmutable** (coexistencia temporal): cambiar `class` a `sealed record`, `{ get; set; }` a `{ get; init; }` sólo en los campos más "puros"; dejar los que tienen call-sites con mutación directa aún como `set` public, temporalmente. Añadir `With*`, `NewHand`, `TrackHeroStack`. Commit — todo sigue compilando y los tests pasan porque los setters legacy aún existen.
2. **Paso 2 — Holder**: añadir `IPostflopContextHolder` y `PostflopContextHolder`, registrar scoped en `Program.cs`, tests unitarios. Commit independiente.
3. **Paso 3 — Migración de call-sites**: fichero a fichero (`GameCoordinator.cs`, `FrmMain.cs`), reemplazar mutaciones por `_holder.Update(ctx => ctx with { ... })`. Eliminar `PostflopContext` de `IGameCoordinator`. Commit.
4. **Paso 4 — Endurecer**: pasar el resto de `set` a `init`. Eliminar `Reset`, `UpdateFlopState`, `UpdateTurnState` y la versión mutadora de `TrackHeroStackForRebuy`. Confirmar build y suite verde.

**Rollback:** cada paso es un commit. Revertir restaura el estado previo. El change se puede abandonar tras el paso 1 o 2 sin haber introducido regresiones; los pasos 3 y 4 son indisociables entre sí.

## Open Questions

- ¿El holder debe exponer un evento `ContextChanged` para que el overlay/UI se entere de transiciones? Respuesta provisional: **no**. El overlay hoy lee el contexto polling-style; cualquier trigger-based aviso es otro change.
- ¿Hace falta versionar el contexto (campo `Version`) para debug/log? Respuesta provisional: **no** para este change; si el equipo lo pide para análisis post-hoc, se añade como campo `init` en otro change.
