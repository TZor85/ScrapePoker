# Plan de Implementacion - ScrapePoker

## Fase 1 — Fundamentos (Prioridad Critica)

> **Estado: COMPLETADA**

### 1.1 Tests reales

- [x] Reemplazar los 8 placeholders con tests que validen HandEvaluator, MonteCarloSimulator, OutsCalculator, BetSizingService y EquityCalculatorService
- [ ] Anadir tests de integracion para el flujo OCR -> Decision
- [x] Prerequisito para todo lo demas — sin tests no se puede refactorizar con confianza

**Archivos creados:**
- `OpenScrape.App.Tests/HandEvaluatorTests.cs` (15 tests)
- `OpenScrape.App.Tests/MonteCarloSimulatorTests.cs` (6 tests)
- `OpenScrape.App.Tests/OutsCalculatorTests.cs` (6 tests)
- `OpenScrape.App.Tests/BetSizingServiceTests.cs` (9 tests)
- `OpenScrape.App.Tests/EquityCalculatorServiceTests.cs` (7 tests)
- `OpenScrape.App.Tests/GameLoopStateMachineTests.cs` (9 tests)

**Resultado:** 58 tests, todos pasan.

### 1.2 Game Logger / Historial de partidas

- [x] Crear entidad `GameRound` en Domain con: mano hero, board, posicion, equity calculada, accion tomada, resultado, pot size, timestamp
- [x] Persistir en Marten (PostgreSQL)
- [x] Registrar cada decision del bot para analisis posterior
- [x] Base necesaria para el genetic algorithm y strategy analyzer

**Archivos creados:**
- `src/OpenScrape.Domain/Entities/GameRound.cs`
- `src/OpenScrape.Domain/ValueObjects/StreetDecision.cs`
- `src/OpenScrape.App/Services/GameLoggerService.cs`
- `src/OpenScrape.Features/GameRound/GetRecentGameRounds.cs`
- `src/OpenScrape.Features/GameRound/GameRoundUseCases.cs`

### 1.3 State Machine para el Game Loop

- [x] Extraer a un servicio `GameLoopStateMachine` con estados: WaitingForHand -> HandDetected -> PreflopAction -> FlopDetected -> FlopAction -> TurnDetected -> TurnAction -> RiverDetected -> RiverAction -> HandComplete
- [x] Validacion de transiciones (impide saltos de estado invalidos)
- [x] Inyectado en FrmMain, integrado en deteccion de nueva mano y ProcessTableInfoAsync
- [ ] Retry logic para OCR fallido
- [ ] Reemplazar completamente los booleans `_isPreflop`, `_isFlop`, `_isTurn`, `_isRiver` con CurrentState

**Archivo creado:**
- `src/OpenScrape.App/Services/GameLoopStateMachine.cs`

---

## Fase 2 — Motor de Decision Avanzado

> **Estado: PENDIENTE**

### Diagnostico del estado actual

**Bugs criticos encontrados en el analisis:**

1. **heroStack y villainStack siempre son 0** — En todas las llamadas a `_pokerCalculator.Calculate()` (lineas 1643, 2248, 2302 de FrmMain.cs), heroStack=0 y villainStack=0. Esto anula los calculos de SPR en BetSizingService y UnifiedPokerCalculator.

2. **Board texture del turn solo analiza 1 carta** — `AnalyzeTurnBoardTexture()` filtra `Position == BoardPosition.Turn` y obtiene solo la 4ta carta. Deberia analizar las 4 cartas comunitarias completas (flop + turn).

3. **Bluff frequencies inconsistentes** — appsettings.json define 15% (FlopBluffFrequency), pero el codigo usa 2% (linea 658) y 1% (linea 851). Los valores del config se ignoran en turn/river.

4. **ProcessRiverAsync guarda resultado en _turnResult** — Variable con nombre incorrecto, deberia ser _riverResult (linea 2302).

**Inventario de thresholds hardcodeados:**

80+ valores dispersos en 3 archivos:

| Archivo | Cantidad | Ejemplo |
|---------|----------|---------|
| FrmMain.cs (Turn handlers) | ~36 thresholds | `equity < 45`, `equity > 80`, `equity > 55` |
| FrmMain.cs (River handlers) | ~36 thresholds | `equity < 40`, `equity > 75`, `equity > 60` |
| UnifiedPokerCalculator.cs | ~12 thresholds | `baseFoldEquity = 20.0`, `+10.0 IP`, `EVWithFoldEq > EV + 5.0` |
| BetSizingService.cs | ~8 thresholds | `spr > 3.0: +25%`, `isPaired: +15%`, `OOP: -10%` |

**Patron repetitivo en FrmMain.cs (20 metodos con mismo patron):**

Cada HandleXxxYyyAction() sigue esta estructura identica:
```
1. if (equity < LOW_THRESHOLD)
   -> fold (con bluff chance de 1-2%)
2. Switch board texture (Dry/Coordinated/Paired):
   -> determinar bet size string
3. if (equity > HIGH_TIER) -> bet size grande + "(Value)"
   elif (equity > MID_TIER) -> bet size medio + "(Value)"
   elif (equity > LOW_TIER && IP) -> bet size chico + "(Thin Value)"
   else -> "Check/Call"
```

Los 20 metodos son:
- HandleOpenRaiseTurnAction, HandleCallTurnAction, HandleRaiseOverLimperTurnAction
- HandleThreeBetTurnAction, HandleOpenRaiseVs3BetTurnAction, HandleOpenRaiseVs3BetAndCallTurnAction
- HandleFourBetTurnAction, HandleCold4BetTurnAction, HandleSqueezeTurnAction, HandleVsSqueezeTurnAction
- (mismos 10 para River)

---

### 2.0 Bugfixes criticos (prerequisito)

> **Prioridad: ANTES de cualquier refactor**

#### 2.0.1 Pasar heroStack real a _pokerCalculator

**Problema:** `_playerGameState.HeroStack` existe y se actualiza via `SetHeroStack()`, pero nunca se pasa al calculador.

**Cambio:** En `ProcessFlopAsync`, `ProcessTurnAsync`, `ProcessRiverAsync` — pasar `heroStack: _playerGameState.HeroStack`.

**Archivos:** `FrmMain.cs` (3 call sites, lineas ~1643, ~2248, ~2302)

**Verificacion:** Test unitario que verifica que SPR > 0 cuando heroStack > 0.

#### 2.0.2 Board texture con todas las cartas comunitarias

**Problema:** `AnalyzeTurnBoardTexture()` filtra solo cartas con `Position == BoardPosition.Turn`. Deberia usar todas las cartas del board.

**Cambio:** Filtrar `Position != BoardPosition.Hand` (incluir flop + turn).

**Archivos:** `FrmMain.cs` (metodo `AnalyzeTurnBoardTexture`, linea ~1484)

**Verificacion:** Test unitario con board 2h 7h Kh 9c -> deberia detectar Coordinated (3 hearts).

#### 2.0.3 Usar _riverResult en lugar de _turnResult para river

**Problema:** `ProcessRiverAsync` almacena el resultado en `_turnResult` (linea 2302).

**Cambio:** Almacenar en `_riverResult`.

**Archivos:** `FrmMain.cs` (linea ~2302)

#### 2.0.4 Bluff frequencies consistentes

**Problema:** config dice 15%, codigo usa 2%/1%.

**Cambio:** Leer `_flopBluffFrequency` y `_turnBluffFrequency` en los handlers correspondientes en lugar de valores literales.

**Archivos:** `FrmMain.cs` (20 handlers, reemplazar `0.02`/`0.01` con `_turnBluffFrequency`/`_flopBluffFrequency`)

---

### 2.1 StrategyProfile — Parametros configurables

> **Dependencias:** 2.0 (bugfixes)
> **Esfuerzo estimado:** Medio

**Objetivo:** Centralizar TODOS los thresholds en una unica entidad configurable, eliminando los 80+ valores hardcodeados.

#### Entidad StrategyProfile

**Archivo:** `src/OpenScrape.Domain/Entities/StrategyProfile.cs`

```csharp
public class StrategyProfile
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }

    // --- Equity Thresholds por Street x HandSituation ---
    // Cada HandSituation (OpenRaise, Call, ThreeBet, etc.) tiene sus propios umbrales
    public Dictionary<string, StreetThresholds> Thresholds { get; set; }

    // --- Fold Equity ---
    public double FoldEquityBase { get; set; }              // actual: 20.0
    public double FoldEquityFlopBonus { get; set; }         // actual: +5.0
    public double FoldEquityRiverPenalty { get; set; }      // actual: -5.0
    public double FoldEquityIPBonus { get; set; }           // actual: +10.0
    public double FoldEquityThreeBetPenalty { get; set; }   // actual: -10.0
    public double FoldEquityMin { get; set; }               // actual: 5.0
    public double FoldEquityMax { get; set; }               // actual: 60.0

    // --- Bet Sizing Adjustments ---
    public double BetSizingSPRDeepMultiplier { get; set; }  // actual: 1.25
    public double BetSizingSPRDeepThreshold { get; set; }   // actual: 3.0
    public double BetSizingSPRShallowMultiplier { get; set; } // actual: 0.75
    public double BetSizingSPRShallowThreshold { get; set; }  // actual: 1.0
    public double BetSizingPairedMultiplier { get; set; }   // actual: 1.15
    public double BetSizingCoordinatedMultiplier { get; set; } // actual: 0.90
    public double BetSizingOOPMultiplier { get; set; }      // actual: 0.90
    public double BetSizingMultiOpponentMultiplier { get; set; } // actual: 0.85

    // --- Bluff Frequencies ---
    public double FlopBluffFrequency { get; set; }          // actual: 0.15
    public double TurnBluffFrequency { get; set; }          // actual: 0.15 (config) / 0.02 (code)
    public double RiverBluffFrequency { get; set; }         // actual: 0.02

    // --- Decision Adjustments ---
    public double DrawEquityBonus { get; set; }             // actual: 2.0
    public double RiverEquityPenalty { get; set; }          // actual: -1.0
    public double IPEquityBonus { get; set; }               // actual: 3.0
    public double DeepStackEquityBonus { get; set; }        // actual: 1.0
    public double BetEVThreshold { get; set; }              // actual: 5.0 (EVWithFoldEq > EV + 5.0)
}
```

#### Value Object StreetThresholds

**Archivo:** `src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs`

```csharp
public record StreetThresholds
{
    // Umbrales por tier de equity
    public double FoldBelow { get; init; }        // ej: 45 (turn) / 40 (river)
    public double ThinValueAbove { get; init; }   // ej: 45 (turn) / 40 (river)
    public double ValueAbove { get; init; }       // ej: 55 (turn) / 55 (river)
    public double StrongValueAbove { get; init; } // ej: 80 (turn) / 75 (river)

    // Bet sizes por tier
    public string StrongValueBetSize { get; init; }   // ej: "Bet 3/4"
    public string ValueBetSize { get; init; }         // ej: "Bet 1/2"
    public string ThinValueBetSize { get; init; }     // ej: "Bet 1/3"

    // Board texture overrides
    public string DryBoardBetSize { get; init; }          // ej: "Bet 1/3"
    public string CoordinatedBoardBetSize { get; init; }  // ej: "Bet 1/2"
    public string PairedBoardBetSize { get; init; }       // ej: "Bet 2/3"
}
```

#### Configuracion JSON

**Archivo:** Seccion nueva en `appsettings.json`

La estructura de Thresholds usa clave compuesta `"{Street}_{HandSituation}"`:
- `"Turn_OpenRaise"`, `"Turn_Call"`, `"Turn_ThreeBet"`, etc.
- `"River_OpenRaise"`, `"River_Call"`, `"River_ThreeBet"`, etc.

**Valores iniciales:** Exactamente los que estan hardcodeados hoy (no cambiar comportamiento, solo externalizar).

#### Servicio StrategyProfileService

**Archivo:** `src/OpenScrape.App/Services/StrategyProfileService.cs`

Responsabilidades:
- Cargar perfil desde `IOptions<StrategyProfile>` (appsettings.json)
- Obtener thresholds por street + handSituation: `GetThresholds(BoardPosition street, HandSituation situation)`
- Persistir perfil en Marten (para versionado futuro / genetic algorithm)
- Hot reload cuando cambia appsettings.json

#### Integracion en FrmMain

Reemplazar los 20 metodos HandleXxxYyyAction con un unico metodo generico:

```csharp
private string DeterminePostflopAction(
    double equity,
    BoardPosition street,
    HandSituation situation,
    TurnBoardTexture boardTexture,  // o RiverBoardTexture
    bool isInPosition)
{
    var thresholds = _strategyProfileService.GetThresholds(street, situation);

    if (equity < thresholds.FoldBelow)
    {
        double bluffFreq = street == BoardPosition.Turn
            ? _strategyProfile.TurnBluffFrequency
            : _strategyProfile.RiverBluffFrequency;
        if (Random.Shared.NextDouble() < bluffFreq)
            return AdjustBetSize(thresholds.ThinValueBetSize + " (Bluff)");
        return "Fold";
    }

    string baseBetSize = boardTexture switch
    {
        TurnBoardTexture.Dry => thresholds.DryBoardBetSize,
        TurnBoardTexture.Coordinated => thresholds.CoordinatedBoardBetSize,
        TurnBoardTexture.Paired => thresholds.PairedBoardBetSize,
        _ => thresholds.ValueBetSize
    };

    if (equity > thresholds.StrongValueAbove)
        return AdjustBetSize(thresholds.StrongValueBetSize + " (Value)");
    if (equity > thresholds.ValueAbove)
        return AdjustBetSize(thresholds.ValueBetSize + " (Value)");
    if (equity > thresholds.ThinValueAbove && isInPosition)
        return AdjustBetSize(thresholds.ThinValueBetSize + " (Thin Value)");

    return "Check/Call";
}
```

Esto reemplaza ~800 lineas de codigo repetitivo (20 metodos x ~40 lineas) con ~30 lineas.

#### Archivos a crear
- `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- `src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs`
- `src/OpenScrape.App/Services/StrategyProfileService.cs`
- `OpenScrape.App.Tests/StrategyProfileTests.cs`

#### Archivos a modificar
- `appsettings.json` — agregar seccion StrategyProfile con todos los parametros
- `src/OpenScrape.App/Program.cs` — registrar IOptions<StrategyProfile> y StrategyProfileService
- `src/OpenScrape.App/Forms/FrmMain.cs` — inyectar StrategyProfileService, reemplazar 20 handlers con DeterminePostflopAction
- `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs` — inyectar StrategyProfile, reemplazar thresholds hardcodeados
- `src/OpenScrape.DecisionMaker/Services/BetSizingService.cs` — inyectar parametros de StrategyProfile

#### Tests
- Cargar StrategyProfile desde JSON y verificar valores
- DeterminePostflopAction: equity baja -> fold, equity alta -> value bet, etc.
- Board texture override funciona correctamente
- Bluff frequency se aplica (test con seed fijo para Random)
- BetSizingService con parametros inyectados produce resultados correctos

---

### 2.2 Board Texture Analyzer

> **Dependencias:** 2.0.2 (fix de board texture)
> **Esfuerzo estimado:** Bajo-Medio

**Objetivo:** Extraer el analisis de board texture a un servicio dedicado en DecisionMaker con scoring mas fino que Dry/Coordinated/Paired.

#### Problema actual

- Solo 3 categorias: Dry, Coordinated, Paired
- Turn texture analiza solo 1 carta (bug)
- No detecta: monotone (3+ same suit), broadway-heavy, low board, connected
- Mismo enum para Turn y River (redundante)

#### Implementacion

**Archivo:** `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`

```csharp
public class BoardTextureAnalyzer
{
    public BoardTextureResult Analyze(List<CardDataOuts> communityCards)
}

public class BoardTextureResult
{
    public bool IsPaired { get; set; }       // Dos o mas cartas del mismo rango
    public bool IsMonotone { get; set; }     // 3+ cartas del mismo suit
    public bool IsTwoTone { get; set; }      // Exactamente 2 suits representados
    public bool IsRainbow { get; set; }      // 3+ suits diferentes
    public bool IsConnected { get; set; }    // 3+ cartas consecutivas
    public bool HasStraightDraw { get; set; }
    public bool HasFlushDraw { get; set; }
    public bool IsBroadwayHeavy { get; set; } // 2+ cartas >= T
    public bool IsLowBoard { get; set; }     // Todas las cartas <= 8
    public double WetnessScore { get; set; } // 0.0 (dry) a 1.0 (wet)

    // Categoria simplificada (retrocompatible con enum actual)
    public BoardTextureCategory Category { get; set; }
}

public enum BoardTextureCategory { Dry, SemiDry, SemiWet, Wet, Paired }
```

**Wetness score** se calcula como suma ponderada:
- Paired: +0.3
- Monotone: +0.4
- TwoTone: +0.2
- Connected (3+ consecutivas): +0.3
- Broadway heavy: +0.1

**Mapping retrocompatible:**
- WetnessScore < 0.2 -> Dry (mapea a TurnBoardTexture.Dry)
- WetnessScore 0.2-0.5 -> SemiDry/SemiWet
- WetnessScore > 0.5 -> Wet (mapea a TurnBoardTexture.Coordinated)
- IsPaired -> Paired (mapea a TurnBoardTexture.Paired)

#### Archivos a crear
- `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`
- `OpenScrape.App.Tests/BoardTextureAnalyzerTests.cs`

#### Archivos a modificar
- `src/OpenScrape.App/Forms/FrmMain.cs` — inyectar BoardTextureAnalyzer, reemplazar AnalyzeTurnBoardTexture y AnalyzeRiverBoardTexture
- `src/OpenScrape.App/Program.cs` — registrar BoardTextureAnalyzer como Singleton

#### Tests
- Board `2h 7h Kh 9c` -> IsMonotone=false, IsTwoTone=true, HasFlushDraw=true
- Board `2h 7h Kh` -> monotone 3 hearts
- Board `7c 7d Ks 2h` -> IsPaired=true
- Board `8c 9d Ts Jh` -> IsConnected=true
- Board `2s 5d 9h Kc` -> Dry (low wetness)
- Board `Ts Js Qs` -> Wet + Broadway + Monotone

---

### 2.3 Postflop Decision Services (Turn y River)

> **Dependencias:** 2.1 (StrategyProfile) + 2.2 (BoardTextureAnalyzer)
> **Esfuerzo estimado:** Alto

**Objetivo:** Extraer la logica de decision de turn y river de FrmMain a servicios dedicados en DecisionMaker, con logica mejorada.

#### Problema actual

- 20 metodos de ~40 lineas cada uno en FrmMain (~800 lineas de codigo repetitivo)
- Logica identica entre metodos (solo cambian los thresholds por 1-5 puntos)
- No hay continuation bet logic (2nd/3rd barrel)
- No hay showdown value analysis en river
- Pot odds calculados pero no integrados en la decision
- No hay contexto de acciones previas (flop action no influye en turn)

#### Servicio PostflopDecisionService

**Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`

Entrada:
```csharp
public class PostflopDecisionRequest
{
    public double EquityPercentage { get; set; }
    public double PotOddsPercentage { get; set; }
    public double ExpectedValue { get; set; }
    public BoardPosition Street { get; set; }
    public HandSituation Situation { get; set; }
    public BoardTextureResult BoardTexture { get; set; }
    public bool IsInPosition { get; set; }
    public int TotalOuts { get; set; }
    public decimal HeroStack { get; set; }
    public decimal PotSize { get; set; }
    public string? PreviousStreetAction { get; set; }  // Accion tomada en street anterior
}
```

Salida:
```csharp
public class PostflopDecisionResult
{
    public string Action { get; set; }           // "Fold", "Check/Call", "Bet 1/3 (Value)", etc.
    public string Reasoning { get; set; }        // Explicacion para logging/overlay
    public double Confidence { get; set; }       // 0.0-1.0 confianza en la decision
}
```

Logica principal:
```
1. Obtener thresholds de StrategyProfile segun street + situation
2. Si equity < FoldBelow:
   a. Verificar si tiene pot odds para call (equity > potOdds -> "Call (Pot Odds)")
   b. Verificar bluff frequency -> posible bluff
   c. Si tiene outs > 8 y no es river -> "Semi-Bluff"
   d. Else -> "Fold"
3. Determinar bet size base segun board texture (usa BoardTextureAnalyzer)
4. Barrel logic (NUEVO):
   a. Si PreviousStreetAction contiene "Bet" y equity se mantuvo -> 2nd/3rd barrel
   b. Si board cambio drasticamente (nueva carta completa draw) -> check-back
5. Aplicar equity tiers (StrongValue/Value/ThinValue)
6. Ajustar bet size con BetSizingService (pasar heroStack REAL)
7. River-specific: showdown value check
   a. Si equity > ThinValueAbove pero < ValueAbove -> "Check/Call" (showdown value)
```

#### Integracion en FrmMain

Reemplazar DetermineTurnAction() y DetermineRiverAction() con:

```csharp
// En ProcessTurnAsync, despues de calcular equity:
var decision = _postflopDecisionService.Decide(new PostflopDecisionRequest
{
    EquityPercentage = _turnResult.EquityPercentage,
    PotOddsPercentage = _turnResult.PotOddsPercentage,
    ExpectedValue = _turnResult.ExpectedValue,
    Street = BoardPosition.Turn,
    Situation = _playerGameState.HandSituation,
    BoardTexture = _boardTextureAnalyzer.Analyze(communityCards),
    IsInPosition = _playerGameState.IsInPosition,
    TotalOuts = _turnResult.TotalOuts,
    HeroStack = _playerGameState.HeroStack,
    PotSize = _playerGameState.PotSize,
    PreviousStreetAction = _responseAction?.Action
});

_responseAction.Action = decision.Action;
_gameLoggerService.LogStreetDecision(new StreetDecision(...));
```

Esto elimina ~800 lineas de FrmMain y las consolida en ~100 lineas de servicio testeable.

#### Archivos a crear
- `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs`

#### Archivos a modificar
- `src/OpenScrape.App/Forms/FrmMain.cs` — eliminar 20 handlers, delegar a PostflopDecisionService
- `src/OpenScrape.App/Program.cs` — registrar PostflopDecisionService
- `src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj` — posible referencia adicional

#### Tests
- Equity baja sin pot odds -> Fold
- Equity baja con pot odds favorables -> Call (Pot Odds)
- Equity alta en dry board -> Value bet grande
- Equity alta en wet board -> Value bet reducido
- 2nd barrel: accion previa fue bet, equity mantenida -> continuar betting
- 3rd barrel: river con equity fuerte -> value bet
- Showdown value: equity media en river -> Check/Call (no bet)
- Semi-bluff: equity baja pero 9+ outs en turn -> Semi-Bluff
- OOP penalty: misma equity OOP reduce bet size
- Board texture cambia drasticamente turn -> check-back

---

### Orden de implementacion Fase 2

```
2.0 Bugfixes criticos (1-2 horas)
  |
  +---> 2.0.1 heroStack real
  +---> 2.0.2 Board texture fix
  +---> 2.0.3 _riverResult typo
  +---> 2.0.4 Bluff frequencies
  |
  v
2.1 StrategyProfile (principal)
  |  Crear entidad + JSON + service + inyectar en FrmMain
  |  Reemplazar 80+ hardcoded values
  |  Tests de configuracion
  |
  v
2.2 BoardTextureAnalyzer (en paralelo con 2.1)
  |  Servicio dedicado en DecisionMaker
  |  Wetness score + categorias finas
  |  Tests unitarios
  |
  v
2.3 PostflopDecisionService (requiere 2.1 + 2.2)
     Consolidar 20 handlers en 1 servicio
     Barrel logic + showdown value
     Eliminar ~800 lineas de FrmMain
     Tests de decision completos
```

### Verificacion Fase 2

| Paso | Criterio de aceptacion |
|------|----------------------|
| 2.0 | `dotnet test` pasa, heroStack > 0 en logs, board texture correcta |
| 2.1 | Todos los thresholds cargados desde JSON, comportamiento identico al actual |
| 2.2 | BoardTextureAnalyzer detecta monotone/connected/paired correctamente |
| 2.3 | 20 handlers eliminados de FrmMain, PostflopDecisionService produce mismas decisiones |
| Final | `dotnet build` sin errores, `dotnet test` todos pasan, FrmMain reducido en ~800 lineas |

### Riesgos y mitigacion

| Riesgo | Probabilidad | Mitigacion |
|--------|-------------|-----------|
| Regresion en decisiones al migrar thresholds | Alta | Tests snapshot: ejecutar con thresholds actuales, guardar output, comparar despues de migrar |
| StrategyProfile demasiado grande para appsettings | Baja | Archivo JSON separado `Data/StrategyProfile.json` si crece mucho |
| Board texture wetness score no calibrado | Media | Empezar con mapping directo a Dry/Coordinated/Paired, refinar despues con datos |
| PostflopDecisionService cambia comportamiento | Media | Implementar con feature flag: si flag off, usa handlers viejos; si on, usa servicio nuevo |

---

## Fase 3 — Automatizacion y Ejecucion

> **Estado: PENDIENTE**

### 3.1 Action Executor (Mouse Automation)

**Objetivo:** Ejecutar las acciones recomendadas automaticamente en la mesa.

**Problema actual:** El bot solo recomienda acciones en el overlay. El usuario debe ejecutarlas manualmente.

**Implementacion:**
- Crear `ActionExecutorService` en OpenScrape.App/Services
- Detectar botones de accion (Fold/Call/Raise) por template matching o color/posicion en las regiones del tableMap
- Mover mouse con curvas de Bezier (no teleport)
- Randomizar timing (delay entre 0.5-2s, distribucion gaussiana)
- Input del bet size en el campo de raise
- Win32 API: `SetCursorPos`, `mouse_event` (ya se usa `CaptureWindowsHelper`)

**Archivos a crear:**
- `src/OpenScrape.App/Services/ActionExecutorService.cs`
- `src/OpenScrape.App/Services/MouseHumanizer.cs`

**Archivos a modificar:**
- `src/OpenScrape.App/Forms/FrmMain.cs` — llamar a ActionExecutor despues de cada decision
- `src/OpenScrape.App/Data/tableMap.json` — agregar regiones de botones Fold/Call/Raise

**Dependencias:** Fase 2 completada (decisiones confiables antes de automatizar).

### 3.2 Verificacion pre-accion

**Objetivo:** Confirmar que el estado de mesa no cambio antes de ejecutar una accion.

**Implementacion:**
- Antes de ejecutar: re-capturar screenshot y comparar con el estado que genero la decision
- Detectar popups, captchas, sit-out
- Validar que los botones siguen visibles
- Si falla -> re-capturar y re-evaluar
- Integrar con `GameLoopStateMachine` (permanecer en estado actual si validacion falla)

**Archivos a crear:**
- `src/OpenScrape.App/Services/PreActionValidator.cs`

**Archivos a modificar:**
- `src/OpenScrape.App/Services/ActionExecutorService.cs` — validar antes de ejecutar

**Dependencias:** 3.1 (Action Executor).

---

## Fase 4 — Analisis y Optimizacion

> **Estado: PENDIENTE**

### 4.1 Strategy Analyzer

**Objetivo:** Dashboard con metricas de rendimiento por estrategia.

**Metricas:**
- BB/100 manos (winrate principal)
- Win/loss por accion (fold/call/bet) por street
- Equity vs Outcome scatter plot (la equity predijo bien?)
- Funds timeline (evolucion del stack)
- ROI por posicion (BTN, CO, SB, BB, etc.)
- Equity realizada vs equity esperada

**Implementacion:**
- Crear `StrategyAnalyzerService` que consulta `GameRound`s de Marten
- Agregar metricas por periodo (sesion, dia, semana)
- Form `FrmAnalytics` con graficos (WinForms chart controls o libreria LiveCharts)

**Archivos a crear:**
- `src/OpenScrape.App/Services/StrategyAnalyzerService.cs`
- `src/OpenScrape.App/Forms/FrmAnalytics.cs`
- `src/OpenScrape.Features/GameRound/GetGameRoundStats.cs`

**Dependencias:** 1.2 (Game Logger con datos suficientes, minimo ~500 manos).

### 4.2 Genetic Algorithm

**Objetivo:** Optimizacion automatica de los parametros de estrategia.

**Implementacion:**
- Fitness function: BB/100 en las ultimas N manos
- Poblacion: variantes del `StrategyProfile` con parametros mutados
- Mutacion: ajustar parametros +/-5-15% basado en correlaciones estadisticas
- Seleccion: "if call_wins > call_losses x 2.0 -> mas agresivo"
- Versionado: timestamp + nombre para tracking evolutivo
- Requiere minimo 2000-5000 manos de datos
- Implementar como servicio background que analiza periodicamente

**Archivos a crear:**
- `src/OpenScrape.DecisionMaker/Optimization/GeneticOptimizer.cs`
- `src/OpenScrape.DecisionMaker/Optimization/StrategyMutator.cs`
- `src/OpenScrape.DecisionMaker/Optimization/FitnessEvaluator.cs`
- `src/OpenScrape.Domain/Entities/StrategyGeneration.cs`

**Dependencias:** 2.1 (parametros configurables) + 4.1 (strategy analyzer) + suficientes datos.

---

## Fase 5 — Reconocimiento Avanzado (Opcional)

> **Estado: PENDIENTE**

### 5.1 Template Matching para cartas

**Objetivo:** Complementar OCR con matching visual de templates por carta.

**Justificacion:** Mas robusto que OCR puro para cartas (imagenes conocidas y finitas: 52 cartas).

**Implementacion:**
- Almacenar templates (52 imagenes de cartas) por plataforma en Marten o filesystem
- Comparar crop de carta con templates usando OpenCvSharp `MatchTemplate`
- Fallback a OCR si template matching tiene confianza baja

**Archivos a crear:**
- `src/OpenScrape.App/Services/TemplateMatchingService.cs`
- `src/OpenScrape.App/Data/CardTemplates/` (52 imagenes por plataforma)

**Dependencias:** Ninguna.

### 5.2 CNN para card recognition

**Objetivo:** Entrenar modelo ML para clasificacion de cartas cuando template matching y OCR fallan.

**Implementacion:**
- ML.NET ya esta parcialmente integrado
- Entrenar con imagenes de cartas especificas de cada mesa
- Clasificacion multiclase (52 clases)
- Fallback cuando template matching y OCR fallan

**Archivos a crear:**
- `src/OpenScrape.App/Services/CardClassifierService.cs`
- Dataset de entrenamiento por plataforma

**Dependencias:** 5.1 (template matching como baseline).

### 5.3 Multi-plataforma

**Objetivo:** Abstraer table scraper con interfaz `ITableScraper` para soportar multiples plataformas de poker.

**Implementacion:**
- Interfaz `ITableScraper` con metodos: `CaptureTable()`, `DetectCards()`, `DetectButtons()`, `GetPlayerInfo()`
- Implementaciones por plataforma (cada una con sus coordenadas, themes, templates)
- El `tableMap.json` actual ya va en esta direccion
- Factory pattern para seleccionar implementacion segun plataforma detectada

**Archivos a crear:**
- `src/OpenScrape.App/Services/ITableScraper.cs`
- `src/OpenScrape.App/Services/Platform/` (una implementacion por plataforma)

**Dependencias:** 5.1 (template matching).

---

## Orden de Implementacion Recomendado

```
Fase 1 (COMPLETADA)
  |
  v
Fase 2.0 (bugfixes criticos)
  |
  v
Fase 2.1 + 2.2 (en paralelo)
  |
  v
Fase 2.3 (depende de 2.1 y 2.2)
  |
  v
Fase 3.1 -> 3.2 (secuencial)
  |
  v
Fase 4.1 (requiere datos de Game Logger)
  |
  v
Fase 4.2 (requiere 4.1 + 2.1 + datos suficientes)
  |
  v
Fase 5 (opcional, en cualquier momento)
```

## Datos Perdidos Actualmente (a resolver en Fase 2)

| Dato | Se detecta | Se persiste | Necesario para |
|------|-----------|-------------|----------------|
| Acciones de villanos | Si | No | Opponent modeling |
| Stacks de villanos | Si | No | SPR analysis |
| Posiciones de villanos | Si | No | Position-based stats |
| Resultado de la mano | No | No | Winrate, fitness |
| Fold rates por villano | No | No | Fold equity dinamica |
| Bet sizing de villanos | Si (Small/Med/Large) | No | Opponent profiling |
| Session aggregates | No | No | BB/hour, ROI |

## Verificacion por Fase

| Fase | Criterio de aceptacion |
|------|----------------------|
| 1 | `dotnet test` -> 58 tests pasan, `dotnet build` sin errores |
| 2.0 | Bugfixes: heroStack real, board texture correcta, bluff freq consistente |
| 2.1 | StrategyProfile cargado desde JSON, comportamiento identico al actual |
| 2.2 | BoardTextureAnalyzer detecta monotone/connected/paired correctamente |
| 2.3 | 20 handlers eliminados, PostflopDecisionService testeable y funcional |
| 3.1 | Bot ejecuta acciones automaticamente en mesa de prueba |
| 3.2 | Validacion pre-accion detecta cambios de estado |
| 4.1 | Dashboard muestra BB/100, equity vs outcome, timeline |
| 4.2 | Genetic optimizer produce generacion con mejor fitness |
| 5.1 | Template matching detecta 52 cartas con >95% accuracy |
