# OpenScrape.Features — Casos Extremos

> Casos límite del módulo de aplicación detectados en el código actual y la lógica del game loop, con disparador, comportamiento real, comportamiento esperado y consecuencias si se ignoran.

---

## EC-01 — `Cold4Bet.json` (archivo) ↔ `[Description("ColdFourBet")]` (enum)

**Disparador:** El game loop entra en una situación clasificada como `GameSituation.Cold4Bet` (un cold 4-bet, es decir, un 4-bet contra un 3-bet sin haber abierto la mano).

**Comportamiento real** (🔴 mismatch detectado):
- `GetActionScenario.ExecuteAsync(GameSituation.Cold4Bet, request)` invoca `tableUseCases.GetTable.ExecuteAsync(situation.GetDescription())`.
- `GameSituation.Cold4Bet.GetDescription()` retorna `"ColdFourBet"` (atributo `[Description("ColdFourBet")]` en `Domain/Enums/Positions.cs:78`).
- Marten consulta `Table` con `Id == "ColdFourBet"`.
- Si el seeder de `App` persiste el documento usando el **nombre del archivo** (`Cold4Bet`) en vez del `[Description]`, `GetTable` retorna `Result.NotFound`.
- `GetActionScenario` detecta `table?.Value == null` y lanza `Exception("Table not found")` que envuelve en otra `Exception($"Error executing ColdFourBet scenario: ...")`.
- El game loop captura la excepción a varios niveles arriba y la mano sigue sin acción válida o el bot se detiene según cómo lo gestione `SetPreflopActionUseCase`.

**Comportamiento esperado:**
- O bien el archivo se llama `ColdFourBet.json`, o el `[Description]` es `"Cold4Bet"`. Sin ambigüedad.
- Test de integración (TT-18) que valide post-seed: para todo `s in Enum.GetValues<GameSituation>()`, `GetTable.ExecuteAsync(s.GetDescription()).Status == Success`.

**Consecuencias si se ignora:**
- 🔴 **Crítico operativo**: el bot falla específicamente en cold 4-bet (situación rara pero ocurre con manos premium). El log se llena de `"Error executing ColdFourBet scenario"` pero el resto del juego sigue.
- Pérdida de valor en spots premium (KK+, AA): el cold 4-bet es un spot lucrativo si está bien jugado.
- Diagnóstico se vuelve complejo: el desarrollador ve excepción en runtime, no falla en boot.

**Cobertura test:** ninguna actualmente. Tareas T-34 + TT-18 lo cubren.

**Pregunta abierta:** Q-FEA-04 (`questions.md`). Decisión humana sobre cuál de los dos nombres es canónico.

---

## EC-02 — Suma de `Hand.Percentage` distinta de 100 en seed

**Disparador:** Una entrada `Positions[i].Hands` contiene manos con `Percentage` que suman 99, 101 u otro valor distinto de 100 (error humano al editar el JSON, redondeo manual mal hecho).

**Comportamiento real** (🟢 detección hardcoded):
- `GetRandomAction` valida en runtime: `if (actions.Sum(a => a.Percentage) != 100) throw new ArgumentException("Los porcentajes deben sumar 100")`.
- La excepción burbujea hasta el catch externo de `ExecuteAsync`, que la envuelve en `Exception($"Error executing {situation.GetDescription()} scenario: Los porcentajes deben sumar 100")`.
- El stack trace original se pierde (DD-11), pero el mensaje sí llega.

**Comportamiento esperado:**
- Idealmente la validación ocurre **al cargar el seed** (al boot) — no en cada llamada al runtime. Detectar el error 1 vez vs detectarlo cada hand.
- Tras la corrección sugerida, el seeder rehúsa cargar un JSON inválido y la app no arranca.

**Consecuencias si se ignora:**
- 🟡 Cualquier sesión con esa situación + esa mano + esos filtros lanza excepción → el bot falla específicamente en ese spot.
- El error es reproducible pero **dependiente del filtro exacto** del request: si la suma==100 falla solo para `HeroPosition=Button` y no para `HeroPosition=CutOff`, el debugging se complica.
- Costo en CPU: la validación se ejecuta en cada llamada (negligible).

**Cobertura test:** TT-05 (input: manos `[40, 40]`).

---

## EC-03 — `request.HandName` o `request.Suited` `null`

**Disparador:** Caller construye `ActionScenarioRequest` sin setear `HandName` o `Suited`. Por ejemplo, durante un test de regresión o un bug en `SetPreflopActionUseCase`.

**Comportamiento real** (🟡 silencioso):
- El filtro `f.Name == request.HandName && f.Suited == request.Suited` ejecuta con `null` en uno o ambos lados.
- En LINQ a memoria: `f.Name == null` retorna `false` para toda fila (porque `Hand.Name` se valida no-vacío en su constructor — ver `Domain/ValueObjects/Hand.cs:5-7`).
- `f.Suited == null` retorna `false` si `Hand.Suited` es `bool` no-nullable (depende del schema; si `Hand.Suited` es `bool`, `bool == bool?` con null retorna `false`).
- Resultado: `hands` queda vacío → retorna `"Fold"`.

**Comportamiento esperado:**
- El DTO debería rechazar `HandName==null` en runtime (guard explícito) o `[Required]` declarativo en construcción.
- Idealmente: `HandName` `string` no-nullable; `Suited` `bool` no-nullable.

**Consecuencias si se ignora:**
- 🟡 Bug silencioso: el bot foldea cuando debería actuar. El log no marca por qué.
- Difícil de detectar sin instrumentación: el caller cree haber pasado los datos correctos.

**Cobertura test:** ninguna actualmente. Recomendado: TT con request mal poblado debe retornar `"Fold"` y emitir log warning.

---

## EC-04 — `Table.Positions` `null`

**Disparador:** Documento Marten persistido con `Positions == null` (corrupción, migración incompleta, seed mal generado).

**Comportamiento real** (🟡 NPE potencial):
- `table.Value.Positions?.Where(...)` usa `?.` → si `Positions==null`, todo el chain devuelve `null`.
- `.FirstOrDefault()?.Hands` también usa `?.` → `null`.
- `.Where(f => f.Name == ...)` se invoca sobre `null`: la línea `34-35` en realidad ejecuta `null?.Hands.Where(...)` → `null`.
- `if (hands == null) return "Fold"` cubre el caso.

**Comportamiento esperado:**
- Defensa explícita en `GetTable.ExecuteAsync` para validar que el documento tiene `Positions != null` antes de devolverlo, o validación al cargar la seed.
- El usuario no debería ver `"Fold"` enmascarando una corrupción de datos.

**Consecuencias si se ignora:**
- 🟡 Bot foldea incorrectamente sin alerta.
- Documento corrupto pasa desapercibido en producción.

**Cobertura test:** ninguna. Recomendado: TT que persista `Table { Positions = null }` y verifique comportamiento ("Fold" sin excepción, idealmente con log warning).

---

## EC-05 — Filtros nullable que matchean filas con campo "vacío" no-null en seed

**Disparador:** El seed tiene una fila con `HeroPosition="Button"` y otra con `HeroPosition=""` (cadena vacía). El request llega con `HeroPosition==null`.

**Comportamiento real** (🟡 ambigüedad):
- Filtro: `request.HeroPosition == null || w.HeroPosition == request.HeroPosition?.GetDescription()`.
- `request.HeroPosition == null` → corto circuito, condición `true` para **todas** las filas.
- Resultado: matchea **ambas** filas; `FirstOrDefault` toma la primera (orden no garantizado del seed).
- Si la primera resulta ser la fila con `HeroPosition=""` (vacía / wildcard / catch-all), se aplican sus manos.

**Comportamiento esperado:**
- Si la convención del seed es "fila vacía == cualquier posición", documentarlo. Si no, normalizar el seed a `HeroPosition=null` para wildcards.
- Idealmente, el filtro debería ser explícito: si `request.HeroPosition` se especifica, exigir match exacto; si es `null`, exigir que la fila también tenga `null` (no `""`).

**Consecuencias si se ignora:**
- 🟡 Comportamiento dependiente del orden de las filas en el JSON. Frágil.
- Editar el JSON puede cambiar la decisión sin que el editor sepa.

**Cobertura test:** ninguna. Recomendado: validar la convención del seed (todos los campos opcionales son `null`, no `""`) en boot.

---

## EC-06 — `UpdateRegionTableMap` con `RegionTableMap.Regions == null`

**Disparador:** El documento `RegionTableMap` con la `Category` solicitada existe pero `Regions == null` (recién creado vacío, migración).

**Comportamiento real** (🔴 NPE silenciado, comportamiento dudoso):
- `region.Regions?.FirstOrDefault(r => r.Name == request.Name)` → `null`.
- `regionToRemove != null` → `false` → no se intenta `Remove`.
- `region.Regions?.Add(regionCategory)` → `null?.Add(...)` → no-op (no lanza).
- `session.Store(region)` + `SaveChangesAsync` → persiste el documento **sin la nueva región** (porque `Add` fue no-op sobre `null`).
- Retorna `Result.Success()` aunque la región **no se guardó**.

**Comportamiento esperado:**
- Inicializar `region.Regions ??= new List<Region>()` antes de `Add`.
- O bien, retornar `Result.CriticalError` si `Regions == null` (caso anómalo).

**Consecuencias si se ignora:**
- 🔴 **Bug funcional silencioso**: el editor de regiones reporta éxito al usuario pero la región no quedó guardada. Próxima sesión OCR fallará.

**Cobertura test:** ninguna. Recomendado: TT que persista `RegionTableMap { Regions = null }` y verifique que `Update` reporta el caso (éxito + región creada o error explícito).

**Lacuna pendiente:** decidir qué hacer en este caso (init defensivo vs error explícito).

---

## EC-07 — Marten falla en `LoadAsync` con `request.Category == null` o vacío

**Disparador:** Caller construye `UpdateRegionTableMapRequest("", "Pot", ...)` con `Category` vacío, o `null` (record positional acepta string null en .NET).

**Comportamiento real** (🟡 dependiente de Marten):
- `LoadAsync<RegionTableMap>("")` o `LoadAsync<RegionTableMap>(null)` puede:
  - Retornar `null` (Marten 8.x si no encuentra) → `Result.NotFound()`.
  - Lanzar `ArgumentException` si Marten valida la clave (versión-dependiente).
- En el primer caso, el caller recibe `NotFound` sin saber si la `Category` era inválida o simplemente no existe.

**Comportamiento esperado:**
- Validar `string.IsNullOrWhiteSpace(request.Category)` al inicio del `ExecuteAsync` y retornar `Result.Invalid("Category is required.")` o equivalente.

**Consecuencias si se ignora:**
- 🟡 Diagnóstico de errores se mezcla: NotFound puede significar "no existe" o "key inválida".

**Cobertura test:** ninguna. Recomendado: TT con `Category=""` y `Category=null` para validar el comportamiento esperado.

---

## EC-08 — `GetRecentGameRounds.Execute(count=0)` o `count<0`

**Disparador:** Caller pide 0 o un valor negativo (UI con campo `int.Parse` mal validado, test mal escrito).

**Comportamiento real** (🟢 LINQ tolerante):
- `count=0` → `.Take(0)` retorna lista vacía → `[]`.
- `count<0` → `.Take(-5)` retorna lista vacía (`Take` cláusula sobre negativo equivale a 0).
- `count=int.MaxValue` → intenta retornar **todas** las sesiones; si la base tiene 100K, llena memoria.

**Comportamiento esperado:**
- Caso `0`/negativo: caller debería validar antes de invocar; el método podría lanzar `ArgumentOutOfRangeException` para count<0.
- Caso `int.MaxValue`: cap interno (ej: `Math.Min(count, 1000)`) para proteger memoria.

**Consecuencias si se ignora:**
- 🟢 `count=0/<0`: cosmético; el grid muestra vacío.
- 🟡 `count=int.MaxValue`: en bases grandes (>10K sesiones) puede congelar la UI durante segundos al cargar.

**Cobertura test:** ninguna. Recomendado: TT-edge con count=0 y count=int.MaxValue.

---

## EC-09 — Ejecuciones concurrentes de `UpdateRegionTableMap` sobre la misma `Category`

**Disparador:** Dos invocaciones simultáneas (UI + script de migración, o doble-click rápido) modifican la misma `RegionTableMap`. La segunda puede haber empezado a leer antes de que la primera persistiera.

**Comportamiento real** (🟡 last-write-wins):
- A `LoadAsync` y B `LoadAsync` reciben la misma versión de `RegionTableMap`.
- A modifica + guarda → versión n+1.
- B modifica (sobre versión n) + guarda → versión n+2 con cambios de B pero **sin** los de A.
- Marten 8 con `LightweightSession` no detecta el conflicto por defecto (no hay optimistic concurrency activado en este módulo).

**Comportamiento esperado:**
- Activar optimistic concurrency: `options.Schema.For<RegionTableMap>().UseOptimisticConcurrency(true)` en `OpenScrape.Infrastructure.Services.AddDataBase`.
- O bien, lock pesimista en el editor de regiones.

**Consecuencias si se ignora:**
- 🟡 Ediciones concurrentes pierden cambios silenciosamente. Improbable en uso normal (un solo usuario, edición secuencial); posible en script de migración + UI abierta simultáneamente.

**Cobertura test:** ninguna. Recomendado: test de integración con dos sesiones paralelas.

**Decisión cruzada:** afecta a `OpenScrape.Infrastructure` (configuración Marten) — no resoluble solo en este módulo.

---

## EC-10 — Marten serializer falla deserializando `Table` (campo añadido al modelo no presente en JSON existente)

**Disparador:** Cambio del modelo `Table` (ej: nuevo campo `Version`) y persistencia previa sin ese campo. Marten / System.Text.Json deserializa con el campo en su default (`null`/`0`).

**Comportamiento real** (🟢 tolerante por default):
- STJ ignora campos faltantes en JSON, asigna defaults a los campos del modelo.
- Si el campo es no-nullable (`int Version`), recibe `0`.
- Si es nullable, recibe `null`.
- `GetTable` retorna `Success` con el documento — ningún error de deserialización.

**Comportamiento esperado (alineamiento STJ default):**
- El comportamiento descrito **es** el esperado: lectura tolerante a versiones antiguas.

**Consecuencias si se ignora:**
- 🟢 Caso normal: app sigue funcionando con datos antiguos.
- 🟡 Si la lógica posterior asume que el campo nuevo está presente y no defaultea bien, comportamiento inesperado downstream.

**Cobertura test:** ninguna. Recomendado: TT que persista `Table` con esquema antiguo (JSON manual) y verifique deserialización con esquema nuevo.

---

## EC-11 — `GetActionScenario` con todos los filtros nullable en `null`

**Disparador:** Caller construye `ActionScenarioRequest { HandName="AKs", Suited=true }` y deja todo lo demás (`HeroPosition`, `OpenRaiser`, etc.) en `null`.

**Comportamiento real** (🟡 wildcard total):
- Cada filtro: `request.X == null || w.X == request.X.GetDescription()` → primer brazo `true` siempre.
- LINQ matchea **todas** las filas de `Positions`.
- `FirstOrDefault` retorna la primera fila (no determinista entre ejecuciones si la seed se reordena).
- Sus `Hands` se filtran por `Name=="AKs" && Suited==true`.
- Si la primera fila no tiene esa mano, retorna `"Fold"`.

**Comportamiento esperado:**
- Caller responsable: `HeroPosition` debería ser obligatorio (es la mínima señal para escoger una fila).
- Idealmente, validación en el DTO: `[Required] HeroPosition`.

**Consecuencias si se ignora:**
- 🟡 Decisión preflop dependiente del orden de filas del seed; no determinista.

**Cobertura test:** ninguna. Recomendado: TT con request sin `HeroPosition` debe retornar `"Fold"` o lanzar excepción (decisión humana).

---

## EC-12 — `GetTable` con `name == ""` o `name == null`

**Disparador:** `GetActionScenario` invoca `GetTable.ExecuteAsync("")` porque `situation.GetDescription()` retorna `""` (enum sin atributo `[Description]` o atributo con valor vacío).

**Comportamiento real** (🟡 NotFound silencioso):
- `Query<Table>().FirstOrDefaultAsync(x => x.Id == "")` → si no hay ningún `Table.Id == ""`, retorna `null` → `Result.NotFound()`.
- `GetActionScenario` interpreta como "tabla no encontrada" y lanza excepción contextual con `situation.GetDescription()` = `""`.

**Comportamiento esperado:**
- Validar `name` no-vacío al inicio del `GetTable.ExecuteAsync`.
- O detectar la condición upstream: `EnumExtensions.GetDescription` debería garantizar valor no-vacío para todo enum miembro.

**Consecuencias si se ignora:**
- 🟡 Mensaje de error sin contexto: `"Error executing  scenario"` (con doble espacio porque la `Description` es vacía).
- Difícil correlacionar con el bug raíz (enum sin atributo).

**Cobertura test:** ninguna. Recomendado: TT que detecte cualquier `GameSituation` cuyo `GetDescription` retorne null/empty.

---

## Tabla resumen

| EC | Caso | Severidad | Cobertura test actual | Recomendación |
|----|------|-----------|----------------------|---------------|
| EC-01 | `Cold4Bet.json` ↔ `ColdFourBet` | 🔴 crítico operativo | Ninguna | Decidir + TT-18 (T-34) |
| EC-02 | `Sum(Percentage) != 100` | 🟢 detección clara | TT-05 | Validar al boot, no por llamada |
| EC-03 | `HandName/Suited == null` | 🟡 silencioso | Ninguna | Guard + log warning |
| EC-04 | `Table.Positions == null` | 🟡 silencioso | Ninguna | Guard explícito |
| EC-05 | filtros wildcard vs filas vacías en seed | 🟡 ambigüedad | Ninguna | Normalizar seed |
| EC-06 | `RegionTableMap.Regions == null` | 🔴 bug silencioso | Ninguna | Init defensivo o error |
| EC-07 | `LoadAsync` con `Category` null/vacío | 🟡 mezcla NotFound/Invalid | Ninguna | Validar input |
| EC-08 | `count` extremo en `GetRecentGameRounds` | 🟡 memoria con int.MaxValue | Ninguna | Cap interno |
| EC-09 | concurrencia en `UpdateRegionTableMap` | 🟡 last-write-wins | Ninguna | Optimistic concurrency en Marten |
| EC-10 | deserialización con esquema evolutivo | 🟢 tolerante | Ninguna | Documentar y testear migración |
| EC-11 | request sin `HeroPosition` | 🟡 no determinista | Ninguna | Validar `[Required]` |
| EC-12 | `GetTable` con `name` vacío | 🟡 mensaje sin contexto | Ninguna | Guard + EnumExtensions garantía |

**Patrón:** la mayoría son casos silenciosos que enmascaran bugs (🟡), no fallos catastróficos. Combinados con DD-07 (Fold como default seguro), el bot prioriza no-romperse sobre alertar — comportamiento correcto en producción pero **dificulta la calibración**. La recomendación general es introducir logging estructurado de warnings en cada uno de estos paths para tener señal sin romper el flujo.
