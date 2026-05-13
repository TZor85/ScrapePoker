# Mapeo a archivos legacy — `OpenScrape.Features`

> Generado por el Arqueólogo del Reversa.
> Lista los archivos del legado que componen este módulo, con referencia directa a rutas y líneas.

---

## Estructura física

| Archivo | Tipo | LOC | Notas |
|---------|------|----:|-------|
| `src/OpenScrape.Features/OpenScrape.Features.csproj` | proyecto | 21 | `net10.0`, ImplicitUsings, Nullable, `<NoWarn>NU1902</NoWarn>` |
| `src/OpenScrape.Features/Services.cs` | clase static | 37 | DI extension `AddUseCases(IServiceCollection)` |
| **Feature `ActionScenario`** | | | |
| `src/OpenScrape.Features/ActionScenario/ActionScenarioRequest.cs` | class | 18 | DTO request mutable |
| `src/OpenScrape.Features/ActionScenario/ActionScenarioUseCases.cs` | record | 7 | aggregator |
| `src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs` | class | 86 | **único use case con lógica no trivial** |
| **Feature `Table`** | | | |
| `src/OpenScrape.Features/Table/TableUseCases.cs` | record | 8 | aggregator |
| `src/OpenScrape.Features/Table/Get/GetTable.cs` | class | 36 | CRUD por Id (string) |
| `src/OpenScrape.Features/Table/GetAll/GetAllTables.cs` | class | 21 | 🔴 **vacío — solo constructor** |
| **Feature `Cards`** | | | |
| `src/OpenScrape.Features/Cards/CardUseCases.cs` | record | 7 | aggregator |
| `src/OpenScrape.Features/Cards/GetAll/GetAllCards.cs` | class | 35 | listado completo |
| `src/OpenScrape.Features/Cards/GetFlop/GetFlopCards.cs` | class | 15 | 🔴 **dead code (record vacío)** |
| **Feature `RegionsTableMap`** | | | |
| `src/OpenScrape.Features/RegionsTableMap/RegionTableMapUseCases.cs` | record | 8 | aggregator |
| `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMap.cs` | class | 59 | edición non-destructiva |
| `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMapRequest.cs` | record | 5 | DTO positional |
| `src/OpenScrape.Features/RegionsTableMap/GetAll/GetAllRegionTableMap.cs` | class | 21 | 🔴 **método comentado** |
| **Feature `GameRound`** | | | |
| `src/OpenScrape.Features/GameRound/GameRoundUseCases.cs` | record | 4 | aggregator |
| `src/OpenScrape.Features/GameRound/GetRecentGameRounds.cs` | class | 25 | últimas N sesiones |

**Total**: 16 archivos `.cs` activos + 1 csproj. **LOC**: ~370.

---

## Mapeo por responsabilidad

### Selección de acción preflop

- **Use case principal**: `src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs:16` → `ExecuteAsync(GameSituation, ActionScenarioRequest)`.
- **Random sampling**: `src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs:51` → `GetRandomAction(List<Hand>)`.
- **Aggregator**: `src/OpenScrape.Features/ActionScenario/ActionScenarioUseCases.cs:5`.
- **Request DTO**: `src/OpenScrape.Features/ActionScenario/ActionScenarioRequest.cs`.

### Lectura de mesa por situación

- **Use case**: `src/OpenScrape.Features/Table/Get/GetTable.cs:17` → `ExecuteAsync(string name)`.
- **Mapper consumido**: `src/OpenScrape.Domain/Mappers/TableDTOMapper.cs:13` → `Table.Id ↔ TableDTO.Name`.
- **Aggregator**: `src/OpenScrape.Features/Table/TableUseCases.cs:6`.

### Catálogo de cartas

- **Use case**: `src/OpenScrape.Features/Cards/GetAll/GetAllCards.cs:17` → `ExecuteAsync()`.
- **Mapper consumido**: `src/OpenScrape.Domain/Mappers/CardDTOMapper.cs` → `Card.Id ↔ CardDTO.Name`.
- **Aggregator**: `src/OpenScrape.Features/Cards/CardUseCases.cs:5`.

### Edición de regiones de captura OCR

- **Use case**: `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMap.cs:15` → `ExecuteAsync(UpdateRegionTableMapRequest, CancellationToken)`.
- **Request DTO**: `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMapRequest.cs:3`.
- **Aggregator**: `src/OpenScrape.Features/RegionsTableMap/RegionTableMapUseCases.cs:6`.

### Histórico de sesiones

- **Use case**: `src/OpenScrape.Features/GameRound/GetRecentGameRounds.cs:15` → `Execute(int count = 20)`.
- **Aggregator**: `src/OpenScrape.Features/GameRound/GameRoundUseCases.cs:3`.

### Composition root del módulo

- **DI extension**: `src/OpenScrape.Features/Services.cs:18` → `AddUseCases(this IServiceCollection)`.

---

## Dependencias entre archivos

```
OpenScrape.Features.csproj
  ├─ Marten 8.24.0 (NuGet)
  ├─ Ardalis.Result 10.1.0 (NuGet)
  └─ ProjectReference → ../OpenScrape.Domain/OpenScrape.Domain.csproj
       ├─ Domain.Entities      (Table, Card, RegionTableMap, GameSession, HandRecord)
       ├─ Domain.Dtos          (TableDTO, CardDTO)
       ├─ Domain.Mappers       (TableDTOMapper.ToDto, CardDTOMapper.ToDto)
       ├─ Domain.ValueObjects  (Hand, Region, PlayerActionSequence)
       └─ Domain.Enums         (GameSituation, TablePosition, EnumExtensions.GetDescription)
```

🟢 Sin dependencias circulares. El módulo es un cliente puro de `OpenScrape.Domain` + `Marten`.

---

## Llamadores externos (consumidores)

| Aggregator | Consumidor | Líneas |
|------------|-----------|-------:|
| `ActionScenarioUseCases` | `src/OpenScrape.App/Aplication/SetPreflopActionUseCase.cs` | 11, 24 |
| `ActionScenarioUseCases` | 9× wrappers en `src/OpenScrape.App/Aplication/UseCases/Actions/GetAction*UseCase.cs` | constructores (instanciados con `new` desde `SetPreflopActionUseCase`) |
| `TableUseCases` | `src/OpenScrape.App/Forms/FrmMain.cs` (constructor injection) | (vía DI scoped) |
| `CardUseCases` | `src/OpenScrape.App/Forms/FrmMain.cs:156` | (vía DI scoped) |
| `RegionTableMapUseCases` | `src/OpenScrape.App/Forms/FrmMain.cs:139` | (vía DI scoped) |
| `GameRoundUseCases` | `src/OpenScrape.App/Forms/FrmMain.cs` (Historial tab) | (vía DI scoped) |

🟡 **OBSERVACIÓN — chain con `new`**: `SetPreflopActionUseCase` instancia 10 wrappers `GetAction*UseCase` con `new` en lugar de inyectarlos. Cualquier reemplazo o mock requeriría cambiar el constructor. Memoria del proyecto registra un fix previo donde `SetPreflopActionUseCase` se movió de Singleton→Scoped, pero los inner wrappers siguen instanciados manualmente.

---

## Llamadores de `IDocumentStore` desde este módulo

| Archivo | Línea | Tipo de sesión | Acción |
|---------|------:|----------------|--------|
| `Cards/GetAll/GetAllCards.cs` | 21 | `QuerySession` | `Query<Card>().ToListAsync()` |
| `Table/Get/GetTable.cs` | 21 | `QuerySession` | `Query<Table>().FirstOrDefaultAsync(Id == name)` |
| `Table/GetAll/GetAllTables.cs` | (15) | — | constructor solo, sin método Execute |
| `RegionsTableMap/Update/UpdateRegionTableMap.cs` | 19 | `LightweightSession` | `LoadAsync<RegionTableMap>(Category)` + `Store` + `SaveChangesAsync` |
| `RegionsTableMap/GetAll/GetAllRegionTableMap.cs` | (16) | (comentado) | comentado: `LightweightSession` + `Query<RegionTableMap>` |
| `GameRound/GetRecentGameRounds.cs` | 17 | `QuerySession` (`await using`) | `Query<GameSession>().OrderByDescending(EndTime).Take(count)` |

🟢 **CONFIRMADO**: ninguna sesión sobrevive más allá de la operación. Patrón `await using` (variante async) solo aplicado en `GetRecentGameRounds`. Los otros usos `using` síncrono podrían cambiarse para coherencia con CLAUDE.md ("Database sessions use `await using` per operation").

---

## Documentos Marten consumidos

| Documento | Archivo Domain | Use cases que lo tocan | Operación |
|-----------|----------------|------------------------|-----------|
| `Table` | `src/OpenScrape.Domain/Entities/Table.cs` | `GetTable`, `GetActionScenario` (vía cascada) | read |
| `Card` | `src/OpenScrape.Domain/Entities/Card.cs` | `GetAllCards` | read |
| `RegionTableMap` | `src/OpenScrape.Domain/Entities/RegionTableMap.cs` | `UpdateRegionTableMap` | load + store |
| `GameSession` | `src/OpenScrape.Domain/Entities/GameSession.cs` | `GetRecentGameRounds` | read (índice `EndTime`) |
| `HandRecord` | `src/OpenScrape.Domain/Entities/GameSession.cs:56` | (no consumido en este módulo) | — |

---

## Configuración relacionada (fuera del módulo)

| Archivo | Sección | Notas |
|---------|---------|-------|
| `src/OpenScrape.App/Data/OpenRaise.json` | seed | seed inicial de `Table` con `Id == "OpenRaise"` |
| `src/OpenScrape.App/Data/BBvsSB.json` | seed | seed `Table` con `Id == "BigBlindVsSmallBlind"` |
| `src/OpenScrape.App/Data/Cold4Bet.json` | seed | 🟡 nombre del fichero `Cold4Bet`, descripción del enum `ColdFourBet` (`Positions.cs:78`) — verificar si el `Id` del documento Marten coincide con la descripción o con el nombre del fichero |
| `src/OpenScrape.App/Data/FourBet.json` | seed | `[Description("FourBet")]` |
| `src/OpenScrape.App/Data/RaiseOverLimpers.json` | seed | `[Description("RaiseOverLimpers")]` |
| `src/OpenScrape.App/Data/RaiseVsSbLimp.json` | seed | (sin enum directo — usado por `GetActionRaiseVsSBLimpUseCase`) |
| `src/OpenScrape.App/Data/Squeeze.json` | seed | `[Description("Squeeze")]` |
| `src/OpenScrape.App/Data/ThreeBet.json` | seed | `[Description("ThreeBet")]` |
| `src/OpenScrape.App/Data/VsSqueeze.json` | seed | `[Description("VsSqueeze")]` |
| `src/OpenScrape.App/Data/VsThreeBet.json` | seed | `[Description("VsThreeBet")]` |
| `src/OpenScrape.App/Data/VsThreeBetAndCall.json` | seed | `[Description("VsThreeBetAndCall")]` |
| `src/OpenScrape.App/Data/Regiones.json` | seed | seed de `RegionTableMap.Regions` |
| `src/OpenScrape.App/Data/Regiones3.json` | seed | seed alternativo (¿segunda mesa? ¿variante?) — validar |
| `src/OpenScrape.App/Data/RegionToTest.json` | seed | seed para testing — validar uso |

🔴 **LACUNA — coincidencia clave**: `Cold4Bet.json` (nombre fichero) vs `[Description("ColdFourBet")]` en `GameSituation.Cold4Bet` (`Positions.cs:78`). Si el `Table.Id` persistido viene del nombre de fichero pero `GetActionScenario` consulta por `GetDescription()`, falla silenciosamente y devuelve `"Fold"`. Validar con el usuario el seeder/loader real (probable `App/Helpers/JsonSeeder` o similar — pendiente Fase 2 módulo `App`).

---

## Pendientes / lacunas para el Detective y el Architect

- [ ] 🔴 Validar coincidencia `JSON filename ↔ Table.Id ↔ GameSituation.GetDescription()`. Cualquier desalineación rompe `GetActionScenario` silenciosamente (devuelve `"Fold"`). Específicamente `Cold4Bet.json` vs `[Description("ColdFourBet")]`.
- [ ] 🔴 Decidir si el filtro `BetSize` (línea 31 comentada) debe re-habilitarse o eliminarse del `ActionScenarioRequest`.
- [ ] 🟡 Decidir destino de los 3 use cases vacíos (`GetAllTables`, `GetAllRegionTableMap`, `GetFlopCards`): completar implementación, eliminar o mover a backlog explícito.
- [ ] 🟡 Decidir si los flags `IsHash/IsColor/IsBoard/IsOnlyNumber` de `UpdateRegionTableMapRequest` deben usarse al crear regiones nuevas (ahora siempre `null`) o eliminarse del DTO.
- [ ] 🟡 Decidir si la tabla hardcoded de `SetIsGreater` (15 entradas con umbrales en BB) debe externalizarse a `appsettings.json` o `StrategyProfile`.
- [ ] 🟡 Sustituir `new Random()` por `Random.Shared` en `GetActionScenario.GetRandomAction`.
- [ ] 🟡 Reemplazar `throw new Exception(...)` en `GetActionScenario.ExecuteAsync` por `throw;` o por `Result<string>` para consistencia con resto del módulo.
- [ ] 🟡 Unificar `using` síncrono vs `await using` en sesiones Marten (alinear `GetTable`, `GetAllCards`, `UpdateRegionTableMap` con `GetRecentGameRounds`).
- [ ] 🟡 Considerar mover los 9 wrappers `GetAction*UseCase` (en `App.Aplication.UseCases.Actions`) a `OpenScrape.Features` (siguiendo el patrón vertical) o registrarlos en DI para consistencia.
