# OpenScrape.Features — Requisitos

> Capa de **casos de uso** de la aplicación. Implementa los flujos de aplicación (Application Layer en Clean Architecture) en estilo **Vertical Slice** (`Feature/Action/UseCase.cs`). Es el único punto autorizado para abrir sesiones Marten en el flujo de lectura/escritura de documentos. **Sin lógica de UI, sin lógica de OCR, sin lógica de equity** — esos viven en `OpenScrape.App` y `OpenScrape.DecisionMaker`. 🟢 (`src/OpenScrape.Features/OpenScrape.Features.csproj`)

---

## Visión General

`OpenScrape.Features` agrupa **cinco features** (slices verticales independientes), cada una con su propio aggregator (`record` con los use cases que la componen):

| Feature | Aggregator | Use cases |
|---------|------------|-----------|
| `ActionScenario` | `ActionScenarioUseCases` | `GetActionScenario` |
| `Table` | `TableUseCases` | `GetTable`, `GetAllTables` (vacío 🔴) |
| `Cards` | `CardUseCases` | `GetAllCards`, `GetFlopCards` (dead code 🔴) |
| `RegionsTableMap` | `RegionTableMapUseCases` | `UpdateRegionTableMap`, `GetAllRegionTableMap` (comentado 🔴) |
| `GameRound` | `GameRoundUseCases` | `GetRecentGameRounds` |

🟢 Los use cases activos suman **5 funcionales** (`GetActionScenario`, `GetTable`, `GetAllCards`, `UpdateRegionTableMap`, `GetRecentGameRounds`) más **3 placeholders** (vacíos o comentados). El módulo tiene ~370 LOC repartidos en 16 archivos `.cs`. (`_reversa_sdd/OpenScrape.Features/legacy-mapping.md`)

El módulo es un cliente puro de `OpenScrape.Domain` + `Marten` (sin dependencias circulares). 🟢

---

## Responsabilidades

- **Selección de acción preflop a partir de tabla de manos.** Dado un `GameSituation` (OpenRaise, ThreeBet, Squeeze, …) y un `ActionScenarioRequest` con la posición y manos en juego, retorna la acción recomendada (`"raise"`, `"call"`, `"fold"`, …) muestreada aleatoriamente entre las opciones permitidas según `Hand.Percentage`. 🟢 (`ActionScenario/Get/GetActionScenario.cs:16`)
- **Lectura de tablas estratégicas por situación.** `GetTable.ExecuteAsync(name)` consulta el documento Marten `Domain.Entities.Table` cuyo `Id == name`, y retorna su DTO via `TableDTOMapper.ToDto()`. 🟢 (`Table/Get/GetTable.cs:17`)
- **Catálogo de cartas para OCR.** `GetAllCards.ExecuteAsync()` retorna todas las `Card` persistidas (52 esperadas) ya proyectadas a `CardDTO`. Es la fuente de verdad consumida por `CardCacheService` (singleton, lazy). 🟢 (`Cards/GetAll/GetAllCards.cs:17`)
- **Edición de regiones de captura OCR.** `UpdateRegionTableMap.ExecuteAsync(request, ct)` carga el `RegionTableMap` por `Category`, sustituye la `Region` de igual `Name` por una nueva (preservando flags `IsHash/IsColor/IsBoard/IsOnlyNumber` del original), y persiste. 🟢 (`RegionsTableMap/Update/UpdateRegionTableMap.cs:15`)
- **Histórico de sesiones para la pestaña Historial.** `GetRecentGameRounds.Execute(count=20)` retorna las últimas `count` `GameSession` ordenadas por `EndTime DESC`. 🟢 (`GameRound/GetRecentGameRounds.cs:15`)
- **Composición DI del módulo.** `Services.AddUseCases(IServiceCollection)` registra los 5 aggregators y los 9 use cases (incluyendo los vacíos) con scope `Scoped`. 🟢 (`Services.cs:18`)

---

## Regras de Negocio

### Reglas funcionales del flujo de selección de acción

- **El `GameSituation` se traduce a `Table.Id` por `GetDescription()`.** `GetActionScenario` llama `tableUseCases.GetTable.ExecuteAsync(situation.GetDescription())` y depende de que el documento Marten persistido tenga ese `Id`. 🟢 (`ActionScenario/Get/GetActionScenario.cs:20`)
- **Si la tabla no existe, lanza excepción.** `GetTable` retorna `Result<TableDTO?>.NotFound()` cuando no encuentra el documento; `GetActionScenario` interpreta `table?.Value == null` como tabla ausente y lanza `Exception("Table not found")` que envuelve en otra excepción con el contexto del situation. 🟢 (`GetActionScenario.cs:21-22`, `GetActionScenario.cs:46-48`)
- **El filtro de manos exige coincidencia exacta de posiciones por `GetDescription()`.** Hero, OpenRaiser, ThreeBetPosition, Limper, Caller, Squeezer comparan strings (no enums) tras `GetDescription()`. Si una posición no se especifica en el request, el filtro la ignora (`request.X == null || w.X == request.X.GetDescription()`). 🟢 (`GetActionScenario.cs:25-30`)
- **El filtro `BetSize` está comentado.** La línea `&& (request.BetSize == null || w.BetSize == request.BetSize)` está deshabilitada — `BetSize` viaja en el DTO del request pero no se usa. 🔴 (`GetActionScenario.cs:31`)
- **El filtro `IsGreater` y `RaiserFolds` sí se aplican.** Permiten distinguir variantes de la misma situación (ej: 3-bet sobre limp vs 3-bet sobre raise). 🟢 (`GetActionScenario.cs:32-33`)
- **Si no hay manos que coincidan, devuelve `"Fold"` (no excepción).** Comportamiento por defecto seguro. 🟢 (`GetActionScenario.cs:37-38`, `42`)
- **La acción se muestrea aleatoriamente entre las manos elegibles, ponderada por `Percentage`.** `GetRandomAction` valida que las percentages sumen exactamente 100, genera `random.Next(1, 101)` y recorre acumulado. 🟢 (`GetActionScenario.cs:51-84`)
- **Si los porcentajes no suman 100, lanza `ArgumentException`.** Garantía dura. 🟢 (`GetActionScenario.cs:60-61`)
- **Fallback de redondeo: devuelve la última acción.** Si por error de redondeo el aleatorio no alcanza ninguna acumulación, retorna `actions.Last()?.Action ?? string.Empty`. 🟢 (`GetActionScenario.cs:79-80`)

### Reglas funcionales del CRUD de regiones

- **`UpdateRegionTableMap` es no destructivo respecto a flags semánticos.** Al sustituir una región por nombre, copia `IsHash`, `IsColor`, `IsBoard`, `IsOnlyNumber` del original a la nueva (incluso si el request las trae). El request **sí** puede modificar `Color`, `InactiveUmbral`, `Umbral`, `PosX/Y`, `Width/Height`. 🟡 (`UpdateRegionTableMap.cs:36-44`)
- **Si la `Category` no existe en BD, retorna `NotFound()`.** No crea documentos; sólo actualiza existentes. 🟢 (`UpdateRegionTableMap.cs:21-22`)
- **El `Name` actúa como clave de la región dentro de un `RegionTableMap.Regions`.** Borrado por `Name` y posterior `Add` de la nueva región. 🟢 (`UpdateRegionTableMap.cs:24-46`)

### Reglas funcionales del histórico

- **`GetRecentGameRounds` ordena por `EndTime DESC`.** Sesiones cerradas más recientes primero. 🟢 (`GetRecentGameRounds.cs:18-20`)
- **`count` por defecto es 20.** Configurable por parámetro. 🟢 (`GetRecentGameRounds.cs:15`)
- **Sesión que no haya cerrado (`EndTime` null o futuro) puede ser excluida del orden.** No verificado en código — depende de cómo Marten maneje `OrderByDescending` con `null`. 🟡

### Reglas estructurales

- **Cada feature es un slice vertical: `Feature/Action/UseCase.cs`.** ActionScenario/Get, Table/Get, Cards/GetAll, RegionsTableMap/Update, GameRound/GetRecentGameRounds. 🟢
- **Los aggregators son `record`s con los use cases inyectados por DI.** Permiten que un solo punto de inyección (`TableUseCases`) exponga varios métodos. 🟢
- **Todos los use cases activos retornan `Result<T>` o `Task<Result<T>>` (Ardalis.Result), excepto:**
  - `GetActionScenario` retorna `Task<string>` (no usa `Result`) 🟡
  - `GetRecentGameRounds.Execute` retorna `Task<List<GameSession>>` (no usa `Result`) 🟡
- **Las sesiones Marten se abren y cierran por operación (no se mantienen como long-lived).** ✅ Cumple guideline de `CLAUDE.md`. 🟢
- **El uso de `await using` vs `using` síncrono es inconsistente.** Sólo `GetRecentGameRounds` usa `await using`; el resto usa `using`. 🟡

### Reglas de namespace

- **El use case `GetAllCards` vive en `OpenScrape.Features.Card.GetAll` (singular).** Todos los demás siguen patrón `OpenScrape.Features.<Carpeta>.<Acción>`; este difiere por la carpeta `Cards/` (plural) vs namespace `Card` (singular). 🟡 (`Cards/CardUseCases.cs:3`)

### Reglas marcadas como dead code 🔴

- **`GetAllTables`** sólo tiene constructor — no expone método `Execute`. Está registrado en DI y consumido vía `TableUseCases` pero nunca usable. 🔴 (`Table/GetAll/GetAllTables.cs`)
- **`GetFlopCards`** sólo declara un `record GetFlopCardsResponse()` vacío y un comentario con un request. No se instancia, no se registra en DI. 🔴 (`Cards/GetFlop/GetFlopCards.cs`)
- **`GetAllRegionTableMap`** declara campo `_store` y constructor pero el método `Execute` está completamente comentado. Está registrado en DI y consumido vía `RegionTableMapUseCases`. 🔴 (`RegionsTableMap/GetAll/GetAllRegionTableMap.cs`)

---

## Requisitos Funcionales

| ID | Requisito | Prioridade | Critério de Aceite |
|----|-----------|------------|---------------------|
| RF-01 | `GetActionScenario.ExecuteAsync(situation, request)` debe consultar la tabla por `situation.GetDescription()` y filtrar las posiciones por sus `GetDescription()`. | Must | Test que pase `GameSituation.OpenRaise` (`Description="OpenRaise"`) y reciba consulta a `Table.Id == "OpenRaise"`. |
| RF-02 | Cuando la tabla no existe, `GetActionScenario` debe lanzar excepción contextualizada con el `situation.GetDescription()`. | Must | Test que mockee `GetTable` retornando `NotFound()` y reciba `Exception` cuyo mensaje contenga `"OpenRaise"`. |
| RF-03 | Si no hay manos que coincidan con el filtro, `GetActionScenario` debe retornar `"Fold"` (string) sin lanzar excepción. | Must | Test con `HandName="ZZ"` (inexistente) recibe `"Fold"`. |
| RF-04 | `GetRandomAction` debe muestrear ponderadamente: la probabilidad de devolver `Action="raise"` debe converger a `Hand.Percentage / 100` con N→∞. | Must | Test estadístico con seed fijo: 10 000 invocaciones para una mano (50% raise, 50% fold) deben caer en `[4 800, 5 200]` raises. |
| RF-05 | Si los porcentajes de las manos elegibles no suman 100, `GetRandomAction` debe lanzar `ArgumentException` con mensaje `"Los porcentajes deben sumar 100"`. | Must | Test con manos `[40, 40]` recibe `ArgumentException`. |
| RF-06 | `GetTable.ExecuteAsync(name)` debe retornar `Result<TableDTO?>.Success` con la DTO correcta cuando el documento existe. | Must | Test de integración con Marten en memoria que persista `Table { Id = "OpenRaise" }` y reciba DTO con mismo nombre. |
| RF-07 | `GetTable.ExecuteAsync(name)` debe retornar `Result<TableDTO?>.NotFound()` cuando el documento no existe (no lanzar). | Must | Test contra base vacía recibe `Result.Status == Status.NotFound`. |
| RF-08 | `GetTable` debe envolver excepciones internas en `Result<TableDTO?>.CriticalError(ex.Message)`. | Should | Test que simule fallo de Marten reciba `Status.CriticalError`. |
| RF-09 | `GetAllCards.ExecuteAsync()` debe retornar todas las `Card` persistidas mapeadas a `CardDTO`. | Must | Test con 52 cards persistidas recibe lista de 52 DTOs. |
| RF-10 | `GetAllCards.ExecuteAsync()` debe retornar `NotFound()` si la tabla está vacía. | Must | Test contra base vacía recibe `Status.NotFound`. |
| RF-11 | `UpdateRegionTableMap.ExecuteAsync(request, ct)` debe sustituir la región existente con mismo `Name` por una nueva conservando los flags `IsHash/IsColor/IsBoard/IsOnlyNumber` del original. | Must | Test que persista región con `IsHash=true`, llame Update con flags `null`, y verifique que la región resultante mantiene `IsHash=true`. |
| RF-12 | `UpdateRegionTableMap` debe retornar `NotFound()` si la `Category` no existe. | Must | Test con category inexistente recibe `Status.NotFound`. |
| RF-13 | `UpdateRegionTableMap` debe respetar el `CancellationToken` recibido en `LoadAsync` y `SaveChangesAsync`. | Should | Test que cancele el token mid-operación y reciba `OperationCanceledException`. |
| RF-14 | `GetRecentGameRounds.Execute(count)` debe retornar las `count` últimas `GameSession` ordenadas por `EndTime DESC`. | Must | Test de integración con 25 sesiones; pedir `count=10` devuelve las 10 con mayor `EndTime`. |
| RF-15 | `GetRecentGameRounds` debe usar `await using` para la sesión Marten. | Should | Inspección visual del archivo. |
| RF-16 | `Services.AddUseCases()` debe registrar los 5 aggregators y los 9 use cases (5 funcionales + 4 placeholders) con scope `Scoped`. | Must | Test que valide `IServiceCollection` tras llamar `AddUseCases()` contiene `ServiceDescriptor` con `Lifetime.Scoped` para cada tipo esperado. |
| RF-17 | Ningún use case debe abrir una sesión Marten que sobreviva más allá de la operación. | Must | Inspección estática: cada `using var session` o `await using var session` está al inicio del try, no se almacena en campo. |
| RF-18 | El campo comentado `BetSize` en `GetActionScenario` debe documentarse como **lacuna pendiente de decisión** (eliminar del DTO o re-habilitar el filtro). | Should | Aparece en `questions.md` como `Q-FEA-XX`. |
| RF-19 | Los placeholders (`GetAllTables`, `GetFlopCards`, `GetAllRegionTableMap`) deben documentarse como dead code candidatos a eliminación o implementación. | Should | Aparece en `questions.md` y en `tasks.md` como tareas explícitas. |
| RF-20 | El módulo no debe exponer ninguna API HTTP/RPC. Es consumido por DI desde `OpenScrape.App`. | Must | Inspección: no hay controladores ASP.NET, no hay endpoints. |

---

## Requisitos Não Funcionais

| Tipo | Requisito inferido | Evidência no código | Confiança |
|------|--------------------|---------------------|-----------|
| Performance | Apertura/cierre de sesión Marten por operación: latencia esperada baja, sin pool ad-hoc en este módulo (Marten gestiona pool internamente). | `GetTable.cs:21`, `GetAllCards.cs:21`, `UpdateRegionTableMap.cs:19`, `GetRecentGameRounds.cs:17` | 🟢 |
| Performance | `GetActionScenario` carga la tabla completa por `GameSituation` y filtra en memoria con LINQ — adecuado porque las tablas son pequeñas (≤ 50 filas por situación, según seeds). | `GetActionScenario.cs:24-35` | 🟡 |
| Performance | Ningún use case implementa caché propia. La caché de cartas vive en `App.Services.CardCacheService` (singleton, fuera de este módulo). | `Cards/GetAll/GetAllCards.cs` (sin caché local) | 🟢 |
| Robustez | Todos los use cases que retornan `Result<T>` envuelven `Exception` en `CriticalError`. Sólo `GetActionScenario` (sin `Result`) re-lanza vía `throw new Exception(...)`. | `GetTable.cs:30-33`, `GetAllCards.cs:28-31`, `UpdateRegionTableMap.cs:53-56` | 🟢 |
| Robustez | El re-lanzamiento de `GetActionScenario` mediante `throw new Exception(...)` **pierde el stack trace original**. Debería ser `throw;` o `Result<string>` para coherencia con resto del módulo. | `GetActionScenario.cs:46-48` | 🔴 |
| Mantenibilidad | Patrón Vertical Slice + aggregator record permite añadir un nuevo use case con sólo 2 archivos: `Feature/Action/UseCase.cs` + entrada en `Services.cs`. | Estructura de carpetas | 🟢 |
| Mantenibilidad | Inconsistencia de namespace: `Cards/` (plural carpeta) → `OpenScrape.Features.Card` (singular namespace). Riesgo de confusión al importar. | `Cards/CardUseCases.cs:3` vs ruta física | 🟡 |
| Mantenibilidad | Inconsistencia `using` síncrono vs `await using`. CLAUDE.md prescribe `await using` para coherencia con `GetRecentGameRounds`. | Comparación entre archivos | 🟡 |
| Trazabilidad | Cada operación de DB pasa por exactamente un use case → fácil tracear modificaciones del modelo persistido. | Estructura | 🟢 |
| Internacionalización | Mensajes de validación (`"Table not found"`, `"Los porcentajes deben sumar 100"`) en mezcla castellano/inglés. | `GetActionScenario.cs:22, 57, 61` | 🟡 |
| Seguridad | Ninguna autenticación/autorización en los use cases — el módulo confía en que el caller (`OpenScrape.App`) ya está autorizado. | Sin atributos `[Authorize]` ni middleware | 🟢 |
| Seguridad | `GetActionScenario` propaga `ex.Message` en la excepción re-lanzada. Si el mensaje original contuviese info sensible (no es el caso ahora con Marten en local), se filtraría. | `GetActionScenario.cs:47` | 🟡 |
| Compatibilidad | Targeting `.NET 10.0`. `<NoWarn>NU1902</NoWarn>` deshabilita warning de paquete `Marten` (referencia transitoria con vulnerabilidad reportada). | `OpenScrape.Features.csproj:8` | 🟡 |
| Testabilidad | Los use cases están desacoplados del `IDocumentStore` real (pueden mockearse o usar Marten en memoria). | Constructor injection | 🟢 |

> Inferido a partir do código. Validar timeouts y comportamiento bajo carga con el equipo de operações.

---

## Critérios de Aceitação

### Selección de acción (RF-01, RF-02, RF-03, RF-04)

```gherkin
Dado un documento Marten Table { Id="OpenRaise", Positions=[{HeroPosition:"Button", Hands:[{Name:"AKs", Suited:true, Action:"raise", Percentage:60}, {Name:"AKs", Suited:true, Action:"call", Percentage:40}]}] }
Y un ActionScenarioRequest { HandName="AKs", Suited=true, HeroPosition=Button }
Cuando se invoca GetActionScenario.ExecuteAsync(GameSituation.OpenRaise, request) 10 000 veces con seed fijo
Entonces aproximadamente 6 000 invocaciones devuelven "raise" y 4 000 devuelven "call"
Y ninguna devuelve "fold"

Dado un GameSituation cuya Description no existe en Marten
Cuando se invoca GetActionScenario.ExecuteAsync(situation, request)
Entonces se lanza Exception cuyo mensaje contiene "Table not found" y la Description del situation

Dado un ActionScenarioRequest cuyo HandName no aparece en ninguna fila de Positions
Cuando se invoca GetActionScenario.ExecuteAsync(situation, request)
Entonces el resultado es exactamente "Fold"
Y no se lanza excepción
```

### Validación de porcentajes (RF-05)

```gherkin
Dado una lista de manos cuyos Percentage suman 80 (no 100)
Cuando se invoca GetRandomAction(hands)
Entonces se lanza ArgumentException con mensaje "Los porcentajes deben sumar 100"
```

### Lectura de tabla (RF-06, RF-07)

```gherkin
Dado un documento Table { Id="ThreeBet" } persistido en Marten
Cuando se invoca GetTable.ExecuteAsync("ThreeBet")
Entonces el resultado tiene Status=Success y Value!=null
Y Value.Name == "ThreeBet"

Dado una base sin documentos Table
Cuando se invoca GetTable.ExecuteAsync("Inexistente")
Entonces el resultado tiene Status=NotFound y Value==null

Dado que Marten está caído
Cuando se invoca GetTable.ExecuteAsync("OpenRaise")
Entonces el resultado tiene Status=CriticalError y Errors contiene el mensaje de la excepción
```

### Catálogo de cartas (RF-09, RF-10)

```gherkin
Dado 52 documentos Card persistidos
Cuando se invoca GetAllCards.ExecuteAsync()
Entonces el resultado tiene Status=Success y Value.Count == 52

Dado una base sin Cards
Cuando se invoca GetAllCards.ExecuteAsync()
Entonces el resultado tiene Status=NotFound
```

### Edición de regiones — preservación de flags (RF-11, RF-12, RF-13)

```gherkin
Dado un RegionTableMap { Category="MesaA", Regions=[{Name="Pot", IsHash=true, IsColor=false, IsBoard=null, IsOnlyNumber=true}] }
Y un UpdateRegionTableMapRequest { Category="MesaA", Name="Pot", PosX=10, PosY=20, IsHash=null, IsColor=null, IsOnlyNumber=null, IsBoard=true }
Cuando se invoca UpdateRegionTableMap.ExecuteAsync(request)
Entonces el resultado tiene Status=Success
Y la región resultante tiene IsHash=true (preservado), IsColor=false (preservado), IsBoard=null (preservado), IsOnlyNumber=true (preservado)
Y la nueva PosX=10, PosY=20

Dado un UpdateRegionTableMapRequest con Category="Inexistente"
Cuando se invoca UpdateRegionTableMap.ExecuteAsync(request)
Entonces el resultado tiene Status=NotFound

Dado un CancellationToken ya cancelado
Cuando se invoca UpdateRegionTableMap.ExecuteAsync(request, token)
Entonces se propaga OperationCanceledException o Status=CriticalError
```

### Histórico (RF-14)

```gherkin
Dado 25 documentos GameSession con EndTime distintos
Cuando se invoca GetRecentGameRounds.Execute(count=10)
Entonces el resultado contiene 10 GameSession
Y están ordenadas por EndTime descendente
Y son las 10 con EndTime más reciente
```

### DI (RF-16, RF-17)

```gherkin
Dado un IServiceCollection vacío
Cuando se invoca services.AddUseCases()
Entonces todos los aggregators (ActionScenarioUseCases, TableUseCases, CardUseCases, RegionTableMapUseCases, GameRoundUseCases) están registrados con Lifetime.Scoped
Y todos los use cases (GetActionScenario, GetTable, GetAllTables, GetAllCards, UpdateRegionTableMap, GetAllRegionTableMap, GetRecentGameRounds) están registrados con Lifetime.Scoped

Dado un grafo DI con AddUseCases() aplicado
Cuando se resuelve TableUseCases en un scope
Entonces se obtiene una instancia con sus dependencias inyectadas
Y al cerrar el scope no quedan IDocumentSession activas
```

---

## Prioridade (MoSCoW)

| Requisito | MoSCoW | Justificativa |
|-----------|--------|---------------|
| `GetActionScenario` (selección preflop por situación + posición) | Must | Caminho crítico — invocado en cada decisión preflop por `SetPreflopActionUseCase`. |
| `GetTable` | Must | Sostén de `GetActionScenario`. Sin él, no hay decisiones preflop. |
| `GetAllCards` | Must | Bootstrap del `CardCacheService`. Sin él, no hay reconocimiento OCR de cartas. |
| `UpdateRegionTableMap` | Must | Único camino de edición de regiones — sin él la calibración OCR es estática (sólo seed). |
| `GetRecentGameRounds` | Should | Histórico de sesiones para la pestaña Historial. La aplicación funciona sin esta vista (gameplay no depende de ella). |
| `Services.AddUseCases()` | Must | Composición DI del módulo. Sin él, ningún use case se resuelve. |
| `GetActionScenario.GetRandomAction` con sampling ponderado | Must | Núcleo de la aleatorización estratégica (Mixed Strategy GTO). |
| Validación porcentajes = 100 en `GetRandomAction` | Must | Garantía de consistencia de la tabla estratégica. Detección temprana de seeds corruptos. |
| Filtros `IsGreater`, `RaiserFolds` | Should | Permiten variantes; sin ellos hay falsos positivos en el matching. |
| Filtro `BetSize` (comentado) | Won't | Deshabilitado actualmente — eliminar del DTO o re-habilitar (decisión humana). |
| `GetAllTables`, `GetFlopCards`, `GetAllRegionTableMap` (placeholders) | Won't | Dead code; eliminar o implementar (decisión humana). |
| `await using` consistente en sesiones Marten | Should | Refactor de bajo riesgo, alta higiene. |
| Reemplazo `new Random()` → `Random.Shared` | Could | Mejora microscópica (asignación + seed) y elimina riesgo de colisión bajo concurrencia. |

> Prioridad inferida por frecuencia de invocación (caminhos críticos), centralidad en la cadeia de dependencias y presença de testes existentes.

---

## Rastreabilidade de Código

| Arquivo | Função / Classe | Cobertura |
|---------|-----------------|-----------|
| `src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs` | `GetActionScenario.ExecuteAsync`, `GetRandomAction` | 🟢 |
| `src/OpenScrape.Features/ActionScenario/ActionScenarioRequest.cs` | DTO request preflop | 🟢 |
| `src/OpenScrape.Features/ActionScenario/ActionScenarioUseCases.cs` | Aggregator | 🟢 |
| `src/OpenScrape.Features/Table/Get/GetTable.cs` | `GetTable.ExecuteAsync` | 🟢 |
| `src/OpenScrape.Features/Table/GetAll/GetAllTables.cs` | constructor only — placeholder | 🔴 |
| `src/OpenScrape.Features/Table/TableUseCases.cs` | Aggregator | 🟢 |
| `src/OpenScrape.Features/Cards/GetAll/GetAllCards.cs` | `GetAllCards.ExecuteAsync` | 🟢 |
| `src/OpenScrape.Features/Cards/GetFlop/GetFlopCards.cs` | `GetFlopCardsResponse` (record vacío) | 🔴 |
| `src/OpenScrape.Features/Cards/CardUseCases.cs` | Aggregator (namespace `Card` singular) | 🟡 |
| `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMap.cs` | `UpdateRegionTableMap.ExecuteAsync` | 🟢 |
| `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMapRequest.cs` | record positional | 🟢 |
| `src/OpenScrape.Features/RegionsTableMap/GetAll/GetAllRegionTableMap.cs` | método comentado | 🔴 |
| `src/OpenScrape.Features/RegionsTableMap/RegionTableMapUseCases.cs` | Aggregator | 🟢 |
| `src/OpenScrape.Features/GameRound/GetRecentGameRounds.cs` | `GetRecentGameRounds.Execute` | 🟢 |
| `src/OpenScrape.Features/GameRound/GameRoundUseCases.cs` | Aggregator | 🟢 |
| `src/OpenScrape.Features/Services.cs` | `AddUseCases` extension | 🟢 |
| `src/OpenScrape.Features/OpenScrape.Features.csproj` | proyecto | 🟢 |
