# Flowchart — `VillainRange.GetForSituation` (3 overloads)

> Función: `VillainRange.GetForSituation`
> Archivo: `src/OpenScrape.Domain/ValueObjects/VillainRange.cs:32-154`
> Doc level: detalhado

## 1. Pirámide de overloads

```mermaid
flowchart TD
    Caller([Caller<br/>cualquier punto<br/>del DecisionMaker]) --> Choice{¿Qué información tengo?}

    Choice -->|"solo HandSituation"| O1[Overload 1<br/>GetForSituation(situation)]
    Choice -->|"+ TablePosition villain"| O2[Overload 2<br/>GetForSituation(situation, position)]
    Choice -->|"+ OpponentProfile observado"| O3[Overload 3<br/>GetForSituation(situation, position, profile)]

    O3 --> O2
    O2 --> O1
    O1 --> Output([VillainRange? base])

    O2 --> Output2([VillainRange ajustado por posición])
    O3 --> Output3([VillainRange ajustado por VPIP+3Bet%])
```

Cada overload superior **delega** al inmediatamente inferior y aplica un ajuste sobre el resultado.

## 2. Overload 1 — switch puro por situación

```mermaid
flowchart TD
    Start([GetForSituation(situation)]) --> Switch{situation}

    Switch -->|OpenRaise| R1["CreateRange<br/>('Caller vs OpenRaise', 25%, _callerVsOpenRaise)"]
    Switch -->|ThreeBet| R2["('Caller vs 3Bet', 10%, _callerVs3Bet)"]
    Switch -->|OpenRaiseVs3Bet| R3["('3Bettor', 8%, _threeBettor)"]
    Switch -->|OpenRaiseVs3BetAndCall| R4["('3Bet pot caller', 12%, _threeBetPotCaller)"]
    Switch -->|FourBet \\| Cold4Bet| R5["('Caller vs 4Bet', 5%, _callerVs4Bet)"]
    Switch -->|Call| R6["('Open Raiser', 20%, _openRaiser)"]
    Switch -->|RaiseOverLimper| R7["('Limper', 40%, _limper)"]
    Switch -->|Squeeze| R8["('Caller vs Squeeze', 12%, _callerVsSqueeze)"]
    Switch -->|VsSqueeze| R9["('Squeezer', 8%, _threeBettor)"]
    Switch -->|DonkBet| R10["('Donk Bettor', 30%, _donkBettor)"]
    Switch -->|DonkBetVsOpenRaise| R11["('Donk vs OR', 25%, _callerVsOpenRaise)"]
    Switch -->|None / otros| R12[null]

    R1 --> Out([Devuelve VillainRange])
    R2 --> Out
    R3 --> Out
    R4 --> Out
    R5 --> Out
    R6 --> Out
    R7 --> Out
    R8 --> Out
    R9 --> Out
    R10 --> Out
    R11 --> Out
    R12 --> NullOut([Devuelve null])
```

## 3. Overload 2 — ajuste posicional

```mermaid
flowchart TD
    Start([GetForSituation(situation, villainPosition)]) --> Base[baseRange = GetForSituation(situation)]

    Base --> NullCheck{base == null<br/>OR position == None?}
    NullCheck -->|Sí| ReturnBase([return base])
    NullCheck -->|No| Pos{villainPosition}

    Pos -->|Early| M07[mul = 0.7]
    Pos -->|Middle| M085[mul = 0.85]
    Pos -->|CutOff| M10[mul = 1.0]
    Pos -->|Button| M13[mul = 1.3]
    Pos -->|SmallBlind| M09[mul = 0.9]
    Pos -->|BigBlind| M11[mul = 1.1]
    Pos -->|otros| M10b[mul = 1.0]

    M07 --> CheckNeutral
    M085 --> CheckNeutral
    M10 --> CheckNeutral
    M13 --> CheckNeutral
    M09 --> CheckNeutral
    M11 --> CheckNeutral
    M10b --> CheckNeutral

    CheckNeutral{|mul - 1.0| < 0.01?} -->|Sí| ReturnBase
    CheckNeutral -->|No| Iter[for each (hand, freq) in base.Hands<br/>adjusted[hand] = min(1.0, freq * mul)]

    Iter --> NewRange[New VillainRange<br/>Name = base.Name + ' (Position)'<br/>RangePercentage = base.RangePercentage * mul<br/>Hands = adjusted]
    NewRange --> Out([return adjusted range])
```

## 4. Overload 3 — ajuste por OpponentProfile

```mermaid
flowchart TD
    Start([GetForSituation(situation, position, profile)]) --> BaseAdj[baseRange = GetForSituation(situation, position)]

    BaseAdj --> Reliable{base != null AND<br/>profile != null AND<br/>profile.HasReliablePreflopData?}
    Reliable -->|No| ReturnBase([return base])
    Reliable -->|Sí| VpipMul[vpipMultiplier = CalculateVpipMultiplier(<br/>baseRange.RangePercentage,<br/>profile.VPIP)]

    VpipMul --> Is3Bet{situation in<br/>{OpenRaiseVs3Bet, VsSqueeze}<br/>AND profile.ThreeBetPct > 0?}
    Is3Bet -->|No| Combine
    Is3Bet -->|Sí| Compute3B[expected3Bet = 6.0<br/>threeBetMul = clamp(<br/>profile.ThreeBetPct / 6,<br/>0.5, 2.0)]
    Compute3B --> Combine

    Combine[combinedMul = vpipMul * threeBetMul] --> NeutralCheck{|combined - 1.0| < 0.05?}
    NeutralCheck -->|Sí| ReturnBase
    NeutralCheck -->|No| Iter[for each hand:<br/>adjusted[hand] = min(1.0, freq * combined)]

    Iter --> NewRange["New VillainRange<br/>Name = base.Name + ' (VPIP=X%)'<br/>RangePercentage *= combined<br/>Hands = adjusted"]
    NewRange --> Out([return adjusted range])
```

## 5. CalculateVpipMultiplier — algoritmo

```
function CalculateVpipMultiplier(baseRangePercentage, observedVPIP):
    expectedVPIP = max(baseRangePercentage, 10.0)
    ratio = observedVPIP / expectedVPIP
    return clamp(ratio, 0.5, 2.0)
```

### Tabla de comportamiento (rangeBase=25%)

| VPIP observado | Cálculo | Multiplier resultante | Interpretación |
|---:|---|---:|---|
| 5% | 5/25 = 0.2 | **0.5** (clamp) | Villain muy tight, rango reducido al mínimo |
| 12% | 12/25 = 0.48 | **0.5** (clamp) | Tight |
| 25% | 25/25 = 1.0 | **1.0** | Neutro (rango calza) |
| 40% | 40/25 = 1.6 | **1.6** | Loose |
| 70% | 70/25 = 2.8 | **2.0** (clamp) | Whale, rango ampliado al máximo |

### Casos límite

- `baseRangePercentage < 10`: clampea a `10` para evitar amplificar excesivamente rangos polarizados (`_threeBettor` 8%, `_callerVs4Bet` 5%).
- VPIP=0 con observaciones reales raras → multiplier=0.5.

## 6. ExpandHandNotation — expansión a combos

```mermaid
flowchart TD
    Start([ExpandHandNotation(notation)]) --> Len{Length < 2?}
    Len -->|Sí| Empty([return []])
    Len -->|No| Parse["rank1 = CharToRank[notation[0]]<br/>rank2 = CharToRank[notation[1]]"]

    Parse --> Valid{rank1 == default<br/>OR rank2 == default?}
    Valid -->|Sí| Empty
    Valid -->|No| Tag["isPair = (Length==2 OR rank1==rank2)<br/>isSuited = (Length==3 AND notation[2]=='s')"]

    Tag --> Type{Type}
    Type -->|isPair| Pair[6 combos:<br/>foreach i in [0..3]<br/>  foreach j in [i+1..3]<br/>    add CardDataOuts(suit_i, rank1), CardDataOuts(suit_j, rank2)]
    Type -->|isSuited| Suited[4 combos:<br/>foreach suit:<br/>  add CardDataOuts(suit, rank1), CardDataOuts(suit, rank2)]
    Type -->|offsuit| Off[12 combos:<br/>foreach i, foreach j != i:<br/>  add CardDataOuts(suit_i, rank1), CardDataOuts(suit_j, rank2)]

    Pair --> Out([return combos])
    Suited --> Out
    Off --> Out
```

## 7. Invariantes garantizadas

🟢 **CONFIRMADO**:
- `null` solo se retorna si `situation` es `None` o no mapeada.
- Frecuencias siempre clampeadas a `[0, 1]` tras cualquier multiplicador.
- VPIP multiplier siempre en `[0.5, 2.0]`.
- ThreeBet multiplier solo afecta `OpenRaiseVs3Bet` y `VsSqueeze`.
- Optimización: si el ajuste es neutro (<1% en posición, <5% en VPIP) se devuelve el rango sin clonar.

🟡 **INFERIDO** (a verificar en consumidores):
- El consumidor (Monte Carlo en `OpenScrape.DecisionMaker.Algorithms.MonteCarloSimulator`) usa `Hands` como pesos para selección ponderada de manos del villain.
- El campo `Name` se usa para logging/debug, no para lógica.
