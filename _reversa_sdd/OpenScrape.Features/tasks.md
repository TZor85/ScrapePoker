# OpenScrape.Features — Tareas de Implementación

> Secuencia de tareas para reimplementar la capa de aplicación (Vertical Slice) a partir del legado, con rastreabilidad línea a línea. **Módulo de tamaño medio** (~370 LOC, 16 archivos `.cs`): 5 features funcionales + 3 placeholders + 1 extension DI. La mayoría de las tareas son funcionales (1 por use case); las restantes son hardening y resolución de dead code.

---

## Pré-requisitos

- [ ] .NET 10 SDK instalado.
- [ ] `OpenScrape.Domain` ya compilable (provee `Table`, `Card`, `RegionTableMap`, `GameSession`, `Hand`, `Region`, `GameSituation`, `TablePosition`, `TableDTOMapper`, `CardDTOMapper`).
- [ ] `OpenScrape.Infrastructure` compilable y `IDocumentStore` resoluble vía DI.
- [ ] PostgreSQL local accesible para los tests de integración (TT-08 a TT-12).
- [ ] Decisiones humanas pendientes (ver `questions.md`):
  - [ ] ¿`BetSize` se re-habilita o se elimina del DTO?
  - [ ] ¿Los 3 placeholders (`GetAllTables`, `GetFlopCards`, `GetAllRegionTableMap`) se completan, eliminan o quedan en backlog?
  - [ ] ¿Se uniformiza el patrón `Result<T>` en `GetActionScenario` y `GetRecentGameRounds`?
  - [ ] ¿Se mueven los 9 wrappers `GetAction*UseCase` desde `App.Aplication.UseCases.Actions` a este módulo?
  - [ ] ¿`Cold4Bet.json` se renombra o se ajusta `[Description]` para que coincidan? (impacto cruzado con seeder en `App`)

---

## Tareas

> Cada tarea referencia el archivo legado de origen y su número de línea cuando aplica.

### Bloque A — Estructura del proyecto

- [ ] **T-01** Crear `OpenScrape.Features.csproj` con `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>` y `<NoWarn>$(NoWarn);NU1902</NoWarn>` (vulnerabilidad transitiva de Marten).
  - Origen en el legado: `src/OpenScrape.Features/OpenScrape.Features.csproj:1-21`
  - Critério de pronto: `dotnet build` pasa sin warnings (excepto los suprimidos).
  - Confianza: 🟢

- [ ] **T-02** Declarar las dependencias NuGet: `Marten 8.24.0`, `Ardalis.Result 10.1.0`. Sin referencias a `Microsoft.Extensions.DependencyInjection.*` directas (basta con la transitoria del SDK web/host).
  - Origen en el legado: `src/OpenScrape.Features/OpenScrape.Features.csproj:11-15`
  - Critério de pronto: `dotnet restore` exitoso. `Marten` y `Ardalis.Result` aparecen en el grafo.
  - Confianza: 🟢

- [ ] **T-03** Declarar la `<ProjectReference>` exclusiva a `OpenScrape.Domain`. Verificar que **no** existe referencia a `App`, `Infrastructure` ni `DecisionMaker`.
  - Origen en el legado: `src/OpenScrape.Features/OpenScrape.Features.csproj:17-19`
  - Critério de pronto: `dotnet list reference` muestra una sola entrada hacia `OpenScrape.Domain`.
  - Confianza: 🟢

### Bloque B — Vertical Slices funcionales (use cases activos)

#### Slice 1: `ActionScenario`

- [ ] **T-04** Crear `ActionScenarioRequest` (`class` mutable, todos los campos `nullable`) con: `HandName`, `Suited`, `HeroPosition`, `OpenRaiser`, `ThreeBetPosition`, `Limper`, `Caller`, `Squeezer`, `BetSize`, `IsGreater`, `RaiserFolds`. Posiciones tipadas como `TablePosition?`.
  - Origen en el legado: `src/OpenScrape.Features/ActionScenario/ActionScenarioRequest.cs:5-18`
  - Critério de pronto: el DTO instancia con `new ActionScenarioRequest()` y todas las propiedades parten en `null`.
  - Confianza: 🟢

- [ ] **T-05** Implementar `GetActionScenario` con constructor `(TableUseCases tableUseCases)` y método `public async Task<string> ExecuteAsync(GameSituation situation, ActionScenarioRequest request)`.
  - Origen en el legado: `src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs:7-49`
  - Critério de pronto: la firma es exactamente esta. Cambiarla rompe `SetPreflopActionUseCase`.
  - Confianza: 🟢

- [ ] **T-06** Dentro de `ExecuteAsync`: invocar `tableUseCases.GetTable.ExecuteAsync(situation.GetDescription())`. Si `table?.Value == null`, lanzar `Exception("Table not found")` interna que se traduce en el `catch` a `Exception($"Error executing {situation.GetDescription()} scenario: {ex.Message}")`.
  - Origen en el legado: `GetActionScenario.cs:20-22, 46-48`
  - Critério de pronto: test que con tabla inexistente reciba `Exception` cuyo mensaje contiene la `Description` del `GameSituation` y el texto `"Error executing"`.
  - Confianza: 🟢

- [ ] **T-07** Implementar el filtrado LINQ sobre `table.Value.Positions`. Las **8 condiciones** (`HeroPosition`, `OpenRaiser`, `ThreeBetPosition`, `Limper`, `Caller`, `Squeezer`, `IsGreater`, `RaiserFolds`) usan el patrón `request.X == null || w.X == request.X.GetDescription()` (las booleanas comparan directo). **Conservar `BetSize` comentado** hasta resolver decisión humana (T-25).
  - Origen en el legado: `GetActionScenario.cs:24-33`
  - Critério de pronto: test con seed conocida confirma que cada filtro nullable se ignora cuando es `null` y filtra correctamente cuando se especifica.
  - Confianza: 🟢 (estructura) / 🟡 (`BetSize` pendiente)

- [ ] **T-08** Tras `FirstOrDefault`, filtrar `Hands` por `f.Name == request.HandName && f.Suited == request.Suited`. Si la lista resultante es `null` o vacía → retornar `"Fold"`.
  - Origen en el legado: `GetActionScenario.cs:34-43`
  - Critério de pronto: con `HandName="ZZ"` (inexistente) la salida es exactamente `"Fold"` y no se lanza excepción.
  - Confianza: 🟢

- [ ] **T-09** Implementar `private string GetRandomAction(List<Hand> actions)`:
  1. Si `actions.Count == 0` → `return string.Empty`.
  2. Si `actions.Sum(a => a.Percentage) != 100` → `throw new ArgumentException("Los porcentajes deben sumar 100")`.
  3. `randomNumber = random.Next(1, 101)` (rango cerrado-abierto: 1..100 inclusive).
  4. Recorrer acumulando `Percentage`; cuando `randomNumber <= accumulated` → devolver `action.Action`.
  5. Fallback de redondeo: `actions.Last()?.Action ?? string.Empty`.
  - Origen en el legado: `GetActionScenario.cs:51-84`
  - Critério de pronto: test estadístico (10 000 invocaciones, seed fijo) confirma desvíos menores al 2 % respecto al `Percentage` declarado.
  - Confianza: 🟢

- [ ] **T-10** Crear `ActionScenarioUseCases` como `record ActionScenarioUseCases(GetActionScenario GetActionScenario)`.
  - Origen en el legado: `src/OpenScrape.Features/ActionScenario/ActionScenarioUseCases.cs:5`
  - Critério de pronto: aggregator instancia vía DI con `GetActionScenario` resoluble.
  - Confianza: 🟢

#### Slice 2: `Table`

- [ ] **T-11** Implementar `GetTable` con campo `private IDocumentStore _documentStore`, ctor `(IDocumentStore documentStore)` y método `public async Task<Result<TableDTO?>> ExecuteAsync(string name)`.
  - Origen en el legado: `src/OpenScrape.Features/Table/Get/GetTable.cs:8-35`
  - Critério de pronto: la firma exacta. Internamente `using var session = _documentStore.QuerySession()`, `session.Query<Domain.Entities.Table>().FirstOrDefaultAsync(x => x.Id == name)`, mapeo via `.ToDto()`. Manejo de `null` → `NotFound()`; excepción → `CriticalError(ex.Message)`.
  - Confianza: 🟢

- [ ] **T-12** Crear `GetAllTables` como **placeholder** (constructor `(IDocumentStore)`, sin método público) **únicamente si la decisión T-26 conserva el placeholder**. Si la decisión es eliminar, omitir esta tarea.
  - Origen en el legado: `src/OpenScrape.Features/Table/GetAll/GetAllTables.cs:11-19`
  - Critério de pronto: si se mantiene → tipo registrable en DI sin lógica. Si se elimina → `Services.cs` deja de mencionarlo y `TableUseCases` se reduce a 1-arity.
  - Confianza: 🔴 (decisión humana)

- [ ] **T-13** Crear `TableUseCases` como `record TableUseCases(GetTable GetTable, GetAllTables GetAllTables)`. Si T-26 elimina `GetAllTables`, reducir a `record TableUseCases(GetTable GetTable)`.
  - Origen en el legado: `src/OpenScrape.Features/Table/TableUseCases.cs:6`
  - Critério de pronto: aggregator instancia vía DI; cambios en arity coordinados con consumidores en `App`.
  - Confianza: 🟢 (estructura) / 🔴 (arity dependiente de T-26)

#### Slice 3: `Cards`

- [ ] **T-14** Implementar `GetAllCards` con `using var session = _documentStore.QuerySession()`, `session.Query<Domain.Entities.Card>().ToListAsync()`, mapeo `c.ToDto()`. Lista vacía → `NotFound()`; excepción → `CriticalError`.
  - Origen en el legado: `src/OpenScrape.Features/Cards/GetAll/GetAllCards.cs:8-33`
  - Critério de pronto: con 52 cartas persistidas devuelve `Success` con 52 DTOs; con base vacía devuelve `NotFound`.
  - Confianza: 🟢

- [ ] **T-15** **(Si decisión T-26 conserva el placeholder)** crear `GetFlopCards` con sólo el `record GetFlopCardsResponse()` y el comentario del request original. **Recomendado: eliminar.**
  - Origen en el legado: `src/OpenScrape.Features/Cards/GetFlop/GetFlopCards.cs`
  - Critério de pronto: si se mantiene → archivo presente sin lógica. Si se elimina → archivo desaparece y `discard_log.md` registra el motivo.
  - Confianza: 🔴 (decisión humana, recomendado descartar)

- [ ] **T-16** Crear `CardUseCases` como `record CardUseCases(GetAllCards GetAllCards)`. **Decisión 🟡:** ¿se mantiene el namespace `OpenScrape.Features.Card` (singular) o se renombra a `OpenScrape.Features.Cards` (plural, igual a la carpeta) para coherencia? Renombrar implica tocar 2 archivos en este módulo y los `using` de los consumidores.
  - Origen en el legado: `src/OpenScrape.Features/Cards/CardUseCases.cs:5`
  - Critério de pronto: aggregator resoluble. Si se renombra el namespace, `dotnet build` pasa sin warnings y todos los `using` se actualizan en `App`/`DecisionMaker`/`Tests`.
  - Confianza: 🟢 (estructura) / 🟡 (namespace)

#### Slice 4: `RegionsTableMap`

- [ ] **T-17** Crear `UpdateRegionTableMapRequest` como `record positional` con: `string Category, string Name, int PosX, int PosY, int Width, int Height, double Umbral, double InactiveUmbral, string Color, bool? IsColor, bool? IsHash, bool? IsOnlyNumber, bool? IsBoard`.
  - Origen en el legado: `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMapRequest.cs:3`
  - Critério de pronto: el record es inmutable; instanciable con sintaxis posicional.
  - Confianza: 🟢

- [ ] **T-18** Implementar `UpdateRegionTableMap` con campo `readonly IDocumentStore _documentStore` y método `public async Task<Result> ExecuteAsync(UpdateRegionTableMapRequest request, CancellationToken ct = default)`. Internamente:
  1. `using var session = _documentStore.LightweightSession()` (no `await using` actualmente — alinear con T-30 si se uniformiza).
  2. `var region = await session.LoadAsync<RegionTableMap>(request.Category, ct)`.
  3. Si `null` → `Result.NotFound()`.
  4. Buscar `Region` con `r.Name == request.Name` en `region.Regions`. Si existe, removerla.
  5. Construir nueva `Region` preservando flags `IsHash/IsColor/IsBoard/IsOnlyNumber` del original (`regionToRemove?.X`), aplicando del request: `PosX/Y`, `Width/Height`, `Color`, `Umbral`, `InactiveUmbral`.
  6. `region.Regions?.Add(nueva)`, `session.Store(region)`, `await session.SaveChangesAsync(ct)`.
  - Origen en el legado: `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMap.cs:6-58`
  - Critério de pronto: test que persista región con `IsHash=true`, llame `Update` con `IsHash=null`, y verifique que la región resultante mantiene `IsHash=true`.
  - Confianza: 🟢

- [ ] **T-19** **(Si decisión T-26 conserva el placeholder)** crear `GetAllRegionTableMap` con campo `private IDocumentStore _store` y constructor. El método `Execute` queda comentado tal cual el legado, o se implementa según decisión humana.
  - Origen en el legado: `src/OpenScrape.Features/RegionsTableMap/GetAll/GetAllRegionTableMap.cs:5-20`
  - Critério de pronto: si se mantiene → tipo registrable en DI. Si se implementa → método público con `await using` + `Query<RegionTableMap>().ToListAsync()`. Si se elimina → archivo desaparece y `RegionTableMapUseCases` se reduce a 1-arity.
  - Confianza: 🔴 (decisión humana)

- [ ] **T-20** Crear `RegionTableMapUseCases` como `record RegionTableMapUseCases(GetAllRegionTableMap GetAllRegionTableMap, UpdateRegionTableMap UpdateRegionTableMap)`. Ajustar arity según T-19.
  - Origen en el legado: `src/OpenScrape.Features/RegionsTableMap/RegionTableMapUseCases.cs:6`
  - Critério de pronto: aggregator resoluble.
  - Confianza: 🟢 (estructura) / 🔴 (arity)

#### Slice 5: `GameRound`

- [ ] **T-21** Implementar `GetRecentGameRounds` con campo `readonly IDocumentStore _documentStore` y método `public async Task<List<GameSession>> Execute(int count = 20)`. Internamente: `await using var session = _documentStore.QuerySession()`, `session.Query<GameSession>().OrderByDescending(s => s.EndTime).Take(count).ToListAsync()`.
  - Origen en el legado: `src/OpenScrape.Features/GameRound/GetRecentGameRounds.cs:6-23`
  - Critério de pronto: test con 25 sesiones y `count=10` devuelve las 10 con `EndTime` más reciente, en orden descendente.
  - Confianza: 🟢

- [ ] **T-22** Crear `GameRoundUseCases` como `record GameRoundUseCases(GetRecentGameRounds GetRecentGameRounds)`.
  - Origen en el legado: `src/OpenScrape.Features/GameRound/GameRoundUseCases.cs:3`
  - Critério de pronto: aggregator resoluble.
  - Confianza: 🟢

### Bloque C — Composición DI

- [ ] **T-23** Crear `Services.cs` con `namespace OpenScrape.Features;` y `public static class Services` que expone el extension method:
  ```csharp
  public static IServiceCollection AddUseCases(this IServiceCollection services) =>
      services
          .AddScoped<GetActionScenario>()
          .AddScoped<ActionScenarioUseCases>()
          .AddScoped<GetTable>()
          .AddScoped<TableUseCases>()
          .AddScoped<GetAllCards>()
          .AddScoped<CardUseCases>()
          .AddScoped<GetAllRegionTableMap>()
          .AddScoped<UpdateRegionTableMap>()
          .AddScoped<RegionTableMapUseCases>()
          .AddScoped<GetAllTables>()
          .AddScoped<GetRecentGameRounds>()
          .AddScoped<GameRoundUseCases>();
  ```
  - Origen en el legado: `src/OpenScrape.Features/Services.cs:18-35`
  - Critério de pronto: tras invocar `AddUseCases`, los 5 aggregators y los use cases activos resuelven en un scope.
  - Confianza: 🟢

- [ ] **T-24** **(Coordinación con `App`)** verificar que `OpenScrape.App.Program.cs` invoca `services.AddUseCases()` exactamente una vez en el bootstrap.
  - Origen en el legado: `src/OpenScrape.App/Program.cs` (referencia cruzada)
  - Critério de pronto: ningún consumidor de `TableUseCases`/`CardUseCases`/etc. recibe `null` en runtime.
  - Confianza: 🟢

### Bloque D — Hardening (decisiones 🔴/🟡)

- [ ] **T-25** **DECISIÓN PENDIENTE 🔴: filtro `BetSize`.** Tres opciones:
  - **A.** Re-habilitar `&& (request.BetSize == null || w.BetSize == request.BetSize)` en `GetActionScenario.cs:31` y validar que las seeds soportan distinción por `BetSize`.
  - **B.** Eliminar el campo `BetSize` del DTO `ActionScenarioRequest` y la línea comentada.
  - **C.** Mantener tal cual (deshabilitado pero presente en DTO) y documentar la deuda.
  - Origen en el legado: `GetActionScenario.cs:31` + `ActionScenarioRequest.cs:15`
  - Critério de pronto: decisión registrada en `decisions.md` y código alineado.
  - Confianza: 🔴

- [ ] **T-26** **DECISIÓN PENDIENTE 🔴: 3 placeholders dead code.**
  - **A.** Implementar (`GetAllTables`, `GetFlopCards`, `GetAllRegionTableMap`).
  - **B.** Eliminar y registrar en `discard_log.md`.
  - **C.** Mover a backlog explícito (issues etiquetados).
  - Recomendación del Reversa: opción **B** para `GetFlopCards` (record vacío, sin valor); opción **A** para `GetAllRegionTableMap` (Hay valor para vista de calibración OCR); opción **A** o **C** para `GetAllTables` según necesidad de la pestaña Tablas.
  - Origen en el legado: `GetAllTables.cs`, `GetFlopCards.cs`, `GetAllRegionTableMap.cs`
  - Critério de pronto: cada placeholder resuelto a uno de los 3 caminos. Ajusta T-12, T-15, T-19, T-20 y `Services.cs`.
  - Confianza: 🔴

- [ ] **T-27** **DECISIÓN PENDIENTE 🟡: uniformizar patrón Result.**
  - `GetActionScenario` retorna `Task<string>` y re-lanza `Exception` envueltas. Migrar a `Task<Result<string>>` requiere tocar `SetPreflopActionUseCase` y todos los wrappers `GetAction*UseCase` en `App.Aplication.UseCases.Actions` (10 archivos).
  - `GetRecentGameRounds.Execute` retorna `Task<List<GameSession>>` directo. Migrar a `Task<Result<List<GameSession>>>` requiere tocar la pestaña Historial en `FrmMain`.
  - Origen en el legado: `GetActionScenario.cs:16, 46-48`, `GetRecentGameRounds.cs:15`
  - Critério de pronto: decisión registrada en `decisions.md`. Si se uniformiza, los tests cubren los 3 paths (`Success`, `NotFound`, `CriticalError`).
  - Confianza: 🟡

- [ ] **T-28** **(Hardening trivial)** Sustituir `new Random()` por `Random.Shared` en `GetActionScenario.GetRandomAction`.
  ```csharp
  int randomNumber = Random.Shared.Next(1, 101);
  ```
  - Origen en el legado: `GetActionScenario.cs:64-65`
  - Critério de pronto: misma distribución estadística (test TT-04 sigue pasando), elimina alocación + reduce dependencia de seed por reloj coarse.
  - Confianza: 🟢

- [ ] **T-29** **(Hardening)** Sustituir `throw new Exception($"Error executing {situation.GetDescription()} scenario: {ex.Message}")` por `throw;` (preserva stack trace) o por retorno `Result<string>.CriticalError(...)` si T-27 se aplica.
  - Origen en el legado: `GetActionScenario.cs:46-48`
  - Critério de pronto: stack trace de la excepción interna llega completo al caller.
  - Confianza: 🟡 (decisión cruzada con T-27)

- [ ] **T-30** **(Hardening de coherencia)** Uniformizar `using` síncrono → `await using` en `GetTable`, `GetAllCards`, `UpdateRegionTableMap`. Alinearlos con `GetRecentGameRounds`.
  - Origen en el legado: `GetTable.cs:21`, `GetAllCards.cs:21`, `UpdateRegionTableMap.cs:19`, `GetRecentGameRounds.cs:17`
  - Critério de pronto: todos los archivos usan `await using var session = ...`. Tests siguen pasando.
  - Confianza: 🟢

- [ ] **T-31** **(Hardening de coherencia)** Marcar `_documentStore`/`_store` como `readonly` en los archivos donde no lo está (`GetTable`, `GetAllCards`, `GetAllTables`, `GetAllRegionTableMap`).
  - Origen en el legado: archivos citados
  - Critério de pronto: 7 de 7 use cases tienen el campo `readonly`.
  - Confianza: 🟢

- [ ] **T-32** **(I18n trivial)** Estandarizar mensajes de error al castellano (consistente con `doc_language=Español` y resto del módulo). Cambiar:
  - `"Table not found"` → `"Tabla no encontrada"` (`GetActionScenario.cs:22`)
  - Confianza: 🟢

- [ ] **T-33** **(Defensa)** En `GetRecentGameRounds.Execute`, envolver el `Query` en try/catch que retorne lista vacía + log estructurado en lugar de propagar la excepción a `FrmMain.HistorialTab`. Alternativa: dejar que el caller (App) gestione (status quo).
  - Origen en el legado: `GetRecentGameRounds.cs:15-22`
  - Critério de pronto: la pestaña Historial nunca crashea por error transitorio de Marten.
  - Confianza: 🟡 (decisión humana sobre dónde gestionar el error)

### Bloque E — Coordinación con `App` (cross-module)

- [ ] **T-34** **(Coordinar con módulo App)** Resolver el mismatch `Cold4Bet.json` (nombre de archivo) ↔ `[Description("ColdFourBet")]` en `Domain.Enums.Positions:78`. Tres opciones:
  - **A.** Renombrar el archivo a `ColdFourBet.json`.
  - **B.** Cambiar la `[Description]` a `"Cold4Bet"`.
  - **C.** Ajustar el seeder para mapear nombre archivo → `Description` enum.
  - Riesgo: si el seeder usa nombre archivo como `Table.Id`, **`GetActionScenario` consultará `"ColdFourBet"` y obtendrá `NotFound` → excepción**. Validar con seeder real (ubicado en `App/Helpers/JsonSeeder` o similar — pendiente análisis Fase 2 módulo App).
  - Origen en el legado: `src/OpenScrape.App/Data/Cold4Bet.json` + `src/OpenScrape.Domain/Enums/Positions.cs:78`
  - Critério de pronto: invocar `GetActionScenario` con `GameSituation.Cold4Bet` retorna acción válida, no excepción.
  - Confianza: 🔴

- [ ] **T-35** **(Decisión arquitectónica 🟡)** Evaluar mover los 9 wrappers `GetAction*UseCase` desde `OpenScrape.App.Aplication.UseCases.Actions` a `OpenScrape.Features.ActionScenario.Get` (vertical slice estricto). Coste: 9 archivos a mover + `Services.cs` actualizado + `SetPreflopActionUseCase` ajusta sus dependencias para inyectar los wrappers vía DI en lugar de instanciarlos con `new`.
  - Origen en el legado: `legacy-mapping.md → Llamadores externos → 9× wrappers`
  - Critério de pronto: `SetPreflopActionUseCase` resuelve los wrappers vía DI (no `new`), tests de integración pasan, cobertura de tests no baja.
  - Confianza: 🟡

### Bloque F — Pulido

- [ ] **T-36** Limpiar `using`s no usados en archivos del módulo (`System.Collections.Generic`, `System.Linq`, `System.Text` se incluyen automáticamente vía `<ImplicitUsings>`).
  - Origen en el legado: `Cards/GetFlop/GetFlopCards.cs:2-6`, `Table/GetAll/GetAllTables.cs:3-7`
  - Critério de pronto: `dotnet format` no detecta cambios.
  - Confianza: 🟢

- [ ] **T-37** Habilitar `dotnet format` y `dotnet format analyzers` sobre el módulo en CI (alineamiento con `CLAUDE.md`).
  - Origen en el legado: comando declarado en `CLAUDE.md`
  - Critério de pronto: pipeline CI ejecuta `dotnet format --verify-no-changes` sin errores en el módulo.
  - Confianza: 🟢

---

## Tareas de Test

- [ ] **TT-01** Test del happy path de `GetActionScenario`: con seed conocida (`OpenRaise` table con 1 fila `HeroPosition=Button`, 1 mano `AKs/raise/100%`), invocar con `request{HandName="AKs", Suited=true, HeroPosition=Button}` retorna `"raise"` (RF-01, RF-04).
- [ ] **TT-02** Test de error en `GetActionScenario`: con tabla `OpenRaise` ausente, invocar lanza `Exception` cuyo `Message.Contains("OpenRaise")` (RF-02).
- [ ] **TT-03** Test de fallback en `GetActionScenario`: con `HandName` que no existe en la fila → retorna exactamente `"Fold"` (RF-03).
- [ ] **TT-04** Test estadístico de `GetRandomAction`: con 2 manos `[raise/60, call/40]` y 10 000 invocaciones (seed fijo) la distribución de `"raise"` cae en `[5 800, 6 200]` (RF-04).
- [ ] **TT-05** Test de validación de `GetRandomAction`: con manos `[40, 40]` lanza `ArgumentException` con mensaje `"Los porcentajes deben sumar 100"` (RF-05).
- [ ] **TT-06** Test del happy path de `GetTable`: persistir `Table{Id="ThreeBet"}` y recibir `Result.Success` con DTO equivalente (RF-06).
- [ ] **TT-07** Test de `NotFound` en `GetTable`: contra base vacía recibe `Status=NotFound` (RF-07).
- [ ] **TT-08** Test de `CriticalError` en `GetTable`: con Marten configurado a una conexión rota, recibe `Status=CriticalError` (RF-08).
- [ ] **TT-09** Test del happy path de `GetAllCards`: 52 documentos persistidos → `Result.Success` con 52 DTOs (RF-09); base vacía → `NotFound` (RF-10).
- [ ] **TT-10** Test de preservación de flags en `UpdateRegionTableMap` (RF-11): persistir región `Pot` con `IsHash=true, IsColor=false`, llamar `Update` con flags `null`, verificar región resultante mantiene los flags originales.
- [ ] **TT-11** Test de `NotFound` en `UpdateRegionTableMap` (RF-12): category inexistente devuelve `Status=NotFound`.
- [ ] **TT-12** Test de cancelación: `UpdateRegionTableMap.ExecuteAsync(req, cancelledToken)` propaga `OperationCanceledException` o devuelve `CriticalError` (RF-13).
- [ ] **TT-13** Test del orden en `GetRecentGameRounds`: 25 sesiones con `EndTime` distintos, `count=10` devuelve las 10 con `EndTime` mayor en orden descendente (RF-14).
- [ ] **TT-14** Test de DI: tras `services.AddUseCases().BuildServiceProvider()`, todos los aggregators y use cases activos resuelven en un scope; ningún resolver retorna `null` (RF-16).
- [ ] **TT-15** Test de aislamiento de sesiones: tras una llamada a `GetTable.ExecuteAsync`, la sesión Marten queda dispuesta — verificar con un `IDocumentStoreMonitor` o reflexión sobre el contador interno de Marten (RF-17).
- [ ] **TT-16** Test de coherencia namespace (post T-16 si se renombra): `OpenScrape.Features.Cards.CardUseCases` resuelve en lugar de `OpenScrape.Features.Card.CardUseCases`.
- [ ] **TT-17** Test cruzado del flujo preflop completo (integración con `App`): construir `SetPreflopActionUseCase` con `ActionScenarioUseCases` real, situación `OpenRaise`, hero=Button, AKs → recibe `"raise"`/`"call"`/`"fold"` válido y consistente con la seed.
- [ ] **TT-18** Test de regresión `Cold4Bet`: con `GameSituation.Cold4Bet` invocar `GetActionScenario` y validar que **no lanza excepción** tras T-34 (cualquiera de las 3 opciones).

---

## Tareas de Migración de Datos

> El módulo no realiza migraciones programáticas. Las seeds se cargan desde `src/OpenScrape.App/Data/*.json` por un seeder en el módulo `App` (pendiente análisis Fase 2). Las tareas de migración listadas son cross-module.

- [ ] **TM-01** **(Cross-module con App)** Validar que el seeder lee correctamente todos los `*.json` de `src/OpenScrape.App/Data/` y los persiste con `Table.Id` que coincide con `GameSituation.GetDescription()` para cada situación. Caso límite urgente: `Cold4Bet.json` (T-34).
  - Origen en el legado: `src/OpenScrape.App/Data/*.json` (12 archivos)
  - Critério de pronto: post-seeding, `GetTable.ExecuteAsync(s.GetDescription())` para todo `s in Enum.GetValues<GameSituation>()` retorna `Success` excepto los explícitamente no soportados.
  - Confianza: 🔴

- [ ] **TM-02** **(Cross-module con App)** Documentar y validar el origen de `Regiones.json` vs `Regiones3.json` vs `RegionToTest.json`. ¿Son tres variantes de la misma mesa? ¿Tres salas distintas? ¿Test fixtures?
  - Origen en el legado: `src/OpenScrape.App/Data/Regiones*.json`
  - Critério de pronto: cada archivo justificado en `decisions.md` o eliminado.
  - Confianza: 🔴

---

## Ordem Sugerida

1. **T-01 a T-03** (proyecto y referencias). Base sin la cual nada compila.
2. **T-04 a T-09** (Slice ActionScenario, núcleo del módulo). Reproduce el flujo crítico preflop 1:1.
3. **T-10 a T-22** (Slices restantes en cualquier orden). Cada uno es independiente.
4. **T-23, T-24** (composición DI y coordinación). Cierra el módulo funcionalmente.
5. **TT-01 a TT-15** (tests). Fija el contrato antes de los cambios opcionales.
6. **T-25 a T-29** (decisiones humanas — `BetSize`, dead code, Result, throw, await using). Pueden tocar consumidores en `App` y `DecisionMaker` — coordinar.
7. **T-30, T-31, T-32** (hardening de coherencia). Refactor seguro.
8. **T-34** (Cold4Bet mismatch — bloqueante operativo). Antes de cualquier deploy real.
9. **T-35** (mover wrappers). Refactor mayor — coordinar con análisis Fase 2 del módulo App.
10. **T-36, T-37** (pulido y CI). Fase final.

**Bloqueos cruzados:**
- T-05/T-06 (GetActionScenario.ExecuteAsync) requieren T-11 (GetTable) construido y T-13 (TableUseCases) registrado en DI.
- T-23 (Services.AddUseCases) requiere todos los use cases del Bloque B definidos.
- T-26 (3 placeholders) impacta T-12, T-15, T-19, T-20 y T-23.
- T-27 (Result uniforme) impacta T-29, T-33, los 9 wrappers en `App` y la pestaña Historial.
- T-34 (Cold4Bet) requiere análisis del seeder en `App` (Fase 2) — puede paralelizarse.
- TT-08, TT-12 requieren PostgreSQL accesible en CI; marcar como `[Category("Integration")]`.

---

## Lacunas Pendentes (🔴)

1. **¿Filtro `BetSize` se re-habilita o se elimina?** (T-25) — Impacto: tablas seed que distinguen acciones por bet size están actualmente colapsadas en una sola fila.
2. **¿Resolución de los 3 placeholders dead code?** (T-26, T-12, T-15, T-19) — Impacto: ruido en DI y arity de aggregators.
3. **¿Uniformizar patrón `Result<T>` en `GetActionScenario` y `GetRecentGameRounds`?** (T-27) — Impacto: refactor cross-module si se aplica.
4. **¿Mismatch `Cold4Bet.json` ↔ `[Description("ColdFourBet")]`?** (T-34) — Impacto crítico: en runtime, `GameSituation.Cold4Bet` lanza excepción al consultar la tabla.
5. **¿Mover los 9 wrappers `GetAction*UseCase` a este módulo?** (T-35) — Impacto: alineamiento con Vertical Slice.
6. **¿Origen de `Regiones3.json` y `RegionToTest.json`?** (TM-02) — Impacto: ambigüedad en la calibración OCR.
