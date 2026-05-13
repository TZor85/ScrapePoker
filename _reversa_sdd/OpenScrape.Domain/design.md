# OpenScrape.Domain — Design Técnico

> Cómo está construida la capa de dominio: estructura de tipos, patrones, persistencia, validaciones e invariantes.

---

## Interface

`OpenScrape.Domain` no expone HTTP ni RPC — es una librería de tipos. Su "API" son los **tipos públicos** consumidos por las capas superiores (`Features`, `DecisionMaker`, `App`).

### Tipos por familia

| Familia | Tipo | Tipo C# | Persistido | Mutabilidad |
|---------|------|---------|:----------:|-------------|
| Documento Marten | `Card` | `class` | ✅ | Mutable (set) |
| Documento Marten | `Table` | `class` | ✅ | Mutable |
| Documento Marten | `RegionTableMap` | `class` | ✅ | Mutable |
| Documento Marten | `GameSession` | `class` | ✅ | Mutable |
| Documento Marten | `HandRecord` | `class` | ✅ | Mutable |
| Documento Marten | `StrategyProfile` | `class` | ✅ | Mutable (~60 props) |
| Entidad mutable | `OpponentProfile` | `class` | ❌ (memoria) | Mutable (contadores) |
| Entidad mutable | `OpponentPositionProfile` | `class` | ❌ | Mutable |
| Entidad mutable | `OverlayConfig` | `class` | ❌ | Mutable |
| Value Object inmutable | `Hand` | `record` | ❌ | `init` |
| Value Object inmutable | `Region` | `record` | ❌ | `init` |
| Value Object inmutable | `StreetDecision` | `record` | ❌ | `init` |
| Value Object inmutable | `StreetThresholds` | `record` | ❌ | `init` |
| Value Object inmutable | `ThresholdKey` | `record sealed` | ❌ | constructor |
| Value Object inmutable | `PlayerActionSequence` | `record` | ❌ | `init` |
| Value Object inmutable | `CategoryStats` | `record` | ❌ | `init` |
| Value Object inmutable | `TelemetryAggregate` | `record` | ❌ | `init` |
| Value Object mutable | `CardDataOuts` | `class` | ❌ | Mutable |
| Value Object mutable | `DrawProbability` | `class` | ❌ | Mutable |
| Value Object mutable | `HandStrength` | `class` | ❌ | Mutable |
| Value Object mutable | `HandEvaluation` | `class` | ❌ | Mutable |
| Value Object mutable | `PotOddsResult` | `class` | ❌ | Mutable |
| Value Object mutable | `VillainRange` | `class` | ❌ | Mutable |
| Value Object mutable | `BankrollSnapshot/Stats/HistoryItem` | `class` | ❌ | Mutable |
| Enum (byte) | `Rank` | `byte` | n/a | Inmutable |
| Enum (byte) | `Suit` | `byte` | n/a | Inmutable |
| Enum (byte) | `HandRank` | `byte` | n/a | Inmutable |
| Enum (byte) | `KickerStrength` | `byte` | n/a | Inmutable |
| Enum (byte) | `PairClassification` | `byte` | n/a | Inmutable |
| Enum (int) | `Positions`, `TablePosition`, `HandSituation`, `BoardPosition`, `BluffConditionType`, `HandResult`, `OpponentType` | `int` | n/a | Inmutable |

### Tipos clave del pipeline (firmas)

| Símbolo | Construcción | Observación |
|---------|--------------|-------------|
| `Hand(Name, Suited?, Action, Percentage)` | `record` con validaciones inline | Lanzar `ArgumentException` / `ArgumentOutOfRangeException` en init |
| `ThresholdKey(BoardPosition, HandSituation)` | `record sealed` con validación constructor | Rechaza `None`/`Hand` |
| `ThresholdKey.TryParse(string?, out ThresholdKey?, out string)` | static method | Retorna `bool`, sin lanzar |
| `StreetThresholds { ... ~30 init props ... }` | `record` puro | Solo `init`; configurado vía JSON binding |
| `StrategyProfile { ... ~60 mutable props ... }` | `class` (Marten) | `Validate()` invocado en startup |
| `OpponentProfile.GetTypeForPosition(bool villainIsIP)` | método | Retorna `OpponentType` con AF posicional |
| `VillainRange.AdjustByOpponent(OpponentProfile)` | método | Escala rango por VPIP/3Bet% |

---

## Fluxo Principal

`Domain` no tiene "fluxo" runtime: es estructura de datos. El "fluxo" relevante es **cómo se construyen y validan sus tipos** durante el arranque y durante una mano.

### Fluxo de arranque (boot)

1. `Program.cs` (App) carga `appsettings.json` y enlaza la sección `Strategy` a `StrategyProfile` con `IOptions`. 🟢 (`src/OpenScrape.App/Program.cs`)
2. Marten lee de PostgreSQL los documentos `Card` (52), `Table`, `RegionTableMap`, `StrategyProfile` activo. 🟢
3. `IValidateOptions<StrategyProfile>` ejecuta `Validate()` con sus 18 reglas cruzadas. Si falla, lanza `StrategyProfileValidationException` y `IHost.RunAsync()` aborta. 🟢 (ADR-0008)
4. `StreetThresholds` por situación se cargan desde JSON files en `src/OpenScrape.App/Data/*.json` y se indexan por `ThresholdKey`. 🟢
5. `CardCacheService` (singleton App) materializa el catálogo de `Card` desde Marten en memoria. 🟢

### Fluxo de uso en una mano

1. Game loop captura pantalla → reconoce cartas → instancia `Hand` con validación inline. 🟢
2. `BoardTextureAnalyzer` calcula `HandRank` a partir de `Card[]`. 🟢
3. `PostflopDecisionService` construye `ThresholdKey(boardPos, situation)` y lookup en `StrategyProfile.Thresholds`. 🟢
4. Cada decisión genera un `StreetDecision` (record inmutable) y se agrega a `HandRecord.Decisions`. 🟢
5. Al terminar la mano, `HandRecord` (mutable durante la mano) se persiste en Marten y `GameSession.Hands` se trunca a 20. 🟢

### Fluxo de cierre de sesión

1. `GameLoggerService.SaveSessionAsync()` actualiza `GameSession.EndTime` y `EndingBankroll`. 🟢
2. Documento se escribe a Marten. 🟢
3. `BBPer100` y `TotalProfit` se calculan en memoria al renderizar el dashboard. 🟢

---

## Fluxos Alternativos

- **Tipo Marten con campo Json-ignored:** `GameSession.Hands` y `GameSession.TotalHands/TotalProfit/BBPer100` están marcados `[JsonIgnore]` para que Marten no los serialice — la lista de hands vive en su propia colección con FK `GameSessionId`. 🟢 (`Entities/GameSession.cs:28-41`)
- **AF con `HandsPlayed < 5`:** `OpponentPositionProfile.AggressionFactorIP/OOP` retorna `-1` (sentinel "sin datos suficientes"). El consumidor (`PostflopDecisionService`) interpreta `-1` como "fallback a estático". 🟢 (`OpponentProfile.cs:26-37`)
- **`ThresholdKey.TryParse` con string nulo/vacío:** retorna `false` con mensaje "La clave está vacía", sin lanzar. 🟢 (`ThresholdKey.cs:41-45`)
- **`Hand.Percentage` fuera de rango:** lanza `ArgumentOutOfRangeException` — fail-fast en construcción, no se permite estado inválido en memoria. 🟢
- **`StrategyProfile.Validate()` inválido:** lanza `StrategyProfileValidationException` durante DI; la app **no arranca**. 🟢 (ADR-0008)
- **Carta no reconocida por OCR:** la capa superior (`ScreenReaderService`) decide. Domain no se entera — solo recibe Cards válidas o no se construye `Hand`. 🟢
- **Auto-rebuy detectado en `HandRecord`:** `NetProfit = (HeroStackEnd - HeroStackStart) - AutoRebuy + BlindPosted` excluye dinero propio del PnL. 🟢 (`GameSession.cs:92`, ADR-0013)

---

## Dependências

### Externas (NuGet)

**Ninguna.** `OpenScrape.Domain.csproj` no referencia paquetes. Esto es deliberado (Clean Architecture). 🟢

### Proyectos del solution

**Ninguna.** El módulo es la raíz del grafo de dependencias. `Features`, `DecisionMaker`, `Infrastructure`, `App` dependen de él, no al revés. 🟢

### Acoplamientos internos del módulo

```
Entities/StrategyProfile        usa  ValueObjects/StreetThresholds
Entities/GameSession.HandRecord usa  Enums/{HandResult, BoardPosition, HandSituation, TablePosition}
                                     ValueObjects/{StreetDecision, TelemetryAggregate}
Entities/OpponentProfile        usa  Enums/{OpponentType, TablePosition}
ValueObjects/VillainRange       usa  Entities/OpponentProfile         🟡 acoplamiento inverso
ValueObjects/ThresholdKey       usa  Enums/{BoardPosition, HandSituation}
ValueObjects/HandStrenght       declara `using OpenScrape.Domain.Entities;` 🟡 import muerto
Mappers/CardDTOMapper           usa  Entities/Card  +  Dtos/CardDTO
Mappers/TableDTOMapper          usa  Entities/Table +  Dtos/TableDTO
```

🟡 `VillainRange` (en `ValueObjects/`) depende de `OpponentProfile` (en `Entities/`). Acoplamiento de capa inverso (los VOs no deberían depender de Entities). Documentado en `legacy-mapping.md` § 3.

---

## Decisões de Design Identificadas

| Decisión | Evidencia en el código | Confianza |
|----------|------------------------|:---------:|
| **Sin dependencias externas en Domain** (Clean Architecture pura) | `OpenScrape.Domain.csproj` no tiene `<PackageReference>` ni `<ProjectReference>` | 🟢 |
| **Records inmutables para value objects en hot path** (thread safety + zero-cost copy) | `Hand.cs`, `StreetDecision.cs`, `StreetThresholds.cs`, `ThresholdKey.cs` usan `record` con `init` | 🟢 |
| **Enums `byte` para tipos que viajan en Monte Carlo** | `Enums/OutsDataEnum.cs` declara `Rank`, `Suit`, `HandRank`, `KickerStrength` como `byte` | 🟢 |
| **Fail-fast en construcción** vs validación tardía | `Hand.cs:5-11`, `ThresholdKey.cs:17-25` lanzan en init, no en uso | 🟢 |
| **Marten document = `class` mutable con `Id` string Guid** | `GameSession.cs:13`, `HandRecord.cs:59`, `StrategyProfile.cs:11` | 🟢 |
| **`HandRecord` con FK explícita a `GameSession`** en lugar de embeber | `HandRecord.GameSessionId` (`Entities/GameSession.cs:62`) | 🟢 |
| **`[JsonIgnore]` para colecciones derivadas** evita duplicación en Marten | `GameSession.Hands`, `TotalHands`, `TotalProfit`, `BBPer100` | 🟢 |
| **`StreetThresholds` como `record` configurado vía JSON binding** | `Microsoft.Extensions.Configuration` enlaza por `init` properties | 🟢 |
| **`ThresholdKey.TryParse` con `out` en lugar de excepción** para parseo robusto | `ThresholdKey.cs:36-77` retorna `bool` + `error` | 🟢 |
| **AF con Laplace smoothing `(a+1)/(p+1)`** evita división por cero y regresa a AF=1 con pocas muestras | `OpponentProfile.cs:99-103`, ADR-0011 | 🟢 |
| **AF posicional separado IP/OOP** (4 contadores en `OpponentProfile` + 4 en `OpponentPositionProfile`) | `OpponentProfile.cs:70-73`, `OpponentPositionProfile.cs:14-17` | 🟢 |
| **Sentinel `-1` para `AF` con datos insuficientes** (vs `Nullable<double>`) | `OpponentProfile.cs:26-37` | 🟢 |
| **`StrategyProfile.Validate()` con 18 reglas cruzadas** disparado por `IValidateOptions<StrategyProfile>` | `Exceptions/StrategyProfileValidationException.cs`, ADR-0008 | 🟢 |
| **`NetProfit` calculado** restando `AutoRebuy` y sumando `BlindPosted` | `GameSession.cs:92`, ADR-0013 | 🟢 |
| **`OpponentProfile.GetTypeForPosition(bool villainIsIP)`** elige el AF correcto en runtime | `OpponentProfile.cs` (ver método) | 🟢 |
| **Tipos legacy 🟡 conservados** (no eliminados) para no romper código que aún los referencie | `Styles`, `HeroHand`, `GameSituation`, `ActionsResponse`, `ListRegions` | 🟡 |
| **Mensajes de error en castellano** (consistente con doc_language) | `Hand.cs:7`, `ThresholdKey.cs:18-26` | 🟢 |
| **`InternalsVisibleTo OpenScrape.App.Tests`** permite testing de tipos `internal` | `OpenScrape.Domain.csproj` | 🟢 |
| **`record sealed ThresholdKey`** previene herencia accidental | `ThresholdKey.cs:10` | 🟢 |
| **`StreetThresholds` con `~30 init` props** en lugar de objeto anidado | `StreetThresholds.cs` — flat para JSON binding directo | 🟢 |

---

## Estado Interno

`Domain` no mantiene estado global. Los únicos campos "estado" viven dentro de instancias:

- **`GameSession.Hands`** — lista en memoria de hasta 20 últimas manos. `[JsonIgnore]`. Cada `HandRecord` se persiste por separado en Marten. Truncamiento gestionado por `OpenScrape.App.Services.GameLoggerService`. 🟢
- **`GameSession.PeakBankroll`** — máximo histórico de bankroll observado en la sesión. Mutable durante la sesión activa. 🟢
- **`OpponentProfile`** — contadores que crecen monotónicamente durante una sesión. ~30 contadores cubriendo VPIP, PFR, 3Bet, postflop por posición, c-bet, donk, check-raise, showdown, barrel. 🟢
- **`HandRecord` durante la mano** — mutable mientras la mano está en curso. Inmutable conceptualmente al persistirse (no hay método `UpdateHand`). 🟢

Todas las propiedades calculadas (`VPIP`, `PFR`, `AF*`, `BBPer100`, `NetProfit`, `TotalProfit`, `TotalHands`, `IsValid`) son **lazy** — derivadas de campos en cada acceso. 🟢

---

## Observabilidade

`Domain` no emite logs ni métricas. Lo "observable" desde la capa Domain es:

- **`TelemetryAggregate`** (`ValueObjects/TelemetryAggregate.cs`) — record que `OpenScrape.App` rellena con métricas por mano (latencia OCR, latencia decisión, latencia render). Domain solo modela el tipo. 🟢
- **`CategoryStats`** (`ValueObjects/CategoryStats.cs`) — agregado por situación (OpenRaise, ThreeBet, …). Se calcula en App, Domain solo modela. 🟢
- **`SessionStatsDto`** (`Dtos/SessionStatsDto.cs`) — proyección leíble del `GameSession` para el panel "Historial". 🟢
- **`BankrollSnapshot/Stats/HistoryItem`** — agregados expuestos a la UI, sin lógica de cálculo en Domain. 🟢

Logs (estructurados con `ILogger`, ADR-0009) viven en `App` y `DecisionMaker`. Marten emite SQL log con su propio mecanismo, configurado en `Infrastructure`.

---

## Riscos e Lacunas

- 🟡 **`VillainRange` depende de `OpponentProfile`** — acoplamiento de capa inverso. Si se reconstruye con dominios separados (`Domain.Strategy`), debe extraerse `VillainRange` o introducirse un puerto.
- 🟡 **`HandStrenght.cs` (typo) declara import muerto a `OpenScrape.Domain.Entities;`** — eliminable.
- 🟡 **`ListRegions` static class** — duplica `RegionTableMap` + `RegionLookupCache`. Candidato a borrado tras verificación de referencias.
- 🟡 **`HeroHand`, `GameSituation`, `Styles` enums** — paralelos a tipos vigentes (`HandRank`, `HandSituation`, `StrategyProfile`). Confirmar referencias antes de borrar.
- 🟡 **`ActionsResponse` clase POCO en carpeta `Enums/`** — categoría incorrecta. Verificar si se usa o es legacy.
- 🟡 **Mutabilidad de `StrategyProfile`** (~60 setters públicos) — riesgo de mutación post-validate. Considerar congelarlo (`record sealed` con `init`) tras la validación inicial. Hoy no hay protección.
- 🟡 **`HandRecord` mutable + persistido** — Marten permite update y la lógica del game loop muta `HandRecord` durante la mano. Si una mano se persiste antes de tiempo y luego se actualiza, hay 2 escrituras. Verificar: `GameLoggerService.EndHand` debe ser el único momento de persistencia.
- 🔴 **`NetProfit` con `BlindPosted` no incluye All-in posted blinds** — si hero entra all-in con la BB porque no tiene fondos, ¿`BlindPosted` cubre el caso? Falta validación. Pregunta para `questions.md`.
- 🔴 **`AutoRebuy` ≠ deposit manual** — el dominio no distingue. Si el sitio permite top-up manual durante la mano, ¿cuenta como rebuy o como deposit? Pregunta abierta.
- 🔴 **`OpponentProfile` no se persiste** — tracking se pierde al cerrar la app. ¿Es deliberado o falta una colección Marten? Confirmar con el usuario en revisión.
- 🟡 **`StrategyProfileValidationException` no incluye lista de reglas violadas estructurada** — solo string. Si en el futuro se quiere render por UI, faltaría serialización.
