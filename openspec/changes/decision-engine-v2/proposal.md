# Proposal: Motor de Decisiones v2 — Explotación y Balance

## Contexto

Tras completar sprints 8-17 (decisiones avanzadas, calibración, integración game loop) y los refinamientos L1-L6, el motor cubre los paths principales de decisión postflop. Sin embargo, un análisis exhaustivo revela **15 gaps estratégicos** que afectan el win rate estimado en 10-15 BB/100 acumulados.

Los gaps se agrupan en 4 categorías:
1. **Explotación** — No explota tendencias detectables del villain (donk bets, barrel frequency, stats faltantes)
2. **Balance** — Decisiones determinísticas que son explotables (check-raise 100%, c-bet frequency fija)
3. **Spots específicos** — Situaciones de poker no cubiertas (BvB, limp-raise, reverse implied river)
4. **Precisión** — Cálculos que pierden equity por simplificación (outs, multiway, board texture)

---

## S18: Sprint Explotación (Alto Impacto)

### S18.1: Explotación de Donk Bets

**Archivo:** `PostflopDecisionService.cs` — `HandleFacingBet()`
**Impacto:** +1-2 BB/100

**Problema:** Se detecta donk bet correctamente (`isDonkBet` vía cross-street aggressor tracking), pero la respuesta es idéntica a facing c-bet. Un donk bettor típico:
- Foldea ~70% ante raise (vs ~40% contra check-raise normal)
- Señala debilidad (range capped: pair débil, draw sin equity)
- Rara vez tiene mano fuerte (si la tuviera, check-raise al agresor)

**Solución:**
- Si `isDonkBet` + equity > ValueAbove → Raise 3.5x (freq 70%, vs 50% normal)
- Si `isDonkBet` + equity marginal → Call freq 60% (vs 40% normal)
- Si `isDonkBet` + nut hand → Raise Pot (value máximo por weakness)
- Nuevo parámetro en StreetThresholds: `DonkBetRaiseFrequency`, `DonkBetCallBonus`

---

### S18.2: Barrel Frequency Tracking

**Archivo:** `OpponentTracker.cs`, `OpponentProfile.cs`, `PostflopDecisionService.cs`
**Impacto:** +2-3 BB/100

**Problema:** El sistema detecta `villainBarreling` (bool) pero no trackea la **frecuencia** con que el villain barrelea. Distintos tipos de villain:
- LAG: barrelea 60% de las veces
- TAG: barrelea 30%
- TP: barrelea 10%
- LP: barrelea 20%

Si villain barrelea más que lo esperado para su tipo, su rango es más fuerte → debemos foldear más.

**Solución:**
```
OpponentProfile:
+ TimesBarreled: int
+ TimesBarrelOpportunity: int
+ BarrelFrequency: double (calculado)
+ HasReliableBarrelData: bool (>= 8 muestras)

PostflopDecisionService (facing bet, turn/river):
- Calcular expectedBarrelFreq por villainType
- Si observedBarrelFreq > expectedBarrelFreq × 1.2 → FoldBelow +3
- Si observedBarrelFreq < expectedBarrelFreq × 0.8 → FoldBelow -2 (bluffea más)
```

---

### S18.3: Stats de Villain Ampliados (WTSD/W$SD/CR%)

**Archivo:** `OpponentProfile.cs`, `OpponentTracker.cs`, `PostflopDecisionService.cs`
**Impacto:** +2-4 BB/100

**Problema:** OpponentTracker solo tiene VPIP/PFR/CBet/FoldToBet/AF. Faltan stats críticos para explotación:

| Stat | Significado | Uso |
|------|-------------|-----|
| WTSD% | Went To ShowDown | Si >50%, villain es calling station → bluffear menos, value bet más |
| W$SD% | Won $ at ShowDown | Si >60%, villain tiene rango fuerte en showdown → respect más |
| CheckRaise% | Frecuencia de check-raise | Si >15%, cuidado con c-bets ligeras |
| DonkBet% | Frecuencia de donk bet | Si >20%, villain es recreacional → exploit |

**Solución:**
```
OpponentProfile:
+ TimesWentToShowdown: int
+ TimesWonAtShowdown: int  
+ TimesCheckRaised: int
+ TimesCheckRaiseOpportunity: int
+ TimesDonkBet: int
+ TimesDonkBetOpportunity: int
+ WTSDPct, WSDPct, CheckRaisePct, DonkBetPct (calculados)
+ HasReliableWTSDData (>= 15 muestras)

OpponentTracker:
+ TrackShowdownResult(villainAlias, bool wentToSD, bool wonSD)
+ TrackCheckRaise(villainAlias, bool didCR, bool hadOpportunity)
+ TrackDonkBet(villainAlias, bool didDonk, bool hadOpportunity)

PostflopDecisionService ajustes:
- WTSD > 50%: BluffFrequency × 0.6, ValueBetThreshold -3
- WTSD < 25%: BluffFrequency × 1.4
- W$SD > 60%: FoldBelow +2 facing bet
- CheckRaise > 15%: C-bet frequency × 0.7
- DonkBet > 20%: isDonkBet → raise frequency +20%
```

---

## S19: Sprint Balance (Alto Impacto)

### S19.1: Check-Raise Mixing (Probabilístico)

**Archivo:** `PostflopDecisionService.cs` — sección check-raise
**Impacto:** +2 BB/100

**Problema:** Cuando equity > CheckRaiseThreshold, el sistema hace check-raise 100% del tiempo. Esto es predecible y explotable por regs que ajustan.

**Solución:** Check-raise probabilístico basado en hand strength y posición:

| Situación | CR Freq | Call Freq |
|-----------|---------|-----------|
| OOP + TwoPair+ | 40% | 60% |
| OOP + TopPair + FlushDraw | 35% | 65% |
| OOP + OESD (9+ outs) | 30% | 70% |
| IP + TwoPair+ (trap) | 20% | 80% |
| IP + draw fuerte | 15% | 85% |

Usar `Random.Shared.NextDouble() < checkRaiseFreq` para la decisión.
Nuevo parámetro en StrategyProfile: `CheckRaiseMixingEnabled` (bool, default true).

---

### S19.2: C-Bet Turn Ajustada por Textura del Runout

**Archivo:** `PostflopDecisionService.cs` — sección c-bet
**Impacto:** +1-2 BB/100

**Problema:** C-bet frequency es fija por street (Flop 65%, Turn 45%, River 30%). No ajusta cuando el turn cambia drásticamente la textura:
- Turn completa flush → freq debería bajar 50%
- Turn parea board → freq baja 40% (villain trips)
- Turn completa straight → freq baja 60%
- Turn es brick bajo → freq sube 10% (favorable)

**Solución:**
```
GetAdjustedCbetFrequency(street, boardChange, baseCbetFreq):
  if street != Turn: return baseCbetFreq
  
  multiplier = 1.0
  if boardChange.FlushCompleted:      multiplier × 0.30  // Casi nunca c-bet
  if boardChange.FlushDrawAppeared:   multiplier × 0.50
  if boardChange.BoardPaired:         multiplier × 0.60
  if boardChange.StraightCompleted:   multiplier × 0.40
  if boardChange.IsBrick:             multiplier × 1.10  // Favorable
  
  return baseCbetFreq × multiplier
```

Nuevos parámetros en StrategyProfile: `CbetTurnFlushCompletedMultiplier`, `CbetTurnPairedMultiplier`, etc.

---

### S19.3: Defensa en 3Bet Pots Postflop

**Archivo:** `PostflopDecisionService.cs` — nuevo path de decisión
**Impacto:** +2-3 BB/100

**Problema:** En 3bet pots, hero como caller OOP no tiene lógica específica. Los thresholds solo aplican +5/+3 genérico, pero la estrategia debería ser **polar**:
- Manos fuertes: check-raise agresivo (35-50% freq en flop)
- Draws con equity: check-raise como semi-bluff
- Manos medias: check-call
- Manos débiles: fold (no flotar OOP en 3bet pot)

**Solución:**
```
Handle3BetPotDefenseOOP(equity, heroHandRank, hasDraws, street):
  if street == Flop:
    if heroHandRank >= TwoPair:        CR freq 50%, call 50%
    if hasComboDrawOrFlushDraw:        CR freq 35%, fold 65%
    if heroHandRank == OnePair (TP+):  CR freq 25%, call 75%
    if equity < FoldBelow:             fold (no float OOP)
  
  if street == Turn:
    if aggressor check (debilidad):
      - Probe bet freq 40% con equity > 45%
      - Check con equity < 45%
    if aggressor bet:
      - CR freq 20% con TwoPair+ (anti-barrel)
      - Call con draw equity > pot odds
      - Fold con equity < FoldBelow + 3 (rango estrecho en turn 3bet)
```

---

## S20: Sprint Spots Específicos (Medio Impacto)

### S20.1: Blind vs Blind Thresholds Dinámicos

**Archivo:** `PostflopDecisionService.cs`, `StreetThresholds`
**Impacto:** +1-2 BB/100

**Problema:** BvB no tiene ajuste. En blind spots:
- SB vs BB: defender 3-4 puntos más amplio (rango villain muy amplio)
- BB vs SB raise: defender ~5 puntos más amplio (descuento por ciega ya puesta)
- BB vs BTN raise: estándar

**Solución:**
```
GetPositionalThresholdAdjustment(heroPosition, villainPosition):
  (SB, BB) → FoldBelow -3, ThinValueAbove -2
  (BB, SB) → FoldBelow -5, ThinValueAbove -3
  (BB, BTN) → FoldBelow -1
  _ → sin ajuste
```

---

### S20.2: Detección de Limp-Raise

**Archivo:** `PreflopAnalyzer.cs`, `VillainRange.cs`, `Positions.cs` (enums)
**Impacto:** +0.5-1 BB/100

**Problema:** Un limper que 3betea tiene rango ~2-3% (AA/KK/QQ/AKs). El sistema lo trata como 3bettor normal (~8%), subestimando masivamente su rango.

**Solución:**
```
HandSituation enum:
+ LimpRaise

PreflopAnalyzer.DetectSituation():
  if limperAction == Limp && subsequentAction == Raise:
    return HandSituation.LimpRaise

VillainRange.GetForSituation(LimpRaise):
  return CreateRange("Limp-Raiser", 3.0, {
    AA: 1.0, KK: 1.0, QQ: 0.7, AKs: 1.0, AKo: 0.5
  })
```

---

### S20.3: Reverse Implied Odds en River por Bluffs Futuros

**Archivo:** `PostflopDecisionService.cs` — `CalculateReverseImpliedOdds()`
**Impacto:** +0.5-1 BB/100

**Problema:** El cálculo actual solo penaliza por draws en board. No captura que villain bluffeará rivers fallidos. Hero con TPWK en board con draws tiene equity real ~5% menor.

**Solución:**
```
En CalculateReverseImpliedOdds():
  if street == Turn && heroHandRank == OnePair && boardHasDraws:
    // Villain bluffeará ~50% de rivers donde draw falla
    bluffRiskPenalty = 0.3 × drawMissFrequency × potSize / (potSize + heroStack)
    reverseImpliedPenalty += bluffRiskPenalty
    
  // Ajuste por villain type
  if villainType == LAG:  bluffRiskPenalty × 1.5
  if villainType == TP:   bluffRiskPenalty × 0.5
```

---

### S20.4: Squeeze Defense

**Archivo:** `PreflopAnalyzer.cs`, `PostflopDecisionService.cs`
**Impacto:** +0.5-1 BB/100

**Problema:** `HandSituation.VsSqueeze` existe pero no hay lógica para decidir 4bet vs call vs fold. El squeeze implica rango más fuerte que 3bet estándar.

**Solución:**
```
HandleSqueezeDecision(heroCards, heroStack, potSize, villainStack):
  equityVsSqueezeRange = CalculateEquity(heroCards, squeezeRange)
  
  4bet: AA/KK/AKs siempre. QQ/AQs si SPR > 2.5
  Call: equity 4-8% + blocker (Ax/Kx) + IP + SPR > 3
  Fold: equity < 4% o OOP sin blocker
  
Thresholds postflop en squeeze pot: FoldBelow +6, ThinValue +4 (más que 3bet normal)
```

---

## S21: Sprint Precisión (Refinamientos)

### S21.1: Outs de Overcards Ajustados por Board Texture

**Archivo:** `OutsCalculator.cs`
**Impacto:** +0.5 BB/100

**Problema:** Cuenta 3 outs fijos por overcard. En board conectado (K98), algunos outs completan straights del villain → deberían ser 2 outs. En board paired, overcard outs = 2.5.

**Solución:**
```
overcardOuts = 3
if boardTexture.IsConnected && straightCompletingRanks.Contains(overcardRank):
  overcardOuts = 2  // Tainted por straight
if boardChange.BoardPaired:
  overcardOuts = 2  // Reducido por trips posibles
if heroBlocksTopCard:
  overcardOuts × 1.2  // Boost por blocker
```

---

### S21.2: Multiway Penalty por Posición Exacta

**Archivo:** `PostflopDecisionService.cs` — multiway penalty section
**Impacto:** +0.5-1 BB/100

**Problema:** La penalización cuadrática OOP es genérica. SB (más OOP de la mesa) necesita ~5% más cautela que BB. EP en multiway también debería tener penalty mayor.

**Solución:**
```
oopMultiplier = heroPosition switch:
  SmallBlind     → 0.70  // Más OOP, más cautela
  BigBlind       → 0.50  // Estándar
  EarlyPosition  → 0.60
  _              → 0.50

if numOpponents >= 2 && villainIsIP && villainIsAggressor:
  oopMultiplier × 1.3  // Villain IP agresivo es más peligroso multiway
```

---

### S21.3: Board Texture "Broadway Wet"

**Archivo:** `BoardTextureAnalyzer.cs`
**Impacto:** +0.5-1 BB/100

**Problema:** Boards como AKQ se clasifican como SemiDry por wetness score, pero cualquier hand con broadway (QJ, JT, QT, AJ) tiene muchos outs. Falta reconocer esta categoría.

**Solución:**
```
En CalculateWetnessScore():
  broadwayCount = boardCards.Count(c => c.Rank >= 10)
  if broadwayCount >= 2 && isConnected:
    wetnessScore += 20  // "BroadwayWet" bonus

O nueva categoría en BoardTextureCategory enum: BroadwayWet

PostflopDecisionService ajustes para BroadwayWet:
  FoldBelow +3 (villain hits more broadway combos)
  ThinValueAbove +2
  C-bet frequency × 0.8 (board favorece caller range)
```

---

### S21.4: Backdoor Draw Overlap Prevention

**Archivo:** `OutsCalculator.cs`
**Impacto:** +0.3 BB/100

**Problema:** Si hero tiene flush draw principal (4 suited) + backdoor straight, los outs se suman sin verificar overlap. Algunas cartas que completan backdoor straight también completan flush → double-count.

**Solución:**
```
Antes de suma final en CalculateOuts():
if hasMainFlushDraw && hasBackdoorStraightDraw:
  // Verificar cartas que overlappean
  overlappingOuts = backdoorStraightCards.Count(c => c.Suit == mainFlushSuit)
  backdoorStraightOuts -= overlappingOuts × 0.5  // Descuento parcial
```

---

### S21.5: Randomización con Margen Variable

**Archivo:** `PostflopDecisionService.cs` — sección randomización
**Impacto:** +0.3 BB/100

**Problema:** `RandomizationMargin = 3%` fijo para todos los villain types. Contra LAG, que ajusta más, necesitas más variación para ser inexplotable.

**Solución:**
```
randomizationMargin = villainType switch:
  LAG     → 5.0  // Más variación contra agresivos
  TAG     → 3.0  // Estándar
  LP      → 4.0
  TP      → 2.0  // Menos variación contra pasivos (no ajustan)
  Unknown → 3.0
```

---

## Resumen de Impacto

| Sprint | Items | BB/100 estimado | Archivos principales |
|--------|-------|-----------------|----------------------|
| S18 — Explotación | S18.1-S18.3 | +5-7 | PostflopDecisionService, OpponentTracker, OpponentProfile |
| S19 — Balance | S19.1-S19.3 | +4-6 | PostflopDecisionService, StrategyProfile |
| S20 — Spots | S20.1-S20.4 | +2-4 | PreflopAnalyzer, PostflopDecisionService, VillainRange |
| S21 — Precisión | S21.1-S21.5 | +1-2 | OutsCalculator, BoardTextureAnalyzer, PostflopDecisionService |
| **Total** | **15 items** | **+12-19** | |

## Dependencias

- S18.3 (stats ampliados) es prerequisito de S18.1 (donk exploit usa DonkBet%) y S18.2 (barrel tracking)
- S19.3 (3bet defense) depende de S19.1 (CR mixing) para check-raise probabilístico
- S21.1-S21.4 son independientes entre sí

## Testing

Cada item requiere tests unitarios específicos. Estimación: ~60-80 tests nuevos.
Tests de integración: escenarios end-to-end por sprint validando que decisiones mejoran en spots concretos.
