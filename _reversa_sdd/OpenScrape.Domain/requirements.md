# OpenScrape.Domain — Requisitos

> Capa de dominio de la aplicación. Contiene entidades, value objects, enums, mappers y excepciones que modelan el negocio de poker scraping y toma de decisiones. **Sin dependencias externas** — `OpenScrape.Domain.csproj` no referencia ningún paquete NuGet ni proyecto.

---

## Visión General

`OpenScrape.Domain` es la capa más interna de la arquitectura Clean. Define el modelo de datos del sistema (mesa, jugadores, mano, sesión), los value objects de cálculo de equity (`Hand`, `Region`, `StreetThresholds`, `VillainRange`, etc.), las enumeraciones de poker (`Rank`, `Suit`, `HandRank`, `BoardPosition`, `HandSituation`, `TablePosition`) y las invariantes que deben respetarse en cualquier capa superior. Es el contrato canónico que `OpenScrape.Features`, `OpenScrape.DecisionMaker` y `OpenScrape.App` consumen. 🟢 (`src/OpenScrape.Domain/OpenScrape.Domain.csproj`)

---

## Responsabilidades

- **Modelado del estado de juego** — define `Card`, `Table`, `GameSession`, `HandRecord`, `RegionTableMap`, `StrategyProfile`, `OpponentProfile` como documentos de Marten. 🟢
- **Modelado de cálculos** — define value objects inmutables que viajan por el pipeline de decisión: `Hand`, `Region`, `CardDataOuts`, `HandStrength`, `HandEvaluation`, `PotOddsResult`, `StreetDecision`, `StreetThresholds`, `ThresholdKey`, `VillainRange`. 🟢
- **Catálogo de enumeraciones de poker** — `Rank` (2..A, byte), `Suit` (♣♦♥♠), `HandRank` (HighCard..RoyalFlush), `KickerStrength` (None/Weak/Medium/Strong), `BoardPosition` (Hand/Flop/Turn/River), `HandSituation` (OpenRaise, ThreeBet, Squeeze, …), `TablePosition` (Early..BigBlind), `PairClassification`. 🟢
- **Invariantes de dominio** — validaciones inline al construir tipos críticos (Hand, ThresholdKey, StrategyProfile.Validate()). 🟢
- **Mappers DTO** — extension methods estáticos `CardDTOMapper` y `TableDTOMapper` para serializar/deserializar entre `Card`/`Table` y sus DTOs. 🟢
- **Excepciones de dominio** — `StrategyProfileValidationException` se lanza al detectar configuración inválida en `appsettings.json`. 🟢
- **Estadísticas de sesión** — `BankrollSnapshot`, `BankrollStats`, `BankrollHistoryItem`, `CategoryStats`, `TelemetryAggregate` para reportes y dashboard. 🟢

---

## Regras de Negocio

### Invariantes de tipos

- **Hand.Name no puede estar vacío.** Si lo está, el constructor lanza `ArgumentException`. 🟢 (`ValueObjects/Hand.cs:5-7`)
- **Hand.Percentage debe estar en `[0, 100]`.** Fuera de rango → `ArgumentOutOfRangeException`. 🟢 (`ValueObjects/Hand.cs:9-11`)
- **ThresholdKey rechaza `BoardPosition.None` y `BoardPosition.Hand`.** Solo Flop/Turn/River son válidos para postflop. 🟢 (`ValueObjects/ThresholdKey.cs:17-20`)
- **ThresholdKey rechaza `HandSituation.None`.** 🟢 (`ValueObjects/ThresholdKey.cs:22-25`)
- **`StrategyProfile.Validate()` debe ejecutarse al cargar config.** Su fallo lanza `StrategyProfileValidationException` y aborta el arranque (DI). 🟢 (`Exceptions/StrategyProfileValidationException.cs`, ADR-0008)

### Reglas estructurales

- **Card es el catálogo OCR persistente.** Cada carta (52) tiene una imagen referenciada y un nombre canónico (`As`, `Kh`, …). 🟢 (`Entities/Card.cs`)
- **Table representa una mesa lógica.** Tiene `RegionTableMap` asociado para escalado y reconocimiento OCR. 🟢 (`Entities/Table.cs`)
- **GameSession agrupa hands por mesa.** Mantiene en memoria los últimos 20 `HandRecord`; manos antiguas ya están persistidas. Computa `TotalHands`, `TotalProfit`, `BBPer100`. 🟢 (`Entities/GameSession.cs`)
- **HandRecord referencia a `GameSessionId`.** Una mano siempre pertenece a una sesión. 🟢
- **`StreetDecision` es record inmutable.** Una vez registrada en una mano, no se modifica. 🟢 (`ValueObjects/StreetDecision.cs`)
- **`OpponentProfile` mantiene contadores AF separados IP/OOP.** Permite calcular `VillainType` en función de la posición efectiva. 🟢 (`Entities/OpponentProfile.cs`)
- **`VillainRange` se ajusta por `OpponentProfile`.** VPIP escala el ancho de rango (factor `[0.5×, 2.0×]`); 3Bet% ajusta rangos 3bet. 🟢 (`ValueObjects/VillainRange.cs`)

### Reglas de evolución de estado (no en este módulo, ver `state-machines.md`)

- **Sesión:** Activa → Cerrada. No se reabre.
- **Mano:** En curso → Completada (con `HandResult`). No se modifica una vez completada.

### Reglas marcadas como tipos legacy candidatos 🟡

- `Styles` enum (`Default`/`Agresive`/`Pasive`) — typo "Agresive", solo 3 valores genéricos. Pre-`StrategyProfile`. 🟡
- `HeroHand` enum (`Positions.cs:51`) — paralelo a `HandRank`, en español. 🟡
- `GameSituation` enum (`Positions.cs:69`) — paralelo a `HandSituation` con `[Description]`. 🟡
- `ActionsResponse` clase POCO mal categorizada en `Enums/`. 🟡
- `ListRegions` static class — reemplazado por `RegionTableMap` + `RegionLookupCache`. 🟡

---

## Requisitos Funcionales

| ID | Requisito | Prioridade | Critério de Aceite |
|----|-----------|------------|---------------------|
| RF-01 | Toda construcción de `Hand` debe validar nombre no vacío y porcentaje en `[0, 100]`. | Must | Test unitario que pase `("", true, "fold", 0)` y reciba `ArgumentException`; pase `("AKs", true, "raise", 150)` y reciba `ArgumentOutOfRangeException`. |
| RF-02 | Toda construcción de `ThresholdKey` debe rechazar `BoardPosition.None`, `BoardPosition.Hand` y `HandSituation.None`. | Must | Test unitario que cubra los 3 casos prohibidos. |
| RF-03 | `ThresholdKey.TryParse(rawKey)` debe parsear strings con formato `"{BoardPosition}_{HandSituation}"` sin lanzar excepciones. | Must | Para input `"Flop_OpenRaise"` retorna `true` y `key != null`; para input `"Hand_OpenRaise"` retorna `false` con error explicativo. |
| RF-04 | `StrategyProfile.Validate()` debe ejecutarse en startup y abortar la aplicación si falla. | Must | DI registra `IValidateOptions<StrategyProfile>`; arrancar con `appsettings.json` corrupto debe lanzar `StrategyProfileValidationException`. |
| RF-05 | `GameSession.TotalProfit` debe calcularse a partir de los `HandRecord` persistidos y los hasta 20 en memoria. | Must | Test que añada 25 manos y verifique que `TotalProfit` agrega los 5 más antiguos persistidos + los 20 en memoria. |
| RF-06 | `GameSession.BBPer100` debe calcularse como `(TotalProfit / BigBlind) * 100 / TotalHands`. | Should | Test con BB=2, profit=40, hands=20 → debe retornar 100 BB/100. |
| RF-07 | `Card`, `Table`, `RegionTableMap`, `StrategyProfile` y `GameSession`/`HandRecord` deben ser persistibles como documentos Marten (sin lógica de DB en el dominio). | Must | Marten configurado en `OpenScrape.Infrastructure` los identifica vía propiedad `Id`. |
| RF-08 | `OpponentProfile.GetTypeForPosition(bool villainIsIP)` debe retornar el `OpponentType` calculado con AF posicional adecuado. | Must | Test con perfil que tiene AF IP=3.5 y AF OOP=1.0; con `villainIsIP=true` retorna LAG, con `villainIsIP=false` retorna TP. |
| RF-09 | `VillainRange` debe ofrecer rangos estáticos por `HandSituation` y rangos adaptativos por `OpponentProfile` (VPIP/3Bet%). | Must | Test que verifica que VPIP=40 ensancha el rango ≥ 1.5× respecto a VPIP=20. |
| RF-10 | `CardDTOMapper.ToDTO()` y `TableDTOMapper.ToDTO()` deben ser deterministas (mismo input ⇒ mismo output). | Must | Test que llame al mapper 100 veces con la misma `Card` y verifique igualdad estructural en cada output. |
| RF-11 | Los enums críticos (`HandRank`, `Rank`, `Suit`, `KickerStrength`, `PairClassification`) deben ser `byte` para permitir empaquetado eficiente en `HandScore`. | Must | Inspección del archivo `Enums/OutsDataEnum.cs`. |
| RF-12 | Tipos `record` (`Hand`, `Region`, `StreetDecision`, `StreetThresholds`, `ThresholdKey`, `CategoryStats`, `TelemetryAggregate`) deben ser **inmutables** (sin setters mutables). | Must | Inspección de cada archivo: solo `init` o getters. |
| RF-13 | El módulo NO debe tener dependencias NuGet ni a otros proyectos del solution. | Must | `OpenScrape.Domain.csproj` solo declara `Nullable` y `InternalsVisibleTo OpenScrape.App.Tests`. |

---

## Requisitos No Funcionales

| Tipo | Requisito inferido | Evidencia en el código | Confianza |
|------|--------------------|------------------------|-----------|
| Performance | Tipos crítica del hot path (`Hand`, `HandScore`, `Card`, `Region`) deben evitar boxing/heap alloc en bucles MC. | `Enums/OutsDataEnum.cs` usa `byte` underlying; `HandStrenght.cs` (sic) comenta optimización para MC | 🟢 |
| Performance | Records con `init` permiten copy-on-write sin sincronización (thread-safe trivialmente). | `ValueObjects/StreetDecision.cs`, `Hand.cs` | 🟢 |
| Mantenibilidad | Tipos persistidos exponen `Id` como `string` (Guid) compatible con Marten document store. | `Entities/StrategyProfile.cs:11`, `Entities/GameSession.cs` | 🟢 |
| Robustez | Validación inline en constructores garantiza imposibilidad de tener instancias inválidas en memoria. | `Hand.cs:5-11`, `ThresholdKey.cs:17-25` | 🟢 |
| Trazabilidad | `StrategyProfileValidationException` es **sealed** y específica del dominio — no se confunde con excepciones genéricas. | `Exceptions/StrategyProfileValidationException.cs` | 🟢 |
| Internacionalización | Mensajes de error de validación en castellano (consistente con `doc_language=Español`). | `Hand.cs:7`, `ThresholdKey.cs:18-26` | 🟢 |
| Seguridad | El dominio **no** maneja secrets, conexión a DB ni autenticación — pertenecen a Infrastructure. | `OpenScrape.Domain.csproj` sin referencias | 🟢 |
| Compatibilidad | Targeting `.NET 10.0` con `<Nullable>enable</Nullable>` — fuerza disciplina de null en toda capa superior. | `OpenScrape.Domain.csproj` | 🟢 |

---

## Critérios de Aceitação

### Construcción de `Hand` (RF-01)

```gherkin
Dado un nombre de mano no vacío "AKs", suited=true, action="raise", percentage=85
Cuando se construye new Hand("AKs", true, "raise", 85)
Entonces el objeto se crea sin excepción y Hand.Name == "AKs"

Dado un nombre vacío ""
Cuando se intenta construir new Hand("", null, "fold", 0)
Entonces se lanza ArgumentException con mensaje "El nombre de la mano no puede estar vacío."

Dado un percentage = 150
Cuando se intenta construir new Hand("AKs", true, "raise", 150)
Entonces se lanza ArgumentOutOfRangeException con mensaje "Percentage debe estar entre 0 y 100, valor: 150"
```

### Construcción de `ThresholdKey` (RF-02)

```gherkin
Dado BoardPosition.Flop y HandSituation.OpenRaise
Cuando se construye new ThresholdKey(Flop, OpenRaise)
Entonces el objeto se crea sin excepción y ToString() == "Flop_OpenRaise"

Dado BoardPosition.None
Cuando se intenta construir new ThresholdKey(None, OpenRaise)
Entonces se lanza ArgumentException con mensaje que mencione "Flop, Turn o River"

Dado BoardPosition.Hand (preflop)
Cuando se intenta construir new ThresholdKey(Hand, OpenRaise)
Entonces se lanza ArgumentException
```

### Parseo de string a `ThresholdKey` (RF-03)

```gherkin
Dado el string "Turn_ThreeBet"
Cuando se llama ThresholdKey.TryParse("Turn_ThreeBet", out var key, out var error)
Entonces retorna true, key != null, key.Street == Turn, key.Situation == ThreeBet

Dado el string "Hand_OpenRaise"
Cuando se llama ThresholdKey.TryParse(...)
Entonces retorna false y error contiene "no es válido para un ThresholdKey postflop"

Dado el string "InvalidFormat"
Cuando se llama ThresholdKey.TryParse(...)
Entonces retorna false y error contiene "Formato inválido"
```

### Estadísticas de sesión (RF-05, RF-06)

```gherkin
Dado un GameSession con BigBlind=2 y 20 manos completadas con profit total = 40 BB ($80)
Cuando se accede a session.BBPer100
Entonces retorna 100.0 (40/2 * 100 / 20)

Dado un GameSession sin manos completadas
Cuando se accede a session.BBPer100
Entonces retorna 0 (sin DivByZero)
```

### Adaptación de `OpponentType` por posición (RF-08)

```gherkin
Dado un OpponentProfile con AggressionFactorIP=4.5 y AggressionFactorOOP=0.5
Cuando hero pregunta GetTypeForPosition(villainIsIP=true)
Entonces retorna OpponentType.LAG

Dado el mismo OpponentProfile
Cuando hero pregunta GetTypeForPosition(villainIsIP=false)
Entonces retorna OpponentType.TP (Tight-Passive)
```

---

## Prioridade (MoSCoW)

| Requisito | MoSCoW | Justificación |
|-----------|--------|---------------|
| Validaciones inline en `Hand`, `ThresholdKey` (RF-01, RF-02) | **Must** | Caminho crítico — toda decisión postflop construye `ThresholdKey`. Una invariante rota corrompe todo el motor. |
| `StrategyProfile.Validate()` en startup (RF-04) | **Must** | Sin esto, `appsettings.json` corrupto silencia errores y la app toma decisiones con thresholds basura. |
| Tipos Marten persistibles (RF-07) | **Must** | Sin persistencia, no hay historial, ni backtester, ni opponent tracking entre sesiones. |
| Enums `byte` para hot path (RF-11) | **Must** | Performance Monte Carlo — cambiar a `int` aumenta tamaño de `HandScore` 4× e impacta MC 50K iter. |
| Records inmutables (RF-12) | **Must** | Multithreading en game loop + telemetría: setters mutables introducirían races. |
| Mappers deterministas (RF-10) | **Should** | Determinismo simplifica testing pero el código actual no es no-determinista. |
| `OpponentProfile` con AF posicional (RF-08) | **Should** | Optimiza la decisión cuando hay datos suficientes; sin él, fallback estático funciona. |
| `VillainRange` adaptativo (RF-09) | **Should** | Mejora precisión de equity multi-villain; con rango estático ya hay decisión razonable. |
| Excepción tipada (`StrategyProfileValidationException`) | **Could** | Una `InvalidOperationException` también funciona, pero pierde rastreabilidad. |
| Tipos legacy 🟡 (`Styles`, `HeroHand`, `GameSituation`, `ActionsResponse`, `ListRegions`) | **Won't** | Marcados para descarte; no formar parte del nuevo dominio si se reconstruye. Ver `discard_log.md` (a generar). |

---

## Rastreabilidade de Código

| Archivo | Función / Clase / Tipo | Cobertura |
|---------|------------------------|-----------|
| `src/OpenScrape.Domain/Entities/Card.cs` | `Card` (Marten) | 🟢 |
| `src/OpenScrape.Domain/Entities/Table.cs` | `Table` (Marten) | 🟢 |
| `src/OpenScrape.Domain/Entities/RegionTableMap.cs` | `RegionTableMap` (Marten) | 🟢 |
| `src/OpenScrape.Domain/Entities/GameSession.cs` | `GameSession`, `HandRecord`, `enum HandResult` | 🟢 |
| `src/OpenScrape.Domain/Entities/StrategyProfile.cs` | `StrategyProfile` (~60 parámetros) | 🟢 |
| `src/OpenScrape.Domain/Entities/OpponentProfile.cs` | `OpponentProfile`, `OpponentPositionProfile`, `enum OpponentType` | 🟢 |
| `src/OpenScrape.Domain/Entities/OverlayConfig.cs` | `OverlayConfig` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/Hand.cs` | `record Hand` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/Region.cs` | `record Region` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/CardDataOuts.cs` | `class CardDataOuts`, `class DrawProbability` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/HandStrenght.cs` (typo) | `class HandStrength` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/HandEvaluation.cs` | `class HandEvaluation` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/PotOddsResult.cs` | `class PotOddsResult` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/StreetDecision.cs` | `record StreetDecision` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs` | `record StreetThresholds` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/ThresholdKey.cs` | `record ThresholdKey` + `TryParse` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/VillainRange.cs` | `class VillainRange` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/PlayerActionSequence.cs` | `record PlayerActionSequence` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/BankrollSnapshot.cs` | `BankrollSnapshot`, `BankrollStats`, `BankrollHistoryItem` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/CategoryStats.cs` | `record CategoryStats` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/TelemetryAggregate.cs` | `record TelemetryAggregate` | 🟢 |
| `src/OpenScrape.Domain/Enums/OutsDataEnum.cs` | `Rank`, `Suit`, `HandRank`, `KickerStrength` (todos `byte`) | 🟢 |
| `src/OpenScrape.Domain/Enums/Positions.cs` | `Positions`, `TablePosition`, `HandSituation`, `BoardPosition`, `PairClassification`, `EnumExtensions` | 🟢 |
| `src/OpenScrape.Domain/Enums/Positions.cs:51` | `HeroHand` 🟡 candidato legacy | 🟡 |
| `src/OpenScrape.Domain/Enums/Positions.cs:69` | `GameSituation` 🟡 candidato legacy | 🟡 |
| `src/OpenScrape.Domain/Enums/Styles.cs` | `Styles` 🟡 candidato legacy | 🟡 |
| `src/OpenScrape.Domain/Enums/BluffConditionType.cs` | `BluffConditionType` | 🟢 |
| `src/OpenScrape.Domain/Enums/ActionsResponse.cs` | `ActionsResponse` 🟡 mal categorizada (clase POCO) | 🟡 |
| `src/OpenScrape.Domain/Enums/ListRegions.cs` | `ListRegions` 🟡 reemplazada por `RegionTableMap` | 🟡 |
| `src/OpenScrape.Domain/Mappers/CardDTOMapper.cs` | `static CardDTOMapper` (extension methods) | 🟢 |
| `src/OpenScrape.Domain/Mappers/TableDTOMapper.cs` | `static TableDTOMapper` (extension methods) | 🟢 |
| `src/OpenScrape.Domain/Dtos/CardDTO.cs` | `class CardDTO` | 🟢 |
| `src/OpenScrape.Domain/Dtos/TableDTO.cs` | `class TableDTO` | 🟢 |
| `src/OpenScrape.Domain/Dtos/SessionStatsDto.cs` | `record SessionStatsDto` | 🟢 |
| `src/OpenScrape.Domain/Exceptions/StrategyProfileValidationException.cs` | `sealed class StrategyProfileValidationException` | 🟢 |

> Cobertura: **33 archivos `.cs`** documentados, ~50 tipos públicos expuestos, **5 tipos 🟡** marcados para descarte/recategorización.
