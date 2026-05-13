# Legacy Mapping — `OpenScrape.Domain`

> Mapeo de archivos del legado que componen este módulo, con referencia directa a paths y tipos.
> Generado por el Arqueólogo del Reversa.

## 1. Estructura física

```
src/OpenScrape.Domain/
├── OpenScrape.Domain.csproj         net10.0, Nullable enabled, InternalsVisibleTo OpenScrape.App.Tests
├── Dtos/
│   ├── CardDTO.cs                   class CardDTO
│   ├── TableDTO.cs                  class TableDTO
│   └── SessionStatsDto.cs           record SessionStatsDto
├── Entities/
│   ├── Card.cs                      class Card (📂 Marten)
│   ├── Table.cs                     class Table (📂 Marten)
│   ├── RegionTableMap.cs            class RegionTableMap (📂 Marten)
│   ├── OverlayConfig.cs             class OverlayConfig
│   ├── GameSession.cs               class GameSession (📂 Marten), HandRecord (📂 Marten), enum HandResult
│   ├── StrategyProfile.cs           class StrategyProfile (📂 Marten)
│   └── OpponentProfile.cs           class OpponentProfile, OpponentPositionProfile, enum OpponentType
├── Enums/
│   ├── ActionsResponse.cs           class ActionsResponse 🟡 (mal categorizada)
│   ├── BluffConditionType.cs        enum BluffConditionType
│   ├── ListRegions.cs               static class ListRegions 🟡 (mal categorizada)
│   ├── OutsDataEnum.cs              enum Rank/Suit/HandRank/KickerStrength
│   ├── Positions.cs                 enum Positions/TablePosition/HandSituation/BoardPosition/HeroHand/GameSituation/PairClassification + EnumExtensions
│   └── Styles.cs                    enum Styles 🟡 (legacy candidate)
├── Mappers/
│   ├── CardDTOMapper.cs             static class CardDTOMapper (extension methods)
│   └── TableDTOMapper.cs            static class TableDTOMapper (extension methods)
├── ValueObjects/
│   ├── BankrollSnapshot.cs          class BankrollSnapshot, BankrollStats, BankrollHistoryItem
│   ├── CardDataOuts.cs              class CardDataOuts, DrawProbability
│   ├── CategoryStats.cs             record CategoryStats
│   ├── Hand.cs                      record Hand
│   ├── HandEvaluation.cs            class HandEvaluation
│   ├── HandStrenght.cs              class HandStrength (typo en filename)
│   ├── PlayerActionSequence.cs      record PlayerActionSequence
│   ├── PotOddsResult.cs             class PotOddsResult
│   ├── Region.cs                    record Region
│   ├── StreetDecision.cs            record StreetDecision
│   ├── StreetThresholds.cs          record StreetThresholds
│   ├── TelemetryAggregate.cs        record TelemetryAggregate
│   ├── ThresholdKey.cs              record ThresholdKey
│   └── VillainRange.cs              class VillainRange
└── Exceptions/
    └── StrategyProfileValidationException.cs   sealed class
```

## 2. Tipos por categoría → archivos

### Documentos persistidos en Marten 📂

| Tipo | Archivo | Notas |
|------|---------|-------|
| `Card` | `Entities/Card.cs` | Catálogo OCR |
| `Table` | `Entities/Table.cs` | Mesa lógica |
| `RegionTableMap` | `Entities/RegionTableMap.cs` | Mapas de regiones |
| `GameSession` | `Entities/GameSession.cs` | Sesión por mesa |
| `HandRecord` | `Entities/GameSession.cs:56` | Mano (FK GameSessionId) |
| `StrategyProfile` | `Entities/StrategyProfile.cs` | Configuración estrategia |

### Value Objects (no persistidos directamente)

| Tipo | Archivo | Tipo C# |
|------|---------|---------|
| `Hand` | `ValueObjects/Hand.cs` | record |
| `Region` | `ValueObjects/Region.cs` | record |
| `PlayerActionSequence` | `ValueObjects/PlayerActionSequence.cs` | record |
| `CardDataOuts` | `ValueObjects/CardDataOuts.cs` | class |
| `DrawProbability` | `ValueObjects/CardDataOuts.cs:19` | class (en mismo archivo) |
| `HandStrength` | `ValueObjects/HandStrenght.cs` | class (typo "Strenght") |
| `HandEvaluation` | `ValueObjects/HandEvaluation.cs` | class |
| `PotOddsResult` | `ValueObjects/PotOddsResult.cs` | class |
| `StreetDecision` | `ValueObjects/StreetDecision.cs` | record |
| `StreetThresholds` | `ValueObjects/StreetThresholds.cs` | record |
| `ThresholdKey` | `ValueObjects/ThresholdKey.cs` | record |
| `VillainRange` | `ValueObjects/VillainRange.cs` | class |
| `BankrollSnapshot` | `ValueObjects/BankrollSnapshot.cs` | class |
| `BankrollStats` | `ValueObjects/BankrollSnapshot.cs:13` | class (en mismo archivo) |
| `BankrollHistoryItem` | `ValueObjects/BankrollSnapshot.cs:32` | class (en mismo archivo) |
| `CategoryStats` | `ValueObjects/CategoryStats.cs` | record |
| `TelemetryAggregate` | `ValueObjects/TelemetryAggregate.cs` | record |

### Enums

| Enum | Archivo | Tipo subyacente |
|------|---------|-----------------|
| `Rank` | `Enums/OutsDataEnum.cs` | byte |
| `Suit` | `Enums/OutsDataEnum.cs` | byte |
| `HandRank` | `Enums/OutsDataEnum.cs` | byte |
| `KickerStrength` | `Enums/OutsDataEnum.cs` | byte |
| `Positions` | `Enums/Positions.cs` | int |
| `TablePosition` | `Enums/Positions.cs` | int |
| `HandSituation` | `Enums/Positions.cs` | int |
| `BoardPosition` | `Enums/Positions.cs` | int |
| `HeroHand` | `Enums/Positions.cs:51` | int 🟡 candidate legacy |
| `GameSituation` | `Enums/Positions.cs:69` | int 🟡 candidate legacy (paralelo a HandSituation) |
| `PairClassification` | `Enums/Positions.cs:98` | byte |
| `Styles` | `Enums/Styles.cs` | int 🟡 candidate legacy |
| `BluffConditionType` | `Enums/BluffConditionType.cs` | int |
| `HandResult` | `Entities/GameSession.cs:117` | int |
| `OpponentType` | `Entities/OpponentProfile.cs:303` | int |

### Excepciones

| Tipo | Archivo |
|------|---------|
| `StrategyProfileValidationException` | `Exceptions/StrategyProfileValidationException.cs` |

### Clases utility mal categorizadas 🟡

| Tipo | Archivo | Problema |
|------|---------|----------|
| `ActionsResponse` | `Enums/ActionsResponse.cs` | Es una **clase POCO**, no un enum. Categoría incorrecta. |
| `ListRegions` | `Enums/ListRegions.cs` | Es una **static class** con `List<string>`, no un enum. Probablemente legacy: el sistema actual usa `RegionTableMap` desde Marten + `RegionLookupCache`. |

## 3. Recomendaciones para migración

### Tipos a evaluar para descarte (`discard_log.md`)

Estos tipos parecen no usarse o estar duplicados. Confirmar buscando referencias en `OpenScrape.Features`, `OpenScrape.DecisionMaker` y `OpenScrape.App` antes de retirar:

1. **`Styles` enum** (`Enums/Styles.cs`) — solo 3 valores genéricos (`Default`/`Agresive`/`Pasive`), typo "Agresive" presente. Posible legacy pre-DecisionMaker.
2. **`HeroHand` enum** (`Enums/Positions.cs:51`) — en español, paralelo a `HandRank` byte. Probablemente reemplazado.
3. **`GameSituation` enum** (`Enums/Positions.cs:69`) — paralelo a `HandSituation` con `[Description]`. Solapamiento confuso. Pista: usa `GetDescription()` extension.
4. **`ActionsResponse` clase** (`Enums/ActionsResponse.cs`) — categoría incorrecta + parece DTO antiguo.
5. **`ListRegions` static class** (`Enums/ListRegions.cs`) — hardcoded list de 41 regiones. Reemplazado por `RegionTableMap` + `RegionLookupCache`.

### Tipos a renombrar / re-categorizar

- `HandStrenght.cs` → renombrar a `HandStrength.cs` (typo).
- Mover `ActionsResponse.cs` y `ListRegions.cs` fuera de `Enums/` (nueva categoría utility o quitar si legacy).

### Tipos a refactorizar (acoplamiento de capa inverso)

- `VillainRange` (en `ValueObjects/`) depende de `OpponentProfile` (en `Entities/`). Considerar mover `VillainRange` a una nueva capa `Domain.Strategy/` o mantener acoplamiento documentándolo.
- `HandStrength` declara `using OpenScrape.Domain.Entities;` pero no lo usa — eliminar import muerto.

### Validaciones inline a preservar

Estas validaciones inline son contratos del dominio y deben mantenerse en cualquier refactor:

- `Hand.Name` no vacío + `Hand.Percentage ∈ [0, 100]` (record init throw).
- `ThresholdKey` rechaza `BoardPosition.None`/`Hand` y `HandSituation.None`.
- `StrategyProfile.Validate()` con sus 18 reglas cruzadas.

## 4. Métricas

| Métrica | Valor |
|---:|:--|
| Archivos `.cs` (excluyendo `obj/`) | 33 |
| Archivos por carpeta | Dtos=3, Entities=7, Enums=6, Mappers=2, ValueObjects=14, Exceptions=1 |
| Tipos públicos | ~50 |
| Documentos Marten | 6 (Card, Table, RegionTableMap, GameSession, HandRecord, StrategyProfile) |
| Records (immutables) | 9 |
| Classes | 23 |
| Enums | 14 |
| Static classes | 4 (CardDTOMapper, TableDTOMapper, ListRegions, EnumExtensions) |
| Excepciones | 1 |
| Validaciones inline | 3 (Hand, ThresholdKey, StrategyProfile.Validate) |
| Algoritmos no triviales | 8 |
| Reglas de negocio extraídas | 29 |
| Dependencias externas | 0 |
