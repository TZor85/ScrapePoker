# Code Analysis — ScrapePoker

> Análisis técnico consolidado generado por el Arqueólogo del Reversa.
> Cada módulo se añade incrementalmente. Nivel de documentación: **detalhado**.
>
> Escala de confianza: 🟢 CONFIRMADO · 🟡 INFERIDO · 🔴 LACUNA

---

## Módulo: `OpenScrape.Domain` 🟢

**Path:** `src/OpenScrape.Domain/`
**Framework:** .NET 10.0 · `Nullable` enabled · `ImplicitUsings` enabled
**Dependencias externas:** ninguna (puro BCL)
**Tipos públicos:** ~50 (clases + records + enums) en 33 archivos C#

### Propósito

Núcleo del modelo de dominio del bot de poker. Define todas las abstracciones inmutables del juego (cartas, mesa, mano, posición, tipo de oponente), entidades persistibles en Marten (sesión, mano, perfiles), value objects de cálculo (rangos, equity, draws, telemetría) y la masiva configuración de estrategia (`StrategyProfile`, ~150 thresholds). Sin lógica de aplicación: solo datos, validaciones de invariantes en constructores y métodos puramente computacionales.

### Estructura

| Carpeta | Tipos | Notas |
|---------|-------|-------|
| `Dtos/` | `CardDTO`, `TableDTO`, `SessionStatsDto` | DTOs ligeros de transferencia |
| `Entities/` | `Card`, `Table`, `RegionTableMap`, `OverlayConfig`, `GameSession`, `HandRecord`, `HandResult`, `StrategyProfile`, `OpponentProfile`, `OpponentPositionProfile`, `OpponentType` | Documentos persistidos (Marten) y agregados raíz |
| `Enums/` | `Rank`, `Suit`, `HandRank`, `KickerStrength`, `Positions`, `TablePosition`, `HandSituation`, `BoardPosition`, `HeroHand`, `GameSituation`, `PairClassification`, `BluffConditionType`, `Styles`, `ActionsResponse` (clase, no enum), `ListRegions` (clase static), `EnumExtensions` | Tipificación del dominio del poker |
| `Mappers/` | `CardDTOMapper`, `TableDTOMapper` | Métodos de extensión bidireccionales |
| `ValueObjects/` | `Hand`, `Region`, `PlayerActionSequence`, `CardDataOuts`, `DrawProbability`, `HandStrength`, `HandEvaluation`, `PotOddsResult`, `StreetDecision`, `StreetThresholds`, `ThresholdKey`, `VillainRange`, `BankrollSnapshot`, `BankrollStats`, `BankrollHistoryItem`, `CategoryStats`, `TelemetryAggregate` | Objetos de valor inmutables |
| `Exceptions/` | `StrategyProfileValidationException` | Falla rápida con todos los errores acumulados |

### 1. Flujo de control y funciones principales

#### `StrategyProfile.Validate()` — `Entities/StrategyProfile.cs:318`

🟢 **Función pura sin parámetros, retorna `List<string>`** (vacía = OK).

Itera sobre `Thresholds` (Dictionary clave→`StreetThresholds`) validando que los 4 tiers de equity respeten el orden:
```
FoldBelow < ThinValueAbove < ValueAbove < StrongValueAbove
```

Después valida 9 invariantes globales: `FoldEquityMin < FoldEquityMax`, orden de SPR thresholds, rangos `[0,100]` para danger pcts, `[0,1]` para `TaintedOutsDiscount` y bluff frequencies, `(0,1]` para BluffCatch multipliers, multiplicadores SPR `> 0`, factores ImpliedOdds en `(0,1]`, e ImpliedOdds shallow < deep.

Cada error se añade con la clave del threshold y los valores conflictivos. Coste: O(N) donde N = nº de thresholds (típico: 30).

#### `OpponentProfile.GetTypeForPosition(bool villainIsInPosition)` — `Entities/OpponentProfile.cs:211`

🟢 Clasifica al villano en uno de 5 tipos mediante un **switch expression sobre tupla `(isLoose, isAggressive)`**:

```
af = villainIsInPosition ? AggressionFactorIP : AggressionFactorOOP
af < 0  → fallback a AggressionFactor global
isLoose = VPIP > 30
isAggressive = af > 1.5

(true,  true ) → LAG    (Loose-Aggressive)
(true,  false) → LP     (Loose-Passive, fish)
(false, true ) → TAG    (Tight-Aggressive, reg)
(false, false) → TP     (Tight-Passive, nit)
```

Si `HandsPlayed < 10` → retorna `Unknown` antes de evaluar.

#### `OpponentProfile.GetProfileForPosition(TablePosition position)` — `Entities/OpponentProfile.cs:241`

🟢 Genera un `OpponentProfile` sintético para una posición específica si y solo si `posProf.IsReliable` (≥10 manos en esa posición). El sintético combina:
- Stats posicionales (VPIP, PFR, IP/OOP aggro/passive)
- Stats globales no-posicionales copiados (postflop bet/raise/call/fold, c-bet, fold-to-cbet, showdown counters)

Si no es reliable, **devuelve `this` (el perfil global) como fallback**, garantizando uso seguro.

#### `VillainRange.GetForSituation(HandSituation)` — `ValueObjects/VillainRange.cs:32`

🟢 **Selector estático** que mapea cada `HandSituation` a un `Dictionary<string, double>` de manos→frecuencia, vía `switch expression` con 12 ramas:

| Situación hero | Rango devuelto | %/desc |
|----------------|----------------|--------|
| OpenRaise | `_callerVsOpenRaise` | 25% |
| ThreeBet | `_callerVs3Bet` | 10% |
| OpenRaiseVs3Bet | `_threeBettor` | 8% (polarizado) |
| OpenRaiseVs3BetAndCall | `_threeBetPotCaller` | 12% (multiway) |
| FourBet / Cold4Bet | `_callerVs4Bet` | 5% (premium) |
| Call | `_openRaiser` | 20% (estándar apertura) |
| RaiseOverLimper | `_limper` | 40% (amplio/débil) |
| Squeeze | `_callerVsSqueeze` | 12% |
| VsSqueeze | `_threeBettor` | 8% |
| DonkBet | `_donkBettor` | 30% |
| DonkBetVsOpenRaise | `_callerVsOpenRaise` | 25% |
| None / otros | `null` (sin filtro) | — |

8 rangos predefinidos con frecuencias por mano canónica (ej: `["AA"] = 1.0`, `["A2s"] = 0.3`).

#### `VillainRange.GetForSituation` — overloads encadenados (`ValueObjects/VillainRange.cs:80, 119`)

🟢 Tres overloads encadenados en pirámide:
1. `(situation)` → rango base
2. `(situation, position)` → ajusta por posición villain (×0.7..×1.3)
3. `(situation, position, profile)` → ajusta por VPIP y 3Bet% observados

El segundo aplica un único multiplicador a todas las frecuencias y las clampea a `[0, 1]`. Si `|mul - 1.0| < 0.01` retorna el rango base sin clonar (optimización).

El tercero combina `vpipMultiplier × threeBetMultiplier` (este último solo activo en `OpenRaiseVs3Bet` y `VsSqueeze`). Si la combinación cambia <5% retorna el ajuste posicional sin re-clonar.

#### `VillainRange.CalculateVpipMultiplier(double baseRangePercentage, double observedVPIP)` — `ValueObjects/VillainRange.cs:163`

🟢 Método `internal static` (visible para tests vía `InternalsVisibleTo`).

```
expectedVPIP = max(baseRangePercentage, 10.0)
ratio        = observedVPIP / expectedVPIP
return Clamp(ratio, 0.5, 2.0)
```

El piso de 10 evita división por VPIP esperado irreal en rangos polarizados (ej: 3Bettor 8%). El clamp evita amplificar el rango más allá del doble o reducirlo a menos de la mitad.

#### `VillainRange.ExpandHandNotation(string notation)` — `ValueObjects/VillainRange.cs:541`

🟢 Expande notación abreviada de poker a tuplas concretas `(CardDataOuts, CardDataOuts)`:

| Tipo | Detección | Combos |
|------|-----------|--------|
| Pareja (`"QQ"`) | `notation.Length == 2` o `rank1 == rank2` | C(4,2) = **6** |
| Suited (`"AKs"`) | `notation.Length == 3 && notation[2] == 's'` | **4** (un palo cada) |
| Offsuit (`"AKo"`) | resto | 4×4 − 4 = **12** (palos distintos) |

Casos límite:
- `notation.Length < 2` → lista vacía
- Caracter de rank no reconocido → lista vacía (`GetValueOrDefault` retorna `default(Rank) == 0`, comprobado explícitamente)

#### `ThresholdKey.TryParse(string?, out ThresholdKey?, out string)` — `ValueObjects/ThresholdKey.cs:36`

🟢 Parser strict-mode (`ignoreCase: false`) de claves `"{BoardPosition}_{HandSituation}"`. Mensajes de error específicos para cada fallo: vacío, formato, BoardPosition inválido, HandSituation inválido, validación del constructor (`None`/`Hand` no admitidos para postflop).

### 2. Algoritmos y lógica embebida

| Algoritmo | Propósito | Ubicación | Complejidad |
|-----------|-----------|-----------|-------------|
| **Laplace smoothing AF** | `(a+1)/(p+1)` evita división por cero y regresa a AF=1 con muestras pequeñas | `OpponentProfile.cs:101` | O(1) |
| **4-quadrant villain typing** | LAG/LP/TAG/TP por VPIP+AF con threshold dual | `OpponentProfile.cs:188`, `:211` | O(1) |
| **Synthetic positional profile** | Combina stats posicionales con globales para calidad de estimación | `OpponentProfile.cs:241` | O(1), copia 14 campos |
| **Multi-step range adjustment** | Pyramid pattern: base → ×position → ×profile (VPIP+3Bet%) | `VillainRange.cs:32, 80, 119` | O(N hands), N≈40 |
| **VPIP ratio clamp** | Evita rangos extremos: ratio = obs/expected, clamp [0.5, 2.0] | `VillainRange.cs:163` | O(1) |
| **Hand notation expansion** | Combo enumeration: 6/4/12 según tipo | `VillainRange.cs:541` | O(1) per call |
| **NetProfit normalization** | Excluye blind obligatoria y auto-rebuy del profit | `GameSession.cs:92` | O(1) |
| **BB/100 win rate** | Normalización temporal universal del win rate | `GameSession.cs:39` | O(N hands) por agregación |
| **StrategyProfile validation** | 13 reglas cruzadas sobre ~150 parámetros, falla rápida con lista de errores | `StrategyProfile.cs:318` | O(N thresholds + 9 globales) |
| **TotalProfit aggregate** | LINQ Sum solo sobre manos con `Result != Unknown` | `GameSession.cs:35` | O(N hands) |

### 3. Estructuras de datos relevantes

#### Agregados raíz Marten

- **`GameSession`**: Documento principal de sesión. Contiene `StartingBankroll`, `EndingBankroll`, `PeakBankroll` y métricas computadas. La lista `Hands` está marcada `[JsonIgnore]` — las manos se persisten como documentos `HandRecord` independientes, vinculadas por `GameSessionId` (FK lógica).
- **`HandRecord`**: Documento por mano. Stack inicial/final, `BlindPosted`, `AutoRebuy`, `NetProfit` (computed), board cards, lista de `StreetDecision`, resultado y opcional `TelemetryAggregate`.
- **`StrategyProfile`**: Configuración serializable con `Validate()` ejecutado al arranque (lanza `StrategyProfileValidationException` con mensaje multi-línea agrupado).
- **`Card`** / **`Table`** / **`RegionTableMap`**: Documentos auxiliares (catálogo de cartas, mapas de mesa, regiones de captura).

#### Diccionario clave: `StrategyProfile.Thresholds`

Tipo: `Dictionary<string, StreetThresholds>` con clave `"{Street}_{Situation}"` (formato legacy). El nuevo `ThresholdKey` proporciona acceso tipado y validación (`Flop_OpenRaise`, `Turn_ThreeBet`, etc.). 🟡 **INFERIDO**: la coexistencia del diccionario string y el record `ThresholdKey` sugiere migración en curso del consumidor (DecisionMaker).

#### Records con validación inline

```csharp
public record Hand(string Name, bool? Suited, string Action, int Percentage)
{
    public string Name { get; init; } = !string.IsNullOrWhiteSpace(Name)
        ? Name : throw new ArgumentException(...);

    public int Percentage { get; init; } = Percentage >= 0 && Percentage <= 100
        ? Percentage : throw new ArgumentOutOfRangeException(...);
}
```

Patrón replicado en `ThresholdKey` (constructor explícito que valida `Street` y `Situation`).

### 4. Constantes, defaults y feature flags

#### Defaults sin datos en `OpponentProfile`

Cuando un oponente no tiene historial suficiente, los stats devuelven valores neutrales razonables:

| Stat | Default sin datos | Justificación |
|------|--------------------|----------------|
| `VPIP` | 50% | Punto medio del espectro |
| `PFR` | 15% | Tight aggressive estándar |
| `ThreeBetPct` | 5% | Frecuencia regular de 3bet |
| `FoldToCBetPct` | 50% | Coin flip baseline |
| `CBetPct` | 50% | Coin flip baseline |
| `WTSDPct` | 35% | Frecuencia esperada de showdown |
| `WSDPct` | 50% | Coin flip baseline |
| `CheckRaisePct` | 8% | Acción rara |
| `DonkBetPct` | 10% | Acción rara |
| `BarrelFrequency` | -1 | Sentinel — usar `ExpectedBarrelFrequency` por tipo |
| `AggressionFactor IP/OOP` | -1 | Sentinel — caller fallback al global |

#### Feature flags y switches en `StrategyProfile`

| Flag | Default | Efecto |
|------|---------|--------|
| `CheckRaiseMixingEnabled` | `true` | Habilita mixing probabilístico de check-raise (S19.1) |
| `ThreeBetPotNoFloat` | `true` | Desactiva floating en 3bet pots (S19.3) |
| `RiverScareSizingReduction` | `true` | Reduce sizing en scare river (S22.2) |
| `BluffFreqEquityScaling` | `true` | Scaling lineal de bluff por equity (S22.6) |
| `HandReEvalOnDrawCompletion` | `true` | Degrada TwoPair si draw completó en river (S22.8) |

#### Bankroll defaults

```csharp
InitialBankroll = 100m  // €/$
BuyInMax = 2m            // 2 BB max buy-in
MinSessionsForRecommendation = 20
RiskOfRuinThreshold = 0.05  // 5%
```

### 5. Treatamiento de errores

- `StrategyProfile.Validate()` **acumula** todos los errores en lugar de fallar al primero — útil para diagnóstico en arranque.
- `StrategyProfileValidationException` empaqueta todos los errores en un mensaje multi-línea formateado:
  ```
  StrategyProfile inválido (3 errores):
    - Flop_OpenRaise: FoldBelow (35) debe ser menor que ThinValueAbove (35)
    - FoldEquityMin (60) debe ser menor que FoldEquityMax (60)
    - DangerFlushCompletePct (-5) debe estar entre 0 y 100
  ```
- `Hand.Name` y `Hand.Percentage` lanzan en `init` → invariantes garantizadas tras construcción.
- `ThresholdKey` rechaza `BoardPosition.None`/`Hand` y `HandSituation.None` con `ArgumentException` que incluye el valor inválido.
- `TryParse` patrón usado en `ThresholdKey`: nunca lanza, retorna `bool` y mensaje de error en `out` — apto para parseo desde config.

### 6. Acoplamientos internos (anomalías de capa)

🟡 **INFERIDO**: aunque la capa Domain no tiene dependencias externas, el grafo interno presenta acoplamientos inversos al esperado:

- `ValueObjects.VillainRange` depende de `Entities.OpponentProfile` (lo correcto sería que ValueObjects sea inferior a Entities). Esto es un **olor a feature creep**: el rango como cálculo derivado debería estar en una capa aplicativa o de servicio, no en VO.
- `ValueObjects.HandStrength.cs` declara `using OpenScrape.Domain.Entities;` pero no usa nada de Entities — el tipo `DrawProbability` que referencia está en `ValueObjects/CardDataOuts.cs`. **Import muerto** confirmado por inspección.
- `Mappers/` depende de `Entities` + `Dtos` + `ValueObjects` (acoplamiento esperado).

### 7. Entidades duplicadas / posibles legacy

🟡 **INFERIDO** — candidatos a `discard_log.md` en migración:

| Tipo | Archivo | Motivo |
|------|---------|--------|
| `Styles` enum | `Enums/Styles.cs` | Solo 3 valores genéricos (`Default`/`Agresive`/`Pasive`); typo "Agresive". Posible legado pre-DecisionMaker. |
| `HeroHand` enum | `Enums/Positions.cs:51` | Nombres en español, paralelo a `HandRank` (inglés/byte). Probablemente reemplazado por `HandRank`. |
| `GameSituation` enum | `Enums/Positions.cs:69` | Paralelo a `HandSituation` con atributos `[Description]`. Solapamiento confuso. |
| `ActionsResponse` clase | `Enums/ActionsResponse.cs` | Está en carpeta `Enums/` pero es una clase POCO. Categoría incorrecta. |

🔴 **LACUNA**: Se requiere validación humana sobre cuáles de estos tipos siguen vivos en `OpenScrape.Features` y `OpenScrape.App` (pendiente Fase 2 — Arqueólogo en módulos siguientes).

### 8. Métricas

| Métrica | Valor |
|---------|-------|
| Archivos `.cs` (no `obj/`) | 33 |
| LOC estimado (no contado) | ~1250 |
| Tipos públicos (clases + records + enums) | ~50 |
| Funciones/métodos no triviales | ~25 |
| Reglas de negocio extraídas | 29 |
| Algoritmos identificados | 8 |
| Anomalías arquitectónicas | 4 candidatos discard |
| Excepciones definidas | 1 (`StrategyProfileValidationException`) |
| Dependencias externas | 0 |

### 9. Diagrama de relaciones (resumen)

Ver `flowcharts/OpenScrape.Domain.md` para diagrama Mermaid completo. Resumen:

```
Enums                  ←─ Domain.ValueObjects ─→ Domain.Entities
   │                          │  ↑                    │
   └──────────────────────────┘  └────────────────────┘
                          ↓
                     Domain.Mappers ← Domain.Dtos
                          ↓
                  (External: Features, DecisionMaker, App)
```

### Diccionario de datos completo

Ver `_reversa_sdd/data-dictionary.md`.

### Mapeo a archivos legacy

Ver `_reversa_sdd/OpenScrape.Domain/legacy-mapping.md`.

---

## Módulo: `OpenScrape.Infrastructure` 🟢

**Path:** `src/OpenScrape.Infrastructure/`
**Framework:** .NET 10.0 · `Nullable` enabled · `ImplicitUsings` enabled
**Dependencias externas:** `Marten 8.24.0`, `Microsoft.Extensions.Configuration.Abstractions 10.0.3`, `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.3`, `Ardalis.Result 10.1.0`
**Dependencias internas:** `OpenScrape.Domain` (entidades persistidas)
**Tipos públicos:** 1 (`static class Services`) en 1 archivo C#
**LOC:** 43 (incluido csproj)

### Propósito

Único responsable de **inicializar el document store de Marten** sobre PostgreSQL. Expone un único método de extensión sobre `IServiceCollection` que:
1. Registra Marten contra la cadena de conexión `DefaultConnection`.
2. Configura `System.Text.Json` como serializador.
3. Declara siete índices para acelerar consultas sobre `GameSession` y `HandRecord`.
4. Activa la creación automática del esquema PostgreSQL (`AutoCreate.All`) si el flag `IsDevelopment` es `true`.

No hay repositorios, sesiones, mappers ni código operacional — todo el acceso a datos se hace consumiendo `IDocumentStore` directamente desde otros módulos (`Features`, `App`, `DecisionMaker`).

### Estructura

| Archivo | Tipo | Notas |
|---------|------|-------|
| `OpenScrape.Infrastructure.csproj` | proyecto | `<NoWarn>NU1902</NoWarn>` suprime CVE transitivo de `OpenTelemetry.Api` (dep de Marten) |
| `Services.cs` | `static class Services` | método único `AddDataBase(IServiceCollection, IConfiguration, bool IsDevelopment)` |

### 1. Flujo de control y funciones principales

#### `Services.AddDataBase(this IServiceCollection services, IConfiguration configuration, bool IsDevelopment)` — `Services.cs:11`

🟢 **Método de extensión estático y único punto de entrada del módulo**. Sin retorno; muta el `IServiceCollection` recibido.

Pasos:

1. **Registrar Marten** vía `services.AddMarten(options => …)`:
    ```
    options.Connection(configuration.GetConnectionString("DefaultConnection")!)
    ```
    El sufijo `!` (null-forgiving) **asume que `DefaultConnection` siempre está presente** — no hay validación; un `appsettings.json` sin la clave producirá `NullReferenceException` en arranque.

2. **Serializador**: `options.UseSystemTextJsonForSerialization()` — fuerza `STJ` en lugar del default Newtonsoft.Json. Implicaciones para entidades persistidas:
    - Atributo `[JsonIgnore]` debe ser `System.Text.Json.Serialization.JsonIgnore` (efectivamente lo es en `GameSession`).
    - Tipos referenciados en `GameSession`/`HandRecord` deben ser deserializables con STJ (decimal/DateTime sí; ningún `DateTimeOffset` involucrado).

3. **Índices declarados explícitamente** (resto se autogenera):

    | Documento | Campo(s) | Tipo | Justificación |
    |-----------|----------|------|---------------|
    | `GameSession` | `EndTime` | simple | Listado del Historial ordenado por fecha |
    | `GameSession` | `SessionId` | simple | Búsqueda por correlación de sesión externa |
    | `GameSession` | `TableName` | simple | Filtrado por mesa |
    | `HandRecord` | `GameSessionId` | simple | Listar manos de una sesión (FK) |
    | `HandRecord` | `Timestamp` | simple | Histórico cronológico |
    | `HandRecord` | `(GameSessionId, Timestamp)` | compuesto | Query "manos de sesión X ordenadas por fecha" |
    | `HandRecord` | `HeroPosition` | simple | Análisis estadístico por posición |

    🟡 **INFERIDO**: Marten genera índices GIN en JSONB por defecto sobre estos paths. No se especifica unicidad ni ordenación (`asc/desc`) — quedan en defaults de Marten.

4. **AutoCreate condicional**:
    ```
    if (IsDevelopment)
        options.AutoCreateSchemaObjects = AutoCreate.All;
    ```
    En producción no se crean tablas/índices automáticamente — requiere migración manual.

#### Invocación desde la composition root

🔴 **LACUNA DE SEGURIDAD** — `Program.cs:52` (módulo App) llama:
```csharp
services.AddDataBase(context.Configuration, true);
```
El segundo parámetro está **hardcoded a `true`**, ignorando completamente la lógica condicional. Efecto: `AutoCreateSchemaObjects = AutoCreate.All` siempre activo, incluso si se despliega con `DOTNET_ENVIRONMENT=Production`. La discriminación por entorno declarada en `Services.cs:36` queda inerte. Validar con el usuario si es intencional o si debería leerse `context.HostingEnvironment.IsDevelopment()`.

### 2. Algoritmos y lógica de negocio

No aplica. El módulo no contiene algoritmos: solo configuración declarativa de Marten.

### 3. Estructuras de datos

No define entidades propias. Consume:
- `OpenScrape.Domain.Entities.GameSession` (persistido)
- `OpenScrape.Domain.Entities.HandRecord` (persistido, definido en `GameSession.cs:56`)

Ver dicccionario completo en `_reversa_sdd/data-dictionary.md`.

#### Documentos persistidos sin Schema.For explícito

🟡 **INFERIDO**: Marten autodescubre y persiste estos documentos al usarlos vía `IDocumentStore.LightweightSession()`:

| Documento | Persistido por | Indexación |
|-----------|----------------|------------|
| `Card` | `CardCacheService.cs:33` | solo PK (`Id`) |
| `RegionTableMap` | `UpdateRegionTableMap.cs:19` | solo PK (`Id`) |
| `Table` | `GetAllTables`, `GetTable` (GetAll/Get use cases) | solo PK (`Id`) |

Sin índices secundarios → consultas por campos no-`Id` hacen full scan sobre `data->>'campo'`. Aceptable porque las cardinalidades son bajas (≤52 cartas, ≤10 regiones, ≤decenas de mesas).

### 4. Metadatos y configuraciones

#### Connection string

```
Server=ep-solitary-grass-abau81p4-pooler.eu-west-2.aws.neon.tech;
Database=neondb;
Username=neondb_owner;
Password=npg_O4jGNATq9wMu;
SSL Mode=VerifyFull;
Channel Binding=Require;
```
- Origen: **Neon Postgres** (managed, eu-west-2).
- Pooler activado → conexiones efímeras OK (`await using var session`).
- TLS verificado + Channel Binding requerido (resistente a MITM).

🔴 **LACUNA / ANOMALÍA DE SEGURIDAD** — La misma cadena con credenciales reales aparece en:
- `appsettings.json` (committed)
- `appsettings.Development.json` (gitignored, pero también committed por accidente o por arrastre histórico)

CLAUDE.md declara explícitamente: *“appsettings.json — Contains strategy config, thresholds, and **placeholder credentials (`CHANGE_ME`)**. Safe to commit.”* — **discrepancia confirmada**. La password expuesta en histórico de Git debe rotarse y `appsettings.json` debe revertirse a placeholder.

#### Encrypter

🟡 **INFERIDO** — `appsettings.json` y `Development.json` definen también:
```
"Encrypter": { "Key": "8UHjPgXZzXCGkhxV2QCnooyJexUzvJrO" }
```
Ningún consumidor de esta clave aparece en el módulo Infrastructure ni en Domain. Habrá que validar en App qué servicio la utiliza (probable cifrado de campos sensibles o de licencia futura — ver openspec/login-sistema-licencias).

#### Supresiones

| Símbolo | Lugar | Motivo |
|---------|-------|--------|
| `NU1902` | `OpenScrape.Infrastructure.csproj:8` | Vulnerabilidad transitiva conocida en `OpenTelemetry.Api` (dependencia de Marten 8.24.0). Suprimida con comentario explicativo. |

### 5. Fluxograma del único método

Ver `_reversa_sdd/flowcharts/OpenScrape.Infrastructure.md`.

### Mapeo a archivos legacy

Ver `_reversa_sdd/OpenScrape.Infrastructure/legacy-mapping.md`.

### Anomalías detectadas (resumen)

| Severidad | Anomalía | Evidencia |
|-----------|----------|-----------|
| 🔴 alta | `IsDevelopment` hardcoded a `true` en composition root | `src/OpenScrape.App/Program.cs:52` |
| 🔴 alta | Credenciales reales en `appsettings.json` (committed) | `src/OpenScrape.App/appsettings.json:3` vs `CLAUDE.md` |
| 🟡 media | Vulnerabilidad NU1902 suprimida sin plan de actualización | `OpenScrape.Infrastructure.csproj:8` |
| 🟡 media | Sin validación explícita de `DefaultConnection` (uso de `!`) | `Services.cs:16` |
| 🟢 informativa | Documentos `Card`/`Table`/`RegionTableMap` sin índices secundarios | autodescubrimiento Marten |

---

## Módulo: `OpenScrape.Features` 🟢

**Path:** `src/OpenScrape.Features/`
**Framework:** .NET 10.0 · `Nullable` enabled · `ImplicitUsings` enabled
**Dependencias externas:** `Marten 8.24.0`, `Ardalis.Result 10.1.0`
**Dependencias internas:** `OpenScrape.Domain` (entidades persistidas, DTOs, mappers, value objects, enums)
**Tipos públicos:** 14 (5 use cases activos + 5 use cases vacíos/legacy + 5 records aggregator + 1 `Services` static + 2 requests) en 16 archivos C#
**LOC:** ~370 (sin contar `obj/`)

### Propósito

Capa de **casos de uso scoped** organizados en feature folders siguiendo Clean Architecture vertical. Cada feature aporta:
- 1+ clase concreta `*UseCase` con un método `ExecuteAsync` (operación CRUD-ish sobre Marten o lógica de selección).
- 1 record `*UseCases` que actúa como agregador (composite injection root) con los use cases de la feature.

Los consumidores externos (capa `App`) inyectan únicamente los records aggregator (ej `TableUseCases`, `ActionScenarioUseCases`) — **patrón Facade ligero** sin interfaces ni abstracciones extra. Ardalis.Result envuelve los retornos para errores tipados (excepto `GetActionScenario`, que retorna `string` y propaga excepciones).

### Estructura

| Feature folder | Use cases | Aggregator | Persistencia | Ardalis.Result |
|----------------|-----------|------------|--------------|:----:|
| `ActionScenario/` | `GetActionScenario` | `ActionScenarioUseCases` | indirecta vía `TableUseCases.GetTable` | ❌ |
| `Table/` | `GetTable`, `GetAllTables` (vacío) | `TableUseCases` | `Table` (autodescubierto) | ✅ (solo `GetTable`) |
| `Cards/` | `GetAllCards`, `GetFlopCards` (vacío) | `CardUseCases` | `Card` (autodescubierto) | ✅ (solo `GetAllCards`) |
| `RegionsTableMap/` | `UpdateRegionTableMap`, `GetAllRegionTableMap` (vacío) | `RegionTableMapUseCases` | `RegionTableMap` | ✅ (solo `Update`) |
| `GameRound/` | `GetRecentGameRounds` | `GameRoundUseCases` | `GameSession` | ❌ (retorna `List<GameSession>`) |

🟡 **INFERIDO**: 3 de 9 use cases existen pero están vacíos (solo constructor + DI del `IDocumentStore`):
- `GetAllTables` — sin método `Execute*`, no usado por nadie.
- `GetAllRegionTableMap` — método comentado dentro del código.
- `GetFlopCards` — solo declara un record vacío `GetFlopCardsResponse()`. Ni siquiera registrado en `Services.cs`.

Probables vestigios de scaffolding inicial nunca completado (ver Anomalías al final).

### Registro DI — `Services.cs`

```csharp
public static IServiceCollection AddUseCases(this IServiceCollection services) =>
    services
        .AddScoped<GetActionScenario>()       .AddScoped<ActionScenarioUseCases>()
        .AddScoped<GetTable>()                .AddScoped<TableUseCases>()
        .AddScoped<GetAllCards>()             .AddScoped<CardUseCases>()
        .AddScoped<GetAllRegionTableMap>()
        .AddScoped<UpdateRegionTableMap>()    .AddScoped<RegionTableMapUseCases>()
        .AddScoped<GetAllTables>()
        .AddScoped<GetRecentGameRounds>()     .AddScoped<GameRoundUseCases>();
```

🟢 **CONFIRMADO** — todos los use cases registrados como `Scoped` (un ciclo de vida por scope; consistente con la inyección desde `FrmMain` resuelto desde scoped provider, ver memoria del proyecto).
🔴 **LACUNA** — `GetFlopCards` no aparece en `Services.cs`, refuerza tesis de dead code. `GetAllTables` se registra pese a no tener `Execute*`.

### 1. Flujo de control y funciones principales

#### `GetActionScenario.ExecuteAsync(GameSituation, ActionScenarioRequest)` — `ActionScenario/Get/GetActionScenario.cs:16`

🟢 **Núcleo del módulo**: única función con lógica de negocio no trivial. Resuelve la acción preflop a recomendar para una situación dada (ej. ThreeBet, Squeeze, Cold4Bet) consultando el "playbook" persistido en Marten como documentos `Table`.

Pasos:

1. **Lookup de la mesa por situación**: `tableUseCases.GetTable.ExecuteAsync(situation.GetDescription())`. La descripción del enum `GameSituation` actúa como **clave primaria** del documento Marten `Table`.
2. **Validación de existencia**: si `table?.Value == null` → `throw new Exception("Table not found")` (se rebobina como `Exception(...)` en el `catch` exterior).
3. **Filtro multi-criterio sobre `Positions`** — busca el primer `PlayerActionSequence` que matchee:
    - `HeroPosition == request.HeroPosition?.GetDescription()` (siempre comparado).
    - 6 criterios opcionales — solo se filtran si el campo del request no es `null` (`OpenRaiser`, `ThreeBetPosition`, `Limper`, `Caller`, `Squeezer`, `IsGreater`, `RaiserFolds`). El campo `BetSize` está **comentado** (línea 31) — anomalía: existe en request y entidad pero no se aplica.
4. **Filtro de manos compatibles**: `?.Hands.Where(f => f.Name == request.HandName && f.Suited == request.Suited)`. Compara por nombre canónico (ej. `"AKs"`, `"QQ"`) y flag suited (nullable).
5. **Selección probabilística**: `GetRandomAction(hands.ToList())` — aleatorización ponderada (ver Algoritmos).
6. **Fallback**: si no hay match → `"Fold"`. Si la acción aleatorizada es `string.IsNullOrEmpty` → también `"Fold"`.

Ramas que devuelven `"Fold"` (siempre seguro): `Hands == null`, `actions.Count == 0`, error de redondeo en cumulative percentages.

#### `GetActionScenario.GetRandomAction(List<Hand> actions)` — `:51`

🟢 **Selección ponderada acumulada**:

```
1. Si la lista está vacía → string.Empty
2. Sum(Percentage) DEBE ser 100 (else throw ArgumentException)
3. Random.Next(1, 101) → randomNumber ∈ [1, 100]
4. Recorre acciones acumulando Percentage:
     accumulated += action.Percentage
     si randomNumber <= accumulated → return action.Action
5. Fallback final: actions.Last().Action ?? string.Empty
```

🔴 **LACUNA**: usa `new Random()` instanciado por llamada. En .NET 10 `Random.Shared` (thread-safe) sería mejor; con varios use cases concurrentes resolviendo la misma situación en el mismo tick podrían generar la misma seed (poco probable en .NET 10 — `Random()` ya usa un seed independiente por instancia desde .NET 6 — pero la asignación es ineficiente).

🟡 **INFERIDO**: la doble validación `actions.Count != 0` seguida de `if (!actions.Any())` (líneas 53 y 56) es redundante. Probablemente vestigio de un refactor previo.

#### `GetTable.ExecuteAsync(string name)` — `Table/Get/GetTable.cs:17`

🟢 **CRUD único de Table**:
1. `_documentStore.QuerySession()` (read-only — observar inconsistencia con resto del módulo, ver Anomalías).
2. `session.Query<Domain.Entities.Table>().FirstOrDefaultAsync(x => x.Id == name)`.
3. `null` → `Result<TableDTO?>.NotFound()`.
4. Mapper `table.ToDto()` (ver `TableDTOMapper.cs`: `Table.Id` → `TableDTO.Name`, copia `Positions` ítem a ítem).
5. Excepciones envueltas en `Result.CriticalError(ex.Message)` — no se relanza.

#### `GetAllCards.ExecuteAsync()` — `Cards/GetAll/GetAllCards.cs:17`

🟢 Lista completa del catálogo de cartas (52 documentos esperados):
1. `QuerySession()` + `session.Query<Card>().ToListAsync()`.
2. `null || !Any()` → `NotFound`.
3. Mapper bidireccional vía `card.ToDto()` (`Card.Id` → `CardDTO.Name`).
4. Excepciones → `Result.CriticalError`.

🟡 **INFERIDO**: Se solapa con `CardCacheService` (singleton lazy en `App` que carga 52 cartas una sola vez por vida del proceso). `GetAllCards` se mantiene **solo para tooling** (carga manual de cartas en UI Tablas/Config, ver memoria del proyecto). Posible candidato a migrar a `CardCacheService` directamente.

#### `UpdateRegionTableMap.ExecuteAsync(UpdateRegionTableMapRequest, CancellationToken)` — `RegionsTableMap/Update/UpdateRegionTableMap.cs:15`

🟢 **Edición no destructiva de regiones de captura OCR**:
1. `_documentStore.LightweightSession()` (write-mode, sin tracking).
2. `session.LoadAsync<RegionTableMap>(request.Category, ct)` — clave primaria = `Category`.
3. `null` → `Result.NotFound()`.
4. **Búsqueda de la región existente**: `region.Regions?.FirstOrDefault(r => r.Name == request.Name)`.
5. **Si existe**: la elimina con `region.Regions?.Remove(regionToRemove)` y **preserva sus flags semánticos** (`IsHash`, `IsColor`, `IsBoard`, `IsOnlyNumber`).
6. **Construye una nueva región**:
    ```csharp
    new Region(
      Category, Name,
      PosX, PosY, Width, Height,         // del request (geometría nueva)
      regionToRemove?.IsHash,            // preservado
      regionToRemove?.IsColor,           // preservado
      regionToRemove?.IsBoard,           // preservado
      request?.Color,                    // del request (nullable, sobre-defensivo)
      regionToRemove?.IsOnlyNumber,      // preservado
      request?.InactiveUmbral,           // del request
      request?.Umbral                    // del request
    )
    ```
7. La añade a `region.Regions` y persiste con `session.Store(region) + SaveChangesAsync(ct)`.
8. Excepciones → `Result.CriticalError(ex.Message)`.

🟡 **INFERIDO**: el patrón "remove + add" no es atómico ni transaccional dentro de la lista en memoria. Si `Remove` no encuentra match (mismatch de `Name`), la nueva región se añade junto a una potencialmente vieja con el mismo nombre. Validar comportamiento en colisiones.

🟡 **INFERIDO**: el request **acepta** flags `IsHash`, `IsColor`, `IsBoard`, `IsOnlyNumber` (`UpdateRegionTableMapRequest.cs:3`), pero el método **los ignora** y siempre preserva los del registro existente. Inconsistencia: o el request debería usarlos (caso de creación inicial cuando `regionToRemove == null`) o deberían eliminarse del DTO. Bug latente: si la región no existe, los flags quedan `null` para siempre.

🟡 **INFERIDO**: `request?.Color`, `request?.InactiveUmbral`, `request?.Umbral` con null-conditional sobre `request` que **nunca puede ser null** (parámetro no anulable, no validado pero no admite null en su firma). Sobre-defensividad inerte.

🔴 **LACUNA**: la reasignación a `region.Regions?.Add(regionCategory)` con null-conditional en una colección persistible — si `Regions == null`, la nueva región se descarta silenciosamente sin error. Validar si Marten autodescubrimiento garantiza colección inicializada.

#### `GetRecentGameRounds.Execute(int count = 20)` — `GameRound/GetRecentGameRounds.cs:15`

🟢 Listado paginado de las últimas N sesiones:
1. `_documentStore.QuerySession()` con `await using`.
2. `Query<GameSession>().OrderByDescending(s => s.EndTime).Take(count).ToListAsync()`.
3. Sin envoltorio Ardalis.Result → retorna `List<GameSession>` directo (inconsistencia con resto del módulo).
4. Sin try/catch → propaga excepciones de Marten al consumidor.

Aprovecha el índice `EndTime` declarado en `Infrastructure/Services.cs:22`.

### 2. Algoritmos y lógica embebida

| Algoritmo | Propósito | Ubicación | Complejidad |
|-----------|-----------|-----------|-------------|
| **Weighted random sampling acumulado** | Selecciona acción según percentages que suman 100 | `GetActionScenario.cs:51` | O(N) lineal |
| **Multi-criteria filter chain** | Filtrado opcional con 7 criterios (skip si `null`) sobre `Table.Positions` | `GetActionScenario.cs:24-33` | O(P × 7) |
| **Hand match by canonical name+suited** | `Name == HandName && Suited == request.Suited` | `GetActionScenario.cs:35` | O(H) |
| **Region preserving update** | Reemplaza región conservando flags semánticos | `UpdateRegionTableMap.cs:23-46` | O(R) |
| **Table lookup by description** | `GameSituation.GetDescription()` → Marten `Table.Id` | `GetActionScenario.cs:20`, `GetTable.cs:24` | O(log N) índice PK |

#### Detalle: filter chain con cortocircuito por null

```csharp
.Where(w => w.HeroPosition == request.HeroPosition?.GetDescription()
         && (request.OpenRaiser == null || w.OpenRaiser == request.OpenRaiser.GetDescription())
         && (request.ThreeBetPosition == null || w.ThreeBetPosition == request.ThreeBetPosition.GetDescription())
         && (request.Limper == null || w.Limper == request.Limper.GetDescription())
         && (request.Caller == null || w.Caller == request.Caller.GetDescription())
         && (request.Squeezer == null || w.Squeezer == request.Squeezer.GetDescription())
         //&& (request.BetSize == null || w.BetSize == request.BetSize)   ← comentado
         && (request.IsGreater == null || w.IsGreater == request.IsGreater)
         && (request.RaiserFolds == null || w.RaiserFolds == request.RaiserFolds))
```

**Patrón uniforme**: cada criterio es un OR entre "campo no especificado" y "campo igual al esperado". Cuando todos los opcionales son `null`, el filtro se reduce a `HeroPosition == request.HeroPosition?.GetDescription()`. Útil para encontrar acción cuando la situación no requiere todos los datos posicionales.

🔴 **LACUNA — filtro `BetSize` desactivado**: la línea está comentada pero el campo existe tanto en `ActionScenarioRequest` como en `PlayerActionSequence`. Probable abandono de filtro por bet size en algún refactor; se debe validar con el usuario si es deuda técnica o decisión de diseño (la magnitud del raise se delega al `IsGreater` boolean).

### 3. Estructuras de datos relevantes

#### Records aggregator (Composite UseCases — pattern uniforme)

5 records de una sola línea, todos públicos, todos receivers de DI:

```csharp
public record ActionScenarioUseCases(GetActionScenario GetActionScenario);
public record TableUseCases(GetTable GetTable, GetAllTables GetAllTables);
public record CardUseCases(GetAllCards GetAllCards);
public record RegionTableMapUseCases(GetAllRegionTableMap GetAllRegionTableMap, UpdateRegionTableMap UpdateRegionTableMap);
public record GameRoundUseCases(GetRecentGameRounds GetRecentGameRounds);
```

Patrón consistente: el consumidor inyecta **un único record por feature** y accede a los use cases concretos por propiedad. Reduce la superficie de inyección sin introducir interfaces.

#### Requests DTO

| Request | Tipo | Campos |
|---------|------|--------|
| `ActionScenarioRequest` | `class` (mutable) | `HandName`, `Suited?`, 6 `TablePosition?` (Hero/OpenRaiser/ThreeBetPosition/Limper/Caller/Squeezer), `BetSize?` (decimal), `IsGreater?`, `RaiserFolds?` |
| `UpdateRegionTableMapRequest` | `record` (positional) | `Category`, `Name`, `PosX`, `PosY`, `Width`, `Height`, `Umbral` (double), `InactiveUmbral` (double), `Color`, 4× `bool?` (`IsColor`, `IsHash`, `IsOnlyNumber`, `IsBoard`) |

🟡 **INCONSISTENCIA**: `ActionScenarioRequest` es `class` con `init` setters, pero el resto de DTOs en el módulo (`UpdateRegionTableMapRequest`) usan `record`. Sin razón aparente para la divergencia.

### 4. Constantes, defaults y configuración

#### Defaults sin documentar

- `GetRecentGameRounds.Execute(int count = 20)` — **20 sesiones por defecto** (alineado con CLAUDE.md: "GameSession — Marten document, one per table session. Keeps last 20 HandRecord").
- `GetActionScenario` retorna `"Fold"` como acción por defecto cuando no hay match. **Convención**: el código consumidor (`SetPreflopActionUseCase`) trata `"Fold"` como sentinel de "no hay regla para esta combinación" y continúa al siguiente camino (Squeeze → OpenRaise → Cold4Bet → RaiseOverLimper → 3Bet).

#### Tipos de sesión Marten — cuadro

| Use case | Sesión | Justificación |
|----------|--------|---------------|
| `GetTable` | `QuerySession` | read-only |
| `GetAllCards` | `QuerySession` | read-only |
| `GetRecentGameRounds` | `QuerySession` (`await using`) | read-only |
| `UpdateRegionTableMap` | `LightweightSession` | escritura sin tracking |
| `GetAllRegionTableMap` | (comentado, sin usar) | — |
| `GetActionScenario` | indirecto vía `GetTable` | hereda `QuerySession` |

🟢 **CONFIRMADO** — uso uniforme: `QuerySession` para read, `LightweightSession` para write. **Inconsistencia menor**: `GetTable` usa `using var session = …` (sync dispose); `GetRecentGameRounds` usa `await using var session` (async dispose, correcto en .NET 10). El primero podría leakear conexiones bajo carga.

### 5. Tratamiento de errores

| Use case | Estrategia | Anti-patrón |
|----------|-----------|-------------|
| `GetActionScenario` | `try/catch` → `throw new Exception($"Error executing {situation}: {ex.Message}")` | 🔴 envuelve en `Exception` base, **pierde stack trace** y tipo original. Debería usar `throw;` o relanzar con `InnerException` |
| `GetTable` | `try/catch` → `Result<TableDTO?>.CriticalError(ex.Message)` | ✅ |
| `GetAllCards` | igual | ✅ |
| `UpdateRegionTableMap` | igual | ✅ |
| `GetRecentGameRounds` | sin `try/catch` → propaga al caller | 🟡 inconsistente; el consumidor (`FrmMain`) deberá manejarlo |

### 6. Acoplamientos y patrones

#### Mapeo `Domain ↔ Marten` — convención `Id ↔ Name`

🟢 **CONFIRMADO** — los mappers `CardDTOMapper` y `TableDTOMapper` colapsan `Entity.Id` (string PK Marten) a `DTO.Name` para la UI. Tradeoff:
- Pro: la UI no expone IDs internos.
- Contra: cualquier consulta DTO→Marten debe re-traducir manualmente (no hay método `GetByName(name)` que esconda esto).

#### Acoplamiento al `Domain.Mappers`

`GetTable` y `GetAllCards` invocan métodos extensión definidos en `OpenScrape.Domain.Mappers` — el módulo `Features` actúa como cliente de la capa de mapping. **Aceptable**: los mappers son DTO↔Entity puros sin lógica de aplicación.

#### Composite root externo (`SetPreflopActionUseCase` en `App`)

🟡 **OBSERVACIÓN**: `SetPreflopActionUseCase` (en `OpenScrape.App.Aplication`) recibe `ActionScenarioUseCases` por DI y **luego instancia con `new` 10 wrappers** (`GetActionOpenRaiseUseCase`, `GetActionSqueezeUseCase`, etc.) en su constructor. Esto rompe parcialmente el patrón DI: la cadena `App.UseCases.Actions.*` no está registrada en `Services.cs` ni en el contenedor.

Memoria del proyecto confirma este punto (`SetPreflopActionUseCase Singleton→Scoped` resuelto, pero los inner wrappers siguen creándose con `new`). No es un bug de Features per se, pero condiciona la arquitectura.

### 7. Anomalías detectadas (resumen)

| Severidad | Anomalía | Evidencia |
|-----------|----------|-----------|
| 🔴 alta | `GetActionScenario` envuelve excepciones en `Exception` base — pierde tipo + stack trace | `GetActionScenario.cs:46` |
| 🔴 alta | Filtro `BetSize` comentado pero campos siguen en `ActionScenarioRequest` y `PlayerActionSequence` | `GetActionScenario.cs:31` |
| 🟡 media | 3 use cases vacíos (`GetAllTables`, `GetAllRegionTableMap`, `GetFlopCards`) — código muerto registrado en DI | `Services.cs:32`, `GetAllTables.cs`, `GetAllRegionTableMap.cs`, `Cards/GetFlop/GetFlopCards.cs` |
| 🟡 media | `UpdateRegionTableMap` ignora flags `IsHash/IsColor/IsBoard/IsOnlyNumber` del request — solo preserva los del registro existente | `UpdateRegionTableMap.cs:30-44` vs `UpdateRegionTableMapRequest.cs:3` |
| 🟡 media | `GetActionScenario.GetRandomAction` — `new Random()` por llamada en lugar de `Random.Shared` | `GetActionScenario.cs:64` |
| 🟡 media | `GetTable` usa `using var` (sync dispose) — el resto del módulo usa `await using var` | `GetTable.cs:21` vs `GetRecentGameRounds.cs:17` |
| 🟡 media | `GetRecentGameRounds` no envuelve en Ardalis.Result — inconsistencia de patrón | `GetRecentGameRounds.cs:15` |
| 🟡 baja | Namespace inconsistente: `Cards/CardUseCases.cs` declara `OpenScrape.Features.Card` (singular), `Cards/GetFlop/GetFlopCards.cs` declara `OpenScrape.Features.Cards.GetFlop` (plural) | `CardUseCases.cs:3` vs `Cards/GetFlop/GetFlopCards.cs:8` |
| 🟡 baja | `ActionScenarioRequest` es `class` con setters `init`-style; resto de requests del módulo son `record` | `ActionScenarioRequest.cs:5` |
| 🟡 baja | Doble validación redundante en `GetRandomAction` (`Count != 0` + `Any()`) | `GetActionScenario.cs:53,56` |
| 🟡 baja | Sobre-defensividad: `request?.Color` cuando `request` no puede ser null | `UpdateRegionTableMap.cs:40-43` |
| 🟢 informativa | Uso uniforme de Scoped para todos los use cases, alineado con consumidor `FrmMain` resuelto desde scope | `Services.cs` |

### 8. Métricas

| Métrica | Valor |
|---------|-------|
| Archivos `.cs` (no `obj/`) | 16 |
| LOC estimado | ~370 (sin csproj) |
| Tipos públicos | 14 |
| Use cases con lógica | 5 (GetActionScenario, GetTable, GetAllCards, UpdateRegionTableMap, GetRecentGameRounds) |
| Use cases vacíos / dead code | 3 (GetAllTables, GetAllRegionTableMap, GetFlopCards) |
| Records aggregator | 5 |
| Reglas de negocio extraídas | 8 |
| Algoritmos identificados | 2 (weighted random, multi-criteria filter) |
| Anomalías arquitectónicas | 12 |
| Dependencias externas | 2 (`Marten`, `Ardalis.Result`) |

### 9. Diagrama de relaciones (resumen)

Ver `flowcharts/OpenScrape.Features.md` para el diagrama Mermaid completo. Resumen de capas:

```
OpenScrape.Domain.Entities ←─ OpenScrape.Features.* ─→ Marten.IDocumentStore
                              │
                              ├─ ActionScenario.GetActionScenario ──→ TableUseCases
                              ├─ Table.{GetTable, GetAllTables}
                              ├─ Cards.GetAllCards
                              ├─ RegionsTableMap.UpdateRegionTableMap
                              └─ GameRound.GetRecentGameRounds
                              ↑
              OpenScrape.App.* (consumidor: FrmMain + SetPreflopActionUseCase)
```

Flowchart por función crítica: ver `flowcharts/OpenScrape.Features-GetActionScenario.md` (lógica completa del selector preflop).

### Diccionario de datos completo

Ver sección `OpenScrape.Features` en `_reversa_sdd/data-dictionary.md`.

### Mapeo a archivos legacy

Ver `_reversa_sdd/OpenScrape.Features/legacy-mapping.md`.

---

## Módulo: `OpenScrape.DecisionMaker` 🟢

**Path:** `src/OpenScrape.DecisionMaker/`
**Framework:** .NET 10.0 · `Nullable` enabled · `ImplicitUsings` enabled
**Dependencias externas:** `Marten 8.24.0` (solo `BankrollTrackerService`), `Microsoft.Extensions.{Logging.Abstractions,Options} 10.0.3`
**Tipos públicos:** ~50 (servicios + DTOs + enums + records + structs) en 38 archivos C#
**InternalsVisibleTo:** `OpenScrape.App.Tests`
**LOC estimado:** ~6,800

### Propósito

Motor de decisión y cálculo de equity del bot de poker. Encapsula los algoritmos puros del juego (evaluación de manos, simulación Monte Carlo, outs/draws, textura de board, equity preflop), los servicios de decisión postflop (10+ paths: facing bet, no-bet, check-raise, float exit, probe bet, pot control, delayed value, c-bet mixing, bluff/semi-bluff, randomización), los calculadores especializados (danger penalty, implied odds, bet sizing, range polarizer, thresholds registry), el tracking thread-safe de oponentes, backtesting histórico, análisis estadístico y telemetría de explotabilidad GTO.

> ⚠️ **Importante:** El facade público `IPokerCalculator → UnifiedPokerCalculator` que CLAUDE.md describe como entry point del motor **no vive en este módulo**. Reside en `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs` y orquesta los servicios de DecisionMaker (`IPostflopDecisionService`, `IMonteCarloSimulator`, `IBoardTextureAnalyzer`, etc.). Este módulo expone **building blocks**, no el facade.

### Estructura

| Carpeta | Tipos | Notas |
|---------|-------|-------|
| `Algorithms/` | `BitHandEvaluator`, `MonteCarloSimulator`, `OutsCalculator`, `BoardTextureAnalyzer`, `PreflopEquityCalculator`, `HandEvaluator` (legacy) + 4 interfaces (`IBoardTextureAnalyzer`, `IMonteCarloSimulator`, `IOutsCalculator`, `IHandEvaluator`) + struct `HandScore` | Motores numéricos puros, sin dependencias en Marten ni en estado mutable |
| `DTOs/` | `PostflopDecisionInput`, `DecisionRequest`, `DecisionResult` | Records inmutables para entrada/salida de decisiones |
| `Interfaces/` | 13 interfaces de servicios (`IPostflopDecisionService`, `IPreflopAnalyzer`, `IBetSizingService`, `IDangerPenaltyCalculator`, `IImpliedOddsCalculator`, `IRangePolarizer`, `IOpponentTracker`, `IThresholdsRegistry`, `IExploitabilityCalculator`, `IBankrollTrackerService`, `IAutoCalibrationService`, `IEquityCalculatorService`, `IStrategyAnalyzerService`, `IStrategyBacktester`) | Contratos para inversión de dependencias en `OpenScrape.App` |
| `Services/` | `PostflopDecisionService` (1893 LOC), `PostflopGameContext`, `PreflopAnalyzer`, `BetSizingService`, `DangerPenaltyCalculator`, `ImpliedOddsCalculator`, `RangePolarizer`, `ThresholdsRegistry`, `OpponentTracker`, `EquityCalculatorService`, `StrategyBacktester`, `StrategyAnalyzerService`, `BankrollTrackerService`, `ExploitabilityCalculator`, `AutoCalibrationService` | Lógica de negocio del motor de decisión |
| (raíz) | `PokerConstants` (~25 constantes algorítmicas) | Constantes derivadas de la teoría del poker (no configurables por estrategia) |
| `obj/` (versionado) | net8.0, net9.0, net10.0 — **anomalía:** restos de migraciones de target | El csproj solo declara net10.0 |

### 1. Flujo de control y funciones principales

#### `PostflopDecisionService.DetermineAction(PostflopDecisionInput input)` — `Services/PostflopDecisionService.cs:69`

🟢 **Único punto de entrada postflop.** Acepta un record con 6 campos requeridos + 36 con defaults (equity, street, situation, board texture, posiciones, perfiles villano, flags cross-street, R/I outs, kicker strength, etc.).

Pipeline en orden estricto:
1. **Equity efectiva** — `equity − dangerPenalty + comboDrawBonus(textura) − reverseImpliedPenalty`, con cap por `DangerCompletedDrawNoBetCap=45` si flush/straight completó y hero no lo tiene; `Math.Max(0, …)` final.
2. **Modo simplificado** — `RaiseOverLimper` con `IsSimplified=true` corta-circuita a `DetermineSimplifiedAction` (5 bets fijos por equity tier, IP/OOP).
3. **Ajustes de thresholds** sobre `FoldBelow`/`ThinValueAbove` cargados de `ThresholdsRegistry`:
   - **Range polarizer** por textura/posición/SPR/street.
   - **Facing bet penalty** escalada por categoría (Underbet 0, Small 1, Medium 4, Large 8) × street multiplier (Turn ×1.15, River ×1.30); `+VillainAggressionPenalty=3` si villainShowedAggression.
   - **Multi-way penalty** — IP lineal (`extra × 2.0`), OOP cuadrático (`extra² × 6.0 × positionDamping(SB/BB/EP/Other) × streetMult(Turn ×1.2, River ×1.4)`); ×amplifier si villain IP+aggressor; reducido a ×0.5 con Flush+, ×0.7 con set en board no paired IP (S22.5).
   - **3-bet/4-bet/squeeze/limp-raise pot adjustment** — incrementos sobre FoldBelow y ThinValueAbove (S20.4).
   - **Blind vs Blind** dinámico (SBvsBB, BBvsSB, BBvsBTN — S20.1).
   - **Broadway-wet** (S21.3) — villano conecta más broadway combos.
   - **Agresor vs caller** — agresor: −5 FoldBelow, −3 ThinValue. Caller: +2 FoldBelow.
   - **Range narrowing** — villain apostó 2+ calles → `+RangeNarrowingPerStreet=3.0 × (n−1)`; bet-check-bet aplica multiplier ×0.5 (debilidad).
   - **Kicker quality** — TPTK −3 FoldBelow, TPWK OOP +2 FoldBelow.
   - **Villain barreling** — barrel real (bet-bet) +5 FoldBelow / +3 ThinValue; bet-check-bet +2 (reactivation).
   - **Sizing escalation** — villain subió bet size entre streets → +`VillainSizingEscalationPenalty`.
   - **Stats reales `villainFoldToBetPct`** (≥0): >60→−5, >45→−2, <30→+4, <40→+2; fallback a tipo estático (LP −4/−2, TAG 0, LAG facing −5/−3, etc.).
   - **WSD%/Barrel/DonkBet/WTSD overrides** — perfiles fiables modulan FoldBelow/ThinValueAbove (S18).
   - **SPR push/fold** — interpolación suave: zona corta `factor = 1 − spr/threshold` aplica `−PushFoldFoldReduction × factor` y `+PushFoldValueIncrease × factor`; isPushFold solo en mitad inferior; zona deep `+SPRDeepFoldIncrease × factor`.
4. **C-bet path** — agresor preflop con equity en `[FoldBelow−15, FoldBelow)` apuesta a frecuencia `GetCbetFrequency(street)` (Flop 65%, Turn 45%, River 30%) modulada por textura del runout (S19.2), broadway-wet, CheckRaise% del villain. C-bet mixing: equity media `[FoldBelow, ThinValueAbove)` chequea a `(1 − cbetFreq)` para proteger checking range (solo HU).
5. **Equity baja** → `HandleLowEquity` (semi-bluff con FE check, draw call, bluff puro, pot odds marginales, bluff catching turn/river con multiplicadores por villainType/runout/blockers, pot commitment expandido S22.7).
6. **Facing bet** → `HandleFacingBet` (push/fold mode, 3-bet pot defense S19.3, donk bet exploitation S18.1, raise vs underbet, raise con TwoPair+, OnePair en board sin flush peligroso, agresor vs donk, thin value con strong hand check, floating IP, pot commitment).
7. **No facing bet** → `HandleNoBet` (3-bet pot OOP S19.3, turn-river plan flush danger, river opportunity con draw completado, river delayed value tras check-check, check-raise OOP/IP con SPR guard S19.1 mixing, slow play, float exit con bad runout abort, probe bet, overbet con nuts, river sizing contextual, strong/value bets con sizing por SPR, pot control, stackoff planning S22.4, randomización adaptativa por villainType, thin value, double barrel con runout check).

🟢 **Tres flowcharts detallados** disponibles:
- `flowcharts/OpenScrape.DecisionMaker.md` — diagrama del módulo completo
- `flowcharts/OpenScrape.DecisionMaker-DetermineAction.md` — pipeline de `DetermineAction`
- `flowcharts/OpenScrape.DecisionMaker-MonteCarloSimulator.md` — selección híbrida exact/MC

#### `MonteCarloSimulator.CalculateEquity(...)` — `Algorithms/MonteCarloSimulator.cs:50`

🟢 **Equity híbrido** según número de community cards:

| Community | Método | Coste |
|-----------|--------|-------|
| 5 (river) | Enumeración exacta C(45,2)=990 manos | Determinístico |
| 4 (turn) | Enumeración exacta 45 rivers × C(44,2) ≈ 42K | Determinístico |
| 3 (flop) | MC paralelizado 50K iteraciones | ~±0.5% varianza |
| 0 (preflop) | MC paralelizado 30K iteraciones | ~±0.5% varianza |

Optimizaciones críticas:
- `BitHandEvaluator.EvaluateHandScore` retorna `HandScore` struct (long compuesto: rank + 5 kickers), comparación O(1) sin allocations en heap.
- `ThreadLocal<CardDataOuts[]>` deck por thread + `ThreadLocal<List<CardDataOuts>>` para hand/opponent buffers — zero allocations por iteración.
- `Parallel.For` con `LocalInit/LocalFinally` y `Interlocked.Add` para acumular sin lock.
- `BuildVillainCombos` pre-expande el rango y descarta combos bloqueados antes del MC.
- `precomputedTotalWeight` calculado una vez (no por iteración).
- `TryDrawFromRange` con 20 intentos antes de skip-on-block; iteraciones skipped no contaminan equity (`SkippedSimulations` separado).
- Fiabilidad: `IsReliable=false` si `BlockedComboPercentage > UnreliableThreshold=20%` (más del 20% del rango villano bloqueado por hero/board).

#### `BitHandEvaluator.EvaluateBestHand(List<CardDataOuts>)` — `Algorithms/BitHandEvaluator.cs:26`

🟢 **Bit-manipulation puro** que sustituye al brute-force `C(7,5)=21` combinaciones del `HandEvaluator` legacy. Usa:
- `Span<int> rankCount = stackalloc int[15]` — conteo de cartas por rank en stack.
- `Span<int> suitCount = stackalloc int[5]` — conteo por palo.
- `int rankBits` — OR de todos los ranks (para detección de straight).
- `Span<int> suitRankBits = stackalloc int[5]` — bitmask de ranks por palo (para straight flush).

Fases: (1) escaneo único, (2) detectar flush, (3) straight flush o flush, (4) grupos (quads/trips/pairs), (5) straight, (6) clasificación jerárquica.

`FindStraightHigh(bits)` itera de high=14 hasta 6, verifica `(bits & 0x1F << (high-4)) == mask`. Wheel = `(1<<14) | (1<<2) | (1<<3) | (1<<4) | (1<<5)`, retorna 5 (no 14).

🟢 `EvaluateHandScore` (línea 253) es la versión zero-alloc: misma lógica pero retorna `readonly struct HandScore` con `CompositeScore` (long con `rank<<20 | k1<<16 | k2<<12 | k3<<8 | k4<<4 | k5`) que permite comparación O(1) vía `long.CompareTo`. Usado intensivamente en MC (~42K evaluaciones por turn).

#### `OutsCalculator.CalculateOuts(...)` — `Algorithms/OutsCalculator.cs:46`

🟢 Cuenta outs aplicando **inclusión-exclusión** entre flush y straight:
```
totalOuts = flushOuts + straightOuts − overlapOuts + overcardOuts(textura, blocker boost) + backdoorOuts
```

Detalles relevantes:
- **Overcards**: solo si `community.Count >= 3` y hero no tiene mano hecha. Outs por overcard ajustados por textura (S21.1): `Coordinated/Wet → OvercardOutsConnectedBoard`, `Paired → OvercardOutsPairedBoard`, default `OvercardOutsBase`. No doble-cuenta si el rank ya es straight-completing rank. Blocker boost si `heroBlocksTopCard` (×`OvercardOutsBlockerBoost`).
- **Backdoor flush**: solo en flop, requiere 3 cartas del mismo palo Y al menos una del hero. +`BackdoorFlushImpliedOuts=1.5`.
- **Backdoor straight**: 3 cartas del hero+board en ventana de 5 ranks consecutivos (con As=1 para wheel) Y al menos una del hero. +`BackdoorStraightImpliedOuts=1.0`.
- **S21.4 Backdoor overlap discount**: si hay flush draw + backdoor straight, descuenta `BackdoorStraightImpliedOuts × BackdoorOverlapDiscount` para evitar doble conteo (cartas del backdoor straight que comparten suit con el flush draw).
- **Tainted outs** (3 categorías): añadir la carta pone 3+ del mismo palo en board (flush draw para villano), parea el board (trips/full para villano), o crea 3 consecutivas (straight draw). Descuento: `TaintedOutsDiscountHeroStrong=0.7` si hero tiene flush draw; `TaintedOutsDiscountHeroWeak=0.3` si no. `EffectiveOuts = cleanOuts + taintedOuts × discount`.
- **Combo draw**: flush draw + (OESD || gutshot). Marca semi-bluff premium.
- **OutsToEquity**: regla del 2 (turn) y 4 (river) en `outs × cardsToCome × 2.0`.

#### `BoardTextureAnalyzer.Analyze` y `AnalyzeBoardChange` — `Algorithms/BoardTextureAnalyzer.cs`

🟢 Dos análisis ortogonales:

**`Analyze(ranks, suits)`** (línea 62): calcula `wetnessScore ∈ [0,100]` sumando 10 contribuciones (Monotone+35, TwoTone+15, Connected+20 o connectedCount×8, FlushPossibility+15, StraightPossibility+15, BroadwayHeavy+10, S21.3 BroadwayConnected+20, Paired-10, Trips-15, ExtraCards+5). Categoriza con umbrales `WetnessDryMax=15`, `SemiDryMax=35`, `SemiWetMax=60`. `SimplifiedTexture` mapea a strings retrocompatibles (`Monotone`, `Paired`, `Wet`, `Coordinated`, `Dry`).

**`AnalyzeBoardChange(previousBoard, newCard)`** (línea 153): detecta `FlushCompleted` (4+ same suit), `FlushDrawAppeared` (3 same suit nuevo), `StraightCompleted` (4+ consecutive con prevHadDraw), `BoardPaired`, `OvercardAppeared`. Calcula `DangerLevel ∈ [0,10]`: flushCompleted+4, flushDrawAppeared+2, straightCompleted+3, boardPaired+2, overcardAppeared+1.

**`AnalyzeInitialBoard`** (línea 349): variante para flop (no es "cambio" sino estado inicial). 2+ same suit = `flushDrawPresent` (DangerLevel+1), 3+ = `flushPossible` (DangerLevel+3). Distinción crítica: `dangerousFlushBoard` en `PostflopDecisionService` solo bloquea raise con OnePair en flop si es **monotone** (DangerLevel ≥ 3), no con simple FlushDrawAppeared.

**`ClassifyRiverCard`** (S22.2, línea 227): `Scare` si flush/straight completed o overcard, `Neutral` si boardPaired o flushDrawAppeared, `Blank` si DangerLevel = 0. Usado para ajustar bet sizing y bluff catch en river.

#### `PostflopGameContext` (record inmutable) — `Services/PostflopGameContext.cs`

🟢 Estado cross-street que vive en `IPostflopContextHolder` (en `OpenScrape.App.Services`) durante la mano. Cada transición devuelve una instancia nueva:
- `WithFlopState(heroBet, villainBet, isPreflopAggressor)` — actualiza HeroBetFlop/VillainBetFlop, deriva `VillainAggressorCheckedFlop = !isPreflopAggressor && !villainBet`.
- `WithTurnState(heroBet, villainBet)` — actualiza HeroBetTurn/VillainBetTurn, deriva `VillainCheckedMiddleStreet = VillainBetFlop && !villainBet` (señal bet-check).
- `TrackHeroStack(currentStack)` — detecta auto-rebuy: si `currentStack > HeroStackPreRebuy + 50m` ignora el aumento y devuelve pre como `EffectiveStack` para no contaminar profit.
- `CombineBoardChanges(previous, current)` — OR de flags, suma DangerLevel acotado a 10, prefiere CompletedFlushSuit más reciente.
- `IsVillainBarreling` (computed) = `VillainBetFlop && VillainBetTurn`.
- `HeroCheckedAllStreets` (computed) = `!HeroBetFlop && !HeroBetTurn`.

#### `OpponentTracker` — `Services/OpponentTracker.cs`

🟢 `ConcurrentDictionary<string, OpponentProfile>` thread-safe (case-insensitive). Acumula 17 contadores por jugador: VPIP/PFR/3Bet preflop, postflop bet/raise/call/fold + IP/OOP versions, c-bet opportunities, fold to c-bet, showdown (reached river, went to SD, won SD), check-raise, donk bet, barrel.

`RegisterSeatAlias(seatName, alias)`: mapea seat ("P3") → alias real ("PlayerA") en operación atómica TryAdd+TryRemove. Consumido por `GameCoordinator` en App al detectar el nombre real del jugador tras varias manos.

`GetAdjustedFoldEquity(playerId, baseFE)`: requiere `HasReliableAFData (≥10 acciones)`. Multiplicadores: LP ×1.25 (fish foldea mucho), TP ×1.10 (nit), TAG ×0.85 (reg resiste), LAG ×0.70 (no foldea fácil).

`GetFoldToBetPct(playerId)`: requiere `HasReliableFoldData (≥8 acciones facing bet)`. Devuelve `-1` sentinel si insuficiente — consumido en `PostflopDecisionService` con fallback a multipliers estáticos por tipo.

#### Servicios complementarios

- **`DangerPenaltyCalculator.Calculate`** (`Services/DangerPenaltyCalculator.cs:31`): penalty = `max(flushPct × equity, straightPct × equity) × streetMultiplier (Flop ×1.3, Turn ×1.0, River ×0.8)`. Skip si hero completó el draw correspondiente (L3). Suma flush draw apareció con penalty proporcional `× 8% equity` reducido por hand strength (TwoPair+ ×0.5, OnePair ×0.75) y blocker (L2: nut vs non-nut). Multiplica por `DangerFacingBetMultiplier=1.4` si facing bet.
- **`ImpliedOddsCalculator.CalculateImpliedOddsFactor`** (`Services/ImpliedOddsCalculator.cs:35`): factor multiplicativo `∈ [0.5, 1.0]` aplicado a pot odds. River = 1.0. Interpolación cuadrática (sqrt) entre `SPRShallowFactor` y `SPRDeepFactor` en zona intermedia. Multiplicadores: street (Flop > Turn), IP bonus, FlushDraw bonus, multiway (OOP empeora +5%/opp, IP con draw mejora −3%/opp).
- **`ImpliedOddsCalculator.CalculateReverseImpliedOdds`** (línea 98): penalty turn/river facing bet con OnePair/TwoPair en draw board. Multiplier por `PairClassification` (Overpair 1.0 → BoardPaired 2.0). Reducción si blocker. River ×0.6. **S20.3 bluff risk**: extra en turn con OnePair, draw board, villain no all-in: `BluffRiskBaseFactor × 0.55 × potRatio × villainTypeMul`.
- **`BetSizingService.CalculateDynamicBetSize`** (`Services/BetSizingService.cs:147`): modula baseSize por SPR, multiway, board (paired ×1.15, coordinated ×0.90), posición (OOP ×0.90), street (Flop ×0.90, Turn ×1.0, River ×1.10). Clamp `[0.10, 1.00]`. Discretiza a strings `Bet 1/4` ... `Bet Pot`.
- **`RangePolarizer`** (`Services/RangePolarizer.cs`): clasifica situación en `Linear|Polarized|Condensed`. Threshold adjustments: Polarized IP (-4, -3), Polarized OOP (-2, -2), Linear IP (0, 0), Linear OOP (+3, +2), Condensed (+6, +4).
- **`ThresholdsRegistry`** (`Services/ThresholdsRegistry.cs`): lookup tipado O(1) en `Dictionary<ThresholdKey, StreetThresholds>` construido en el ctor parseando claves string del `StrategyProfile`. Lanza `KeyNotFoundException` si la clave no existe (validador de arranque debe haberla detectado antes). Sustituye el acceso por string crudo eliminando el fallback silencioso.
- **`PreflopAnalyzer`** (`Services/PreflopAnalyzer.cs`): clase con métodos estáticos que implementa `IPreflopAnalyzer` (delegación trivial). `IsPreflopAggressor`, `HasRangeAdvantageOnBoard` (boards con A/K favorecen agresor 3-bet pot, low boards favorecen caller), `CalculateCbetAdjustment`, `DetectDonkBet`, `CategorizeOpponentBet` (≤30% Small, ≤70% Medium, >70% Large — Underbet NO se detecta aquí).
- **`PreflopEquityCalculator`** (`Algorithms/PreflopEquityCalculator.cs`): tabla estática de 169 manos heads-up. `AdjustEquityForOpponents`: `equity^(1 + log2(numOpp) × 0.35)` calibrado para AA(0.852) vs 5 opp ≈ 0.49.
- **`EquityCalculatorService.CalculateFullEquity`** (`Services/EquityCalculatorService.cs:44`): orquestador que combina `PreflopEquityCalculator` (community vacía) o `MonteCarloSimulator + OutsCalculator` (postflop). Genera `RecommendedAction` heurístico **con thresholds hardcoded** (0.6 RAISE, 0.3 CALL, 0.25+8outs SEMI-BLUFF) — convive en paralelo con `PostflopDecisionService` que sí lee del `StrategyProfile`.
- **`StrategyBacktester.RunBacktest`** (`Services/StrategyBacktester.cs:20`): replaya cada decisión histórica vía `IPostflopDecisionService.DetermineAction`. Compara con la acción original (función `Simplify` normaliza a 6 categorías: All-In, Raise, Fold, Call, Bet, Check). Estima impacto BB con multiplicadores conservadores (Fold→Call/Bet ×0.5, Call/Bet→Fold ×0.3, Check→Bet ×0.2, Bet→Check ×0.15).
- **`StrategyAnalyzerService`** (`Services/StrategyAnalyzerService.cs`): métricas agregadas (WinRate, BBPer100, BiggestWin/Loss) por `TablePosition`, `BoardPosition`, `HandSituation`. `EquityAccuracy` por buckets de 10% equity (≥3 muestras) — `100 − Σ|predictedEquity − actualWinRate| / nBuckets`.
- **`BankrollTrackerService`** (`Services/BankrollTrackerService.cs`): consume `Marten.IDocumentStore`. Calcula stats sobre últimas 100 sesiones: PeakBankroll, MaxDrawdown, WinRateBB100, StdDeviation, RiskOfRuin (`exp(−2 × bankroll_BB × winRate / σ²)` con clamp -700, niveles `<0.05 Green / <0.15 Yellow / ≥0.15 Red`), LimitRecommendation.
- **`ExploitabilityCalculator`** (`Services/ExploitabilityCalculator.cs`): registra hasta `MaxRecords=10000` decisiones (FIFO con `ConcurrentQueue`). Compara EV de la decisión tomada vs mejor respuesta GTO simplificada. `ExploitabilityMbb = (bestEV − ourEV) × 100 / BigBlind` (BigBlind constante = 1.0). Categoriza leaks (OverBluffing/OverCalling/UnderBluffing/UnderValue/PotOddsError) y agrega top 5 por sesión.
- **`AutoCalibrationService`** (`Services/AutoCalibrationService.cs`): toma `TopLeaks` de `SessionAnalysis`, propone ajustes capped a `MaxAdjustmentPerCycle=5.0`. Requiere ≥`MinDecisionsForCalibration=20` y `gtoDistance > ExploitabilityCalibrationThreshold=15`. Recalibration trigger cada `RecalibrateThreshold=50` decisiones.

### 2. Estructuras de datos clave

| Tipo | Kind | Propósito |
|------|------|-----------|
| `PostflopDecisionInput` | record | Input de `DetermineAction` (6 required + 36 con defaults) |
| `PostflopDecisionResult` | record | Output: Action string, Reason, IsBluff/IsBarrel/IsCheckRaise/IsFloating |
| `DecisionRequest` / `DecisionResult` | sealed record | Facade DTO consumido por `OpenScrape.App.PokerDecisionFacade` |
| `PostflopGameContext` | sealed record | Estado cross-street inmutable durante la mano |
| `BoardTextureResult` | record | Wetness score + 11 flags (monotone, paired, connected, broadway, etc.) |
| `BoardChangeResult` | record | Detección de cambios entre streets + DangerLevel [0,10] |
| `MonteCarloSimulator.EquityResult` | class (nested) | Equity + WinProb + IsReliable + BlockedComboPercentage + HandDistribution |
| `OutsCalculator.OutsResult` | class (nested) | TotalOuts + TaintedOuts + CleanOuts + EffectiveOuts + 8 flags de tipos de draw |
| `EquityCalculatorService.FullEquityAnalysis` | class (nested) | Combina equity + outs + recommendedAction + EV + potOdds |
| `BetSizingOption` | record | (Size, Label, Type) — opción discreta de bet sizing |
| `HandScore` | readonly struct | CompositeScore (long con rank+5 kickers), comparación O(1) sin alloc |
| `BacktestResult` / `DecisionDivergence` | class | Output del backtest A/B con divergencias agrupadas y estimación BB/100 |
| `StrategyAnalysisResult` y satélites | class | Métricas agregadas para UI tab Historial |
| `DecisionAnalysis` / `DecisionRecord` / `SessionAnalysis` / `LeakInfo` / `GTODistance` | class | Telemetría GTO (ExploitabilityCalculator) |
| `CalibrationResult` / `ParameterAdjustment` / `CalibrationPreview` | class | Auto-calibración de thresholds |

**Enums propios del módulo:** `BetSizeCategory` (NoBet/Underbet/Small/Medium/Large), `BoardTextureCategory` (Dry/SemiDry/SemiWet/Wet/Paired), `RiverCardType` (Blank/Neutral/Scare — S22.2), `BetSizingType` (Value/ThinValue/Bluff/Overbet), `RangeType` (Linear/Polarized/Condensed), `PostflopAction` (Bet/Raise/Call/Fold), `LeakCategory` (None/OverBluffing/OverCalling/UnderBluffing/UnderValue/PotOddsError).

### 3. Algoritmos

| Algoritmo | Función | Coste / Detalle |
|-----------|---------|-----------------|
| **Bit-manipulation hand evaluator** | `BitHandEvaluator.EvaluateBestHand` | O(7) escaneo + O(13) straight; sin LINQ ni `C(n,5)` |
| **HandScore composite key** | `HandScore.BuildComposite` | `long` con rank en bits altos + 5 kickers de 4 bits → comparación O(1) |
| **Monte Carlo híbrido** | `MonteCarloSimulator.CalculateEquity` | River exacto C(45,2), Turn exacto 45×C(44,2), Flop/Preflop MC 50K/30K paralelo |
| **Outs con inclusión-exclusión + backdoor** | `OutsCalculator.CalculateOuts` | `flush + straight − overlap + overcards(textura) + backdoor(overlap discount)` |
| **Tainted outs discount variable** | `OutsCalculator.CalculateTaintedOuts` | `cleanOuts + taintedOuts × (0.7 si hero flush draw, 0.3 si no)` |
| **Board wetness scoring** | `BoardTextureAnalyzer.CalculateWetnessScore` | 10 contribuciones con pesos en `PokerConstants.Wetness*` |
| **Board change danger detection** | `AnalyzeBoardChange` + `ClassifyRiverCard` | `DangerLevel ∈ [0,10]`; mapeo Blank/Neutral/Scare |
| **All-in EV vs fold** | `CalculateAllinEV` | `(equity/100) × (pot + stack) − (1 − equity/100) × stack` |
| **Projected river SPR (S22.4)** | `CalculateProjectedRiverSPR` | `(stack − bet) / (pot + 2×bet)` para detectar stackoff inevitable |
| **SPR push/fold smooth interpolation** | `GetSPRAdjustment` | Lineal en zona corta, lineal en zona deep; isPushFold solo en mitad inferior |
| **Implied odds cuadrática** | `CalculateImpliedOddsFactor` | `sqrt`-based interpolation entre shallow y deep factor |
| **Risk-of-ruin clásico** | `BankrollTrackerService.CalculateRiskOfRuinInternal` | `exp(−2 × bankroll_BB × winRate / σ²)`, clamp -700 |
| **Multi-way OOP cuadrático con damping posicional** | `DetermineAction` (líneas 230-284) | `extra² × OOP_penalty × posDamping × streetMult`, +amplifier si villain IP+aggressor |
| **Range narrowing por línea agresiva** | `DetermineAction` (líneas 344-356) | `+3.0 × (streets − 1) × (0.5 si bet-check-bet, 1.0 si bet-bet-bet)` |
| **Equity accuracy por buckets** | `StrategyAnalyzerService.CalculateEquityAccuracy` | Buckets de 10%, accuracy = `100 − Σ|pred − actual| / nBuckets` |
| **Hand notation expansion** | `MonteCarloSimulator.BuildVillainCombos` (vía `Domain.VillainRange`) | Pares 6, suited 4, offsuit 12 |

### 4. Constantes y configuraciones

`PokerConstants` (~25 constantes algorítmicas, no configurables por estrategia):

| Categoría | Constantes |
|-----------|-----------|
| HandEvaluator multipliers | `HighCardMultiplier=1`, `PairMultiplier=10⁶`, …, `StraightFlushMultiplier=10¹³` |
| OutsCalculator | `FlushDrawOuts=9`, `BackdoorFlushImpliedOuts=1.5`, `BackdoorStraightImpliedOuts=1.0`, `OvercardOutsPerCard=3` |
| MonteCarlo | `DeckSize=52`, `DefaultMonteCarloIterations=10_000` |
| Reglas del 2 y 4 | `TurnOutsMultiplier=2.17`, `RiverOutsMultiplier=4.35` |
| Decisión | `MinOutsForDraw=8`, `MarginalPotOddsFactor=0.80`, `MaxOpponentsForBluff=2` |
| Facing bet penalty | `Large=8.0`, `Medium=4.0`, `Small=1.0`, `VillainAggressionPenalty=3.0`, `Underbet=0.0` |
| Street multipliers | `FacingBetTurnMultiplier=1.15`, `FacingBetRiverMultiplier=1.30` |
| Multiway base | `MultiwayFoldBelowIP=2.0`, `MultiwayFoldBelowOOP=6.0`, `MultiwayThinValueIP=2.0`, `MultiwayThinValueOOP=4.0` |
| Agresor vs caller | `AggressorVsDonkFoldReduction=5.0`, `AggressorVsDonkThinValueReduction=3.0`, `CallerVsCbetFoldIncrease=2.0` |
| Bluff catch | `BluffCatchLAGMultiplier=0.80`, `LP=0.85`, `TAG=1.00`, `TP=1.20`, `BrickRunoutMultiplier=0.85`, `ScareRunoutMultiplier=1.15` |
| Pot commitment | `PotCommitmentSPRThreshold=0.5` |
| Range narrowing | `RangeNarrowingPerStreet=3.0` |
| Randomización | `RandomizationMargin=3.0`, `RandomizationBetFrequency=0.70`, `PotControlMinEquity=40.0`, `PotControlMaxEquity=55.0` |
| Wetness scoring | `WetnessMonotoneScore=35.0`, `WetnessTwoToneScore=15.0`, `WetnessConnectedScore=20.0`, … |
| Wetness umbrales | `WetnessDryMax=15.0`, `WetnessSemiDryMax=35.0`, `WetnessSemiWetMax=60.0` |
| Kicker | `StrongKickerMinRank=13`, `MediumKickerMinRank=10`, `DrawMinOuts=8`, `DrawEquityThreshold=15.0` |

### 5. Anomalías relevantes

🔴 **Críticas:**
- **`PostflopDecisionService` 1893 LOC con >40 ramas de decisión** — viola SRP; mezcla cálculo de thresholds, ajustes contextuales, generación de strings de bet sizing, mixing aleatorio y telemetría. Refactor en sub-strategies (FacingBetStrategy, NoBetStrategy, LowEquityStrategy + sub-paths) sería el camino natural.
- **`AutoCalibrationService` hardcodea `OldValue=45/40`** en `CalculateAdjustmentForLeak` en vez de leer del `StrategyProfile` actual. Resultado: el delta propuesto no es relativo al perfil cargado, las correcciones quedan desincronizadas (bug latente o dead code).
- **`Random.Shared.NextDouble()` directo en producción** para mixing en c-bet (×3 invocaciones), check-raise (×2), randomización adaptativa, donk bet, 3-bet pot probe/CR/anti-barrel/IP-call. Imposible reproducir decisiones para debugging o tests deterministas. Falta abstracción `IRandomProvider` inyectable.
- **Interfaces acopladas a tipos nested de la implementación** — `IMonteCarloSimulator` referencia `MonteCarloSimulator.EquityResult`, `IOutsCalculator` referencia `OutsCalculator.OutsResult`, `IEquityCalculatorService` referencia `EquityCalculatorService.FullEquityAnalysis`. Cambiar el tipo de retorno requiere tocar interfaz e implementación a la vez.

🟡 **Medias:**
- **`HandEvaluator` (legacy, 206 LOC, brute-force C(n,5)) coexiste con `BitHandEvaluator`** sin justificación documentada; `HandEvaluator.EvaluateHandScore` aliena memoria creando `new BitHandEvaluator()` por llamada.
- **Pot commitment block duplicado** en `HandleFacingBet` (líneas 772-795) y `HandleLowEquity` (líneas 1665-1688) — copy-paste pendiente de extraer.
- **Bloque `if` vacío** en `PostflopDecisionService.cs:1198-1203` con solo un comentario.
- **`goto skipBluffCatch`** en `HandleNoBet` (líneas 1602/1663) — refactorizable a if/else.
- **`ExploitabilityCalculator.BigBlind = 1.0` hardcoded** — implica que mbb está en unidades de 1 BB pero los `potSize` se pasan en decimal sin normalizar. Si el dominio usa unidades reales (no BB), el cálculo de mbb está mal escalado.
- **`OutsCalculator` reconstruye el deck por llamada** en vez de reutilizar un `DeckTemplate` static como hace `MonteCarloSimulator`.
- **`BankrollTrackerService.GetBankrollStats` hace N+1 queries** (un `Query<HandRecord>` dentro del foreach por sesión).
- **`BankrollTrackerService` usa `using` síncrono** en vez de `await using` (contradice CLAUDE.md).

🟢 **Bajas:**
- `BankrollTrackerService` captura excepciones genéricas sin logging.
- `PreflopAnalyzer` mezcla métodos estáticos + interface explicit implementation que delega trivialmente — la interfaz no aporta valor real.
- `EquityCalculatorService.CalculateOuts` no propaga `heroBlocksTopCard` ni `boardTexture` (pierde calibración S21.1).
- `EquityCalculatorService.GenerateRecommendation` usa thresholds hardcoded — recomendación divergente vs `PostflopDecisionService`.
- `obj/` versiona artefactos para net8.0/net9.0/net10.0 cuando solo se declara net10.0 (restos de migración).
- `PreflopEquityCalculator.LoadPreflopEquitiesFromFile / SavePreflopEquities` son public sin consumidores visibles.
- `GetHandVulnerabilityAdjustment` para `PairClassification.None` devuelve fallback ambiguo (2.0).
- `BoardTextureAnalyzer` no devuelve `Monotone` como categoría discreta — la adaptación está repartida entre productor y consumidor sin un único punto canónico.
- `OpponentTracker.GetProfile` con string vacío devuelve `OpponentProfile` nuevo (no en diccionario) — sin singleton para id-vacío.
- `IncreaseBetSize/ReduceBetSize` manipulan strings con `Contains+Replace` — frágil ante variaciones de formato.

### 6. Diagrama de dependencias

```
                            OpenScrape.Domain
                                   ↑
                                   │
          ┌────────────────────────┼────────────────────────┐
          │                        │                        │
   Algorithms/                Services/                  DTOs/
   ──────────                 ────────                   ────
   IBoardTextureAnalyzer      IPostflopDecisionService   PostflopDecisionInput
   IMonteCarloSimulator       IPreflopAnalyzer           DecisionRequest
   IOutsCalculator            IBetSizingService          DecisionResult
   IHandEvaluator             IDangerPenaltyCalculator
                              IImpliedOddsCalculator
   BitHandEvaluator           IRangePolarizer
   HandEvaluator (legacy) ──→ BitHandEvaluator           PostflopGameContext (record)
   MonteCarloSimulator   ───→ BitHandEvaluator           PokerConstants (static)
   OutsCalculator                                        ↑
   BoardTextureAnalyzer        IOpponentTracker           │
   PreflopEquityCalculator     IThresholdsRegistry  ──→  StrategyProfile.Thresholds
                               IExploitabilityCalculator
                               IBankrollTrackerService ──→ Marten.IDocumentStore
                               IAutoCalibrationService ──→ IExploitabilityCalculator
                               IEquityCalculatorService──→ MonteCarloSimulator + OutsCalculator + PreflopEquityCalculator
                               IStrategyAnalyzerService
                               IStrategyBacktester      ──→ IPostflopDecisionService
                               PostflopDecisionService  ──→ BetSizingService + RangePolarizer + ThresholdsRegistry +
                                                            DangerPenaltyCalculator (static) + ImpliedOddsCalculator (static)
                                                            ↑
                                                            │
                                              OpenScrape.App.UnifiedPokerCalculator
                                              (facade IPokerCalculator — vive en App, no aquí)
```

Ver `flowcharts/OpenScrape.DecisionMaker.md` para el diagrama Mermaid completo.

### 7. Diccionario de datos completo

Ver sección `OpenScrape.DecisionMaker` en `_reversa_sdd/data-dictionary.md`.

### 8. Mapeo a archivos legacy

Ver `_reversa_sdd/OpenScrape.DecisionMaker/legacy-mapping.md`.

---

## Módulo: `OpenScrape.App` 🟢

**Path:** `src/OpenScrape.App/`
**Framework:** .NET 10.0 · WinForms (`net10.0-windows`) · `Nullable` enabled · `ImplicitUsings` enabled
**Dependencias externas (paquetes):** Tesseract 5.2.0, OpenCvSharp4 4.10, SkiaSharp 3.119, Marten 8.24, Microsoft.Extensions.Hosting 10.0.3, Microsoft.Extensions.Logging
**Dependencias internas:** `OpenScrape.Domain`, `OpenScrape.Features`, `OpenScrape.Infrastructure`, `OpenScrape.DecisionMaker`
**Tipos públicos:** ~120 (clases + records + enums) en 124 archivos C# (~13.117 LOC sin obj/bin)

### Propósito

Application & UI layer. Es a la vez **composition root** (`Program.cs` cablea ~50 servicios via `Microsoft.Extensions.Hosting`), **entry point WinForms** (`Application.Run(FrmMain)`), **pipeline operacional** (captura ventana → OCR Tesseract → detección color píxel → orquestación de mano → invoca el motor `DecisionMaker`) y **presentación** (overlay flotante, 5 tabs FrmMain, popups Hand Detail/Detection Debug). Encapsula toda la lógica I/O del bot: Win32 P-Invoke, sesiones Marten, telemetría histograma, file logging JSON. Persistencia y decisiones quedan delegadas a sus respectivos paquetes.

### Estructura

| Carpeta | Tipos principales | Notas |
|---------|-------------------|-------|
| `Program.cs` (224 LOC) | `Program` static | Composition root, fail-fast `StrategyProfileValidator`, `CreateAsyncScope` |
| `Configuration/` | `FeatureFlags` (1 flag), `GameLoopOptions` (CaptureIntervalMs, StopTimeoutMs) | `IOptions<T>` consumibles |
| `Forms/` (7 forms + 6 designers) | `FrmMain` (4502 LOC, 5 tabs), `FrmOverlay` (561, action panel + 9 filas), `FrmHandDetail` (135, RichTextBox coloreado), `FrmDetectionDebug` (366, calibración OCR), `FormImage` (visor PNG navegable), `FormAction`, `FormListApps` (EnumWindows filtrando "NL H") | UI WinForms |
| `Services/` (38 archivos, ~5450 LOC sin Logging/) | `GameCoordinator` (793), `TableLayoutService` (664), `ScreenReaderService` (521), `OcrService` (462), `ImageCropperService` (395), `PokerHandEvaluator` (388), `DetectionLoggerService` (362), `GameLoggerService` (374), `PokerDecisionFacade` (231), `GameLoopCoordinator` (213), `GameLoopStateMachine` (166), `UiSyncService` (130), `LruCache<T,V>` (111), `PositionCalculator` (114), `StrategyProfileValidator` (97), `RegionLookupCache` (64), `CardCacheService` (44), `ColorDetectionService` (63), `OverlayPositioner` (31), `ActionFormatter` (49), `PostflopContextHolder` (35), `StrategyProfileService` (33) + 13 interfaces | Coordinación + I/O técnica |
| `Services/Logging/` | `TextBoxLogger`, `TextBoxLoggerProvider`, `TextBoxLoggerOptions`, `TextBoxLoggerExtensions` | `ILoggerProvider` que renderiza en `tbResume` con `BeginInvoke` cross-thread y rotación por `MaxLines` |
| `Telemetry/` (6 archivos) | `IMetricsCollector` + `MetricsCollector` (149), `Histogram` (88), `ScopedMeasurement` (struct), `MetricsSnapshot` (record), `TelemetryCategories` (16 categorías + DisplayOrder + SessionOnly) | Histograma logarítmico 30 buckets, thread-safe, dual: última mano + sesión |
| `Aplication/` y `Aplication/UseCases/` (40 archivos) | `UnifiedPokerCalculator` (476, fachada `IPokerCalculator`), `SetPreflopActionUseCase` (cascada 11 ramas), `OutsCalculatorUseCase` (12 tipos draw), `PotOddsCalculator`, `GetCardsFlopUseCase`/`Turn`/`River`, `Actions/Get*UseCase` (10 escenarios preflop) | Use-cases que pegan UI con Features/DecisionMaker |
| `Helpers/` (13 archivos) | `CaptureWindowsHelper` (245, P-Invoke User32+GDI32, `PrintWindow PW_RENDERFULLCONTENT`), `ImagePreprocessorHelper` (407, Parallel.For grayscale + median + contrast + binarize + deskew), `EncrypterHelper` (145, AES-CBC + SHA256), `WindowsInformationHelper` (82, `EnumWindows` filtro), `CoordinateScaler` (45), `HandHelper`, `UserHandHelper`, `ColorHelper`, `AppThemeHelper`, `ObtainActionHelper`, `PlayerRegionParser`, `Helpers/FlopHelper/*` | I/O Windows + crypto + parsing |
| `Entities/` | `PlayerGameState` (~30 props), `Player` (15 props), `BoardData`, `BoardTextures` (`TurnBoardTexture`/`RiverBoardTexture` enum 3 valores), `TableScrapeFlopResult` + `BoardTexture`/`HeroHandStrength`/`DrawingOpportunities` | DTOs operacionales del game loop |
| `Models/` | `BestHandResult` (record interno), `HandEvaluationResult`, `NormalizedCard`, `Region` | DTOs internos |
| `Data/` (15 JSON) | `OpenRaise.json`, `BBvsSB.json`, `ThreeBet.json`, `VsThreeBet.json`, `Squeeze.json`, `Cold4Bet.json`, `FourBet.json`, `RaiseOverLimpers.json`, `RaiseVsSbLimp.json`, `VsSqueeze.json`, `VsThreeBetAndCall.json`, `Cartas2.json`, `Regiones`/`Regiones3`/`RegionToTest.json`, `tableMap.json` | Estrategia preflop por escenario |
| `tessdata/`, `Resources/tessdata/` | `eng.traineddata` (×2 ubicaciones, anomalía Scout) | Tesseract OCR data |

### 1. Flujo de control y funciones principales

#### `Program.Main()` — `Program.cs:34`

🟢 STA Single-Threaded Apartment (requerido por WinForms). Lee `DOTNET_ENVIRONMENT` (default `"Development"`), construye `Host` con:
- `AddDataBase(IsDevelopment=true)` — Marten + 7 índices (delegado al módulo `Infrastructure`)
- `AddUseCases()` — registra cases del módulo `Features`
- 13 `Configure<TOption>()` — secciones de `appsettings.json`
- 4+13 algoritmos `OpenScrape.DecisionMaker` registrados como `Singleton` con **forwarding pattern** (`AddSingleton<Concrete>` + `AddSingleton<IFace>(sp => sp.GetRequiredService<Concrete>())`) — comparten instancia, Concrete inyectable directamente para tests
- 13 servicios `OpenScrape.App` `Singleton` (Ocr, Color, Cropper, ScreenReader, StateMachine, RegionCache, CardCache, etc.) y 6 `Scoped` (`GameLoggerService`, `PokerDecisionFacade`, `GameLoopCoordinator`, `UiSyncService`, `TableLayoutService`, `GameCoordinator`, `PostflopContextHolder`)
- `FrmMain` `Transient`

Tras `host.Build()`:
1. **Fail-fast `StrategyProfileValidator.Validate(profile)`** — si lanza `StrategyProfileValidationException`, `MessageBox` + `Environment.Exit(1)`. Evita arrancar la UI con perfil inválido (todos los errores acumulados en un único mensaje).
2. Inicializa `CoordinateScaler` desde `CaptureSettings` solo si `IsReferenceSet=true` y dimensiones parseables.
3. `host.Services.CreateAsyncScope()` (no síncrono porque `GameLoopCoordinator` es `IAsyncDisposable`).
4. `Application.Run(form)` y `scope.DisposeAsync().AsTask().GetAwaiter().GetResult()` en `finally`.

#### `FrmMain` ctor — `FrmMain.cs:189`

🟢 **Recibe 44 servicios** vía constructor injection. Null-checks exhaustivos con `?? throw new ArgumentNullException(nameof(x))`. Después:
- `_textBoxLoggerProvider.SetTextBoxTarget(tbResume)` — el `ILogger` empieza a flush a UI (antes bufferiza si `BufferUntilTargetReady=true`).
- `_session = GenerateRandomNumbers()` — 10 dígitos derivados de `RandomNumberGenerator.Fill` para sesión local (independiente del `SessionId` Marten).
- `InitializeHistorialTab/BankrollTab/MetricsTab` configuran `DataGridView`s con columnas y eventos.

#### `FrmMain.btnCapture_Click` — `FrmMain.cs:642`

🟢 **Pipeline canónico de juego, ~270 LOC**. Es el "main" del game loop. Llamado por:
- Click manual del usuario (modo test/debug)
- `BackgroundWorker1_DoWork` cuando detecta `IsActionColorInRange` (B≈24 ±3 sobre uAction)

Pasos resumidos (detalle en `flowcharts/OpenScrape.App.md` y `OpenScrape.App-FrmMain.md`):

1. `_cycleTimer = _metrics.Measure(TelemetryCategories.CycleTotal)` y `Interlocked.Increment(ref _cycleCounter)`.
2. Limpiar overlay (`UpdateEquityPercentage("")` etc.) y `_executeCapture = true`.
3. Si `cbTest` no está marcado: `GetImageWhilePlaying()` (PrintWindow + clone), guardar PNG, init `CoordinateScaler` en primera captura, persistir dimensiones a `appsettings.json`.
4. `SetTableHand()`: lee pot, hole cards, hand number, table name. Detecta cambio de mano vía `_tableHand` parseable o texto, con heurística anti-OCR-flake en postflop (`ratio > 100` o `lengthDiffers` → exige 3+ indicadores secundarios).
5. Si nueva mano: snapshot postflop si misma mano, `_playerGameState = new`, `gameLoopStateMachine.Reset() → HandDetected`, `contextHolder.StartNewHand()`. Restaura estado postflop guardado vía `ForceState`.
6. `HandleNewHandAsync(prevPot, prevHoleCards, prevPosition, prevHeroStack)` — `gameLogger.EndHand(prevHeroStack)` + `SaveSessionAsync` + tracker `RecordHandPlayed`/`RecordVPIP`/`RecordPFR` para todos los activos + `StartNewHandAsync(handNum, holeCard1, holeCard2, position, stack, opps, blindPosted)`.
7. **Inicialización condicional** (`needsInitialization = Players==0 || (cbTest && !isTestPostflop) || Position==None`):
   - **Full init:** `SetEmptyPlayer` → `SetSitOutPlayer` → `SetActivePlayer` → `InitializePlayers` (dealer + posiciones + aliases).
   - **Refresh:** `SetEmptyPlayer` → `SetActivePlayer` → `RefreshPlayerStates` (detecta jugadores que dejaron la mesa) → `SetDealerPlayer` solo si cambió count o Position=None.
8. `SetBetPlayer()` → `SetHeroStack()` (con auto-rebuy detect) → `RetryEmptyAliases()`.
9. `ProcessTableInfoAsync(potOddsResult)` rama preflop o postflop con retries (2× 200ms) para hole cards.
10. **Postflop:** `ProcessFlopAsync` → `pokerCalculator.Calculate` → `coordinator.DetermineFlopAction` → `gameLogger.LogStreetDecision`. Análogo turn/river con `_turnBoardTexture`/`_riverBoardTexture` adicionales.
11. `_frmOverlay.UpdateAction(_responseAction.Action)` final.

Coste por ciclo: ~1-3 segundos típico (cap a `CaptureIntervalMs=100ms` si feature flag ON, sino sin throttle más allá del polling del BackgroundWorker).

#### `FrmMain.BackgroundWorker1_DoWork` — `FrmMain.cs:2737`

🟢 Loop infinito `while(true)` ejecutado en thread dedicado vía `backgroundWorker1.RunWorkerAsync()` (arrancado desde `btnWindow_Click`). Cada iteración:
1. Verifica `_frmOverlay.Visible` (sale si no), `_handle != Zero`.
2. Captura ventana del poker (`_useCase.Execute(_handle)`).
3. `PerformEnhancedDetection(bitmap, regionAction, flop)`:
   - `LockBits` 32bppArgb directo (anti-marshaling de `GetPixel`)
   - Lee píxel central + 8 vecinos en patrón cruz (±2 px) → promedia `R/G/B`
   - `IsActionColorInRange` = `Math.Abs(avgB - 24) ≤ 3` (tolerancia color hero turn)
   - `IsFlopVisible` = `Math.Abs(flopColor.B - 255) ≤ 10`
4. `this.Invoke((MethodInvoker) ...)` ejecuta en UI thread:
   - Si `ShouldCaptureFlop` → `gameLoopStateMachine.TryTransition(FlopDetected, 3)`
   - Si `ShouldCapture` → log + `btnCapture_Click(sender, e)` reentrante
5. `LogDetectionStatistics` cada 1000 iteraciones (success rate).
6. `btnWindow_Click(sender, e)` recheck handle/move.
7. Delay adaptativo: 200ms si capturó (más lento), 100ms si no.

#### `OcrService.ExtractTextFromRegionAndDebug` — `Services/OcrService.cs:141`

🟢 Detalle completo en `flowcharts/OpenScrape.App-OcrService.md`. Resumen del flujo:
1. `lock(_lock)` (Tesseract no es thread-safe).
2. `GetCroppedBitmap(image, x, y, w, h)` → LRU cache 200 entries.
3. `ComputeDHash(cropped)` (resize 9×8, diff horizontal de luminancia → ulong 64-bit).
4. Si `_ocrCache.TryGet(hash, ...)` → fast-path con `Confidence=-1`.
5. Sino, **4 attempts con distintos preprocesamientos**: default umbral, lower (umbral-20), higher (umbral+20), contrast (×1.5).
6. Cada attempt: `ProcessBitmap` (binarización por brightness) → `InvertBitmap` (255-pixel) → `ConfigureTesseract(onlyNumber)` (`tessedit_char_whitelist`, `pageseg_mode=7` single line) → `_engine.Process(pix)` → `(text, confidence)`.
7. Best = `OrderByDescending(Confidence).ThenByDescending(Length).First`. Fallback a longest si todos confidence=0.
8. `ProcessText(text)` — `.→,` y trunc 2 decimales si parsea.

#### `ScreenReaderService.ReadBetValue/ReadStackValue/ReadHandNumber` — `Services/ScreenReaderService.cs:66/134/202`

🟢 **Multi-lectura por consenso**, todos siguen el mismo patrón:
1. `using var _ = _metrics.Measure("OcrXxx")`.
2. **Lectura 1:** `PreprocessImageForOCR` (System.Drawing grayscale + binarize 128) → `OcrService.ExtractTextFromRegionAndDebug` con `umbral` principal.
3. **Lectura 2:** `PreprocessImageForOCR` → `OcrService` con `inactiveUmbral`.
4. **Lectura 3:** `OcrService` directo sin preprocesamiento (fallback).
5. `CleanOcrNumericText` aplica regexes para tolerar `BB`, `IBS` (artefactos de "BB"), `88` final, etc.
6. `decimal.TryParse(NumberStyles.Any, CultureInfo.CurrentCulture)`.
7. **Consenso:**
   - `ReadBetValue` prioriza `ocr3` (lectura directa) si != 0; sino `ocr1==ocr2`; sino el primero != 0
   - `ReadStackValue` prefiere igualdad 3-vías, luego 2-vías, luego `ocr3` como fallback
   - `ReadHandNumber` prefiere igualdad 3-vías o 2-vías, sino `ocr3`

#### `ScreenReaderService.NormalizeBetValue/NormalizeStackValue` — `:299/349`

🟢 Corrección **post-OCR**:
- **Artefacto "8" inicial** (cuando hay separador decimal): `850→50`, `815,50→15,50`. Detectado si `parts[0].Length > 2 && parts[0][0] == '8'`.
- **Separador decimal perdido** (sin coma/punto): si `rawValue >= 300` o `rawValue > potSize × 5` y longitud ≥ 3, inserta separador a 2 decimales (`593 → 5,93`).
- Para stacks, usa umbral más alto (`>= 500` y longitud ≥ 4) porque stacks raramente > 300 BB.

#### `TableLayoutService.SetDealerPlayer` — `Services/TableLayoutService.cs:93`

🟢 Detección por color **del dealer button (dorado)**:
1. `state.Players.ForEach(p => p.Dealer = false)` (limpiar previos).
2. Para cada región `Dealer/p[0..N]dealer` con `IsColor=true`:
3. `IsDealerButtonColor(bitmap, x, y, searchRadius=3)` itera 7×7 píxeles alrededor del centro buscando `R≥200 && G≥140 && B≤80`.
4. Asigna `detectedDealerPosition` solo al primer match.
5. `SetDealerForPlayer(state, playerNumber, emptyPositions)`:
   - Si `playerNumber == 0` → hero es dealer, `Position = Button`
   - Si no, `DetermineP0Position(state, dealerPosition)` → `PositionCalculator.AssignAllPositions(dealerPosition, players)`:
     - Heads-up: dealer = SB, otro = BB
     - **Moving blinds:** SB salta SitOut consecutivos a la izquierda del dealer; BB salta SitOut a la izquierda de SB sin rebasar dealer; SitOut "consumidos" por el salto quedan sin posición ese mano
     - Resto: labels Early/Middle/CutOff según `effectiveCount = total - skippedForSB - skippedForBB`
6. `SetVillainPosition(state, p0Pos, dealerPosition)` propaga a todos los villains via `PositionCalculator.AssignVillainPositions`.

#### `GameCoordinator.DetermineFlopAction` — `Services/GameCoordinator.cs:333`

🟢 Detallado en `flowcharts/OpenScrape.App-GameCoordinator.md`. Resumen:
1. Extrae `maxBet`, `potSize`, `inPosition`, `numOpponents`, `villainStack`.
2. `GetOpponentBetSize(maxBet, potSize)`: `Underbet ≤ 15%` / `Small ≤ 30%` / `Medium ≤ 70%` / `Large > 70%`.
3. `boardTextureAnalyzer.Analyze` + `AnalyzeInitialBoard` (guarda en `contextHolder.InitialBoardDanger`).
4. `DetectDonkBet(state, maxBet, inPosition, situation)` → cross-street: hero agresor incluye `HeroBetFlop`/`HeroBetTurn` no solo preflop.
5. `PreflopAnalyzer.HasRangeAdvantageOnBoard` + `CalculateCbetAdjustment` → `effectiveEquity = rawEquity + cbetAdjustment`.
6. **Construye `PostflopDecisionInput` con 30+ campos** (street, situation, texture, IP, villain bet/profile, equity, outs efectivos S22.1, hand rank, kicker, blocker, fold equity ajustado por tracker).
7. `postflopDecisionService.DetermineAction(input)` → 10+ paths del motor (ver `OpenScrape.DecisionMaker`).
8. `TrackVillainPostflopAction`: registra `RecordPostflopAction` (Bet/Check) y `RecordCBetOpportunity` si era preflop aggressor.
9. **Actualiza `PostflopGameContext`**: `HeroBetFlop`, `VillainBetFlop`, `VillainBetSizeFlop`, `HeroFloatedFlop` si called bet, `VillainAggressorCheckedFlop`.
10. `exploitabilityCalculator.AnalyzeDecision` + `RecordDecision` (queue 10K).
11. Genera `StringBuilder` con bloque `═══ [FLOP] ═══` y delega a `_logger.LogError(logText)` que lo enruta al `tbResume` UI.
12. `gameLoggerService.LogStreetDecision(StreetDecision)` + `UpdateSituation`.

`DetermineTurnAction` y `DetermineRiverAction` siguen el mismo patrón con diferencias específicas (ver flowchart): `dangerPenalty`, `combinedBoardChange`, `riverIsAggressor` cross-street, `IsAnyoneAllIn` si `villainStack ≤ 0` en river, `RiverCardType` S22.2.

#### `GameLoopStateMachine.TryTransition(GameState, int visibleBoardCards)` — `Services/GameLoopStateMachine.cs:73`

🟢 Sobrecarga con validación de cartas visibles. `expectedMinCards`:
- `FlopDetected/FlopAction`: 3
- `TurnDetected/TurnAction`: 4
- `RiverDetected/RiverAction`: 5
- Otros: 0

Si `visibleBoardCards > 0 && expectedMinCards > 0 && visibleBoardCards < expectedMinCards` → bloquea con warning. Si `visibleBoardCards > expectedMaxCards` → warn (posible desfase) pero continúa. Delega al `TryTransition(GameState)` base, que verifica `_validTransitions[CurrentState].Contains(newState)` bajo `lock(_stateLock)`.

#### `GameLoggerService.FinalizeAndPersistHandAsync` — `Services/GameLoggerService.cs:211`

🟢 Persiste cada mano como **documento HandRecord independiente**:
1. Asigna `GameSessionId` y `EndTime`.
2. `_currentHand.Telemetry = _metrics.EndHand()` ANTES de persistir (snapshot inmutable).
3. `Interlocked.Increment(ref _sessionTotalHands)`; acumula `_sessionTotalProfit` bajo `_dbWriteLock`.
4. Agrega a `_currentSession.Hands` y trunca a últimas `MaxHandsInMemory=20` (las anteriores ya están persistidas).
5. `_dbWriteLock.WaitAsync` (`SemaphoreSlim`) → `_store.LightweightSession()` → `Store(_currentHand)` → `SaveChangesAsync` → `using` para release. Si excepción, log error.
6. `_handScope?.Dispose()` cierra correlation scope (`HandNumber`).

#### `MetricsCollector.EndHand()` — `Telemetry/MetricsCollector.cs:74`

🟢 Bajo `lock(_handLock)`:
1. Si `_currentHandId is null`, retorna `null`.
2. Itera `_categories` (excluye `TelemetryCategories.SessionOnly` = `Persistence.SaveHand`); para cada uno:
   - `lock(state.Lock)` → `ToStats(state.LastHand)` (P50/P95/Max/Count) → `state.LastHand.Reset()`.
3. Construye `TelemetryAggregate(handId, UtcNow, phases)`.
4. `_currentHandId = null`.

`Histogram.GetPercentile(percentile)`: `target = ceil(_count × percentile)`; iteración lineal de buckets acumulando hasta superar target. Coste O(buckets=30). Devuelve `BucketBoundsTicks[i]`.

`Histogram` es **logarítmico de 30 buckets**: `bound[i] = 1e-5 × 10^(i × 0.2)` segundos → cubre **10 μs hasta ~6.3 s**, sobrestima percentil hasta ~37% (nunca subestima).

#### `PokerDecisionFacade.EvaluateAsync` — `Services/PokerDecisionFacade.cs:44`

🟢 **5 fases medidas con telemetría**:
1. `Decision.Equity`: `ResolveVillainProfile` (request ya tiene profile, sino tracker.GetProfile si reliable) → `_calculator.Calculate(...)`.
2. `Decision.Texture`: `_boardTextureAnalyzer.Analyze` + `ComputeBoardChange` (flop=`AnalyzeInitialBoard`, turn/river=`AnalyzeBoardChange` con `PreviousBoard`) + `ClassifyRiverCard` si river.
3. `Decision.Profile`: `villainType` por `GetTypeForPosition(!IsInPosition)` o Unknown; `villainFoldToBetPct` si `HasReliableFoldData`.
4. `Decision.DecisionService`: construye `PostflopDecisionInput` y delega a `_decisionService.DetermineAction`.
5. `Decision.Sizing`: `ExtractBetSize(action)` → mapping fixed `{"1/4":0.25, "1/3":0.33, "1/2":0.5, "2/3":0.66, "3/4":0.75, "Pot":1.0}`.

Devuelve `DecisionResult` con `RecommendedAction`, `EquityPercent`, `Reason`, `BoardTexture`, `BetSize`, `PotOddsPercent`, `EV`, flags `IsBluff`/`IsBarrel`/`IsCheckRaise`/`IsFloating`, `CalculationDetail`.

> **Nota arquitectónica:** la `Facade` es el camino "canónico" planeado en el refactor `refactor-frmmain-coordinators`, pero hoy `FrmMain` y `GameCoordinator` consumen directamente `IPokerCalculator` y `IPostflopDecisionService` sin pasar por la facade. Es un punto de futuro cutover, no de uso actual.

#### `UnifiedPokerCalculator.Calculate` — `Aplication/UseCases/UnifiedPokerCalculator.cs:80`

🟢 **8 pasos**:
1. `PotOddsPercentage` = `betToCall / (potSize + betToCall) × 100`.
2. `EquityPercentage` = `CalculateEquity(...)`:
   - Preflop (community vacío): obtiene `VillainRange` por `(situation, position, opponentProfile)` con cascada de fallbacks (full → posicional → genérico → `OpenRaise` por defecto). Si hay range, MC con `GetAdaptiveIterations` (250→1500 según rough equity); cache por `preflop|hand|sit|opps`. Si no, lookup `_preflopEquityCalculator`.
   - Postflop: cache por `hand|comm|opps|sit`; MC con `monteCarloIterations`.
3. `OutsCalculator.CalculateOuts` → `TotalOuts`, `EffectiveOuts` (S22.1 tainted descontados), `DrawTypes`, `HasComboDraw`.
4. `_handEvaluator.EvaluateBestHand` (≥5 cartas): `HeroHandRank`, `Kicker` para top pair (≥13 Strong, ≥10 Medium, sino Weak), `PairType` via `ClassifyPair`.
5. `_boardTextureAnalyzer.Analyze` (≥3 community): `BoardTexture`, `BoardWetnessScore`.
6. `CalculateFoldEquity` con ajustes por street/IP/3bet/multiway (`× 1/(1 + 0.3×(numOpponents-1))`).
7. `CalculateExpectedValue` y `CalculateEVWithFoldEquity`.
8. `CalculateShouldCall` ajusta equity por draws/river/IP/SPR deep, compara vs pot odds.
9. `GenerateRecommendedAction` → `("Fold"|"Call"|"Bet Xx pot", betSizePercentage)`.

`ClassifyPair` distingue `Overpair`, `TopPair`, `MiddlePair`, `BottomPair`, `PocketPairUnder`, `BoardPaired` según contribución de hole cards al par.

> **Nota arquitectónica:** `UnifiedPokerCalculator` y `PokerDecisionFacade` son **dos puntos de entrada paralelos** al motor de decisión. UPC se usa hoy desde `FrmMain.ProcessFlopAsync/Turn/River` (devuelve `PokerCalculationResult` rico que se usa para overlay y log). PDF se planea como interfaz limpia para coordinator. Anomalía: dualidad pendiente de unificar.

#### `SetPreflopActionUseCase.Execute` — `Aplication/SetPreflopActionUseCase.cs:41`

🟢 **Cascada de 11 ramas según `HandSituation` y `IsSecondAction`**:
- IsSecondAction + Call → `GetHeroCallOpenRaiseAndGetSqueezeAction` → `VsSqueeze`/None
- IsSecondAction + ThreeBet/Squeeze → `GetHero3BetAndOpenRaiser4BetAction` → `FourBet`/None
- IsSecondAction + OpenRaise/RaiseOverLimper → `GetOpenRaiseVs3BetAction` → `OpenRaiseVs3Bet`
- IsSecondAction + OpenRaise/ROL + `Exist4Bet` → `GetOpenRaiseVs3BetAndCallAction` → `OpenRaiseVs3BetAndCall`
- Squeeze, 3bet, OpenRaise/ROL, Cold4Bet... cada uno con su use case dedicado
- Default: Fold

Cada `GetActionXxxUseCase` delega a `ActionScenarioUseCases.GetActionScenario.ExecuteAsync(GameSituation, request)` (módulo Features) que carga JSONs `Data/*.json` y resuelve por `(HeroPosition, HandName, Suited)`.

### 2. Algoritmos no triviales

| Algoritmo | Ubicación | Complejidad / coste |
|-----------|-----------|---------------------|
| `dHash` perceptual de imágenes (`ComputeDHash`) | `OcrService.cs:407` y `ImageCropperService.cs:166` | resize 9×8 + 64 diffs → 64-bit hash; comparación O(1) con popcount Brian Kernighan |
| Pixel similarity con terminación temprana | `ImageCropperService.CalculateSimilarity` | itera bytes ARGB 4-en-4; cada 32 píxeles verifica si `pixelesSimilares + restantes < umbral` y aborta |
| `LruCache<TKey, TValue>` thread-safe | `Services/LruCache.cs` | `Dictionary` + `LinkedList` con `lock`; `TryGet/Set/GetOrAdd` O(1) amortizado |
| Histograma logarítmico | `Telemetry/Histogram.cs` | 30 buckets `bound[i] = 1e-5 × 10^(i × 0.2)`; `Add` O(30) lineal vs binaria (más rápido en N=30); percentil O(30) |
| `PerformEnhancedDetection` 9-pixel cross sample | `FrmMain.cs:2905` | LockBits 32bppArgb; lee centro + 8 vecinos en patrón cruz ±2px; promedia avgB; tolerancia ±3 sobre B=24 (color hero turn) |
| `SetHeroStack` auto-rebuy detect | `FrmMain.SetHeroStack` | tracking `_heroStackPreRebuy`; si stack sube bruscamente a 100BB durante mano activa, no actualiza pre-rebuy → profit calculado correctamente al `EndHand` |
| `PositionCalculator.AssignAllPositions` (moving blinds) | `Services/PositionCalculator.cs:25` | SB/BB saltan tramos consecutivos de SitOut; jugadores "consumidos" quedan sin label; restantes labels según `effectiveCount` |
| `ImagePreprocessorHelper.ProcessImage` pipeline | `Helpers/ImagePreprocessorHelper.cs` | grayscale `Format8bppIndexed` con paleta + `Parallel.For` por filas + median + contrast + deskew + binarize; opcional resize ×2 si imagen pequeña |
| `EncrypterHelper` AES-CBC + SHA256 | `Helpers/EncrypterHelper.cs` | key = `SHA256(secret).ComputeHash`; IV fija = `byte[16]` ceros (debilidad criptográfica documentada) |
| `Histogram.FindBucket` | `Telemetry/Histogram.cs:65` | búsqueda lineal over 30 buckets (más rápido que binaria a este tamaño) |

### 3. Estructuras de datos clave

| Tipo | Path | Mutabilidad | Notas |
|------|------|-------------|-------|
| `PlayerGameState` | `Entities/PlayerGameState.cs` | mutable | Hero+Players+BoardCards+Pot+Position+HandSituation. Default `Players = [Player(P0)]`. Computed `HavePocketPair`, `IsSuited`. |
| `Player` | `Entities/Player.cs` | mutable | Name/Alias/Bet/Stack/Active/SitOut/Empty/HasFolded/Position/ValuePosition/`WasPreflopAggressor` |
| `BoardData` | `Entities/PlayerGameState.cs` | mutable | Name/Force/Suit/`Position` (BoardPosition)/`Location` (1-5) |
| `ResponseAction` | `Entities/PlayerGameState.cs` | mutable | Action/HandSituation/IsSecondAction |
| `OcrResult` | `Services/OcrService.cs:448` | mutable + IDisposable | Text/Image/Confidence/Attempts/`IsHighConfidence ≥0.70 o cache=-1` |
| `PostflopGameContext` | `OpenScrape.DecisionMaker` (record) | inmutable, accedido vía Holder | Cross-street state thread-safe via `Volatile.Read` |
| `ScopedMeasurement` | `Telemetry/ScopedMeasurement.cs` | readonly struct + IDisposable | zero-allocation; `Stopwatch.GetTimestamp` start/end |
| `MetricsSnapshot` | `Telemetry/MetricsSnapshot.cs` | sealed record | Vista inmutable LastHand+Session |
| `Histogram` | `Telemetry/Histogram.cs` | mutable, no thread-safe (sync externo) | 30 long[] buckets + max + count |
| `LruCache<TKey, TValue>` | `Services/LruCache.cs` | mutable, thread-safe (`lock`) | `Dictionary<TKey, LinkedListNode>` + `LinkedList<(TKey, TValue)>` |
| `RegionLookupCache` | `Services/RegionLookupCache.cs` | mutable | Doble dict `byMapId` y `byMapId+regionName`, ambos `OrdinalIgnoreCase`. O(1) lookup. |
| `DetectionResult` (record nested) | `FrmMain.cs:3033` | inmutable record | ActionColor/FlopColor/AverageActionB/SampleCount/IsActionColorInRange/ShouldCapture/ShouldCaptureFlop/IsFlopVisible |
| `DetectionStatistics` | `Services/DetectionLoggerService.cs:354` | mutable | TotalColorDetections/TurnDetections/Errors/ScreenshotsSaved + computed `SuccessRate` |
| `CategoryState` (private) | `Telemetry/MetricsCollector.cs:143` | mutable, sync via `Lock` | LastHand histogram + Session histogram |

Diccionario completo en `data-dictionary.md`.

### 4. Configuración y feature flags

| Sección appsettings | Tipo bound | Notas |
|---------------------|-----------|-------|
| `OverlayConfig` | `OpenScrape.Domain.Entities.OverlayConfig` | Opacity, FontSize, ActionFontSize, HorizontalOffsetPercent, VerticalOffset |
| `StrategyProfile` | `OpenScrape.Domain.Entities.StrategyProfile` | ~150 parámetros (validados al arrancar) |
| `GameLoop` | `Configuration.GameLoopOptions` | CaptureIntervalMs=100, StopTimeoutMs=2000 |
| `Features` | `Configuration.FeatureFlags` | `UseGameLoopCoordinator: bool = false` (cutover Fase 6) |
| `CaptureSettings` | leído inline en Program.cs | IsReferenceSet/ReferenceImageWidth/ReferenceImageHeight |
| `ConnectionStrings:DefaultConnection` | string | PostgreSQL Neon. **Anomalía:** valor real comprometido en `appsettings.json` en lugar del placeholder `CHANGE_ME` |

`appsettings.Development.json` (gitignored) overridea credenciales y `Encrypter.Key`. `launchSettings.json` define `DOTNET_ENVIRONMENT=Development` para Visual Studio.

### 5. Anomalías relevantes

🔴 **Críticas:**
- **God class `FrmMain` (4502 LOC, 95+ métodos)** con responsabilidades mixtas: UI + game loop + OCR coordination + persistence + config writer. El propio archivo documenta el plan de migración en bloque comentado al final (`// INVENTARIO DE ESTADO MUTABLE — refactor-frmmain-coordinators (Fase 1.4)`), pero la migración está pendiente: `IGameLoopCoordinator` está como esqueleto con feature flag OFF (`UseGameLoopCoordinator=false`).
- **`SaveReferenceDimensionsToConfig`** (`FrmMain.cs:2561`) reescribe `appsettings.json` con string-building manual (no JSON serializer), ramificando solo para `CaptureSettings.*`. Frágil: si una clave contiene escape `,` o cambia de orden, puede corromper el archivo. Además, mezcla `using StreamWriter/Reader` sobre el mismo `MemoryStream` y luego `File.WriteAllText` — anti-pattern.
- **`BackgroundWorker1_DoWork`** (`FrmMain.cs:2737`) usa loop infinito `while(true)` sin `CancellationToken`. Solo sale cuando `_frmOverlay.Visible == false` (`e.Cancel = true`) o por `Catch` global con `e.Cancel = true`. La parada al cerrar (`FrmMain_FormClosing`) confía en que el loop chequea `Overlay.Visible` cada iteración.
- **`btnCapture_Click`** método de ~270 LOC con responsabilidades mixtas (captura, OCR, lifecycle de manos, decisiones, persistencia, overlay update). Es prácticamente el "main" del game loop.
- **Doble entry point al motor de decisión:** `UnifiedPokerCalculator` (usado hoy por `FrmMain`) y `PokerDecisionFacade` (planeado, no usado en producción). Refactor pendiente.

🟡 **Medias:**
- **Hardcoded path absoluto** en `FrmMain.GetImageWhilePlaying`: `"C:", "Code", "Poker", "ScrapePoker", "resources", "Games"`. No portable. Debería derivar de `AppDomain.CurrentDomain.BaseDirectory`.
- **`OcrService.GetCroppedBitmap`** keyifica con `image.GetHashCode()` (identidad del objeto, no contenido). Si el caller reusa el `Bitmap` con un PNG distinto, devuelve cache stale. En el flujo actual `_formImage.pbImage.Image` se reasigna con `new Bitmap` cada captura, lo cual evita el bug, pero el contrato es frágil.
- **`PostflopDecisionService` se inyecta dos veces en `FrmMain`** (vía `IPostflopDecisionService` y vía `GameCoordinator` que también lo recibe). Acoplamiento incrementado.
- **`EncrypterHelper`** usa **IV fija de 16 ceros** — debilidad criptográfica conocida (CBC con IV constante). Aceptable solo si el secret se rota, pero no documentado.
- **`GameLoggerService.GetRecentSessionsWithStatsAsync` hace N+1 queries** (un `Query<HandRecord>` por cada GameSession). Para 50 sesiones = 51 queries.
- **`ProcessRiverAsync` cuenta cartas con `!string.IsNullOrEmpty(d.Name)`** mientras `ProcessFlopAsync`/`Turn` cuentan por `Position == BoardPosition.X` o `count >= N`. Inconsistencia heredada de fix OCR-3.
- **Estado mutable en field `_handle == IntPtr.Zero`** se chequea en algunos métodos pero no en todos los P-Invoke (potencial NRE si la ventana del poker se cerró).
- **`tbResume.Text` se persiste íntegro en cada nueva mano** (`HandleNewHandAsync`) → write amplification (cada mano escribe todo el log acumulado).
- **`FormImage.btnLoad_Click` tiene path hardcoded `C:\Code\Poker\ScrapePoker\resources\Games`** y comentario `//portatil`. Setup-specific.
- **Carpetas vestigio:** `src/OpenScrape.Application/` y `src/OpenScrape.Core/` solo contienen `bin/obj/` (referencias del Scout).
- **Doble ubicación `eng.traineddata`** (`Resources/tessdata/` embedded + `tessdata/` con CopyToOutputDirectory). El `OcrService` ctor extrae la embebida solo si la del output no existe.

🟢 **Bajas:**
- `_papel = _formImage.pbImage.CreateGraphics()` en `UpdateRegionDisplay` crea `Graphics` sin disposar fuera del `using` (recurso GDI fugado por iteración).
- `TextBoxLogger` mezcla `_logger.LogError` / `LogInformation` / `LogDebug` y `Console.WriteLine` indistintamente; existe el provider correlated pero conviven con `Debug.WriteLine` directo.
- `DetectionLoggerService` escribe JSON línea-por-línea con `File.AppendAllText` sin batching → contención si crece.
- `StrategyProfileValidator` valida ~30 thresholds requeridos pero no contempla `StreetThresholds` opcionales que pudieran añadirse en futuro (rigidez intencional para fail-fast).
- `WindowsInformationHelper.FindWindows` filtra `.NET`, `GDI+`, `Hidden`, `DDE`, `System`, `Opera` — heurística específica del entorno de desarrollo.
- `LruCache.Values` retorna snapshot que puede quedar inconsistente vs `_lruList` si hay concurrent modification entre `lock` (caller debe asumir snapshot point-in-time).
- `OcrResult` no expone `Confidence` casteado a percentil legible (es `float ∈ [0,1]` o `-1` para cache hit).
- `MetricsCollector.StartHand` con mano anterior pendiente loggea warning y descarta — comportamiento documentado.

### 6. Diagrama de dependencias

```
                      OpenScrape.Domain (entities/value objects/enums)
                                ↑                           ↑
                                │                           │
                  OpenScrape.Features          OpenScrape.Infrastructure
                  (use cases)                  (Marten setup)
                                ↑                           ↑
                                │                           │
                                ├───────────────────────────┤
                                │                           │
                       OpenScrape.DecisionMaker             │
                       (algorithms + services)              │
                                ↑                           │
                                │                           │
                                └────────┬──────────────────┘
                                         │
                              OpenScrape.App
                              (composition root)
                                         │
                  ┌──────────────────────┼──────────────────────┐
                  │                      │                      │
              Forms/                Services/              Aplication/UseCases/
              ─────                 ────────               ──────────────────
              FrmMain (4502 LOC)    GameCoordinator        UnifiedPokerCalculator
              FrmOverlay            ScreenReaderService    SetPreflopActionUseCase
              FrmHandDetail         TableLayoutService     OutsCalculatorUseCase
              FrmDetectionDebug     OcrService             GetCardsFlop/Turn/River
              FormImage             PokerDecisionFacade    Actions/Get*UseCase ×10
              FormAction            GameLoopCoordinator
              FormListApps          UiSyncService
                                    GameLoopStateMachine
              Helpers/              CardCacheService          Telemetry/
              ─────                 RegionLookupCache         ─────
              CaptureWindowsHelper  GameLoggerService         IMetricsCollector
              ImagePreprocessor     DetectionLoggerService    MetricsCollector
              EncrypterHelper       MetricsCollector          Histogram
              CoordinateScaler      Logging/TextBoxLogger     ScopedMeasurement
              WindowsInfoHelper                               TelemetryCategories
              ColorHelper           Configuration/
              AppThemeHelper        ─────                     Entities/
              ObtainActionHelper    GameLoopOptions           ─────
              PlayerRegionParser    FeatureFlags              PlayerGameState
              UserHandHelper                                  Player
              HandHelper                                      BoardData
              FlopHelper/...                                  TableScrapeFlopResult
                                                              BoardTextures
```

Ver `flowcharts/OpenScrape.App.md` para diagramas Mermaid completos.

### 7. Diccionario de datos completo

Ver sección `OpenScrape.App` en `_reversa_sdd/data-dictionary.md`.

### 8. Mapeo a archivos legacy

Ver `_reversa_sdd/OpenScrape.App/legacy-mapping.md`.
