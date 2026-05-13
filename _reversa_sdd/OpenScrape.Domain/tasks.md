# OpenScrape.Domain — Tareas de Implementación

> Secuencia de tareas para reimplementar la capa de dominio a partir del legado, con rastreabilidad línea a línea.

---

## Pré-requisitos

- [ ] .NET 10 SDK instalado (sin paquetes externos requeridos en este módulo)
- [ ] Convención de naming acordada con `doc_language=Español` (mensajes de error, comentarios XML)
- [ ] Decisión sobre **tipos legacy** revisada: ¿se descartan `Styles`, `HeroHand`, `GameSituation`, `ActionsResponse`, `ListRegions` o se conservan? (ver `_reversa_sdd/OpenScrape.Domain/questions.md`)
- [ ] Decisión sobre **acoplamiento `VillainRange ↔ OpponentProfile`** revisada (mover `VillainRange` o introducir puerto)

---

## Tareas

> Cada tarea referencia el archivo legado de origen y su número de línea cuando aplica.

### Bloque A — Estructura del proyecto y enums fundacionales

- [ ] **T-01** Crear `OpenScrape.Domain.csproj` con `net10.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, sin `PackageReference`, con `InternalsVisibleTo OpenScrape.App.Tests`.
  - Origen en el legado: `src/OpenScrape.Domain/OpenScrape.Domain.csproj`
  - Critério de pronto: `dotnet build` pasa sin warnings; tests del proyecto `OpenScrape.App.Tests` pueden referenciar tipos `internal`.
  - Confianza: 🟢

- [ ] **T-02** Crear los enums `byte` del hot path en `Enums/OutsDataEnum.cs`: `Rank`, `Suit`, `HandRank`, `KickerStrength`.
  - Origen en el legado: `src/OpenScrape.Domain/Enums/OutsDataEnum.cs`
  - Critério de pronto: `sizeof(Rank) == 1`. Test: tarjeta `As♠` se materializa con `Rank.A` y `Suit.Spades` ocupando 2 bytes en una struct empaquetada.
  - Confianza: 🟢

- [ ] **T-03** Crear los enums `int` de posición/situación en `Enums/Positions.cs`: `Positions`, `TablePosition`, `HandSituation`, `BoardPosition`, `PairClassification` (`byte`), `EnumExtensions`.
  - Origen en el legado: `src/OpenScrape.Domain/Enums/Positions.cs`
  - Critério de pronto: `BoardPosition.Hand`, `Flop`, `Turn`, `River` existen; `HandSituation.OpenRaise..Squeeze` cubre los 12 valores documentados.
  - Confianza: 🟢

- [ ] **T-04** Crear `Enums/BluffConditionType.cs` y `Enums/HandResult` (declarado en `Entities/GameSession.cs:117`).
  - Origen en el legado: `src/OpenScrape.Domain/Enums/BluffConditionType.cs`, `src/OpenScrape.Domain/Entities/GameSession.cs:117`
  - Critério de pronto: enums compilan, valores stable IDs.
  - Confianza: 🟢

- [ ] **T-05** Decisión sobre tipos legacy 🟡 `Styles`, `HeroHand`, `GameSituation`, `ActionsResponse`, `ListRegions`. Si se conservan, replicar archivos. Si se descartan, NO crear y registrar en `discard_log.md`.
  - Origen en el legado: `src/OpenScrape.Domain/Enums/Styles.cs`, `Positions.cs:51`, `Positions.cs:69`, `Enums/ActionsResponse.cs`, `Enums/ListRegions.cs`
  - Critério de pronto: decisión documentada en `questions.md`; código legacy aislado o eliminado.
  - Confianza: 🔴 (decisión humana pendiente)

### Bloque B — Value objects inmutables (records)

- [ ] **T-06** Crear `record Hand(string Name, bool? Suited, string Action, int Percentage)` con validaciones inline en `init`.
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/Hand.cs:1-12`
  - Critério de pronto: `new Hand("", null, "fold", 0)` lanza `ArgumentException` con mensaje "El nombre de la mano no puede estar vacío.". `new Hand("AKs", true, "raise", 150)` lanza `ArgumentOutOfRangeException`.
  - Confianza: 🟢

- [ ] **T-07** Crear `record sealed ThresholdKey(BoardPosition Street, HandSituation Situation)` con validación constructor + método estático `TryParse(rawKey, out, out)`.
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/ThresholdKey.cs:1-77`
  - Critério de pronto: `new ThresholdKey(BoardPosition.None, HandSituation.OpenRaise)` lanza `ArgumentException` mencionando "Flop, Turn o River". `ThresholdKey.TryParse("Flop_OpenRaise", out var k, out _)` retorna `true`. `ToString()` produce `"Flop_OpenRaise"`.
  - Confianza: 🟢

- [ ] **T-08** Crear `record StreetThresholds` con ~30 propiedades `init` cubriendo equity tiers, bet sizing por textura, bluff, check-raise, overbet, combo draw, probe bet, modo simplificado.
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs:1-75`
  - Critério de pronto: instancia con valores por defecto compila; binding desde JSON (`Microsoft.Extensions.Configuration`) puebla `FoldBelow`, `ThinValueAbove`, `ValueAbove`, `StrongValueAbove` y los 12+ strings de bet size.
  - Confianza: 🟢

- [ ] **T-09** Crear `record StreetDecision` capturando equity, pot odds, EV, acción recomendada, acción tomada, pot size, bet size, situación, posición y campos opcionales (`Reason`, `BoardTexture`, `TotalOuts`, `SPR`).
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/StreetDecision.cs`
  - Critério de pronto: persistible como propiedad embebida en `HandRecord.Decisions`; igualdad estructural confirmada por test.
  - Confianza: 🟢

- [ ] **T-10** Crear `record Region(string Name, int X, int Y, int Width, int Height)` (forma final inferida del legado).
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/Region.cs`
  - Critério de pronto: serializable; usado por `RegionTableMap`.
  - Confianza: 🟡 (verificar firma exacta antes de implementar)

- [ ] **T-11** Crear los records auxiliares: `PlayerActionSequence`, `CategoryStats`, `TelemetryAggregate`.
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/{PlayerActionSequence,CategoryStats,TelemetryAggregate}.cs`
  - Critério de pronto: cada record compila aislado; sin lógica embebida.
  - Confianza: 🟢

### Bloque C — Value objects mutables (clases)

- [ ] **T-12** Crear `class CardDataOuts` y `class DrawProbability` (en mismo archivo).
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/CardDataOuts.cs:1-19+`
  - Critério de pronto: clase con propiedades mutables; sin lógica más allá de holders.
  - Confianza: 🟢

- [ ] **T-13** Crear `class HandStrength`. **Nota:** filename del legado tiene typo (`HandStrenght.cs`) — corregir o conservar según política del proyecto.
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/HandStrenght.cs`
  - Critério de pronto: clase sin imports muertos (eliminar `using OpenScrape.Domain.Entities;` no utilizado).
  - Confianza: 🟢

- [ ] **T-14** Crear `class HandEvaluation` y `class PotOddsResult`.
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/{HandEvaluation,PotOddsResult}.cs`
  - Critério de pronto: clases compilan; campos coinciden con legado.
  - Confianza: 🟢

- [ ] **T-15** Crear `class VillainRange` con rangos estáticos por `HandSituation` y método de ajuste por `OpponentProfile` (VPIP escala ancho `[0.5×, 2.0×]`, 3Bet% ajusta rangos 3bet).
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/VillainRange.cs`
  - Critério de pronto: test verifica que `OpponentProfile { VPIP=40 }` produce rango ≥ 1.5× respecto a VPIP=20.
  - Confianza: 🟢
  - **Bloqueo:** decisión sobre acoplamiento `VillainRange → OpponentProfile` (T-26).

- [ ] **T-16** Crear `class BankrollSnapshot`, `BankrollStats`, `BankrollHistoryItem` (en el mismo archivo).
  - Origen en el legado: `src/OpenScrape.Domain/ValueObjects/BankrollSnapshot.cs:1-32+`
  - Critério de pronto: clases compilan; usadas por dashboard.
  - Confianza: 🟢

### Bloque D — Documentos persistidos (Marten)

- [ ] **T-17** Crear `class Card { Id, … }` con campos OCR y nombre canónico.
  - Origen en el legado: `src/OpenScrape.Domain/Entities/Card.cs`
  - Critério de pronto: 52 cartas materializables; `Id` string Guid único.
  - Confianza: 🟢

- [ ] **T-18** Crear `class Table` con referencia a `RegionTableMap` y configuración por mesa.
  - Origen en el legado: `src/OpenScrape.Domain/Entities/Table.cs`
  - Critério de pronto: persistible en Marten; lookup por nombre.
  - Confianza: 🟢

- [ ] **T-19** Crear `class RegionTableMap` con regiones (List<Region>) y mapeo a coordenadas escaladas.
  - Origen en el legado: `src/OpenScrape.Domain/Entities/RegionTableMap.cs`
  - Critério de pronto: 41+ regiones cargables desde `Data/Regiones.json`; lookup por nombre acelerado por `RegionLookupCache` en App.
  - Confianza: 🟢

- [ ] **T-20** Crear `class GameSession` con `Id`, `SessionId`, `TableName`, `StartTime`, `EndTime`, `BigBlind`, bankroll fields y propiedades calculadas con `[JsonIgnore]` (`Hands`, `TotalHands`, `TotalProfit`, `BBPer100`, `IsValid`).
  - Origen en el legado: `src/OpenScrape.Domain/Entities/GameSession.cs:1-50`
  - Critério de pronto: Marten persiste sin duplicar `Hands`. Test: con BB=2 y 20 manos profit total 40 BB ($80), `BBPer100 == 100.0`. Sin manos ⇒ retorna `0` (sin DivByZero).
  - Confianza: 🟢

- [ ] **T-21** Crear `class HandRecord` con `Id`, `GameSessionId` (FK), `HandNumber`, hero state (cards, position, stack start/end), `BlindPosted`, `AutoRebuy`, propiedad calculada `NetProfit`, board (`FlopCards`, `TurnCard?`, `RiverCard?`), `List<StreetDecision> Decisions`, `Result`, `Situation`, `Telemetry?`.
  - Origen en el legado: `src/OpenScrape.Domain/Entities/GameSession.cs:52-115`
  - Critério de pronto: `NetProfit = (HeroStackEnd - HeroStackStart) - AutoRebuy + BlindPosted`. Test: HSS=100, HSE=105, AutoRebuy=100, BlindPosted=1 ⇒ NetProfit = 6. ADR-0013.
  - Confianza: 🟢

- [ ] **T-22** Crear `enum HandResult { Unknown, Won, Lost, Push }` en `Entities/GameSession.cs:117`.
  - Origen en el legado: `src/OpenScrape.Domain/Entities/GameSession.cs:117-123`
  - Critério de pronto: 4 valores; default `Unknown`.
  - Confianza: 🟢

- [ ] **T-23** Crear `class StrategyProfile` con ~60 propiedades públicas mutables agrupadas por categoría (Fold Equity, Bet Sizing, Bluff Frequencies, C-Bet Frequencies, Kicker, Danger Penalties, Implied Odds, Range Advantage, Barrel Detection, SPR, Multiway, 3-Bet/4-Bet, Reverse Implied, Bluff Catching, Combo Draw, Tainted Outs, Floating IP, Slow Play, Check-Raise Mixing, BvB, Limp-Raise, Squeeze Defense, Overcard Outs, Broadway Wet, Backdoor Overlap, Randomization, River Runout, Stackoff, Multiway Nut Advantage, Pot Commitment, Hand Re-Eval).
  - Origen en el legado: `src/OpenScrape.Domain/Entities/StrategyProfile.cs:1-300+`
  - Critério de pronto: todas las propiedades con valores default coincidentes con el legado (FoldEquityBase=20.0, FlopBluffFrequency=0.15, CbetFrequencyFlop=0.65, …).
  - Confianza: 🟢

- [ ] **T-24** Implementar `StrategyProfile.Validate()` con las 18 reglas cruzadas (orden de tiers, rangos válidos, bet sizes parseables, frecuencias en `[0,1]`, multiplicadores positivos).
  - Origen en el legado: `src/OpenScrape.Domain/Entities/StrategyProfile.cs:Validate()`
  - Critério de pronto: profile válido pasa; profile con `FoldBelow > ThinValueAbove` lanza `StrategyProfileValidationException`. Tests cubren las 18 reglas.
  - Confianza: 🟢

- [ ] **T-25** Crear `sealed class StrategyProfileValidationException : Exception`.
  - Origen en el legado: `src/OpenScrape.Domain/Exceptions/StrategyProfileValidationException.cs`
  - Critério de pronto: excepción tipada; mensaje en castellano enumera reglas violadas. ADR-0008.
  - Confianza: 🟢

### Bloque E — Entidad mutable de oponente

- [ ] **T-26** Crear `class OpponentPositionProfile` con contadores granulares por posición (`HandsPlayed`, `TimesVPIP`, `TimesPFR`, `TimesAggressive/PassiveIP`, `TimesAggressive/PassiveOOP`) y propiedades calculadas (`VPIP`, `PFR`, `AggressionFactorIP`, `AggressionFactorOOP`, `IsReliable`).
  - Origen en el legado: `src/OpenScrape.Domain/Entities/OpponentProfile.cs:9-41`
  - Critério de pronto: AF retorna `-1` (sentinel) si `Aggressive+Passive < 5`. Con `(Aggressive=2, Passive=1)` retorna `(2+1)/(1+1) = 1.5` (Laplace smoothing). `IsReliable == true` con `HandsPlayed >= 10`.
  - Confianza: 🟢

- [ ] **T-27** Crear `class OpponentProfile` con ~30 contadores (preflop, postflop, c-bet, donk, check-raise, showdown, barrel) y propiedades calculadas (`VPIP`, `PFR`, `ThreeBetPct`, AF posicional). Implementar `GetTypeForPosition(bool villainIsIP)` con fallback a contadores agregados si datos posicionales insuficientes.
  - Origen en el legado: `src/OpenScrape.Domain/Entities/OpponentProfile.cs:43-300`
  - Critério de pronto: con AF IP=4.5, AF OOP=0.5, `GetTypeForPosition(true)` retorna `LAG`; `GetTypeForPosition(false)` retorna `TP`. ADR-0011.
  - Confianza: 🟢

- [ ] **T-28** Crear `enum OpponentType` (LAG, TAG, LP, TP, Unknown) en `OpponentProfile.cs:303`.
  - Origen en el legado: `src/OpenScrape.Domain/Entities/OpponentProfile.cs:303`
  - Critério de pronto: 5 valores.
  - Confianza: 🟢

### Bloque F — DTOs y Mappers

- [ ] **T-29** Crear `class CardDTO`, `class TableDTO`, `record SessionStatsDto` en `Dtos/`.
  - Origen en el legado: `src/OpenScrape.Domain/Dtos/{CardDTO,TableDTO,SessionStatsDto}.cs`
  - Critério de pronto: DTOs sin lógica; serializan a JSON sin acoplarse a Marten.
  - Confianza: 🟢

- [ ] **T-30** Crear `static class CardDTOMapper` y `static class TableDTOMapper` con extension methods (`ToDTO`, `ToEntity`).
  - Origen en el legado: `src/OpenScrape.Domain/Mappers/{CardDTOMapper,TableDTOMapper}.cs`
  - Critério de pronto: round-trip estructural exitoso (Card → DTO → Card preserva campos). Mappers deterministas (mismo input ⇒ mismo output, validable con 100 invocaciones).
  - Confianza: 🟢

### Bloque G — Configuración auxiliar

- [ ] **T-31** Crear `class OverlayConfig` (no persistido, configuración de overlay UI).
  - Origen en el legado: `src/OpenScrape.Domain/Entities/OverlayConfig.cs`
  - Critério de pronto: clase mutable con campos UI (size, opacity, position).
  - Confianza: 🟡 (verificar estructura exacta antes de implementar)

---

## Tarefas de Teste

- [ ] **TT-01** Test del happy path de `Hand` — input válido produce instancia con `Name` igual al pasado y sin excepción. (RF-01)
- [ ] **TT-02** Test del caso de error `Hand` — nombre vacío y percentage fuera de rango lanzan las excepciones esperadas con mensaje en castellano. (RF-01)
- [ ] **TT-03** Test del happy path de `ThresholdKey` — `(Flop, OpenRaise)` produce `ToString() == "Flop_OpenRaise"`. (RF-02)
- [ ] **TT-04** Test del caso de error `ThresholdKey` — `(None, …)`, `(Hand, …)`, `(_, None)` lanzan `ArgumentException`. (RF-02)
- [ ] **TT-05** Test de `ThresholdKey.TryParse` — strings válidos retornan `true`; vacío, malformado, valores fuera de enum retornan `false` con error explicativo. (RF-03)
- [ ] **TT-06** Test de `StrategyProfile.Validate()` — profile válido pasa; profile con `FoldBelow > ThinValueAbove` lanza `StrategyProfileValidationException`. Cubrir las 18 reglas, una por test. (RF-04)
- [ ] **TT-07** Test de `GameSession.BBPer100` — caso BB=2, 20 manos, profit=40 BB ⇒ 100.0. Caso sin manos ⇒ 0 (sin DivByZero). (RF-05, RF-06)
- [ ] **TT-08** Test de `HandRecord.NetProfit` — caso con auto-rebuy: HSS=100, HSE=105, AutoRebuy=100, BlindPosted=1 ⇒ NetProfit = 6. (ADR-0013)
- [ ] **TT-09** Test de `OpponentProfile.GetTypeForPosition` — con AF IP=4.5 / OOP=0.5, IP→LAG, OOP→TP. (RF-08)
- [ ] **TT-10** Test de `OpponentPositionProfile.AggressionFactorIP` — `< 5` muestras retorna `-1`; con `(Agg=2, Pas=1)` retorna `1.5` (Laplace). (RF-08, ADR-0011)
- [ ] **TT-11** Test de `VillainRange` adaptativo — VPIP=40 ensancha rango ≥ 1.5× respecto VPIP=20. (RF-09)
- [ ] **TT-12** Test de mappers deterministas — `Card.ToDTO()` invocado 100 veces con la misma `Card` produce 100 DTOs estructuralmente iguales. (RF-10)
- [ ] **TT-13** Test de inmutabilidad — verificar por reflection que records (`Hand`, `StreetDecision`, `StreetThresholds`, `ThresholdKey`, `Region`, `PlayerActionSequence`, `CategoryStats`, `TelemetryAggregate`) solo tienen setters `init`. (RF-12)
- [ ] **TT-14** Test de tipo subyacente de enums — `sizeof(HandRank) == 1`, `sizeof(Rank) == 1`, `sizeof(Suit) == 1`, `sizeof(KickerStrength) == 1`, `sizeof(PairClassification) == 1`. (RF-11)
- [ ] **TT-15** Test de ausencia de dependencias — leer `OpenScrape.Domain.csproj` y assert `<PackageReference>` count == 0 y `<ProjectReference>` count == 0. (RF-13)

---

## Tarefas de Migração de Dados

> Domain no contiene migrations de DB. La estructura física vive en `OpenScrape.Infrastructure` (Marten) y se versiona allí.

- [ ] **TM-01** Confirmar con `OpenScrape.Infrastructure` que los nombres de propiedad usados aquí coinciden con los esperados por el setup Marten (`StoreOptions.Schema.For<T>()`). Verificar `Card`, `Table`, `RegionTableMap`, `GameSession`, `HandRecord`, `StrategyProfile`.
  - Critério de pronto: smoke test que persiste un `GameSession` con un `HandRecord` y los recupera sin pérdida de campos.

---

## Ordem Sugerida

1. **Bloque A (T-01 → T-05)** primero — sin enums fundacionales no compila el resto.
2. **Bloque B (T-06 → T-11)** — value objects inmutables, dependen solo de enums.
3. **Bloque C (T-12 → T-16)** — value objects mutables. T-15 (`VillainRange`) depende de `OpponentProfile` (T-26/T-27); diferir al final del bloque o tras Bloque E.
4. **Bloque G (T-31)** — config auxiliar puede paralelo.
5. **Bloque D (T-17 → T-25)** — entidades persistidas. T-23 y T-24 (`StrategyProfile.Validate()`) van juntas.
6. **Bloque E (T-26 → T-28)** — `OpponentProfile`, base para T-15.
7. **Bloque F (T-29, T-30)** — DTOs y mappers, último porque dependen de Card/Table.

**Bloqueos clave:**
- T-15 (`VillainRange`) bloqueado por T-26/T-27 (`OpponentProfile`).
- T-21 (`HandRecord`) bloqueado por T-09 (`StreetDecision`) y T-11 (`TelemetryAggregate`).
- T-24 (`StrategyProfile.Validate()`) bloqueado por T-23 (props) y T-25 (excepción).
- T-30 (Mappers) bloqueado por T-17/T-18 (entidades) y T-29 (DTOs).

---

## Lacunas Pendentes (🔴)

> Estas decisiones requieren validación humana antes de implementar.

- 🔴 **`OpponentProfile` no persistido entre sesiones** — ¿es deliberado (privacy/perf) o falta una colección Marten? Decidir antes de T-27. → `questions.md` Q-01
- 🔴 **`AutoRebuy` ≠ deposit manual** — ¿el dominio debe distinguir entre rebuy automático y top-up manual? Afecta `NetProfit`. → `questions.md` Q-02
- 🔴 **All-in posted blind** — si hero entra all-in con la BB porque no tiene fondos, ¿`BlindPosted` cubre el caso? Falta validación. → `questions.md` Q-03
- 🔴 **Tipos legacy 🟡 a descartar o conservar** — `Styles`, `HeroHand`, `GameSituation`, `ActionsResponse`, `ListRegions`. Decisión bloquea T-05 (afecta `discard_log.md` del Curator).
- 🟡 **`VillainRange → OpponentProfile` acoplamiento de capa** — ¿se mueve `VillainRange` a una nueva capa `Domain.Strategy/` o se conserva? Decisión bloquea T-15.
- 🟡 **`HandStrenght.cs` typo en filename** — corregir nombre del archivo (`HandStrength.cs`) o conservar typo por compatibilidad de includes. Decisión bloquea T-13.
- 🟡 **Mutabilidad `StrategyProfile` post-validate** — ¿congelar (clonar a `record sealed`) tras `Validate()` o aceptar mutabilidad continua? Decisión cosmética; no bloquea.
- 🟡 **`StrategyProfileValidationException` con lista estructurada** — ¿incluir `IReadOnlyList<string> ViolatedRules` o solo `Message`? No bloquea, pero impacta UI.
