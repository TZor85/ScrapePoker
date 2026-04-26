## 1. Auditoría y preparación

- [x] 1.1 Conteo real: **20 mutaciones de campo** (16 en GameCoordinator líneas 353,405-411,503,546-551,636,653,709; 4 en FrmMain líneas 1205-1206,1229-1230) + **2 llamadas a `Reset()`** (GameCoordinator:58, FrmMain:713). La cifra original de "44 call-sites" del proposal incluía lecturas; aquí sólo contamos mutaciones.
- [x] 1.2 `FrmMain` muta `VillainBetSize{Flop,Turn}` y `VillainBet{Flop,Turn}` durante reprocess mid-street. `GameCoordinator` muta el conjunto completo al procesar cada street. Son mutaciones sobre **instancias distintas**, confirmando el bug silencioso. Tras el holder único, la direccionalidad unifica sin cambio semántico: cualquiera que mute, todos ven.
- [x] 1.3 `PostflopGameContextRebuyTests` tiene 6 tests; todos usan `ctx.TrackHeroStackForRebuy(X)` que devuelve `decimal` y muta. Migración mecánica a `var (next, eff) = ctx.TrackHeroStack(X)` preservando aserciones.
- [x] 1.4 Hallazgo adicional: `UpdateFlopState`, `UpdateTurnState`, `TrackHeroStackForRebuy` (versión mutadora) son **dead code** en producción — sólo los tests los usan. Su eliminación no rompe ningún call-site.

## 2. Record inmutable (migración atómica)

- [x] 2.1 Declarar `public sealed record PostflopGameContext`.
- [x] 2.2 Añadir `public static PostflopGameContext NewHand() => new()`.
- [x] 2.3 Añadir `WithFlopState(heroBet, villainBet, isPreflopAggressor)`.
- [x] 2.4 Añadir `WithTurnState(heroBet, villainBet)` con derivación de `VillainCheckedMiddleStreet`.
- [x] 2.5 Añadir `TrackHeroStack(currentStack)` que devuelve `(PostflopGameContext, decimal)` no-mutador.
- [x] 2.6 **Desviación del plan:** migración atómica en vez de coexistencia. Todas las propiedades a `init` directamente. Los 20 call-sites se migran en §5-6 dentro del mismo ciclo de edición.
- [x] 2.7 Build: verde tras migración completa.
- [x] 2.8 Tests: verde tras migración completa.

## 3. Tests del record inmutable

- [x] 3.1 Crear `OpenScrape.App.Tests/PostflopGameContextImmutableTests.cs` — 9 tests cubriendo `NewHand`, `WithFlopState`/`WithTurnState`, igualdad por valor, propiedades derivadas.
- [x] 3.2 Reescribir `PostflopGameContextRebuyTests.cs` a la nueva API `TrackHeroStack` (6 tests verdes, incluido el renombrado `NewHand_InicializaHeroStackPreRebuyAZero`).
- [x] 3.3 Tests verdes.

## 4. PostflopContextHolder

- [x] 4.1 Crear `src/OpenScrape.App/Services/IPostflopContextHolder.cs`.
- [x] 4.2 Crear `src/OpenScrape.App/Services/PostflopContextHolder.cs` con `lock` + `Volatile.Read`.
- [x] 4.3 Registrar en `Program.cs` como scoped (justo antes del `GameCoordinator`).
- [x] 4.4 Crear `OpenScrape.App.Tests/PostflopContextHolderTests.cs` — 7 tests (construcción, Update, StartNewHand, ArgumentNullException, DI misma/distintas instancias).
- [x] 4.5 Tests verdes.

## 5. Migración de call-sites — GameCoordinator

- [x] 5.1 Inyectar `IPostflopContextHolder _contextHolder` en el constructor.
- [x] 5.2 Eliminar `public PostflopGameContext PostflopContext { get; } = new()` de `GameCoordinator` y la prop equivalente de `IGameCoordinator`.
- [x] 5.3 Migrar 16 mutaciones: flop (1 `with` bloque para 5 campos + 1 single-field `InitialBoardDanger`), turn (1 `with` bloque para 5 campos + 1 single-field `LastBoardChange`), river (1 `with` bloque para `PreviousStreetWasBet` + 2 single-field para `IsAnyoneAllIn` y `LastBoardChange`).
- [x] 5.4 `PostflopContext.Reset()` → `_contextHolder.StartNewHand()`.
- [x] 5.5 `CombineBoardChanges` sigue como estático puro, sin cambios.
- [x] 5.6 Build verde, suite verde tras migración completa.

## 6. Migración de call-sites — FrmMain

- [x] 6.1 Eliminar el campo `private readonly PostflopGameContext _postflopContext = new();`.
- [x] 6.2 Inyectar `IPostflopContextHolder contextHolder` en el constructor de `FrmMain`.
- [x] 6.3 Migrar las 4 mutaciones (2 en reprocess flop, 2 en reprocess turn) a `_contextHolder.Update(c => c with { ... })` agrupadas por bloque. `_postflopContext.Reset()` → `_contextHolder.StartNewHand()`.
- [x] 6.4 Build verde, suite verde.

## 7. Endurecimiento — eliminar superficie mutable

- [x] 7.1 Todas las propiedades ya son `{ get; init; }` desde §2.1.
- [x] 7.2 `Reset`, `UpdateFlopState`, `UpdateTurnState` y la versión mutadora de `TrackHeroStackForRebuy` ya eliminadas al reescribir el record.
- [x] 7.3 Grep `PostflopContext\.\w+\s*=|_postflopContext\.\w+\s*=|\.UpdateFlopState\(|\.UpdateTurnState\(|\.TrackHeroStackForRebuy\(` en `src/`: cero resultados. El único `.Reset()` restante es `_gameLoopStateMachine.Reset()`, no del contexto.
- [x] 7.4 `new PostflopGameContext(` en `src/`: cero resultados. `NewHand() => new()` usa el constructor sin parámetros del record.
- [x] 7.5 Build + `dotnet format --verify-no-changes`: verde.
- [x] 7.6 Suite completa verde (1138/1138).

## 8. Verificación final

- [x] 8.1 `dotnet build OpenScrape.sln --configuration Release` → 0 errores.
- [x] 8.2 `DecisionMatrixIntegrationTests`: dentro de la suite — verde (216 casos inalterados).
- [ ] 8.3 *(Manual, pendiente del usuario)* Arranque manual de la app: verificar que el overlay pinta el estado cross-street como antes (barreling, check-raise, float).
- [x] 8.4 `openspec validate postflop-context-inmutable` → "is valid".
- [x] 8.5 Commit `127a2f5`: `refactor(decision): immutable PostflopGameContext with scoped holder`.
