# OpenScrape.Features — Preguntas Abiertas

> Lacunas detectadas en la capa de aplicación que requieren validación humana antes de implementar/migrar.
> Modo de respuesta: `file` (responder editando este archivo, secciones marcadas con `📝 Respuesta:`).
> Cada pregunta tiene un ID estable (`Q-FEA-NN`) para referencia desde `tasks.md`, `decisions.md`, `edge-cases.md`.

---

## Q-FEA-01 — ¿El filtro `BetSize` se re-habilita, se elimina o se deja en deuda?

**Contexto:**
- `GetActionScenario.cs:31` tiene la línea `&& (request.BetSize == null || w.BetSize == request.BetSize)` **comentada**.
- `ActionScenarioRequest.BetSize` (DTO) sigue declarado y poblable por el caller.
- Si las seeds JSON contemplan `BetSize` por fila para distinguir variantes (ej: 3-bet vs over-3-bet con sizing distinto), la lógica actual colapsa esas filas en una sola → la primera gana.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Re-habilitar el filtro** | Permite distinguir variantes por sizing, granularidad mayor. | Requiere validar que las seeds tienen `BetSize` poblado correctamente; si no, todas las consultas con `BetSize!=null` retornan `"Fold"`. |
| **B. Eliminar `BetSize` del DTO + línea comentada** | DTO más limpio, sin campo inerte. | Si en el futuro se quisiera distinguir por sizing, refactor mayor. |
| **C. Status quo (comentado, DTO mantiene el campo)** | Sin cambios. | Deuda permanente confusa para el siguiente lector. |

**Preguntas concretas:**
1. ¿Las seeds (`OpenRaise.json`, `ThreeBet.json`, etc.) usan `BetSize` para algo?
2. ¿La estrategia preflop actualmente requiere distinguir acciones por sizing del raise?
3. ¿El `BetSize` que envía el caller (`SetPreflopActionUseCase`) es preciso (en BB) o aproximado?

**Bloqueo:** T-25 en `tasks.md`. **Decisiones afectadas:** DD-03 (consistencia DTO/lógica). **Casos extremos relacionados:** EC-05.

🔴 **Respuesta:**

> Opcion A

---

## Q-FEA-02 — ¿Qué hacer con los 3 placeholders dead code (`GetAllTables`, `GetFlopCards`, `GetAllRegionTableMap`)?

**Contexto:**
- `GetAllTables` (`Table/GetAll/GetAllTables.cs`): tiene constructor `(IDocumentStore)` pero ningún método público. Está registrado en DI y es parte de `TableUseCases`.
- `GetFlopCards` (`Cards/GetFlop/GetFlopCards.cs`): solo declara un `record GetFlopCardsResponse()` vacío y un comentario con un request muerto. **No** está registrado en DI.
- `GetAllRegionTableMap` (`RegionsTableMap/GetAll/GetAllRegionTableMap.cs`): constructor + el método `Execute` completamente comentado. Registrado en DI y parte de `RegionTableMapUseCases`.

**Implicaciones por cada placeholder:**

| Placeholder | Opción A (implementar) | Opción B (eliminar) | Opción C (mantener) |
|-------------|------------------------|---------------------|---------------------|
| `GetAllTables` | Útil para una pestaña Tablas que liste todas las situaciones disponibles. ~20 LOC. | Pestaña Tablas tendría que reescribirse o usar otro método. | DI sigue contaminado, riesgo de uso accidental. |
| `GetFlopCards` | Difícil ver el caso de uso (ya hay `GetAllCards`). | Recomendado: eliminar archivo entero. | Sin valor. |
| `GetAllRegionTableMap` | Útil para mostrar todas las categorías de regiones (calibración OCR multi-mesa). ~10 LOC. | El editor de regiones tendría que listar `Categories` por otro path. | DI sigue contaminado. |

**Preguntas concretas:**
1. ¿La pestaña Tablas en la UI actualmente consume `GetAllTables` o usa otro mecanismo?
2. ¿Hay algún caso de uso real para `GetFlopCards` (filtrar las 52 cartas por flop)?
3. ¿La calibración OCR multi-mesa requiere listar `RegionTableMap.Category` como índice?
4. ¿Aceptas la recomendación del Reversa (implementar `GetAllRegionTableMap`, evaluar `GetAllTables`, eliminar `GetFlopCards`)?

**Bloqueo:** T-26, T-12, T-15, T-19, T-20 en `tasks.md`. **Decisiones afectadas:** DD-12. **Casos extremos relacionados:** —.

🔴 **Respuesta:**

> _(escribe aquí — preferentemente una decisión por placeholder)_

---

## Q-FEA-03 — ¿Uniformizar el patrón `Result<T>` en los 2 use cases que escapan?

**Contexto:**
- `GetActionScenario.ExecuteAsync` retorna `Task<string>` y re-lanza `Exception` envuelta. Los demás use cases retornan `Task<Result<T>>`.
- `GetRecentGameRounds.Execute` retorna `Task<List<GameSession>>` directo, sin try/catch.
- Migrar a `Result<T>` afecta:
  - **`GetActionScenario`**: `SetPreflopActionUseCase` + 9 wrappers `GetAction*UseCase` (consumen el `string` retornado).
  - **`GetRecentGameRounds`**: pestaña Historial en `FrmMain` + cualquier query del backtester si lo usa.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Uniformizar a `Result<T>` en ambos** | Patrón único en todo el módulo, errores controlados sin try/catch en consumidores. Coherencia con DD-03. | Refactor cross-module: 10+ archivos en `App.Aplication.UseCases.Actions` + tabs en `FrmMain`. |
| **B. Mantener heterogéneo (status quo)** | Cero cambios. | Cada consumidor usa un patrón distinto, complica mantenimiento. |
| **C. Uniformizar solo `GetActionScenario`** (es el más invocado) | Beneficio principal con menor superficie. | `GetRecentGameRounds` queda como excepción documentada. |

**Preguntas concretas:**
1. ¿El refactor cross-module justifica el beneficio de coherencia, o el costo es prohibitivo en este momento?
2. Si se uniformiza `GetActionScenario` → `Task<Result<string>>`: el caller `SetPreflopActionUseCase` debe traducir `Status.NotFound` a `"Fold"` o re-lanzar. ¿Qué prefieres?
3. ¿Quieres aprovechar el refactor para introducir `ILogger<GetActionScenario>` y registrar el spot fallido (señal de calibración)?

**Bloqueo:** T-27, T-29 en `tasks.md`. **Decisiones afectadas:** DD-03, DD-11. **Casos extremos relacionados:** EC-01, EC-02.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-FEA-04 — ¿`Cold4Bet.json` ↔ `[Description("ColdFourBet")]`: cuál es el canónico?

**Contexto:**
- El archivo seed se llama `src/OpenScrape.App/Data/Cold4Bet.json`.
- `GameSituation.Cold4Bet` (`Domain/Enums/Positions.cs:78`) tiene `[Description("ColdFourBet")]`.
- `GetActionScenario` consulta Marten con `situation.GetDescription()` → `"ColdFourBet"`.
- Si el seeder en `App` (pendiente análisis Fase 2) persiste `Table.Id` desde el nombre del archivo (`"Cold4Bet"`), `GetTable` devuelve `NotFound` y `GetActionScenario` lanza excepción **cada vez** que el bot encuentra esa situación.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Renombrar archivo a `ColdFourBet.json`** | Alineación con el `[Description]`. Minimal touch al enum. | Touch al filesystem, ajustar referencia en JsonSeeder (1 línea). |
| **B. Cambiar `[Description]` a `"Cold4Bet"`** | Minimal touch al filesystem. | Inconsistente con la convención del resto: `OpenRaise`, `ThreeBet`, `FourBet` (todos en text/long form). |
| **C. Ajustar el seeder para mapear `nombre archivo → [Description]` del enum** | Funciona sin tocar archivos ni enum. | Lógica de mapeo manual y frágil. Cualquier nuevo archivo requiere actualizar el mapeo. |

**Preguntas concretas:**
1. ¿Confirmas que el seeder usa el nombre del archivo como `Table.Id`? (Pendiente análisis del módulo App.)
2. ¿Qué convención prefieres adoptar globalmente: short form (`Cold4Bet`, `OpenRaise`) o long form (`ColdFourBet`, `OpenRaise`)?
3. ¿Hay otros mismatches potenciales entre `Data/*.json` y `[Description]` del enum? Ejemplos a verificar:
   - `RaiseOverLimpers.json` ↔ `[Description("RaiseOverLimpers")]` 🟢 ok
   - `VsThreeBetAndCall.json` ↔ `[Description("VsThreeBetAndCall")]` 🟢 ok (asumido)
   - `RaiseVsSbLimp.json` ↔ ¿qué `[Description]` lo invoca? (no encontrado en `legacy-mapping.md`)

**Bloqueo:** T-34, TT-18 en `tasks.md`. **Decisiones afectadas:** DD-09. **Casos extremos relacionados:** EC-01, EC-12.

🔴 **Respuesta:**

> _(escribe aquí — opción + verificación de los demás mismatches)_

---

## Q-FEA-05 — ¿Mover los 9 wrappers `GetAction*UseCase` desde `App.Aplication.UseCases.Actions/` a este módulo?

**Contexto:**
- `SetPreflopActionUseCase` (en `OpenScrape.App.Aplication`) instancia con `new` 9 wrappers: `GetActionOpenRaiseUseCase`, `GetActionThreeBetUseCase`, `GetActionVsThreeBetUseCase`, `GetActionFourBetUseCase`, `GetActionVsFourBetUseCase`, `GetActionSqueezeUseCase`, `GetActionVsSqueezeUseCase`, `GetActionRaiseVsSBLimpUseCase`, `GetActionColdFourBetUseCase`. Cada uno encapsula una invocación a `ActionScenarioUseCases.GetActionScenario.ExecuteAsync(situation, request)`.
- Estos wrappers no están en DI; son `new`-instanciados directamente.
- Romper este patrón implica: 9 archivos a mover, registración en DI, refactor del constructor de `SetPreflopActionUseCase` (de 1 dependencia a ~10), y de los tests asociados.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Mover a `OpenScrape.Features.ActionScenario.Get/`** + DI | Vertical Slice estricto. Wrappers mockeables. Tests sin Marten real. | 9 archivos movidos, `Services.cs` extendido, `SetPreflopActionUseCase` con muchos params (puede mitigarse con un aggregator). |
| **B. Registrar en DI sin mover** (quedan en `App.Aplication`) | Refactor menor: solo `App.Program.cs` añade los registros + `SetPreflopActionUseCase` los recibe. | Wrappers viven fuera del slice — vertical slice impuro pero menos churn. |
| **C. Status quo (`new` en SetPreflopActionUseCase)** | Cero cambios. | Imposible mockear, deuda DI permanente. |

**Preguntas concretas:**
1. ¿La política del proyecto es Vertical Slice estricto (wrappers en Features) o pragmatic (wrappers donde están + DI)?
2. ¿Tests actuales de `SetPreflopActionUseCase` cubren los 9 wrappers o son "humo" cubriendo solo el agregador?
3. Si se mueve, ¿quieres un sub-aggregator `PreflopActionUseCases` que agrupe los 9 wrappers para evitar 10 params en `SetPreflopActionUseCase`?

**Bloqueo:** T-35 en `tasks.md`. **Decisiones afectadas:** DD-15. **Cross-module:** afecta `OpenScrape.App` (Fase 2 pendiente).

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-FEA-06 — ¿Origen y propósito de `Regiones3.json` y `RegionToTest.json`?

**Contexto:**
- `src/OpenScrape.App/Data/Regiones.json` parece la seed canónica de `RegionTableMap` (calibración por defecto del overlay OCR).
- Existen dos archivos adicionales con nombres ambiguos:
  - `Regiones3.json` — ¿segunda mesa? ¿variante experimental? ¿calibración para otra resolución?
  - `RegionToTest.json` — sugiere fixture de testing, pero el seeder podría cargarlo igual.
- Si el seeder los carga todos, `RegionTableMap.Category` puede tener entradas inesperadas.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Documentar cada archivo y mantener** | Conservar configuraciones útiles para distintas mesas/resoluciones. | Riesgo de que el seeder sobrescriba la canónica si los nombres colisionan. |
| **B. Eliminar `RegionToTest.json` y `Regiones3.json`** si son legacy | Repo más limpio. | Si alguno tiene info no replicable (calibración manual), pérdida real. |
| **C. Mover a carpeta `Data/test-fixtures/`** | Separación clara producción vs test. | Cambio de path en seeder + tests asociados. |

**Preguntas concretas:**
1. ¿Cuál es el propósito de cada uno? (preguntar al autor o consultar git log: `git log -p -- src/OpenScrape.App/Data/Regiones3.json`)
2. ¿Algún archivo describe una mesa distinta (PokerStars vs GG vs PartyPoker)?
3. ¿El seeder los carga todos o solo `Regiones.json`?

**Bloqueo:** TM-02 en `tasks.md`. **Casos extremos relacionados:** —. **Cross-module:** afecta `OpenScrape.App` (Fase 2).

🔴 **Respuesta:**

> _(escribe aquí — un párrafo por archivo es suficiente)_

---

## Q-FEA-07 — ¿Los flags `IsHash/IsColor/IsBoard/IsOnlyNumber` del request en `UpdateRegionTableMapRequest` son inertes a propósito?

**Contexto:**
- El record `UpdateRegionTableMapRequest` declara los 4 flags como `bool?`.
- `UpdateRegionTableMap.cs:36-44` siempre los toma del **original** (`regionToRemove?.IsX`), nunca del request.
- Resultado: el caller puede setearlos pero **nunca** se aplican.
- ¿Era la intención preservarlos siempre (DD-08) o era un bug nunca corregido?

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Eliminar los flags del DTO** | DTO honesto. Caller no puede engañarse. | Si el editor de regiones quería ofrecer "cambiar tipo de región", endpoint adicional. |
| **B. Aplicar como merge** (`request.X ?? original.X`) | Caller puede actualizar flags si lo necesita. | Riesgo: edit accidental cambia semántica de región (ej: "carta" → "número"). |
| **C. Status quo (preservar siempre del original)** + comentar el motivo en el DTO | Comportamiento estable + claridad para el siguiente lector. | Deuda DTO mantiene los flags inertes. |

**Preguntas concretas:**
1. ¿La UI de calibración (FrmEditarRegiones) ofrece cambiar el tipo de región (carta vs número vs hash) post-creación?
2. Si no, ¿prefieres eliminar los flags del DTO (Opción A) o documentar como inertes (Opción C)?
3. ¿Hay un endpoint separado para crear regiones nuevas (vs actualizar) que sí use los flags?

**Bloqueo:** parte de T-26 / T-17. **Decisiones afectadas:** DD-08. **Casos extremos relacionados:** EC-06.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-FEA-08 — Pulido de coherencia: `using` síncrono vs `await using`, `new Random()` vs `Random.Shared`, `throw new Exception(...)` vs `throw;`, namespace `Card` vs `Cards`

**Contexto:**
Cuatro inconsistencias menores acumuladas detectadas en `decisions.md`:
- DD-05: 4 use cases usan `using var session = ...` (sync); solo `GetRecentGameRounds` usa `await using`.
- DD-10: `GetActionScenario` usa `new Random()` ad-hoc en hot path.
- DD-11: `GetActionScenario` usa `throw new Exception($"...{ex.Message}")` (pierde stack trace).
- DD-13: namespace `OpenScrape.Features.Card` (singular) en carpeta `Cards/` (plural).

**Implicaciones agregadas:**

| Refactor | LOC tocados | Riesgo | Beneficio |
|----------|-------------|--------|-----------|
| `using` → `await using` | 4 archivos × 1 línea | 🟢 muy bajo | Coherencia con CLAUDE.md, mejor disposición async |
| `new Random()` → `Random.Shared` | 1 línea | 🟢 muy bajo | Sin alocación, sin riesgo histórico de seed-coarse |
| `throw new Exception(...)` → `throw;` o `throw new Exception(msg, ex)` | 1 línea | 🟢 bajo (acoplado con T-27) | Stack trace preservado, diagnóstico real |
| Renombrar namespace `Card` → `Cards` | 2 archivos del módulo + N consumidores en App | 🟡 medio (cross-module) | Coherencia |

**Preguntas concretas:**
1. ¿Aceptas resolver los 3 primeros (sync→async, Random.Shared, throw) en una sola tarea de pulido (T-28 + T-29 + T-30)?
2. ¿El renombrado de namespace `Card → Cards` se prioriza ahora o se deja para refactor futuro?
3. ¿Prefieres `throw;` (preserva stack puro) o `throw new Exception(msg, ex)` (añade contexto al inner)?

**Bloqueo:** T-28, T-29, T-30, T-16 en `tasks.md`. **Decisiones afectadas:** DD-05, DD-10, DD-11, DD-13.

🔴 **Respuesta:**

> _(escribe aquí — puede ser una sola línea: "todo sí" + preferencia sobre throw)_

---

## Q-FEA-09 — ¿Validar suma de `Hand.Percentage == 100` al cargar la seed o por llamada?

**Contexto:**
- `GetRandomAction` valida `actions.Sum(Percentage) == 100` en cada invocación; si falla, `ArgumentException`.
- Si el JSON tiene una entrada mal sumada, **se detecta en la primera vez que el bot encuentra ese spot**, no al boot.
- Costo en CPU es despreciable, pero el bug puede pasar desapercibido durante días si el spot es raro.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Validar al cargar la seed** (en seeder o validador en `App`) | Falla rápida en boot. Imposible deploy con seed corrupta. | Requiere lógica de validación en el seeder + tests. |
| **B. Validar al cargar `Table` en Marten** (en post-deserialization hook) | Falla cuando se accede a la tabla por primera vez. | Marten no tiene hook trivial para esto; requiere wrapper o validador externo. |
| **C. Status quo (validar por invocación)** | Cero cambio. | Bug latente posible. |

**Preguntas concretas:**
1. ¿El seeder se ejecuta solo al boot, o las seeds se reescriben durante runtime?
2. ¿Tienes un test que valide la integridad de **todos** los `Data/*.json` en CI?
3. Si no, ¿aceptas la tarea de añadir un test "all seeds sum to 100" en `App.Tests`?

**Bloqueo:** parte de T-09 (validación) + nueva tarea propuesta para `App`. **Decisiones afectadas:** DD-06. **Casos extremos relacionados:** EC-02.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-FEA-10 — ¿`HeroPosition` debería ser obligatorio en `ActionScenarioRequest`?

**Contexto:**
- Todos los campos del DTO son nullable, incluyendo `HeroPosition`.
- Si llega `null`, el filtro lo trata como wildcard → puede matchear filas incorrectas (EC-11).
- En la práctica, `HeroPosition` es la mínima señal para escoger una fila — sin ella, la decisión es no determinista.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Hacer `HeroPosition` requerida** (no-nullable o validación en ctor) | Imposible request mal poblado. | Romper compatibilidad con tests/callers que la dejaban null. |
| **B. Validar en `ExecuteAsync` y retornar `Result.Invalid("HeroPosition is required")`** (ligado a Q-FEA-03) | Misma garantía sin cambiar el tipo. | Requiere migrar `GetActionScenario` a `Result<T>`. |
| **C. Status quo + log warning** | Bajo riesgo. | Caller puede pasarlo `null` y enmascarar bug. |

**Preguntas concretas:**
1. ¿`SetPreflopActionUseCase` siempre construye el request con `HeroPosition` poblada?
2. ¿Hay algún caller (test, debug) que necesite el wildcard?
3. ¿Aceptas Opción A (cambio de contrato) o Opción B (validación runtime)?

**Bloqueo:** ninguna tarea explícita aún; podría añadirse como T-38. **Casos extremos relacionados:** EC-05, EC-11.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-FEA-11 — ¿Cómo se traduce `"Fold"` cuando es matemáticamente imposible (BB unraised)?

**Contexto:**
- `GetActionScenario` retorna `"Fold"` como default seguro (DD-07).
- En BB con sólo limpers o con nadie habiendo subido, el caller no puede foldear (debería hacer Check).
- ¿`SetPreflopActionUseCase` traduce `"Fold"` → `"Check"` en BB? ¿Lo hace `FrmMain`? ¿Lo hace alguien más arriba?

**Preguntas concretas:**
1. ¿Quién es responsable de traducir `"Fold"` → `"Check"` en BB unraised?
2. ¿Hay logging que detecte la traducción para identificar seeds incompletas?
3. ¿`GetActionScenario` debería retornar un valor especial (`"Default"`) y dejar al caller decidir Fold vs Check según contexto?

**Bloqueo:** análisis cross-module pendiente (Fase 2 App). **Decisiones afectadas:** DD-07. **Casos extremos relacionados:** EC-11.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-FEA-12 — ¿Activar optimistic concurrency en `RegionTableMap`?

**Contexto:**
- `UpdateRegionTableMap` usa `LightweightSession` sin concurrency control. Dos editores paralelos pueden sobrescribirse mutuamente (EC-09).
- Probabilidad real baja en uso desktop single-user, pero puede ocurrir si la calibración corre en script + UI abierta.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Activar `UseOptimisticConcurrency(true)` en `OpenScrape.Infrastructure`** | Detecta conflictos automáticamente, retorna error en SaveChanges. | Requiere campo `_version` (Marten lo gestiona) y manejo de `ConcurrencyException` en este módulo. |
| **B. Lock pesimista en el editor de regiones** | Bloqueo explícito en UI. | Solo cubre UI; scripts paralelos siguen sin protección. |
| **C. Status quo** | Cero cambio. | Race silenciosa posible. |

**Preguntas concretas:**
1. ¿Hay scripts de migración o herramientas externas que modifican `RegionTableMap` en paralelo con la UI?
2. ¿Aceptas el sobrecoste de manejar `ConcurrencyException` en `UpdateRegionTableMap`?

**Bloqueo:** afecta `OpenScrape.Infrastructure.Services.AddDataBase` (cross-module). **Casos extremos relacionados:** EC-09.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Resumen — Bloqueantes vs Pulido

| ID | Severidad | Bloqueante para deploy | Refactor cross-module |
|----|-----------|------------------------|------------------------|
| Q-FEA-01 | 🟡 medio | No | No |
| Q-FEA-02 | 🟡 medio | No | No |
| Q-FEA-03 | 🟡 medio | No | Sí (App) |
| Q-FEA-04 | 🔴 alto | **Sí** | Sí (App seeder) |
| Q-FEA-05 | 🟡 medio | No | Sí (App) |
| Q-FEA-06 | 🟡 bajo | No | Sí (App) |
| Q-FEA-07 | 🟡 bajo | No | No |
| Q-FEA-08 | 🟢 bajo | No | Parcial (namespace cross) |
| Q-FEA-09 | 🟡 medio | No | Sí (App seeder) |
| Q-FEA-10 | 🟡 medio | No | Posible (App) |
| Q-FEA-11 | 🟡 medio | No | Sí (App) |
| Q-FEA-12 | 🟡 bajo | No | Sí (Infrastructure) |

**Bloqueante crítico para producción:** **Q-FEA-04** (`Cold4Bet`). Sin esta resolución, el bot se detiene cada vez que el escenario aparece. Resolver primero.

**Recomendado en paralelo (sin bloquear deploy):** Q-FEA-08 (pulido), Q-FEA-07 (DTO), Q-FEA-02 (placeholders).

**Decisiones estructurales (deferibles):** Q-FEA-03 (Result uniforme), Q-FEA-05 (mover wrappers).
