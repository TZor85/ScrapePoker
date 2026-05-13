# OpenScrape.Domain — Decisiones de Diseño

> Registro de decisiones arquitecturales detectadas en la capa de dominio. Cada decisión cita su evidencia en código y, cuando aplica, su ADR correspondiente en `_reversa_sdd/adrs/`.

---

## DD-01 — Capa Domain sin dependencias externas (Clean Architecture pura)

**Decisión:** `OpenScrape.Domain.csproj` no declara ningún `<PackageReference>` ni `<ProjectReference>`. Es la raíz del grafo de dependencias.

**Contexto:** Proyecto adopta Clean Architecture en 5 capas. Domain debe poder compilarse y testearse de forma totalmente aislada para garantizar que las invariantes del negocio no dependan de detalles tecnológicos (DB, OCR, UI).

**Alternativas consideradas:**
1. Permitir `Marten` en Domain — descartado: acopla DB al núcleo, dificulta testing.
2. Permitir `Microsoft.Extensions.Configuration` en Domain — descartado: el Strategy se valida en startup desde App, Domain solo modela el tipo.

**Consecuencias positivas:**
- 0 conflictos de versiones de NuGet en el núcleo.
- Tests de Domain corren sin Marten, sin .NET Hosting, sin Tesseract.
- Migraciones tecnológicas (cambio de Marten → EF Core, p.ej.) no tocan Domain.

**Consecuencias negativas:**
- Mappers entre `Card` ↔ `CardDTO` deben vivir en Domain pese a ser plumbing (alternativa: en Infrastructure, pero rompería el principio de "Domain conoce sus DTOs").
- Validaciones complejas (`StrategyProfile.Validate()`) viven en Domain pero se invocan vía `IValidateOptions<>` desde App.

**Evidencia:** `src/OpenScrape.Domain/OpenScrape.Domain.csproj` (sin `<PackageReference>`). 🟢

**ADR relacionado:** ADR-0001 (Clean Architecture cinco capas).

---

## DD-02 — Tipos persistidos como `class` mutable, no `record`

**Decisión:** `Card`, `Table`, `RegionTableMap`, `GameSession`, `HandRecord`, `StrategyProfile` son `class` con setters públicos mutables (`{ get; set; }`).

**Contexto:** Marten necesita mutar instancias durante la deserialización (constructor parameterless + setters). Records con `init` solo permiten construcción inmutable, lo cual complica el binding desde el document store.

**Alternativas consideradas:**
1. `record` con `init` y constructor con todos los campos — descartado: setter de Marten requiere mutación post-construcción para campos nuevos en migrations.
2. `record` con factory pattern — descartado: añade indirección sin beneficio claro.

**Consecuencias positivas:**
- Compatibilidad directa con Marten (binding por convención).
- Permite añadir campos en migraciones sin romper documents existentes.

**Consecuencias negativas:**
- Mutabilidad post-construcción no protegida (ej: `GameSession.BigBlind` puede cambiar tras persistirse, ver EC-08).
- `StrategyProfile` tiene ~60 setters expuestos sin protección — ver DD-12.

**Evidencia:** `Entities/GameSession.cs:13`, `Entities/HandRecord.cs:59`, `Entities/StrategyProfile.cs:11`. 🟢

**ADR relacionado:** ADR-0003 (Marten document DB).

---

## DD-03 — Value Objects del hot path como `record` inmutable

**Decisión:** Tipos que viajan por el pipeline de decisión son `record` con propiedades `init`-only: `Hand`, `StreetDecision`, `StreetThresholds`, `ThresholdKey`, `Region`, `PlayerActionSequence`, `CategoryStats`, `TelemetryAggregate`.

**Contexto:** El game loop ejecuta a 1-2 Hz, captura UI desde un thread, calcula equity en otro, renderiza overlay en el principal. Records inmutables eliminan races sin necesidad de locks.

**Alternativas consideradas:**
1. `class` con propiedades `private set` — descartado: requiere boilerplate, sin igualdad estructural.
2. `struct` — descartado: copy semantics caro para records con muchos campos (`StreetThresholds` tiene ~30).

**Consecuencias positivas:**
- Thread-safety sin sincronización: una instancia compartida entre threads no puede ser corrompida.
- Igualdad estructural por defecto: tests más simples, deduplicación natural.
- "Copy with mutation" via `with`: `existingDecision with { Action = "Call" }`.

**Consecuencias negativas:**
- Igualdad estructural por defecto puede ser sorpresiva (ver EC-13).
- Performance: cada modificación crea una nueva instancia (heap alloc), pero records de pocos campos son ~equivalentes a class para GC moderno.

**Evidencia:** `ValueObjects/Hand.cs`, `StreetDecision.cs`, `StreetThresholds.cs`, `ThresholdKey.cs`, etc. 🟢

---

## DD-04 — Enums del hot path con underlying type `byte`

**Decisión:** `Rank`, `Suit`, `HandRank`, `KickerStrength`, `PairClassification` se declaran con `byte` underlying type explícitamente.

**Contexto:** Monte Carlo simula 50K iteraciones flop, 30K preflop. Una struct compacta `HandScore` con `Rank` (1B) + `Suit` (1B) + `HandRank` (1B) cabe en 4 bytes (con padding); con `int` ocuparía 16 bytes.

**Alternativas consideradas:**
1. `int` (default C#) — descartado: 4× tamaño de empaquetado en `HandScore`, impacto medible en 50K iter.
2. `short` — descartado: no aporta sobre `byte` (todos los valores caben en 8 bits) y no es alineamiento natural.

**Consecuencias positivas:**
- `HandScore` struct optimizado para zero-alloc evaluation (`BitHandEvaluator`).
- Cache locality mejor en arrays de `HandScore[]` durante MC.

**Consecuencias negativas:**
- Cast explícito requerido al combinar con `int` (`(int)HandRank.Flush`).
- Si en el futuro se necesitan más de 256 valores, refactor a `int` requerido.

**Evidencia:** `Enums/OutsDataEnum.cs` declara `: byte` para los 4 enums; `Enums/Positions.cs:98` para `PairClassification`. 🟢

**ADR relacionado:** ADR-0010 (Monte Carlo híbrido — depende de `HandScore` compacto).

---

## DD-05 — Validación inline en constructores (fail-fast)

**Decisión:** Los tipos críticos (`Hand`, `ThresholdKey`) validan sus invariantes **en `init`/constructor**, lanzando `ArgumentException` o `ArgumentOutOfRangeException`.

**Contexto:** Un objeto `Hand` con nombre vacío o porcentaje fuera de rango corrompería todo el pipeline downstream (rangos villain, equity, decisión). Detectar la corrupción al consumirlo (10 capas más arriba) es mucho más caro que rechazarla en construcción.

**Alternativas consideradas:**
1. Validación tardía con métodos `IsValid()` — descartado: no garantiza que el código consumidor llame a la validación.
2. Static factory `Hand.Create()` con `Result<Hand>` — descartado: añade indirección y no es idiomático en C# clásico.

**Consecuencias positivas:**
- Imposible tener instancias inválidas en memoria (invariante por construcción).
- Errores en orígenes de datos (JSON malformado) detectados al cargar, no al usar.

**Consecuencias negativas:**
- Construcción puede lanzar — APIs externas (JSON deserializer) deben capturar.
- En tests, "construir un objeto inválido" para probar resilencia downstream requiere reflection o setter via `record with`.

**Evidencia:** `Hand.cs:5-11`, `ThresholdKey.cs:17-25`. 🟢

---

## DD-06 — `ThresholdKey.TryParse` con patrón out-`bool` (no excepción)

**Decisión:** Parseo de string `"{Street}_{Situation}"` se hace via `static bool TryParse(string?, out ThresholdKey?, out string)`, que **nunca lanza** y siempre escribe en `error`.

**Contexto:** Cargar `appsettings.json` puede encontrar claves malformadas. Una excepción durante el binding es difícil de diagnosticar (stack trace genérico de Microsoft.Extensions.Configuration). Un retorno booleano + mensaje explícito permite acumular errores y reportarlos juntos.

**Alternativas consideradas:**
1. Constructor que lanza — descartado: el constructor *sí* lanza (DD-05) para invariantes; `TryParse` es la fachada que evita lanzar en parseo.
2. `Result<ThresholdKey, string>` (Ardalis o similar) — descartado: introduce dependencia en Domain.

**Consecuencias positivas:**
- Validación de configuración acumula múltiples errores antes de fallar startup.
- Compatible con el patrón de `Microsoft.Extensions.Configuration` (`Bind` con error collector).

**Consecuencias negativas:**
- Dos rutas de error para el mismo tipo: `TryParse` (string parsing) vs `new ThresholdKey()` (semantic invariant). El consumidor debe saber cuál usar.

**Evidencia:** `ThresholdKey.cs:36-77`. 🟢

---

## DD-07 — Sentinel `-1` para AF con datos insuficientes

**Decisión:** `OpponentPositionProfile.AggressionFactorIP` retorna `-1` (no `null`, no excepción) cuando `Aggressive + Passive < 5`.

**Contexto:** El consumidor (`PostflopDecisionService.GetVillainType`) debe distinguir entre "AF=0.5 (passive)" y "datos insuficientes". `Nullable<double>` requiere `?` en cada lookup; un sentinel negativo (físicamente imposible para un AF real, que es siempre ≥ 0) permite chequeo simple `af < 0`.

**Alternativas consideradas:**
1. `double?` nullable — descartado: añade `Nullable.HasValue` checks en hot path.
2. Tupla `(bool hasData, double af)` — descartado: verbose para un solo valor.
3. Lanzar excepción — descartado: caso esperado, no error.

**Consecuencias positivas:**
- Chequeo simple `if (af < 0) → fallback`.
- Sin alocación de `Nullable<double>` en hot path.

**Consecuencias negativas:**
- Sentinel mágico no es auto-documentado. Comentario `/// Retorna -1 si datos insuficientes` necesario.
- Si en el futuro se permitiera AF negativo (improbable), el sentinel rompería.

**Evidencia:** `OpponentProfile.cs:26-28, 31-37`. 🟢

**ADR relacionado:** ADR-0011 (OpponentTracker Laplace + reliability).

---

## DD-08 — AF con Laplace smoothing `(a+1)/(p+1)`

**Decisión:** `AggressionFactor = (TimesAggressive + 1) / (TimesPassive + 1)` en lugar de `Aggressive / Passive`.

**Contexto:** Cuando `Passive == 0`, la fórmula clásica explota (divide por cero). Laplace smoothing es un truco bayesiano que añade 1 a numerador y denominador, equivalente a un prior uniforme.

**Alternativas consideradas:**
1. `Passive / max(1, Passive)` — descartado: sigue dando AF=Aggressive cuando Passive=0, sin suavizado real.
2. Si `Passive == 0` retornar valor constante grande — descartado: cliff artificial sin gradiente.
3. Maximum likelihood con regularización L2 — descartado: complejidad innecesaria.

**Consecuencias positivas:**
- AF bien definido para todos los samples ≥ 1.
- AF tiende a 1.0 (neutral) con pocas muestras, suavizando clasificaciones extremas.
- Sin necesidad de chequeo `if (Passive == 0)`.

**Consecuencias negativas:**
- AF "real" subestimado para villains realmente agresivos con muchos samples (`Aggressive=100, Passive=0` da AF=101 en lugar de ∞ — pero el efecto es despreciable más allá de AF≈10).
- Combinado con sentinel `-1` (DD-07): dos protecciones para el mismo problema (datos pocos + datos extremos).

**Evidencia:** `OpponentProfile.cs:99-103`. 🟢

**ADR relacionado:** ADR-0011.

---

## DD-09 — `[JsonIgnore]` para colecciones derivadas en `GameSession`

**Decisión:** `GameSession.Hands`, `TotalHands`, `TotalProfit`, `BBPer100`, `IsValid` están marcadas `[JsonIgnore]` para que Marten no las serialice.

**Contexto:** `Hands` es la colección embebida, pero los `HandRecord` viven en su propia colección Marten con FK `GameSessionId`. Embebrlos en el documento `GameSession` duplicaría datos y crecería sin límite (sesiones de 8h pueden tener 600+ hands).

**Alternativas consideradas:**
1. Embebir hands en `GameSession` (1 documento) — descartado: documentos enormes, latencia de serialización.
2. Solo persistir `HandRecord` y reconstruir `GameSession` por proyección — descartado: requiere proyección Marten compleja, incompatible con migraciones simples.

**Consecuencias positivas:**
- `GameSession` documento liviano (~1 KB), `HandRecord` documento por mano (~5-10 KB).
- Carga lazy: solo se cargan las hands cuando se piden.
- Métricas calculadas en memoria sobre la colección cargada.

**Consecuencias negativas:**
- `GameSession.Hands` está vacía justo después de deserializar — el consumidor debe cargar hands aparte.
- Riesgo: si alguien quita `[JsonIgnore]` de `Hands`, Marten silenciosamente embebería en cada save (ver EC-14).

**Evidencia:** `GameSession.cs:28-49`. 🟢

---

## DD-10 — `HandRecord.NetProfit` con `BlindPosted` y `AutoRebuy`

**Decisión:** `NetProfit = (HeroStackEnd - HeroStackStart) - AutoRebuy + BlindPosted`.

**Contexto:** Profit "naive" (`HSE - HSS`) tiene dos distorsiones:
1. Si hero hace fold en BB (costo: 1 BB), profit aparente = `-1 BB` aunque no hubo decisión voluntaria. Sumar `BlindPosted` lo neutraliza.
2. Si hero recibe auto-rebuy de 100 BB durante la mano, profit aparente sube +100 BB sin que sea ganancia. Restar `AutoRebuy` lo corrige.

**Alternativas consideradas:**
1. Solo restar BB obligatoria — descartado: rebuy aún distorsiona.
2. Solo restar rebuy — descartado: foldear en ciegas aparece como pérdida sistemática.
3. Track separado de "voluntary action" para excluir manos sin decisión — descartado: requiere refactor mayor del game loop.

**Consecuencias positivas:**
- BB/100 refleja decisiones reales del hero, no costos administrativos.
- Comparable entre sesiones con diferentes BB.

**Consecuencias negativas:**
- Cálculo no obvio para auditar. Auditor que revisa "perdiste 5 stack pero NetProfit=+1" debe entender la fórmula.
- Si OCR falla al detectar `AutoRebuy` (queda 0), el profit aparece inflado (ver EC-16).

**Evidencia:** `GameSession.cs:80-92`. 🟢

**ADR relacionado:** ADR-0013 (auto-rebuy 50BB threshold).

---

## DD-11 — `record sealed` para evitar herencia accidental

**Decisión:** `ThresholdKey` se declara `record sealed` (no permite subclases).

**Contexto:** Una subclase de `ThresholdKey` podría sobrescribir `Equals`/`GetHashCode` y romper el contrato de uso como clave en `Dictionary<ThresholdKey, StreetThresholds>`. También podría bypass-ear la validación del constructor con un constructor protegido.

**Alternativas consideradas:**
1. `record` no-sealed — descartado: permite herencia que rompe invariantes.
2. `class sealed` — descartado: sin igualdad estructural por defecto.

**Consecuencias positivas:**
- Imposible romper contrato vía subclase.
- Compilador previene herencia con error CS0509.

**Consecuencias negativas:**
- Si en el futuro se necesita un `ThresholdKeyExtended`, requiere refactor.

**Evidencia:** `ThresholdKey.cs:10`. 🟢

---

## DD-12 — `StrategyProfile` mutable expuesto post-validate (no congelado)

**Decisión:** `StrategyProfile` queda mutable después de `Validate()`. No se clona a un tipo "frozen" inmutable.

**Contexto:** Marten necesita mutar `StrategyProfile` para deserialización y eventual update (cambio de perfil activo). Congelarlo tras validate complicaría el flujo de save/update.

**Alternativas consideradas:**
1. Pattern "builder" — `StrategyProfileBuilder` muta, `Build()` retorna `record sealed` immutable — descartado: refactor mayor + duplicación de tipos.
2. Inmutabilidad por convención (no setters internos) — descartado: rompe binding de `IOptions`.

**Consecuencias positivas:**
- Compatibilidad con `IOptions<StrategyProfile>` y Marten.
- `IOptionsMonitor<StrategyProfile>` puede recargar en runtime sin refactor.

**Consecuencias negativas:**
- 🟡 **Riesgo:** un consumidor descuidado puede mutar `StrategyProfile.FoldEquityBase` durante la mano y romper consistencia.
- No hay contrato técnico que prevenga la mutación post-validate.

**Mitigación recomendada (no implementada):** clonar a `StrategyProfileSnapshot` (record sealed) tras `Validate()` y entregar el snapshot a los consumidores. Pendiente como mejora futura.

**Evidencia:** `Entities/StrategyProfile.cs:11-300+` (todas las props con `{ get; set; }`). 🟢

---

## DD-13 — Tipos legacy 🟡 conservados, no eliminados

**Decisión:** `Styles`, `HeroHand`, `GameSituation`, `ActionsResponse`, `ListRegions` permanecen en el código aunque están marcados como candidatos legacy.

**Contexto:** Eliminar tipos requiere verificar que no hay referencias vivas en `Features`, `DecisionMaker` o `App`. El Archaeologist no completó esta búsqueda exhaustiva.

**Alternativas consideradas:**
1. Eliminar inmediatamente — descartado: riesgo de romper builds o llamadas en runtime no testeadas.
2. Marcar `[Obsolete]` — no aplicado aún, pero recomendado.

**Consecuencias positivas:**
- Build estable mientras la decisión se valida.
- Permite a Curator (en flujo de migración) decidir descarte vs preservación.

**Consecuencias negativas:**
- 🟡 Confusión de modelo: `HeroHand` (en español) paralelo a `HandRank` (inglés) crea dos representaciones para el mismo concepto.
- `ListRegions` static class duplica `RegionTableMap` + `RegionLookupCache`.

**Evidencia:** `Enums/Styles.cs`, `Enums/Positions.cs:51`, `Enums/Positions.cs:69`, `Enums/ActionsResponse.cs`, `Enums/ListRegions.cs`. 🟡

**Acción de seguimiento:** ver `_reversa_sdd/OpenScrape.Domain/questions.md` Q-05 (decisión humana).

---

## DD-14 — Mensajes de error en castellano

**Decisión:** Los mensajes de excepción en validaciones inline (`Hand`, `ThresholdKey`, `StrategyProfile`) están en castellano: "El nombre de la mano no puede estar vacío.", "Percentage debe estar entre 0 y 100, valor: 150".

**Contexto:** El proyecto declara `doc_language = Español` y el usuario es hispanohablante (CLAUDE.md instruye respuestas en castellano). Mensajes en inglés crean inconsistencia con el dashboard y los logs visibles en UI.

**Alternativas consideradas:**
1. Inglés (estándar técnico) — descartado: rompe consistencia del proyecto.
2. Localización con resources `.resx` — descartado: overhead para un proyecto monolingüe.

**Consecuencias positivas:**
- Errores legibles para el usuario final (cuando se exponen vía UI o logs).
- Coherencia con comentarios XML, commits y documentación.

**Consecuencias negativas:**
- Stack traces en herramientas internacionales mezclan idiomas (mensaje ES, frame names EN).
- Si el proyecto se exporta o internacionaliza, todos los mensajes deben extraerse y traducirse.

**Evidencia:** `Hand.cs:7,11`, `ThresholdKey.cs:18-26`. 🟢

---

## DD-15 — Mappers DTO como `static class` con extension methods

**Decisión:** Conversión `Card ↔ CardDTO` se implementa como extension methods en `static class CardDTOMapper` (mismo patrón para `Table`).

**Contexto:** AutoMapper o mappers configurados (`IMapper`) añaden dependencia y configuración. Para 2 tipos DTO + 2 mappings, mappers manuales con extension methods son más explícitos y zero-cost.

**Alternativas consideradas:**
1. AutoMapper — descartado: dependency externa para 2 mappings.
2. Constructor `Card(CardDTO dto)` — descartado: acopla Card a su DTO.
3. Records con copy: `dto with { ... }` — no aplica (los entities son `class`).

**Consecuencias positivas:**
- Mappings explícitos y leíbles.
- Sin runtime cost (sin reflection ni configuración).
- Determinismo trivial (RF-10).

**Consecuencias negativas:**
- Si en el futuro hay 20 DTOs, escalar manualmente es tedioso.
- Cambios en `Card` requieren recordar actualizar `CardDTOMapper`.

**Evidencia:** `Mappers/CardDTOMapper.cs`, `Mappers/TableDTOMapper.cs`. 🟢

---

## DD-16 — `Validate()` como invariante de startup (no de runtime)

**Decisión:** `StrategyProfile.Validate()` se invoca en startup vía `IValidateOptions<StrategyProfile>`. Si falla, la app aborta antes de aceptar manos.

**Contexto:** Validar en cada decisión (runtime) es caro y, si falla a mitad de mano, deja al usuario en estado inconsistente. Validar en startup garantiza que el sistema operacional siempre tiene config válida.

**Alternativas consideradas:**
1. Validación lazy en primer lookup — descartado: error tarde, en mitad de mano.
2. Validación en cada mano — descartado: overhead innecesario.

**Consecuencias positivas:**
- App no arranca con config corrupta. El usuario ve error en logs antes de poder jugar.
- Errores claros: la excepción enumera reglas violadas.

**Consecuencias negativas:**
- No detecta mutaciones post-startup (ver DD-12). Si alguien muta `StrategyProfile` durante runtime, no se re-valida.
- `IValidateOptions` requiere wiring DI específico — sin él, la validación se silencia.

**Evidencia:** `Exceptions/StrategyProfileValidationException.cs`, ADR-0008. 🟢

**ADR relacionado:** ADR-0008 (thresholds tipados, startup validation).

---

## DD-17 — `InternalsVisibleTo OpenScrape.App.Tests`

**Decisión:** El csproj declara `<InternalsVisibleTo Include="OpenScrape.App.Tests" />` para permitir testing de tipos `internal`.

**Contexto:** Algunos helpers de Domain son `internal` (no expuestos a otras capas). Para testearlos sin exponerlos públicamente, `InternalsVisibleTo` es el mecanismo idiomático en .NET.

**Alternativas consideradas:**
1. Hacer los helpers `public` — descartado: ensancha la API pública sin necesidad.
2. Tests internos en el mismo proyecto — descartado: rompe separación tests/prod.
3. `IInternalsVisibleToAttribute` con clave fuerte — descartado: overkill para proyecto monolítico.

**Consecuencias positivas:**
- Testing exhaustivo de invariantes internos sin exponer detalles.
- Curiosidad: `OpenScrape.App.Tests` (no `OpenScrape.Domain.Tests`) es el único proyecto de tests del solution.

**Consecuencias negativas:**
- Tests del Domain viven en otro proyecto (`OpenScrape.App.Tests`), no junto al código testado.
- Si se introduce `OpenScrape.Domain.Tests` en el futuro, debe añadirse a `InternalsVisibleTo`.

**Evidencia:** `OpenScrape.Domain.csproj`. 🟢

---

## Resumen

17 decisiones de diseño identificadas, distribuidas por categoría:

| Categoría | Decisiones |
|-----------|-----------|
| Estructura del proyecto | DD-01, DD-17 |
| Mutabilidad y persistencia | DD-02, DD-03, DD-09, DD-12 |
| Performance | DD-04 |
| Validación e invariantes | DD-05, DD-06, DD-11, DD-16 |
| Modelado de oponentes | DD-07, DD-08 |
| Cálculos derivados | DD-10 |
| Pragmatismo | DD-13, DD-14, DD-15 |

🟡 Decisiones con riesgo identificado: DD-12 (mutabilidad post-validate), DD-13 (legacy conservado).
🟢 Resto: confirmadas en código y consistentes con ADRs existentes.
