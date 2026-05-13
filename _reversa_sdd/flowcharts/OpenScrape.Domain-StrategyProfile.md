# Flowchart — `StrategyProfile.Validate()`

> Función: `StrategyProfile.Validate()`
> Archivo: `src/OpenScrape.Domain/Entities/StrategyProfile.cs:318-382`
> Doc level: detalhado

## 1. Visión general

```mermaid
flowchart TD
    Start([Validate]) --> Init["errors = new List(string)"]

    Init --> LoopHeader["foreach (key, t) in Thresholds"]
    LoopHeader --> EquityOrder{"Validar orden de tiers de equity:<br/>FoldBelow < ThinValueAbove < ValueAbove < StrongValueAbove"}

    EquityOrder -->|Si rota| AddE["errors.Add 'key: tierA >= tierB'"]
    AddE --> LoopHeader
    EquityOrder -->|OK| LoopHeader

    LoopHeader -->|fin| Globals["Validar 9 invariantes globales"]

    Globals --> G1["FoldEquityMin < FoldEquityMax"]
    G1 -->|fail| AddG1[errors.Add]
    G1 -->|OK| G2

    AddG1 --> G2["SPRPushFoldThreshold < SPRDeepCautionThreshold"]
    G2 -->|fail| AddG2[errors.Add]
    G2 -->|OK| G3

    AddG2 --> G3["DangerFlushCompletePct ∈ [0, 100]"]
    G3 -->|fail| AddG3[errors.Add]
    G3 -->|OK| G4

    AddG3 --> G4["DangerStraightCompletePct ∈ [0, 100]"]
    G4 -->|fail| AddG4[errors.Add]
    G4 -->|OK| G5

    AddG4 --> G5["TaintedOutsDiscount ∈ [0, 1]"]
    G5 -->|fail| AddG5[errors.Add]
    G5 -->|OK| G6

    AddG5 --> G6["FlopBluffFrequency ∈ [0, 1]"]
    G6 -->|fail| AddG6[errors.Add]
    G6 -->|OK| G7

    AddG6 --> G7["TurnBluffFrequency ∈ [0, 1]"]
    G7 -->|fail| AddG7[errors.Add]
    G7 -->|OK| G8

    AddG7 --> G8["RiverBluffFrequency ∈ [0, 1]"]
    G8 -->|fail| AddG8[errors.Add]
    G8 -->|OK| G9

    AddG8 --> G9["BluffCatch multipliers ∈ (0, 1]"]
    G9 -->|fail| AddG9[errors.Add]
    G9 -->|OK| G10

    AddG9 --> G10["BetSizing SPR multipliers > 0"]
    G10 -->|fail| AddG10[errors.Add]
    G10 -->|OK| G11

    AddG10 --> G11["ImpliedOdds factors ∈ (0, 1]"]
    G11 -->|fail| AddG11[errors.Add]
    G11 -->|OK| G12

    AddG11 --> G12["ImpliedOdds shallow < deep threshold"]
    G12 -->|fail| AddG12[errors.Add]
    G12 -->|OK| Done

    AddG12 --> Done([return errors])
```

## 2. Reglas detalladas (orden de aplicación)

| # | Regla | Mensaje en caso de fallo |
|--:|-------|--------------------------|
| 1 | `t.FoldBelow < t.ThinValueAbove` (por threshold) | `{key}: FoldBelow ({a}) debe ser menor que ThinValueAbove ({b})` |
| 2 | `t.ThinValueAbove < t.ValueAbove` (por threshold) | `{key}: ThinValueAbove ({a}) debe ser menor que ValueAbove ({b})` |
| 3 | `t.ValueAbove < t.StrongValueAbove` (por threshold) | `{key}: ValueAbove ({a}) debe ser menor que StrongValueAbove ({b})` |
| 4 | `FoldEquityMin < FoldEquityMax` | `FoldEquityMin ({a}) debe ser menor que FoldEquityMax ({b})` |
| 5 | `SPRPushFoldThreshold < SPRDeepCautionThreshold` | `SPRPushFoldThreshold ({a}) debe ser menor que SPRDeepCautionThreshold ({b})` |
| 6 | `0 ≤ DangerFlushCompletePct ≤ 100` | `DangerFlushCompletePct ({v}) debe estar entre 0 y 100` |
| 7 | `0 ≤ DangerStraightCompletePct ≤ 100` | `DangerStraightCompletePct ({v}) debe estar entre 0 y 100` |
| 8 | `0 ≤ TaintedOutsDiscount ≤ 1` | `TaintedOutsDiscount ({v}) debe estar entre 0 y 1` |
| 9 | `0 ≤ FlopBluffFrequency ≤ 1` | `FlopBluffFrequency ({v}) debe estar entre 0 y 1` |
| 10 | `0 ≤ TurnBluffFrequency ≤ 1` | (idem) |
| 11 | `0 ≤ RiverBluffFrequency ≤ 1` | (idem) |
| 12 | `0 < BluffCatchFoldBelowMultiplier ≤ 1` | `BluffCatchFoldBelowMultiplier ({v}) debe estar entre 0 (excl.) y 1` |
| 13 | `0 < BluffCatchTurnEquityMultiplier ≤ 1` | (idem) |
| 14 | `BetSizingSPRDeepMultiplier > 0` | `BetSizingSPRDeepMultiplier ({v}) debe ser > 0` |
| 15 | `BetSizingSPRShallowMultiplier > 0` | (idem) |
| 16 | `0 < ImpliedOddsSPRDeepFactor ≤ 1` | `ImpliedOddsSPRDeepFactor ({v}) debe estar entre 0 (excl.) y 1` |
| 17 | `0 < ImpliedOddsSPRShallowFactor ≤ 1` | (idem) |
| 18 | `ImpliedOddsSPRShallowThreshold < ImpliedOddsSPRDeepThreshold` | `ImpliedOddsSPRShallowThreshold ({a}) debe ser menor que Deep ({b})` |

## 3. Modelo de error consolidado

`Validate()` **acumula** errores en lugar de fallar al primero. Esto permite que el operador vea todos los problemas en un único mensaje, en lugar de iteraciones de "arreglar uno → recompilar → ver el siguiente".

El consumidor típico es `OpenScrape.App.Program.cs`/composición DI, que:

```mermaid
sequenceDiagram
    participant App as Program.cs
    participant Cfg as appsettings.json
    participant SP as StrategyProfile
    participant Ex as StrategyProfileValidationException

    App->>Cfg: Load IOptions<StrategyProfile>
    Cfg-->>App: profile (poblado vía Bind)
    App->>SP: profile.Validate()
    SP-->>App: List<string> errors

    alt errors vacío
        App->>App: Continue startup
    else errors.Count > 0
        App->>Ex: throw new SPValidationException(errors)
        Ex-->>App: Multiline message
        App->>App: Crash startup<br/>📍 Falla rápida con diagnóstico completo
    end
```

## 4. Defaults seguros (sin override en config)

Si no se sobrescribe nada vía `appsettings.json`, los defaults compilados pasan la validación:

| Campo | Default | Pasa validación? |
|-------|--------:|:----------------:|
| `FoldEquityBase` | 20.0 | ✅ |
| `FoldEquityMin` | 5.0 | ✅ (< 60.0) |
| `FoldEquityMax` | 60.0 | ✅ |
| `SPRPushFoldThreshold` | 2.0 | ✅ (< 4.0) |
| `SPRDeepCautionThreshold` | 4.0 | ✅ |
| `DangerFlushCompletePct` | 35.0 | ✅ |
| `DangerStraightCompletePct` | 18.0 | ✅ |
| `TaintedOutsDiscount` | 0.5 | ✅ |
| `FlopBluffFrequency` | 0.15 | ✅ |
| `TurnBluffFrequency` | 0.12 | ✅ |
| `RiverBluffFrequency` | 0.10 | ✅ |
| `BluffCatchFoldBelowMultiplier` | 0.75 | ✅ |
| `BluffCatchTurnEquityMultiplier` | 0.90 | ✅ |
| `BetSizingSPRDeepMultiplier` | 1.25 | ✅ |
| `BetSizingSPRShallowMultiplier` | 0.75 | ✅ |
| `ImpliedOddsSPRDeepFactor` | 0.65 | ✅ |
| `ImpliedOddsSPRShallowFactor` | 0.95 | ✅ |
| `ImpliedOddsSPRShallowThreshold` | 2.0 | ✅ (< 4.0) |
| `ImpliedOddsSPRDeepThreshold` | 4.0 | ✅ |

## 5. Lo que NO valida

🟡 **INFERIDO** — gaps potenciales en la validación:

- **Coherencia de bet sizes**: no valida que las strings de bet (`"Bet 1/2"`, `"Raise 3x"`) sean parseables por el `BetSizingService`.
- **Coherencia de frecuencias C-bet**: `CbetFrequencyFlop/Turn/River` no validan el rango `[0, 1]`.
- **Coherencia de `MultiwayOOPMultiplierSB/BB/EP`**: no valida que estén en `(0, 1]`.
- **S18-S22 calibration parameters** (50+ campos de sprints recientes): ninguno está incluido en `Validate()`. Posible deuda técnica.
- **Bankroll fields** (`InitialBankroll`, `BuyInMax`, etc.): no validados.
- **Coherencia entre `Thresholds` y enums**: no valida que las claves del diccionario sean parseables como `ThresholdKey` (e.g., podría haber `"Flop_Wrong"` y la validación pasaría).

🔴 **LACUNA**: Estas validaciones gap pueden causar bugs silenciosos en runtime. Recomendable extender `Validate()` o introducir `StrategyProfileValidator` (servicio dedicado) en futuras iteraciones.

## 6. Test surface

🟡 **INFERIDO**: NUnit tests probablemente cubren cada regla aisladamente. Verificar en `OpenScrape.App.Tests/StrategyProfileTests` (visible en CLAUDE.md, "StrategyProfile (12) tests").
