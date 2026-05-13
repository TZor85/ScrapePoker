# Data Dictionary — ScrapePoker

> Diccionario de datos consolidado generado por el Arqueólogo del Reversa.
> Cada módulo añade sus tipos. Los tipos persistidos en Marten están marcados con 📂.
>
> Escala: 🟢 CONFIRMADO · 🟡 INFERIDO · 🔴 LACUNA

---

## Módulo: `OpenScrape.Domain`

### 📂 `GameSession` — `Entities/GameSession.cs`

**Documento Marten** — un documento por sesión/mesa.

| Campo | Tipo | Obligatorio | Default | Notas |
|-------|------|:-----------:|---------|-------|
| `Id` | `string` | ✅ | `Guid.NewGuid()` | Clave primaria Marten |
| `SessionId` | `string` | ✅ | `""` | Identificador lógico de sesión |
| `TableName` | `string` | ✅ | `""` | Nombre de la mesa |
| `StartTime` | `DateTime` | ✅ | `DateTime.UtcNow` | Inicio de sesión |
| `EndTime` | `DateTime` | ✅ | `DateTime.UtcNow` | Fin de sesión |
| `BigBlind` | `decimal` | ✅ | `0.50m` | Big blind de la mesa |
| `StartingBankroll` | `decimal` | ✅ | `0` | Bankroll al inicio |
| `EndingBankroll` | `decimal` | ✅ | `0` | Bankroll al cierre |
| `PeakBankroll` | `decimal` | ✅ | `0` | Pico observado |
| `Hands` | `List<HandRecord>` | ❌ | `[]` | `[JsonIgnore]` — NO persistido |
| `TotalHands` | `int` | ❌ | computed | `Hands.Count` (no persistido) |
| `TotalProfit` | `decimal` | ❌ | computed | Sum stack diff de manos con Result conocido |
| `BBPer100` | `double` | ❌ | computed | `(TotalProfit/BigBlind)/TotalHands*100` |
| `IsValid` | `bool` | ❌ | computed | `SessionId+TableName != "" && BigBlind > 0` |

### 📂 `HandRecord` — `Entities/GameSession.cs:56`

**Documento Marten** — un documento por mano. FK lógica `GameSessionId`.

| Campo | Tipo | Obligatorio | Default | Notas |
|-------|------|:-----------:|---------|-------|
| `Id` | `string` | ✅ | `Guid.NewGuid()` | Clave primaria Marten |
| `GameSessionId` | `string` | ✅ | `""` | FK hacia `GameSession.Id` |
| `HandNumber` | `long` | ✅ | `0` | Número de mano en la sesión |
| `Timestamp` | `DateTime` | ✅ | `DateTime.UtcNow` | Inicio de mano |
| `HeroCard1` | `string` | ✅ | `""` | Carta 1 del hero |
| `HeroCard2` | `string` | ✅ | `""` | Carta 2 del hero |
| `HeroPosition` | `TablePosition` | ✅ | `None` | Posición del hero |
| `HeroStackStart` | `decimal` | ✅ | `0` | Stack al inicio de mano |
| `HeroStackEnd` | `decimal` | ✅ | `0` | Stack al final de mano |
| `BlindPosted` | `decimal` | ✅ | `0` | SB/BB obligatoria pagada |
| `AutoRebuy` | `decimal` | ✅ | `0` | Rebuy automático detectado |
| `NetProfit` | `decimal` | ❌ | computed | `(HeroStackEnd - HeroStackStart) - AutoRebuy + BlindPosted` |
| `FlopCards` | `List<string>` | ❌ | `[]` | Cartas del flop |
| `TurnCard` | `string?` | ❌ | `null` | Carta del turn |
| `RiverCard` | `string?` | ❌ | `null` | Carta del river |
| `Decisions` | `List<StreetDecision>` | ❌ | `[]` | Decisiones por street |
| `PotSizeFinal` | `decimal` | ✅ | `0` | Pote al cierre |
| `LastStreetPlayed` | `BoardPosition` | ✅ | `None` | Última calle alcanzada |
| `NumOpponents` | `int` | ✅ | `0` | Oponentes activos |
| `Result` | `HandResult` | ✅ | `Unknown` | Won/Lost/Push/Unknown |
| `Situation` | `HandSituation` | ✅ | `None` | Situación inicial |
| `Telemetry` | `TelemetryAggregate?` | ❌ | `null` | Métricas de rendimiento |

### 📂 `Card` — `Entities/Card.cs`

**Documento Marten** — catálogo de cartas con imagen para reconocimiento OCR.

| Campo | Tipo | Obligatorio | Notas |
|-------|------|:-----------:|-------|
| `Id` | `string` | ✅ | Identificador (ej: `"As"`) |
| `ImageBase64` | `string?` | ❌ | Imagen serializada |
| `BinaryValue` | `string?` | ❌ | Hash binario para comparación rápida |
| `Hall` | `List<string>?` | ❌ | Variantes/aliases visuales por sala |
| `Force` | `int` | ✅ | Fuerza relativa |
| `Suit` | `int` | ✅ | Palo (codificado int, no `Suit` enum) |

### 📂 `Table` — `Entities/Table.cs`

**Documento Marten** — definición lógica de mesa.

| Campo | Tipo | Obligatorio | Notas |
|-------|------|:-----------:|-------|
| `Id` | `string` | ✅ | Nombre/clave de la mesa |
| `Positions` | `List<PlayerActionSequence>?` | ❌ | Secuencias de acción por posición |

### 📂 `RegionTableMap` — `Entities/RegionTableMap.cs`

**Documento Marten** — mapeo de regiones de captura OCR por mesa.

| Campo | Tipo | Obligatorio | Notas |
|-------|------|:-----------:|-------|
| `Id` | `string` | ✅ | Clave de mapa |
| `Regions` | `List<Region>?` | ❌ | Coordenadas de captura |

### `OverlayConfig` — `Entities/OverlayConfig.cs`

Configuración visual del overlay (cargada desde `appsettings.json`).

| Campo | Tipo | Default | Notas |
|-------|------|---------|-------|
| `Opacity` | `double` | `0.8` | Opacidad del overlay [0, 1] |
| `FontSize` | `float` | `10` | Tamaño fuente texto base |
| `ActionFontSize` | `float` | `14` | Tamaño fuente acción recomendada |
| `VerticalOffset` | `int` | `75` | Desplazamiento vertical px |
| `HorizontalOffsetPercent` | `double` | `0.15` | Desplazamiento horizontal % ancho |

### 📂 `StrategyProfile` — `Entities/StrategyProfile.cs`

**Documento Marten** — configuración masiva de estrategia. ~150 parámetros tunables. Validado al arranque vía `Validate()`.

#### Identidad
| Campo | Tipo | Default |
|-------|------|---------|
| `Id` | `string` | `Guid.NewGuid()` |
| `Name` | `string` | `"Default"` |
| `CreatedAt` | `DateTime` | `DateTime.UtcNow` |
| `Thresholds` | `Dictionary<string, StreetThresholds>` | `{}` — clave `"{Street}_{Situation}"` |

#### Fold Equity (7 campos)
| Campo | Default | Significado |
|-------|---------|-------------|
| `FoldEquityBase` | 20.0 | FE base sin ajustes |
| `FoldEquityFlopBonus` | 5.0 | +5 si flop |
| `FoldEquityRiverPenalty` | -5.0 | -5 si river |
| `FoldEquityIPBonus` | 10.0 | +10 si IP |
| `FoldEquityThreeBetPenalty` | -10.0 | -10 si 3bet pot |
| `FoldEquityMin` | 5.0 | Floor |
| `FoldEquityMax` | 60.0 | Cap |

#### Bet Sizing (8 multiplicadores)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `BetSizingSPRDeepMultiplier` | 1.25 | SPR > `BetSizingSPRDeepThreshold` |
| `BetSizingSPRDeepThreshold` | 3.0 | — |
| `BetSizingSPRShallowMultiplier` | 0.75 | SPR < `BetSizingSPRShallowThreshold` |
| `BetSizingSPRShallowThreshold` | 1.0 | — |
| `BetSizingPairedMultiplier` | 1.15 | Board paired |
| `BetSizingCoordinatedMultiplier` | 0.90 | Board coordinated |
| `BetSizingOOPMultiplier` | 0.90 | Hero OOP |
| `BetSizingMultiOpponentMultiplier` | 0.85 | Multiway |

#### Bluff Frequencies (3+3+4)
| Campo | Default | Calle/Contexto |
|-------|---------|----------------|
| `FlopBluffFrequency` | 0.15 | Flop |
| `TurnBluffFrequency` | 0.12 | Turn |
| `RiverBluffFrequency` | 0.10 | River |
| `CbetFrequencyFlop` | 0.65 | C-bet flop (separada de bluff) |
| `CbetFrequencyTurn` | 0.45 | C-bet turn |
| `CbetFrequencyRiver` | 0.30 | C-bet river |
| `BluffSPRShortThreshold` | 2.0 | SPR corto threshold |
| `BluffSPRShortMultiplier` | 0.5 | Reduce bluff con SPR < 2 |
| `BluffSPRDeepThreshold` | 4.0 | SPR profundo threshold |
| `BluffSPRDeepMultiplier` | 1.2 | Aumenta bluff con SPR > 4 |

#### Kicker (2)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `KickerStrongEquityBonus` | 3.0 | TPTK → +3 equity facing bet |
| `KickerWeakEquityPenalty` | 2.0 | TPWK OOP → +2 FoldBelow |

#### Danger Penalties (15+ campos)
| Campo | Default | Significado |
|-------|---------|-------------|
| `DangerFlushCompletePct` | 35.0 | Flush completado: equity × 35% penalty |
| `DangerStraightCompletePct` | 18.0 | Straight completado: equity × 18% |
| `DangerBoardPairedPenalty` | 5.0 | Board paired: -5 flat |
| `DangerOvercardPenalty` | 3.0 | Overcard turn/river: -3 flat |
| `DangerFlushDrawPenalty` | 5.0 | Flush draw 3 same suit: 5 flat |
| `DangerFacingBetMultiplier` | 1.4 | ×1.4 si facing bet |
| `DangerHeroBlocksReduction` | 0.5 | Hero bloquea palo (legacy global) |
| `DangerNutBlockerReduction` | 0.35 | Nut blocker (As del palo): ×0.35 |
| `DangerNonNutBlockerReduction` | 0.55 | Non-nut blocker: ×0.55 |
| `DangerBlockerBoard4FlushReduction` | 0.7 | Board 4-flush: blocker menos relevante |
| `DangerFlushDrawNutBlockerReduction` | 0.50 | Flush DRAW + nut blocker (L2) |
| `DangerFlushDrawNonNutBlockerReduction` | 0.70 | Flush DRAW + non-nut blocker |
| `DangerPenaltyFlopMultiplier` | 1.3 | Flop más riesgo (2 calles por venir) |
| `DangerPenaltyRiverMultiplier` | 0.8 | River menos riesgo (definitivo) |
| `DangerCompletedDrawNoBetCap` | 45.0 | Equity max para apostar con draw completed sin tenerlo |

#### Implied Odds (9)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `ImpliedOddsSPRDeepFactor` | 0.65 | SPR > `Deep`: alto implied odds |
| `ImpliedOddsSPRMediumFactor` | 0.80 | SPR Medium: moderado |
| `ImpliedOddsSPRShallowFactor` | 0.95 | SPR < `Shallow`: casi nulo |
| `ImpliedOddsSPRDeepThreshold` | 4.0 | — |
| `ImpliedOddsSPRShallowThreshold` | 2.0 | — |
| `ImpliedOddsFlushDrawBonus` | 0.90 | Flush draws más ocultos |
| `ImpliedOddsIPBonus` | 0.92 | IP controla pot size |
| `ImpliedOddsFlopMultiplier` | 0.90 | Flop: 2 calles para extraer valor |
| `ImpliedOddsTurnMultiplier` | 0.95 | Turn: 1 calle |

#### C-Bet & Range Advantage (5)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `CbetRangeAdvantageBonus` | 8.0 | Hero agresor con range advantage |
| `CbetAggressorBonus` | 4.0 | Hero agresor preflop |
| `CbetCallerDisadvantage` | -3.0 | Hero caller con board favorable al raiser |
| `CbetMonotoneReduction` | 0.5 | Reduce bonus en monotone (rangos equalizan) |
| `CbetMultiwayReduction` | 3.0 | Por cada oponente extra |

#### Barrel Detection (4)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `VillainBarrelFoldIncrease` | 5.0 | Bet 2 calles consecutivas: +5 FoldBelow |
| `VillainBarrelThinValueIncrease` | 3.0 | +3 ThinValueAbove |
| `VillainSizingEscalationPenalty` | 4.0 | Sizing escalado entre streets |
| `VillainBetCheckBetPenalty` | 2.0 | Bet-check-bet (draw fallido) |

#### SPR Push/Fold (5)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `SPRPushFoldThreshold` | 2.0 | Threshold push/fold |
| `SPRPushFoldFoldReduction` | 8.0 | -8 FoldBelow con SPR < 2 |
| `SPRPushFoldValueIncrease` | 10.0 | +10 ValueAbove con SPR < 2 |
| `SPRDeepCautionThreshold` | 4.0 | Threshold cautela deep |
| `SPRDeepFoldIncrease` | 3.0 | +3 FoldBelow con SPR > 4 |

#### Multiway (5)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `MultiwayStreetMultiplierTurn` | 1.2 | Turn ×1.2 multiway |
| `MultiwayStreetMultiplierRiver` | 1.4 | River ×1.4 multiway |
| `MultiwayOOPQuadraticDamping` | 0.5 | Damping cuadrático OOP |
| `MultiwayOOPMultiplierSB` | 0.70 | SB multiway |
| `MultiwayOOPMultiplierBB` | 0.50 | BB multiway |
| `MultiwayOOPMultiplierEP` | 0.60 | EP multiway |
| `MultiwayIPAggressorAmplifier` | 1.3 | IP+Aggressor amplifier |

#### 3-Bet/4-Bet Pot Adjustments (4)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `ThreeBetPostflopFoldIncrease` | 5.0 | +5 FoldBelow en 3bet pot |
| `ThreeBetPostflopValueIncrease` | 3.0 | +3 ValueAbove |
| `FourBetPostflopFoldIncrease` | 8.0 | +8 FoldBelow en 4bet pot |
| `FourBetPostflopValueIncrease` | 5.0 | +5 ValueAbove |

#### Check-Raise (1+1+4)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `CheckRaiseSPRMinThreshold` | 1.5 | SPR < 1.5 → skip CR |
| `CheckRaiseLowSPRMinEquity` | 60.0 | Equity mín si SPR low |
| `CheckRaiseDrawMinEquity` | 40.0 | Equity mín CR semi-bluff |
| `CheckRaiseMixingEnabled` | `true` | Mixing probabilístico |
| `CRMixFreqOOPStrong` | 0.40 | CR mixing OOP TwoPair+ |
| `CRMixFreqOOPTopPairDraw` | 0.35 | CR mixing OOP TP+draw |
| `CRMixFreqOOPDraw` | 0.30 | CR mixing OOP draw fuerte |
| `CRMixFreqIPTrap` | 0.20 | CR mixing IP trap |

#### Reverse Implied (4+3)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `ReverseImpliedFlushDrawPenalty` | 7.0 | Penalty turn/river facing bet flush draw |
| `ReverseImpliedCoordinatedPenalty` | 4.0 | Coordinated board |
| `ReverseImpliedOnePairMultiplier` | 1.5 | OnePair más vulnerable |
| `ReverseImpliedBlockerReduction` | 0.5 | Hero bloquea palo del draw |
| `BluffRiskBaseFactor` | 0.30 | S20.3: bluff risk base |
| `BluffRiskLAGMultiplier` | 1.5 | LAG mayor riesgo |
| `BluffRiskTPMultiplier` | 0.5 | TP menor riesgo |

#### Bluff Catching (2)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `BluffCatchFoldBelowMultiplier` | 0.75 | equity ≥ FoldBelow×0.75 → call |
| `BluffCatchTurnEquityMultiplier` | 0.90 | Umbral turn más estricto |

#### Combo Draw (6)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `ComboDrawEquityBonus` | 6.0 | Bonus equity flush+straight draw |
| `ComboDrawTextureDry` | 1.2 | Multiplier por textura Dry |
| `ComboDrawTextureSemiDry` | 1.0 | SemiDry |
| `ComboDrawTextureSemiWet` | 0.8 | SemiWet |
| `ComboDrawTextureWet` | 0.6 | Wet |
| `ComboDrawTextureMonotone` | 0.5 | Monotone |

#### Tainted Outs (3)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `TaintedOutsDiscount` | 0.5 | Out vale la mitad si villain también mejora |
| `TaintedOutsDiscountHeroStrong` | 0.7 | Hero con flush draw → 0.7 |
| `TaintedOutsDiscountHeroWeak` | 0.3 | Hero sin flush draw → 0.3 |

#### Floating IP (3)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `FloatingIPMinEquity` | 25.0 | Equity mín float |
| `FloatingIPMaxEquity` | 35.0 | Equity max float |
| `FloatingIPMinOuts` | 6 | Outs mín para draw real |

#### Slow Play (1)
| `SlowPlayMinEquity` | 72.0 | Equity mín para check con nuts |

#### Sprints S18-S22 — calibración avanzada
Ver `Entities/StrategyProfile.cs:184-281`. Suman ~50 parámetros adicionales agrupados por sprint:
- **S18.1**: Donk bet exploitation (3 campos)
- **S18.2**: Barrel frequency (4 campos)
- **S18.3**: Expanded villain stats — WTSD, WSD, CR multipliers (5 campos)
- **S19.1**: Check-raise mixing (5 campos)
- **S19.2**: C-bet turn ajustada por runout (5 campos)
- **S19.3**: 3-bet pot defense (6 campos)
- **S20.1-4**: Blind vs Blind, Limp-Raise, Bluff Risk, Squeeze (12 campos)
- **S21.1-5**: Overcard outs, multiway por posición, Broadway Wet, Backdoor overlap, Randomización (12 campos)
- **S22.1-8**: River runout, posición villain, multiway nut, bluff freq scaling, pot commitment, hand re-eval (10+ campos)

#### Bankroll (4)
| Campo | Default | Aplicación |
|-------|---------|------------|
| `InitialBankroll` | 100.0m | Bankroll inicial |
| `BuyInMax` | 2.0m | BuyIn max (BB) |
| `MinSessionsForRecommendation` | 20 | Min sesiones para recomendación |
| `RiskOfRuinThreshold` | 0.05 | Threshold ROI |

### 📂 `OpponentProfile` — `Entities/OpponentProfile.cs:47`

**Documento/embebido** — perfil estadístico acumulado por oponente.

| Campo | Tipo | Default | Persist | Notas |
|-------|------|---------|:-------:|-------|
| `PlayerId` | `string` | `""` | ✅ | ID del oponente |
| `HandsPlayed` | `int` | `0` | ✅ | Manos jugadas |
| `TimesVoluntarilyPutMoneyIn` | `int` | `0` | ✅ | Contador VPIP |
| `TimesPreflopRaised` | `int` | `0` | ✅ | Contador PFR |
| `TimesThreeBet` | `int` | `0` | ✅ | Contador 3bet |
| `TimesPostflopBet/Raised/Called/Folded` | `int[4]` | `0` | ✅ | Contadores postflop globales |
| `TimesCBet` | `int` | `0` | ✅ | C-bets realizadas |
| `TimesCBetOpportunity` | `int` | `0` | ✅ | Oportunidades C-bet |
| `TimesFoldedToCBet` | `int` | `0` | ✅ | Folds a C-bet |
| `TimesFacedCBet` | `int` | `0` | ✅ | C-bets enfrentadas |
| `TimesAggressive/Passive IP/OOP` | `int[4]` | `0` | ✅ | Postflop por posición relativa (S22.3) |
| `TimesReachedRiver/WentToShowdown/WonAtShowdown` | `int[3]` | `0` | ✅ | Showdown counters (S18.3) |
| `TimesCheckRaised/CheckRaiseOpportunity` | `int[2]` | `0` | ✅ | CR counters |
| `TimesDonkBet/DonkBetOpportunity` | `int[2]` | `0` | ✅ | Donk counters |
| `TimesBarreled/BarrelOpportunity` | `int[2]` | `0` | ✅ | Barrel counters |
| `PositionProfiles` | `Dictionary<TablePosition, OpponentPositionProfile>` | `{}` | ✅ | Stats por posición exacta |

**Computed (no persist):**

| Computed | Fórmula | Default sin datos |
|----------|---------|-------------------|
| `VPIP` | `TimesVPIP / HandsPlayed × 100` | `50` |
| `PFR` | `TimesPFR / HandsPlayed × 100` | `15` |
| `ThreeBetPct` | `Times3Bet / HandsPlayed × 100` | `5` |
| `AggressionFactor` | `(bet+raise+1) / (called+1)` Laplace | `1.0` |
| `AggressionFactorIP` | `(aggIP+1)/(passIP+1)` if `≥5` else `-1` | `-1` |
| `AggressionFactorOOP` | `(aggOOP+1)/(passOOP+1)` if `≥5` else `-1` | `-1` |
| `FoldToCBetPct` | `TimesFoldedToCBet / TimesFacedCBet × 100` | `50` |
| `CBetPct` | `TimesCBet / TimesCBetOpportunity × 100` | `50` |
| `WTSDPct` | `WentToShowdown / ReachedRiver × 100` | `35` |
| `WSDPct` | `WonAtShowdown / WentToShowdown × 100` | `50` |
| `CheckRaisePct` | `Times / Opp × 100` | `8` |
| `DonkBetPct` | `Times / Opp × 100` | `10` |
| `BarrelFrequency` | `Times / Opp × 100` | `-1` |
| `Type` | switch (VPIP>30, AF>1.5) | `Unknown` if <10 hands |
| `ExpectedBarrelFrequency` | switch Type → 60/30/20/10 | `30` |
| `IsReliable` | `HandsPlayed ≥ 20` | `false` |
| `HasReliablePreflopData` | `HandsPlayed ≥ 10` | `false` |
| `HasReliableCBetData` | `Opp ≥ 5 ∧ Faced ≥ 5` | `false` |
| `HasReliableAFData` | `bet+raise+called ≥ 10` | `false` |
| `HasReliableFoldData` | `folded+called+raised ≥ 8` | `false` |
| `HasReliableWTSDData` | `ReachedRiver ≥ 15` | `false` |
| `HasReliableWSDData` | `WentToShowdown ≥ 10` | `false` |
| `HasReliableCheckRaiseData` | `Opp ≥ 10` | `false` |
| `HasReliableDonkBetData` | `Opp ≥ 8` | `false` |
| `HasReliableBarrelData` | `Opp ≥ 8` | `false` |

### `OpponentPositionProfile` — `Entities/OpponentProfile.cs:9`

Perfil reducido por posición específica.

| Campo | Tipo | Notas |
|-------|------|-------|
| `HandsPlayed` | `int` | — |
| `TimesVPIP` | `int` | — |
| `TimesPFR` | `int` | — |
| `TimesAggressive/Passive IP/OOP` | `int[4]` | — |
| `VPIP/PFR` | `double` computed | Default 50/15 sin datos |
| `AggressionFactorIP/OOP` | `double` computed | -1 si <5 muestras |
| `IsReliable` | `bool` computed | `≥10` manos |

### Value Objects (sin persistencia directa)

#### `Hand` — `ValueObjects/Hand.cs` (record)

| Campo | Tipo | Validación |
|-------|------|------------|
| `Name` | `string` | No vacío (lanza ArgumentException) |
| `Suited` | `bool?` | — |
| `Action` | `string` | — |
| `Percentage` | `int` | `[0, 100]` (lanza OutOfRange) |

#### `Region` — `ValueObjects/Region.cs` (record)

| Campo | Tipo |
|-------|------|
| `Category`, `Name`, `Color?` | `string` |
| `PosX`, `PosY`, `Width`, `Height` | `int` |
| `IsHash?`, `IsColor?`, `IsBoard?`, `IsOnlyNumber?` | `bool?` |
| `InactiveUmbral?`, `Umbral?` | `double?` |

#### `PlayerActionSequence` — `ValueObjects/PlayerActionSequence.cs` (record)

| Campo | Tipo |
|-------|------|
| `Name`, `HeroPosition` | `string` |
| `OpenRaiser?`, `ThreeBetPosition?`, `Limper?`, `Caller?`, `Squeezer?` | `string?` |
| `BetSize?` | `decimal?` |
| `IsGreater?`, `RaiserFolds?` | `bool?` |
| `Hands` | `List<Hand>` |

#### `CardDataOuts` — `ValueObjects/CardDataOuts.cs`

| Campo | Tipo | Notas |
|-------|------|-------|
| `Suit` | `Suit` enum | — |
| `Rank` | `Rank` enum | — |
| `Id` | `string` computed | `"{Rank}_{Suit}"` |

#### `DrawProbability` — `ValueObjects/CardDataOuts.cs:19`

| Campo | Tipo | Notas |
|-------|------|-------|
| `Outs` | `List<CardDataOuts>` | Outs del draw |
| `Probability` | `double` | Porcentaje (ej: 34.97) |
| `ProbabilityDescription` | `string` computed | `"34.97%"` |

#### `HandStrength` — `ValueObjects/HandStrenght.cs`

13 campos `DrawProbability`, uno por tipo de draw: Flush, Straight, Gutshot, Set, FullHouse, Overcard, TwoPair, DoubleGutshot, StraightFlush, FourOfAKind, ThreeOfAKind, OnePair, HighCard.

#### `HandEvaluation` — `ValueObjects/HandEvaluation.cs`

| Campo | Tipo | Notas |
|-------|------|-------|
| `Rank` | `HandRank` | — |
| `Score` | `long` | Score numérico para comparación |
| `Cards` | `List<CardDataOuts>` | — |
| `Kickers` | `List<int>` | — |

#### `PotOddsResult` — `ValueObjects/PotOddsResult.cs`

| Campo | Tipo |
|-------|------|
| `PotOddsPercentage` | `decimal` |
| `EquityPercentage` | `decimal` |
| `ShouldCall` | `bool` |
| `Street?` | `string?` |

#### `StreetDecision` — `ValueObjects/StreetDecision.cs` (record)

| Campo | Tipo | Default |
|-------|------|---------|
| `Street` | `BoardPosition` | — |
| `EquityPercent` | `double` | — |
| `PotOddsPercent` | `double` | — |
| `ExpectedValue` | `double` | — |
| `RecommendedAction` | `string` | — |
| `ActionTaken` | `string` | — |
| `PotSizeAtDecision` | `decimal` | — |
| `BetSize` | `decimal` | — |
| `Situation` | `HandSituation` | — |
| `IsInPosition` | `bool` | — |
| `Reason?` | `string?` | `null` |
| `BoardTexture?` | `string?` | `null` |
| `TotalOuts` | `int` | `0` |
| `SPR` | `double` | `0` |

#### `StreetThresholds` — `ValueObjects/StreetThresholds.cs` (record)

40+ campos por combinación Street×Situation. Resumen por categoría:

- **Equity tiers (4)**: `FoldBelow`, `ThinValueAbove`, `ValueAbove`, `StrongValueAbove`
- **Bet sizes por tier (3)**: `Strong/Value/ThinValueBetSize` (defaults `"Bet 3/4"`, `"Bet 1/2"`, `"Bet 1/3"`)
- **Bet sizes por board (5)**: `Dry/Coordinated/Paired/Monotone/Wet BoardBetSize`
- **Bluff control (4)**: `CanBluff`, `BluffFrequencyMultiplier=1.0`, `BluffBetSize`, `BluffCondition` (`BluffConditionType`)
- **Low equity (1)**: `LowEquityAction = "Fold" | "Call"`
- **Position handling (2)**: `ThinValueIPOnly=true`, `ThinValueOOPFallback = "CheckFold" | "CheckCall"`
- **Sizing adjustments (2)**: `ReduceSizeForLargeBet=true`, `ReduceSizeForOOP`
- **Check-raise (3)**: `CanCheckRaise`, `CheckRaiseThreshold=75.0`, `CheckRaiseBetSize="Raise 3x"`
- **Overbet (3)**: `CanOverbet`, `OverbetBetSize="Bet 1.25x Pot"`, `OverbetMinEquity=80.0`
- **Combo draw (2)**: `ComboDrawBetSize="Bet 3/4"`, `ComboDrawOutsThreshold=12`
- **Double barrel (1)**: `CanDoubleBarrel=true`
- **Probe bet (4)**: `CanProbeBet`, `ProbeBetSize="Bet 1/3"`, `ProbeBetIPSize="Bet 1/2"`, `ProbeBetMinEquity=25.0`
- **Simplified mode (6)**: `IsSimplified` + 5 bets fijos (RaiseOverLimper)

#### `ThresholdKey` — `ValueObjects/ThresholdKey.cs` (record)

| Campo | Tipo | Validación |
|-------|------|------------|
| `Street` | `BoardPosition` | rechaza `None` y `Hand` |
| `Situation` | `HandSituation` | rechaza `None` |

`ToString()` → `"{Street}_{Situation}"`. `TryParse(string)` con mensajes detallados.

#### `VillainRange` — `ValueObjects/VillainRange.cs`

| Campo | Tipo | Notas |
|-------|------|-------|
| `Hands` | `Dictionary<string, double>` | Notación canónica → frecuencia [0,1] |
| `Name` | `string` | Descriptivo (ej: "Top 8% - vs 3Bet") |
| `RangePercentage` | `double` | % aproximado del total |

8 rangos predefinidos estáticos: `_callerVsOpenRaise` (~25%), `_threeBettor` (~8%), `_callerVs3Bet` (~10%), `_threeBetPotCaller` (~12%), `_callerVs4Bet` (~5%), `_openRaiser` (~20%), `_limper` (~40%), `_callerVsSqueeze` (~12%), `_donkBettor` (~30%).

#### `BankrollSnapshot` — `ValueObjects/BankrollSnapshot.cs`

| Campo | Tipo |
|-------|------|
| `Timestamp` | `DateTime` |
| `Bankroll` | `decimal` |
| `BigBlind` | `decimal` |
| `SessionProfit` | `decimal` |
| `SessionHands` | `int` |
| `SessionId` | `string` |

#### `BankrollStats` — `ValueObjects/BankrollSnapshot.cs:13`

15 campos: `CurrentBankroll`, `StartingBankroll`, `PeakBankroll`, `MaxDrawdown`, `MaxDrawdownPercent`, `WinRateBB100`, `StdDeviation`, `TotalSessions`, `WinningSessions`, `TotalHands`, `RiskOfRuin`, `AverageSessionProfit`, `LimitRecommendation`, `RiskLevel="Green"`, `LastUpdated`.

#### `BankrollHistoryItem` — `ValueObjects/BankrollSnapshot.cs:32`

| Campo | Tipo |
|-------|------|
| `Date` | `DateTime` |
| `Hands` | `int` |
| `Profit` | `decimal` |
| `BBPer100` | `double` |
| `BankrollAfter` | `decimal` |

#### `CategoryStats` — `ValueObjects/CategoryStats.cs` (record)

| Campo | Tipo |
|-------|------|
| `P50Ms` | `double` |
| `P95Ms` | `double` |
| `MaxMs` | `double` |
| `Count` | `long` |

#### `TelemetryAggregate` — `ValueObjects/TelemetryAggregate.cs` (record)

| Campo | Tipo | Notas |
|-------|------|-------|
| `HandId` | `string` | FK lógica al `HandRecord.Id` |
| `CapturedAt` | `DateTime` | — |
| `Phases` | `IReadOnlyDictionary<string, CategoryStats>` | Métricas por fase del pipeline |

### DTOs

#### `CardDTO` — `Dtos/CardDTO.cs`

| Campo | Tipo | Obligatorio |
|-------|------|:-----------:|
| `Name` | `string` | ✅ |
| `ImageBase64` | `string?` | ❌ |
| `BinaryValue` | `string?` | ❌ |
| `Hall` | `List<string>?` | ❌ |
| `Force` | `int` | ❌ |
| `Suit` | `int` | ❌ |

#### `TableDTO` — `Dtos/TableDTO.cs`

| Campo | Tipo | Obligatorio |
|-------|------|:-----------:|
| `Name` | `string` | ✅ |
| `Positions` | `List<PlayerActionSequence>?` | ❌ |

#### `SessionStatsDto` — `Dtos/SessionStatsDto.cs` (record)

9 campos posicionales: `Id`, `SessionId`, `TableName`, `StartTime`, `EndTime`, `BigBlind`, `TotalHands`, `TotalProfit`, `BBPer100`.

### Enums

| Enum | Valores | Tipo subyacente | Notas |
|------|---------|-----------------|-------|
| `Rank` | Two=2..Ace=14 | `byte` | Optimizado para SoA / bitops |
| `Suit` | Clubs=1..Spades=4 | `byte` | — |
| `HandRank` | HighCard=1..RoyalFlush=10 | `byte` | Score base de mano |
| `KickerStrength` | None=0..Strong=3 | `byte` | TPTK/TPWK classification |
| `Positions` | None, OutOfPosition, InPosition | `int` | IP/OOP relativo |
| `TablePosition` | None, Early, Middle, CutOff, Button, SmallBlind, BigBlind | `int` | Posición absoluta en mesa |
| `HandSituation` | 14 valores (None..LimpRaise) | `int` | Situación preflop |
| `BoardPosition` | None, Hand, Flop, Turn, River | `int` | Calle |
| `HeroHand` | 14 valores en español (Nada..EscaleraReal) | `int` | 🟡 Posible legacy |
| `GameSituation` | 11 valores con `[Description]` | `int` | 🟡 Paralelo a HandSituation |
| `PairClassification` | None=0..Overpair=6 | `byte` | Sub-clasificación TopPair |
| `HandResult` | Unknown, Won, Lost, Push | `int` | — |
| `OpponentType` | Unknown, TAG, LAG, TP, LP | `int` | — |
| `Styles` | Default, Agresive, Pasive | `int` | 🟡 Posible legacy (typo "Agresive") |
| `BluffConditionType` | None, Always, OOPOnly, IPCoordinatedSmallOnly | `int` | Reemplaza strings antiguos |

### Excepciones

#### `StrategyProfileValidationException` — `Exceptions/StrategyProfileValidationException.cs`

| Campo | Tipo | Notas |
|-------|------|-------|
| `Errors` | `IReadOnlyList<string>` | Lista de todos los errores |
| `Message` | `string` (BCL) | Construido automáticamente con `BuildMessage()` |

`BuildMessage()` formatea: `"StrategyProfile inválido (N errores):\n  - error1\n  - error2"`.

### Clases utilitarias

#### `ListRegions` — `Enums/ListRegions.cs`

Clase static con `List<string> Regions` — 41 nombres de regiones de captura OCR (`p1bet`, `b0card1`, `tablename`, etc.). 🟡 **INFERIDO**: ubicada en `Enums/` pero no es enum. Posible legacy hardcoded — el sistema actual usa `RegionTableMap` desde Marten + `RegionLookupCache` (visto en CLAUDE.md).

#### `ActionsResponse` — `Enums/ActionsResponse.cs`

Clase POCO (no enum). 4 campos: `Action: string`, `Hands: List<string>`, `Style: Styles`, `Position: Positions`. 🟡 Categoría incorrecta (Enums/).

#### `EnumExtensions` — `Enums/Positions.cs:117`

Método `GetDescription(this Enum)` que extrae `[DescriptionAttribute]` por reflection. Usado por `GameSituation`.

---

## Módulo: `OpenScrape.Infrastructure`

> Este módulo no define entidades propias — solo configura la persistencia Marten para los documentos de `OpenScrape.Domain`. Las entidades persistidas ya están descritas en la sección anterior.

### Documentos persistidos vía Marten

| Documento | Definido en | Schema.For explícito | Índices declarados |
|-----------|-------------|:---:|---------------------|
| `GameSession` | `Domain/Entities/GameSession.cs` | ✅ | `EndTime`, `SessionId`, `TableName` |
| `HandRecord` | `Domain/Entities/GameSession.cs:56` | ✅ | `GameSessionId`, `Timestamp`, `(GameSessionId, Timestamp)` compuesto, `HeroPosition` |
| `Card` | `Domain/Entities/Card.cs` | ❌ (autodescubierto) | solo PK (`Id`) |
| `RegionTableMap` | `Domain/Entities/RegionTableMap.cs` | ❌ (autodescubierto) | solo PK (`Id`) |
| `Table` | `Domain/Entities/Table.cs` | ❌ (autodescubierto) | solo PK (`Id`) |

### Configuración Marten — Tabla resumen

| Aspecto | Valor | Origen |
|---------|-------|--------|
| Connection string key | `ConnectionStrings:DefaultConnection` | `Services.cs:16` |
| Proveedor | PostgreSQL (Neon, eu-west-2) | `appsettings.json:3` |
| Pooler | sí (`-pooler` en host) | connection string |
| TLS | `SSL Mode=VerifyFull;Channel Binding=Require` | connection string |
| Serializador | System.Text.Json | `Services.cs:19` `UseSystemTextJsonForSerialization()` |
| Esquema auto-creado | `AutoCreate.All` si `IsDevelopment=true` | `Services.cs:36-39` |
| Tipo de sesión preferido | `LightweightSession` (sin tracking) | uso en App/Features (`await using`) |
| Tipo para queries puras | `QuerySession` (read-only) | uso en GameLogger/BankrollTracker |

### Índices PostgreSQL generados

🟡 **INFERIDO** — Marten 8.x crea índices GIN sobre el JSONB `data` con paths concretos. Los siete índices declarados producen estructuras equivalentes a:

```
CREATE INDEX mt_doc_gamesession_idx_endtime
  ON mt_doc_gamesession ((data ->> 'EndTime'));
CREATE INDEX mt_doc_gamesession_idx_sessionid
  ON mt_doc_gamesession ((data ->> 'SessionId'));
CREATE INDEX mt_doc_gamesession_idx_tablename
  ON mt_doc_gamesession ((data ->> 'TableName'));

CREATE INDEX mt_doc_handrecord_idx_gamesessionid
  ON mt_doc_handrecord ((data ->> 'GameSessionId'));
CREATE INDEX mt_doc_handrecord_idx_timestamp
  ON mt_doc_handrecord ((data ->> 'Timestamp'));
CREATE INDEX mt_doc_handrecord_idx_gamesessionid_timestamp
  ON mt_doc_handrecord ((data ->> 'GameSessionId'), (data ->> 'Timestamp'));
CREATE INDEX mt_doc_handrecord_idx_heroposition
  ON mt_doc_handrecord ((data ->> 'HeroPosition'));
```

(Nombres exactos dependen de Marten; los paths sí son fijos.)

---

## Módulo: `OpenScrape.Features`

> Este módulo no define entidades persistibles propias — orquesta operaciones sobre los documentos Marten declarados en `OpenScrape.Domain` (`Table`, `Card`, `RegionTableMap`, `GameSession`).
> Define **DTOs de request** (entrada) y **records aggregator** (composite use cases). El retorno se canaliza vía `Ardalis.Result<T>` excepto en dos use cases (ver tabla en code-analysis.md §5).

### Requests

#### `ActionScenarioRequest` — `ActionScenario/ActionScenarioRequest.cs` (class, mutable)

| Campo | Tipo | Obligatorio | Default | Notas |
|-------|------|:-----------:|---------|-------|
| `HandName` | `string?` | ❌ | `null` | Notación canónica (ej `"AKs"`, `"QQ"`, `"JTo"`) |
| `Suited` | `bool?` | ❌ | `null` | Match exacto vs `Hand.Suited` |
| `HeroPosition` | `TablePosition?` | ❌ | `null` | Compara `GetDescription()` con `PlayerActionSequence.HeroPosition` |
| `OpenRaiser` | `TablePosition?` | ❌ | `null` | Skipped si `null` |
| `ThreeBetPosition` | `TablePosition?` | ❌ | `null` | Skipped si `null` |
| `Limper` | `TablePosition?` | ❌ | `null` | Skipped si `null` |
| `Caller` | `TablePosition?` | ❌ | `null` | Skipped si `null` |
| `Squeezer` | `TablePosition?` | ❌ | `null` | Skipped si `null` |
| `BetSize` | `decimal?` | ❌ | `null` | 🔴 **Filtro comentado en `GetActionScenario.cs:31`** — el campo existe pero no participa en el match |
| `IsGreater` | `bool?` | ❌ | `null` | Bet size threshold encoded as boolean (mapeo posicional en `SetPreflopActionUseCase.SetIsGreater`) |
| `RaiserFolds` | `bool?` | ❌ | `null` | Skipped si `null` |

#### `UpdateRegionTableMapRequest` — `RegionsTableMap/Update/UpdateRegionTableMapRequest.cs` (record positional)

| Campo | Tipo | Obligatorio | Default | Notas |
|-------|------|:-----------:|---------|-------|
| `Category` | `string` | ✅ | — | PK del documento Marten `RegionTableMap.Id` |
| `Name` | `string` | ✅ | — | Identificador de región dentro del mapa |
| `PosX` | `int` | ✅ | — | Coordenada X de captura |
| `PosY` | `int` | ✅ | — | Coordenada Y de captura |
| `Width` | `int` | ✅ | — | Ancho de captura |
| `Height` | `int` | ✅ | — | Alto de captura |
| `Umbral` | `double` | ✅ | — | Umbral de detección activo |
| `InactiveUmbral` | `double` | ✅ | — | Umbral inactivo (jugador out) |
| `Color` | `string` | ✅ | — | Color esperado (hex) |
| `IsColor` | `bool?` | ❌ | `null` | 🟡 **Ignorado por el use case** — solo se preservan los del registro existente |
| `IsHash` | `bool?` | ❌ | `null` | 🟡 ignorado (idem) |
| `IsOnlyNumber` | `bool?` | ❌ | `null` | 🟡 ignorado (idem) |
| `IsBoard` | `bool?` | ❌ | `null` | 🟡 ignorado (idem) |

### Records aggregator (Composite UseCases)

| Record | Archivo | Composición |
|--------|---------|-------------|
| `ActionScenarioUseCases` | `ActionScenario/ActionScenarioUseCases.cs` | `(GetActionScenario)` |
| `TableUseCases` | `Table/TableUseCases.cs` | `(GetTable, GetAllTables)` ⚠ `GetAllTables` sin método Execute |
| `CardUseCases` | `Cards/CardUseCases.cs` | `(GetAllCards)` |
| `RegionTableMapUseCases` | `RegionsTableMap/RegionTableMapUseCases.cs` | `(GetAllRegionTableMap, UpdateRegionTableMap)` ⚠ `GetAllRegionTableMap` con cuerpo comentado |
| `GameRoundUseCases` | `GameRound/GameRoundUseCases.cs` | `(GetRecentGameRounds)` |

### Use cases — firmas y retornos

| Use case | Archivo | Firma | Retorno | Sesión Marten |
|----------|---------|-------|---------|---------------|
| `GetActionScenario.ExecuteAsync` | `ActionScenario/Get/GetActionScenario.cs:16` | `(GameSituation, ActionScenarioRequest)` | `Task<string>` (acción, default `"Fold"`) | indirecta vía `GetTable` |
| `GetTable.ExecuteAsync` | `Table/Get/GetTable.cs:17` | `(string name)` | `Task<Result<TableDTO?>>` | `QuerySession` |
| `GetAllTables.*` | `Table/GetAll/GetAllTables.cs` | — | — | 🔴 sin método Execute (clase vacía) |
| `GetAllCards.ExecuteAsync` | `Cards/GetAll/GetAllCards.cs:17` | `()` | `Task<Result<List<CardDTO>?>>` | `QuerySession` |
| `GetFlopCards.*` | `Cards/GetFlop/GetFlopCards.cs` | — | — | 🔴 dead code (record vacío) |
| `UpdateRegionTableMap.ExecuteAsync` | `RegionsTableMap/Update/UpdateRegionTableMap.cs:15` | `(UpdateRegionTableMapRequest, CancellationToken)` | `Task<Result>` | `LightweightSession` |
| `GetAllRegionTableMap.*` | `RegionsTableMap/GetAll/GetAllRegionTableMap.cs` | — | — | 🔴 método comentado |
| `GetRecentGameRounds.Execute` | `GameRound/GetRecentGameRounds.cs:15` | `(int count = 20)` | `Task<List<GameSession>>` | `QuerySession` (`await using`) |

### Constantes y valores literales

| Constante | Valor | Ubicación | Notas |
|-----------|-------|-----------|-------|
| Default count | `20` | `GetRecentGameRounds.cs:15` parámetro default | Ventana de "últimas N sesiones" |
| Random range | `1..100` | `GetActionScenario.cs:65` `Random.Next(1, 101)` | Asume `Sum(Percentage) == 100` |
| Default action | `"Fold"` | `GetActionScenario.cs:38, 42` | Sentinel "no hay regla aplicable" |

### Documentos Marten consumidos (no propios)

| Documento | Uso | Tipo de sesión | Vista |
|-----------|-----|----------------|-------|
| `Table` | leído por `GetTable`, `GetActionScenario` (vía cascada) | `QuerySession` | filtrado por `Id == situation.GetDescription()` |
| `Card` | leído por `GetAllCards` | `QuerySession` | listado completo |
| `RegionTableMap` | leído + escrito por `UpdateRegionTableMap` | `LightweightSession` | `LoadAsync(Category)` + `Store(region)` |
| `GameSession` | leído por `GetRecentGameRounds` | `QuerySession` | `OrderByDescending(EndTime).Take(count)` (usa índice declarado) |

---

## Módulo: `OpenScrape.DecisionMaker`

> Estructuras del motor de decisión y cálculo de equity. Records inmutables (input/output, contextos cross-street), classes mutables (resultados de cálculos numéricos), enums propios del módulo y constantes algorítmicas. **No declara entidades persistidas** — consume `OpenScrape.Domain.Entities` (`StrategyProfile`, `OpponentProfile`, `GameSession`, `HandRecord`).

### DTOs de decisión

#### `PostflopDecisionInput` (record, `DTOs/PostflopDecisionInput.cs:14`)

> Objeto-parámetro inmutable que sustituye los 36+ argumentos individuales de `DetermineAction()`.

| Campo | Tipo | Required | Default | Descripción |
|-------|------|----------|---------|-------------|
| `Equity` | `double` | ✅ | — | Equity raw del hero `[0, 100]` |
| `Street` | `BoardPosition` | ✅ | — | Flop / Turn / River |
| `Situation` | `HandSituation` | ✅ | — | OpenRaise / ThreeBet / Squeeze / DonkBet / etc. |
| `BoardTexture` | `string` | ✅ | — | "Dry" / "Coordinated" / "Wet" / "Paired" / "Monotone" |
| `IsInPosition` | `bool` | ✅ | — | Hero IP vs villano |
| `VillainBetSize` | `BetSizeCategory` | ✅ | — | NoBet / Underbet / Small / Medium / Large |
| `PotOdds` | `double` | — | 0 | Pot odds en porcentaje `[0, 100]` |
| `TotalOuts` | `int` | — | 0 | Outs totales sin descuento (clasificación) |
| `EffectiveOuts` | `double` | — | 0 | Outs con descuento por tainted (S22.1) — semi-bluff EV |
| `PreviousStreetBet` | `bool` | — | false | Hero apostó en street anterior (barrel context) |
| `VillainShowedAggression` | `bool` | — | false | Villain bet/raise en alguna street previa |
| `BoardChange` | `BoardChangeResult?` | — | null | Cambio detectado al caer turn/river |
| `HeroBlocksDangerSuit` | `bool` | — | false | Hero tiene carta del flush draw del villain |
| `HeroStack` | `decimal` | — | 0 | Stack del hero (para SPR) |
| `PotSize` | `decimal` | — | 0 | Tamaño del pot (para SPR + sizing) |
| `HasFlushDraw` | `bool` | — | false | Hero tiene flush draw activo |
| `NumOpponents` | `int` | — | 1 | Players activos en la mano |
| `HeroIsAggressor` | `bool` | — | false | Cross-street: agresor preflop O HeroBetFlop/Turn |
| `HeroHandRank` | `HandRank` | — | HighCard | Rank de la mejor mano del hero |
| `HasComboDraw` | `bool` | — | false | Flush + straight draw simultáneo |
| `VillainAggressorCheckedPreviousStreet` | `bool` | — | false | Probe bet trigger |
| `VillainBarreling` | `bool` | — | false | Bet en flop+turn (barrel real vs bet-check-bet) |
| `VillainType` | `OpponentType` | — | Unknown | LAG / LP / TAG / TP |
| `PairClassification` | `PairClassification` | — | None | Overpair / TopPair / MiddlePair / BottomPair / etc. |
| `FoldEquity` | `double` | — | 0 | FE estimado `[0, 100]` |
| `VillainBetSizeFlop` | `BetSizeCategory` | — | NoBet | Para detección de sizing escalation |
| `VillainBetSizeTurn` | `BetSizeCategory` | — | NoBet | Idem |
| `VillainCheckedMiddleStreet` | `bool` | — | false | bet-check-bet (debilidad vs barrel) |
| `HeroHasNutBlocker` | `bool` | — | false | Granular blocker reduction |
| `HeroFloatedFlop` | `bool` | — | false | Float exit trigger en turn |
| `VillainFoldToBetPct` | `double` | — | -1 | -1 = stats insuficientes (fallback a tipo estático) |
| `HeroKickerStrength` | `KickerStrength` | — | None | TPTK = Strong, TPMK = Medium, TPWK = Weak |
| `TurnCalledWithFlushDanger` | `bool` | — | false | Turn-river plan: river check si flush completa |
| `HeroBlocksTopCard` | `bool` | — | false | Card removal: villain tiene menos value combos |
| `HeroCheckedAllStreets` | `bool` | — | false | Delayed value river path |
| `IsAnyoneAllIn` | `bool` | — | false | Desactiva fold equity y reverse implied |
| `IsDonkBet` | `bool` | — | false | Donk bet exploitation path (S18.1) |
| `VillainProfile` | `OpponentProfile?` | — | null | Stats reales para overrides WSD/Barrel/Donk/CheckRaise/WTSD |
| `HeroPosition` | `TablePosition` | — | None | Para BvB y multiway OOP damping |
| `VillainPosition` | `TablePosition` | — | None | Para BvB |
| `IsBroadwayWet` | `bool` | — | false | S21.3: AKQ, KQJ — adjustments específicos |
| `RiverCardType` | `RiverCardType` | — | Neutral | S22.2: Blank / Neutral / Scare |

#### `PostflopDecisionResult` (record, `Services/PostflopDecisionService.cs:14`)

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `Action` | `string` | — | "Fold" / "Call" / "Raise 3x (Value)" / "Bet 1/2 (Bluff)" / "All-In (Value)" / "Check (Check-Raise)" / etc. |
| `Reason` | `string?` | null | Explicación del path tomado (consumido por backtest y logs) |
| `IsBluff` | `bool` | false | Flag descriptivo |
| `IsBarrel` | `bool` | false | Bet+ consecutivo en turn/river siendo agresor |
| `IsCheckRaise` | `bool` | false | Action contiene "(Check-Raise)" |
| `IsFloating` | `bool` | false | Float IP en flop |

#### `DecisionRequest` (sealed record, `DTOs/DecisionRequest.cs:14`)

> DTO del facade `IPokerCalculator → UnifiedPokerCalculator` (ubicado en `OpenScrape.App`). No consumido directamente por servicios de DecisionMaker.

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `HeroCards` | `IReadOnlyList<CardDataOuts>` | required | 2 cartas del hero |
| `CommunityCards` | `IReadOnlyList<CardDataOuts>` | required | 0/3/4/5 community cards |
| `Street` | `BoardPosition` | required | Flop / Turn / River |
| `Situation` | `HandSituation` | required | Situación preflop |
| `HeroStack` / `VillainStack` / `PotSize` / `BetToCall` | `decimal` | 0 | Cantidades |
| `IsInPosition` | `bool` | false | — |
| `NumOpponents` | `int` | 1 | — |
| `HeroPosition` / `VillainPosition` | `TablePosition` | None | — |
| `VillainId` | `string` | "Unknown" | Lookup en `IOpponentTracker` |
| `VillainBetSize` | `BetSizeCategory` | NoBet | — |
| `HeroIsAggressor` / `PreviousStreetBet` / ... | `bool` | false | 11 flags cross-street |
| `VillainBetSizeFlop` / `VillainBetSizeTurn` | `BetSizeCategory` | NoBet | — |
| `PreviousBoard` | `IReadOnlyList<CardDataOuts>?` | null | Para `AnalyzeBoardChange` |
| `HeroBlocksDangerSuit` / `HeroBlocksTopCard` / `HeroHasNutBlocker` / `IsBroadwayWet` | `bool` | false | Flags de mano/board |
| `VillainProfile` | `OpponentProfile?` | null | Override opcional (si null, facade lo resuelve) |
| `HandSituationTag` | `string?` | null | Pasado a `IPokerCalculator` |
| `MonteCarloIterations` | `int?` | null | Override de iteraciones MC |

#### `DecisionResult` (sealed record, `DTOs/DecisionResult.cs:10`)

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `RecommendedAction` | `string` | required | Acción legible |
| `EquityPercent` | `double` | required | `[0, 100]` |
| `Reason` | `string` | required | Path de decisión (no-null por contrato) |
| `BoardTexture` | `string?` | null | "Dry" / "Wet" / etc. |
| `BetSize` | `double?` | null | Porcentaje del pot, si aplica |
| `PotOddsPercent` | `double` | 0 | — |
| `ExpectedValue` | `double` | 0 | EV de la acción |
| `IsBluff` / `IsBarrel` / `IsCheckRaise` / `IsFloating` | `bool` | false | Flags |
| `CalculationDetail` | `object?` | null | Opaque (raw equity/outs/etc.) |

### Estado cross-street

#### `PostflopGameContext` (sealed record, `Services/PostflopGameContext.cs:12`)

> Único poseedor del estado durante una mano. Vive en `IPostflopContextHolder` (en `OpenScrape.App.Services`). Cada transición devuelve instancia nueva — never mutated.

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `VillainBetFlop` / `VillainBetTurn` / `HeroBetFlop` / `HeroBetTurn` | `bool` | false | Quién apostó qué calle |
| `PreviousStreetWasBet` | `bool` | false | Hero apostó en street anterior |
| `VillainBetSizeFlop` / `VillainBetSizeTurn` | `BetSizeCategory` | NoBet | Para sizing escalation |
| `VillainAggressorCheckedFlop` | `bool` | false | Agresor preflop checkeó flop → probe |
| `VillainCheckedMiddleStreet` | `bool` | false | bet-check-bet pattern → debilidad |
| `HeroFloatedFlop` | `bool` | false | Call con aire + posición → bet turn |
| `TurnCalledWithFlushDanger` | `bool` | false | River check si flush completa |
| `HeroCheckedAllStreets` | `bool` (computed) | — | `!HeroBetFlop && !HeroBetTurn` |
| `IsAnyoneAllIn` | `bool` | false | Desactiva FE y reverse implied |
| `TurnBetCommitsToRiver` | `bool` | false | S22.4: turn bet → projected SPR < threshold |
| `InitialBoardDanger` | `BoardChangeResult` | Safe | Estado base de peligro del flop |
| `LastBoardChange` | `BoardChangeResult` | Safe | Último cambio detectado |
| `IsVillainBarreling` | `bool` (computed) | — | `VillainBetFlop && VillainBetTurn` |
| `HeroStackPreRebuy` | `decimal` | 0 | 0 = aún no registrado; sentinel para detectar auto-rebuy |

> **Constantes privadas:** `AutoRebuyThreshold = 50m` (umbral para considerar aumento de stack como rebuy de la sala).

### Resultados de algoritmos

#### `BoardTextureResult` (record, `Algorithms/BoardTextureAnalyzer.cs:13`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Category` | `BoardTextureCategory` | Dry / SemiDry / SemiWet / Wet / Paired |
| `WetnessScore` | `double` | `[0, 100]` |
| `IsMonotone` / `IsTwoTone` / `IsRainbow` | `bool` | Distribución de palos |
| `IsPaired` | `bool` | 2+ cartas mismo rank |
| `IsConnected` | `bool` | 3+ cartas con gap ≤ 2 entre consecutivas |
| `IsBroadwayHeavy` | `bool` | 2+ (flop) o 3+ (turn/river) cartas ≥ 10 |
| `IsLowBoard` | `bool` | Todas < 9 |
| `HasFlushPossibility` | `bool` | 3+ del mismo palo |
| `HasStraightPossibility` | `bool` | 3+ cartas en ventana de 5 |
| `SimplifiedTexture` | `string` (computed) | "Monotone" / "Paired" / "Wet" / "Coordinated" / "Dry" |

#### `BoardChangeResult` (record, `Algorithms/BoardTextureAnalyzer.cs:44`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `FlushCompleted` | `bool` | 4+ same suit en board (villano necesita 1 carta) |
| `FlushDrawAppeared` | `bool` | 3 same suit nuevo (villano necesita 2) |
| `StraightCompleted` | `bool` | 4+ consecutive con prevHadDraw |
| `BoardPaired` | `bool` | Pareja nueva en board |
| `OvercardAppeared` | `bool` | Carta más alta que todas las previas |
| `CompletedFlushSuit` | `int` | -1 = none, sino el suit codificado |
| `DangerLevel` | `int` | `[0, 10]`: flushCompleted+4, flushDrawAppeared+2, straightCompleted+3, boardPaired+2, overcardAppeared+1 |

> `BoardChangeResult.Safe` (static) = `(false, false, false, false, false, -1, 0)`.

#### `MonteCarloSimulator.EquityResult` (class, `Algorithms/MonteCarloSimulator.cs:35`)

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `WinProbability` | `double` | — | `[0, 1]` |
| `TieProbability` | `double` | — | `[0, 1]` |
| `LoseProbability` | `double` | — | `[0, 1]` |
| `Equity` | `double` | — | `Win + 0.5 × Tie` |
| `Simulations` | `int` | — | Iteraciones efectivas (sin skipped) |
| `SkippedSimulations` | `int` | 0 | TryDrawFromRange falló tras 20 intentos |
| `IsReliable` | `bool` | true | false si BlockedComboPercentage > 20% |
| `BlockedComboPercentage` | `double` | — | % del rango villano bloqueado por hero/board |
| `HandDistribution` | `Dictionary<HandRank, int>` | empty | Frecuencia de cada rank en las simulaciones |

> Constantes: `UnreliableThreshold = 0.20` (20%).

#### `OutsCalculator.OutsResult` (class, `Algorithms/OutsCalculator.cs:27`)

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `TotalOuts` | `int` | 0 | Suma con inclusión-exclusión + overcards + backdoor |
| `TaintedOuts` | `int` | 0 | Outs que también ayudan al villano |
| `CleanOuts` | `int` | 0 | TotalOuts − TaintedOuts |
| `EffectiveOuts` | `double` | 0 | `cleanOuts + taintedOuts × discount` (0.7 o 0.3) |
| `HasFlushDraw` | `bool` | false | 9 outs de flush |
| `HasOpenEndedStraightDraw` | `bool` | false | 2+ ranks completan escalera |
| `HasGutshotStraightDraw` | `bool` | false | 1 rank completa escalera |
| `HasStraightFlushDraw` | `bool` | false | overlap entre flush y straight outs |
| `HasOvercards` | `bool` | false | Hero tiene cartas más altas que el board |
| `OvercardCount` | `int` | 0 | Número de overcards |
| `HasBackdoorFlushDraw` | `bool` | false | 3 cartas mismo palo en flop, hero contribuye |
| `HasBackdoorStraightDraw` | `bool` | false | 3 cartas en ventana de 5 en flop, hero contribuye |
| `HasComboDraw` | `bool` | false | flush draw + (OESD || gutshot) |
| `OutsToEquity` | `double` | 0 | `outs × cardsToCome × 2.0` (regla del 2 y 4) |
| `DrawTypes` | `List<string>` | empty | Etiquetas legibles ("Flush Draw", "Combo Draw", etc.) |

#### `EquityCalculatorService.FullEquityAnalysis` (class, `Services/EquityCalculatorService.cs:30`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `OverallEquity` | `double` | `[0, 1]` (preflop usa lookup, postflop MC) |
| `WinProbability` | `double` | `[0, 1]` |
| `TieProbability` | `double` | `[0, 1]` |
| `Outs` | `int` | Solo postflop |
| `OutsToEquity` | `double` | Solo postflop |
| `DrawTypes` | `List<string>` | Solo postflop |
| `HandDistribution` | `Dictionary<HandRank, int>` | Solo postflop |
| `RecommendedAction` | `string` | "RAISE/BET" / "CALL" / "CALL (Pot Odds)" / "SEMI-BLUFF" / "FOLD" |
| `PotOdds` | `double` | `callAmount / (potSize + callAmount)` |
| `ExpectedValue` | `double` | `equity × potSize − (1 − equity) × callAmount` |

> ⚠️ `RecommendedAction` usa thresholds hardcoded (0.6/0.3/0.25, 8 outs) — divergente con `PostflopDecisionService` que sí lee `StrategyProfile`.

#### `BetSizingOption` (record, `Services/BetSizingService.cs:20`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Size` | `double` | Fracción del pot `[0.10, 1.00]` |
| `Label` | `string` | "Overbet" / "Pot" / "3/4 Pot" / "2/3 Pot" / "1/2 Pot" / "1/3 Pot" |
| `Type` | `BetSizingType` | Value / ThinValue / Bluff / Overbet |

#### `HandScore` (readonly struct, `Algorithms/IHandEvaluator.cs:26`)

> Struct ligero zero-alloc para Monte Carlo. Implementa `IComparable<HandScore>`.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `CompositeScore` | `long` | Bits `[60-56]=HandRank`, `[48-36]=Kicker1`, ..., `[11-0]=Kicker4+5` — comparación O(1) vía `long.CompareTo` |
| `Rank` | `HandRank` | Para clasificación en histograma |

> `HandScore.BuildComposite(rankValue, k1=0, k2=0, k3=0, k4=0, k5=0)` construye el long.

### Telemetría GTO

#### `DecisionAnalysis` (class, `Services/ExploitabilityCalculator.cs:19`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `OurDecision` | `string` | Acción tomada |
| `OurDecisionEV` | `double` | EV calculado de la acción |
| `BestResponse` | `string` | Mejor respuesta GTO simplificada |
| `BestResponseEV` | `double` | EV de la mejor respuesta |
| `ExploitabilityMbb` | `double` | `(bestEV − ourEV) × 100 / BigBlind=1.0` |
| `IsExploitable` | `bool` | `> ExploitabilityThreshold=10 mbb` |
| `LeakCategory` | `LeakCategory` | None / OverBluffing / OverCalling / UnderBluffing / UnderValue / PotOddsError |
| `Recommendation` | `string` | Texto explicativo |

#### `DecisionRecord` (class, `Services/ExploitabilityCalculator.cs:31`)

> Registro persistido en `ConcurrentQueue<DecisionRecord>` (max 10K, FIFO).

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | `Guid` | `Guid.NewGuid()` |
| `Timestamp` | `DateTime` | `DateTime.UtcNow` |
| `OurDecision` | `string` | — |
| `Equity` / `PotOdds` / `FoldEquity` | `double` | — |
| `Street` / `Situation` | enum | — |
| `IsInPosition` | `bool` | — |
| `BoardTexture` | `string` | — |
| `PotSize` | `decimal` | — |
| `VillainBetSize` | `BetSizeCategory` | — |
| `PreFlopAction` | `HandSituation?` | — |
| `OurDecisionEV` / `BestResponseEV` / `ExploitabilityMbb` | `double` | — |

#### `SessionAnalysis` (class, `Services/ExploitabilityCalculator.cs:51`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TotalDecisions` | `int` | Records analizados |
| `AverageExploitabilityMbb` / `MaxExploitabilityMbb` / `MinExploitabilityMbb` | `double` | Estadísticas |
| `DecisionsByStreet` / `DecisionsByPosition` / `DecisionsByTexture` | `Dictionary<string, int>` | Conteos |
| `ExploitabilityByStreet` / `ExploitabilityByPosition` / `ExploitabilityByTexture` | `Dictionary<string, double>` | Promedios |
| `TopLeaks` | `List<LeakInfo>` | Top 5 |
| `ExploitableDecisionCount` / `ExploitablePercentage` | `int` / `double` | — |

#### `LeakInfo` (class, `Services/ExploitabilityCalculator.cs:68`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Category` | `LeakCategory` | — |
| `Frequency` | `int` | Número de decisiones del leak |
| `AverageExploitabilityMbb` / `TotalExploitabilityMbb` | `double` | — |
| `RecommendedAdjustment` | `string` | Texto explicativo |
| `SampleSpots` | `List<string>` | Hasta 3 ejemplos: `"Street Situation Decision"` |

#### `GTODistance` (class, `Services/ExploitabilityCalculator.cs:78`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `DistanceMbb` | `double` | `AverageExploitabilityMbb` |
| `Status` | `string` | "GTO" (<10 mbb) / "Near-GTO" (<50) / "Exploitable" / "NoData" |
| `Recommendations` | `List<string>` | Hasta 3 textos |
| `TotalDecisionsAnalyzed` | `int` | — |
| `ExploitablePercentage` | `double` | `[0, 100]` |

### Auto-calibración

#### `CalibrationResult` (class, `Services/AutoCalibrationService.cs:7`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Success` | `bool` | False si no hay datos suficientes o exploitability ya está bajo threshold |
| `DecisionsAnalyzed` | `int` | — |
| `Adjustments` | `List<ParameterAdjustment>` | Ajustes propuestos |
| `PreviousExploitability` / `EstimatedNewExploitability` | `double` | mbb |
| `Timestamp` | `DateTime` | `DateTime.UtcNow` |
| `Message` | `string` | Texto explicativo |

#### `ParameterAdjustment` (class, `Services/AutoCalibrationService.cs:18`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `ParameterName` | `string` | "FoldBelow" / "ThinValueAbove" |
| `OldValue` | `double` | ⚠️ **hardcoded** 45 o 40 — bug latente, no consulta `StrategyProfile` actual |
| `NewValue` | `double` | OldValue ± delta (capped a `MaxAdjustmentPerCycle=5.0`) |
| `Reason` | `string` | Descripción del leak detectado |
| `CausedBy` | `LeakCategory` | — |

### Backtest

#### `BacktestResult` (class, `Services/StrategyBacktester.cs:181`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TotalHands` | `int` | Manos analizadas |
| `TotalDecisions` | `int` | Decisiones evaluadas |
| `ChangedDecisions` | `int` | Divergencias entre original y replay |
| `BigBlind` | `decimal` | Default 0.50m |
| `ChangeRate` | `double` (computed) | `ChangedDecisions / TotalDecisions × 100` |
| `EstimatedBBImpact` | `double` | Suma de impactos por divergencia |
| `EstimatedBBPer100Impact` | `double` | `EstimatedBBImpact / TotalHands × 100` |
| `ChangesByType` | `Dictionary<string, int>` | "Fold → Call": 15, etc. |
| `ChangesByStreet` | `Dictionary<BoardPosition, int>` | — |
| `Divergences` | `List<DecisionDivergence>` | Detalle |

#### `DecisionDivergence` (class, `Services/StrategyBacktester.cs:231`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `HandNumber` | `long` | — |
| `Street` | `BoardPosition` | — |
| `Equity` | `double` | — |
| `SPR` | `double` | — |
| `Situation` | `HandSituation` | — |
| `IsInPosition` | `bool` | — |
| `OriginalAction` / `NewAction` | `string` | — |
| `NewReason` | `string` | — |
| `BoardTexture` | `string` | — |

### Análisis estadístico

#### `StrategyAnalysisResult` (class, `Services/StrategyAnalyzerService.cs:13`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TotalHands` / `HandsWon` / `HandsLost` / `HandsPush` / `HandsUnknown` | `int` | Conteos |
| `WinRate` | `double` (computed) | `HandsWon / TotalHands × 100` |
| `TotalProfit` / `BiggestWin` / `BiggestLoss` | `decimal` | — |
| `BBPer100` | `double` | `(profit/BB) / handCount × 100` |
| `StatsByPosition` | `Dictionary<TablePosition, PositionStats>` | — |
| `StatsByStreet` | `Dictionary<BoardPosition, StreetStats>` | — |
| `StatsBySituation` | `Dictionary<HandSituation, SituationStats>` | — |
| `Sessions` | `List<SessionSummary>` | Ordenado por StartTime desc |
| `EquityAccuracy` | `double` | `100 − Σ|pred − actual| / nBuckets` por buckets de 10% |
| `EquityVsOutcomes` | `List<EquityVsOutcome>` | Pares (equity, won, profit, action) |

> Tipos satélite: `PositionStats` (Position, Hands, Won, Lost, Profit, WinRate, AvgProfitPerHand), `StreetStats` (Street, TotalDecisions, Bets/Calls/Raises/Folds/Checks, AvgEquity, AvgPotOdds), `SituationStats` (Situation, Hands, Won, Profit, WinRate), `SessionSummary` (SessionId, TableName, StartTime, EndTime, Hands, Profit, BBPer100), `EquityVsOutcome` (Equity, Won, Profit, Action).

### Enums propios del módulo

| Enum | Valores | Archivo | Notas |
|------|---------|---------|-------|
| `BetSizeCategory` | NoBet, Underbet, Small, Medium, Large | `Services/PostflopDecisionService.cs:12` | Underbet < 15% pot, Small ≤ 30%, Medium ≤ 70%, Large > 70% |
| `BoardTextureCategory` | Dry, SemiDry, SemiWet, Wet, Paired | `Algorithms/BoardTextureAnalyzer.cs:5` | Umbrales en `PokerConstants.Wetness*Max` |
| `RiverCardType` | Blank, Neutral, Scare | `Algorithms/BoardTextureAnalyzer.cs:11` | S22.2: ajusta bet sizing y bluff catch en river |
| `BetSizingType` | Value, ThinValue, Bluff, Overbet | `Services/BetSizingService.cs:12` | Usado en `BetSizingOption` |
| `RangeType` | Linear, Polarized, Condensed | `Services/RangePolarizer.cs:7` | Selección por textura/posición/SPR |
| `PostflopAction` | Bet, Raise, Call, Fold | `Services/OpponentTracker.cs:268` | Para `RecordPostflopAction` |
| `LeakCategory` | None, OverBluffing, OverCalling, UnderBluffing, UnderValue, PotOddsError | `Services/ExploitabilityCalculator.cs:9` | Categorización de errores GTO |

### Constantes algorítmicas (`PokerConstants`)

| Categoría | Constante | Valor | Notas |
|-----------|-----------|-------|-------|
| HandEvaluator | `HighCardMultiplier` | 1 | Score base |
| HandEvaluator | `PairMultiplier` | 1_000_000 | Escalonado ×10 hasta StraightFlush |
| HandEvaluator | `TwoPairMultiplier` | 10_000_000 | — |
| HandEvaluator | `ThreeOfAKindMultiplier` | 100_000_000 | — |
| HandEvaluator | `StraightMultiplier` | 1_000_000_000 | — |
| HandEvaluator | `FlushMultiplier` | 10_000_000_000 | — |
| HandEvaluator | `FullHouseMultiplier` | 100_000_000_000 | — |
| HandEvaluator | `FourOfAKindMultiplier` | 1_000_000_000_000 | — |
| HandEvaluator | `StraightFlushMultiplier` | 10_000_000_000_000 | — |
| OutsCalculator | `FlushDrawOuts` | 9 | — |
| OutsCalculator | `BackdoorFlushImpliedOuts` | 1.5 | — |
| OutsCalculator | `BackdoorStraightImpliedOuts` | 1.0 | — |
| OutsCalculator | `OvercardOutsPerCard` | 3 | — |
| MonteCarlo | `DeckSize` | 52 | — |
| MonteCarlo | `DefaultMonteCarloIterations` | 10_000 | Override por street en `GetAdaptiveIterations` |
| Reglas 2/4 | `TurnOutsMultiplier` | 2.17 | Equity = outs × 2.17 |
| Reglas 2/4 | `RiverOutsMultiplier` | 4.35 | Equity = outs × 4.35 |
| Decisión | `MinOutsForDraw` | 8 | Threshold para semi-bluff/draw call |
| Decisión | `MarginalPotOddsFactor` | 0.80 | Multiplier para call con pot odds marginales |
| Decisión | `MaxOpponentsForBluff` | 2 | Bluff multiway con ≤2 opp |
| Facing bet | `FacingBetPenaltyLarge` | 8.0 | — |
| Facing bet | `FacingBetPenaltyMedium` | 4.0 | — |
| Facing bet | `FacingBetPenaltySmall` | 1.0 | — |
| Facing bet | `FacingBetPenaltyUnderbet` | 0.0 | Underbet = no respeto adicional |
| Facing bet | `VillainAggressionPenalty` | 3.0 | — |
| Street multiplier | `FacingBetTurnMultiplier` | 1.15 | — |
| Street multiplier | `FacingBetRiverMultiplier` | 1.30 | — |
| Multiway | `MultiwayFoldBelowIP` | 2.0 | Lineal |
| Multiway | `MultiwayFoldBelowOOP` | 6.0 | Cuadrático: extra² × 6.0 × damping |
| Multiway | `MultiwayThinValueIP` | 2.0 | — |
| Multiway | `MultiwayThinValueOOP` | 4.0 | — |
| Agresor vs caller | `AggressorVsDonkFoldReduction` | 5.0 | — |
| Agresor vs caller | `AggressorVsDonkThinValueReduction` | 3.0 | — |
| Agresor vs caller | `CallerVsCbetFoldIncrease` | 2.0 | — |
| Bluff catch | `BluffCatchLAGMultiplier` | 0.80 | < 1.0 = call más amplio |
| Bluff catch | `BluffCatchLPMultiplier` | 0.85 | — |
| Bluff catch | `BluffCatchTAGMultiplier` | 1.00 | — |
| Bluff catch | `BluffCatchTPMultiplier` | 1.20 | > 1.0 = call más estrecho |
| Bluff catch runout | `BluffCatchBrickRunoutMultiplier` | 0.85 | — |
| Bluff catch runout | `BluffCatchScareRunoutMultiplier` | 1.15 | — |
| Pot commitment | `PotCommitmentSPRThreshold` | 0.5 | — |
| Range narrowing | `RangeNarrowingPerStreet` | 3.0 | — |
| Randomización | `RandomizationMargin` | 3.0 | ±3% equity → mixing zone |
| Randomización | `RandomizationBetFrequency` | 0.70 | Default si villainType = Unknown |
| Pot control | `PotControlMinEquity` | 40.0 | — |
| Pot control | `PotControlMaxEquity` | 55.0 | — |
| Wetness | `WetnessMonotoneScore` | 35.0 | — |
| Wetness | `WetnessTwoToneScore` | 15.0 | — |
| Wetness | `WetnessConnectedScore` | 20.0 | — |
| Wetness | `WetnessConnectedPerCount` | 8.0 | — |
| Wetness | `WetnessFlushPossibilityScore` | 15.0 | — |
| Wetness | `WetnessStraightPossibilityScore` | 15.0 | — |
| Wetness | `WetnessBroadwayScore` | 10.0 | — |
| Wetness | `WetnessPairedReduction` | -10.0 | — |
| Wetness | `WetnessTripsReduction` | -15.0 | — |
| Wetness | `WetnessExtraCardsBonus` | 5.0 | — |
| Wetness umbrales | `WetnessDryMax` | 15.0 | < → Dry |
| Wetness umbrales | `WetnessSemiDryMax` | 35.0 | < → SemiDry |
| Wetness umbrales | `WetnessSemiWetMax` | 60.0 | < → SemiWet, ≥ → Wet |
| Kicker | `StrongKickerMinRank` | 13 | K+ |
| Kicker | `MediumKickerMinRank` | 10 | T+ |
| Kicker | `DrawMinOuts` | 8 | — |
| Kicker | `DrawEquityThreshold` | 15.0 | — |

### Constantes privadas (servicios)

| Servicio | Constante | Valor | Notas |
|----------|-----------|-------|-------|
| `MonteCarloSimulator` | `UnreliableThreshold` | 0.20 | 20% combos bloqueados |
| `MonteCarloSimulator` | `HandRankCount` | 11 | HandRank 1-10, índice 0 no usado |
| `BitHandEvaluator` | `WheelMask` | `(1<<14)|(1<<2)|(1<<3)|(1<<4)|(1<<5)` | A-2-3-4-5 |
| `BetSizingService` | `FlopStreetFactor` | 0.90 | — |
| `BetSizingService` | `TurnStreetFactor` | 1.0 | — |
| `BetSizingService` | `RiverStreetFactor` | 1.10 | — |
| `RangePolarizer` | `SPRCondensedThreshold` | 3.0 | < → Condensed |
| `RangePolarizer` | `DryTextureThreshold` | 25.0 | wetness < → Polarized IP / Linear OOP |
| `RangePolarizer` | `WetTextureThreshold` | 50.0 | wetness > → Linear |
| `BankrollTrackerService` | `MaxSessionsForStats` | 100 | — |
| `BankrollTrackerService` | (private) | `_lastBigBlind = 0.02m` | Default si no hay sesiones |
| `ExploitabilityCalculator` | `ExploitabilityThreshold` | 10.0 | mbb |
| `ExploitabilityCalculator` | `NearGTOThreshold` | 50.0 | mbb |
| `ExploitabilityCalculator` | `MinFoldEquityForBluff` | 33.0 | % |
| `ExploitabilityCalculator` | `MaxEquityForBluff` | 35.0 | % |
| `ExploitabilityCalculator` | `BigBlind` | 1.0 | ⚠️ hardcoded constant — asume normalización a 1 BB |
| `ExploitabilityCalculator` | `MaxRecords` | 10000 | FIFO queue |
| `AutoCalibrationService` | `MinDecisionsForCalibration` | 20 | — |
| `AutoCalibrationService` | `RecalibrateThreshold` | 50 | Decisiones desde última calibración |
| `AutoCalibrationService` | `ExploitabilityCalibrationThreshold` | 15.0 | mbb |
| `AutoCalibrationService` | `MaxAdjustmentPerCycle` | 5.0 | Cap del delta |
| `AutoCalibrationService` | `CalibrationMinImprovement` | 2.0 | mbb |

### Tipos consumidos del Domain (no propios)

| Tipo | Origen | Uso |
|------|--------|-----|
| `StrategyProfile` | `Domain.Entities` | `IOptions<StrategyProfile>` inyectado en casi todos los servicios |
| `OpponentProfile` | `Domain.Entities` | `OpponentTracker.GetProfile`, `PostflopDecisionInput.VillainProfile` |
| `OpponentPositionProfile` | `Domain.Entities` | `OpponentTracker.RecordHandPlayed/VPIP/PFR` |
| `GameSession` / `HandRecord` | `Domain.Entities` | `BankrollTrackerService`, `StrategyAnalyzerService`, `StrategyBacktester` |
| `ThresholdKey` / `StreetThresholds` | `Domain.ValueObjects` | `ThresholdsRegistry` parser |
| `StreetDecision` | `Domain.ValueObjects` | `StrategyBacktester` lee de `HandRecord.Decisions` |
| `CardDataOuts` | `Domain.ValueObjects` | Cartas en algoritmos (MC, OutsCalculator, BoardTextureAnalyzer, PreflopEquityCalculator) |
| `VillainRange` | `Domain.ValueObjects` | `MonteCarloSimulator.BuildVillainCombos` y `CalculateBlockedComboPercentage` |
| `HandEvaluation` | `Domain.ValueObjects` | Output de `BitHandEvaluator.EvaluateBestHand` |
| `BankrollSnapshot` / `BankrollStats` / `BankrollHistoryItem` | `Domain.ValueObjects` | Output de `BankrollTrackerService` |
| `BoardPosition` / `HandSituation` / `TablePosition` / `HandRank` / `KickerStrength` / `PairClassification` / `OpponentType` / `BluffConditionType` / `Rank` / `Suit` / `HandResult` | `Domain.Enums` | Tipificación |

---

## Módulo: `OpenScrape.App` 🟢

> Application & UI layer: composition root, WinForms, OCR, game loop, telemetría.

### Configuration/

| Tipo | Campo | Valor / Tipo | Notas |
|------|-------|--------------|-------|
| `FeatureFlags` | `SectionName` | `"Features"` | Const sección IOptions |
| `FeatureFlags` | `UseGameLoopCoordinator` | `bool = false` | Cutover Fase 6 OFF en producción |
| `GameLoopOptions` | `SectionName` | `"GameLoop"` | Const sección IOptions |
| `GameLoopOptions` | `CaptureIntervalMs` | `int = 100` | Intervalo entre ticks del PeriodicTimer |
| `GameLoopOptions` | `StopTimeoutMs` | `int = 2000` | Timeout WaitForStop |

### Entities/

| Tipo | Campo | Tipo | Default | Notas |
|------|-------|------|---------|-------|
| `PlayerGameState` | `IsDealer` | bool | false | Hero es dealer |
| `PlayerGameState` | `PotSize` | decimal | 0 | Pot detectado por OCR |
| `PlayerGameState` | `HoleCard1Face / HoleCard2Face` | string | "" | Ej "As", "Kh" |
| `PlayerGameState` | `HoleCard1Rank / HoleCard2Rank` | int | 0 | 2-14 |
| `PlayerGameState` | `HoleCard1Suit / HoleCard2Suit` | int | 0 | 1-4 |
| `PlayerGameState` | `Kicker` | int | 0 | |
| `PlayerGameState` | `IsInPosition` | bool | false | |
| `PlayerGameState` | `Position` | TablePosition | None | |
| `PlayerGameState` | `CurrentBet` | decimal | 0 | |
| `PlayerGameState` | `HeroStack` | decimal | 0 | |
| `PlayerGameState` | `HandSituation` | HandSituation | — | |
| `PlayerGameState` | `Players` | List\<Player\> | [P0] | Inicializado con hero |
| `PlayerGameState` | `BoardCards` | List\<BoardData\> | [] | |
| `PlayerGameState` | `HavePocketPair` | bool computed | — | `HoleCard1Rank == HoleCard2Rank` |
| `PlayerGameState` | `IsSuited` | bool computed | — | `HoleCard1Suit == HoleCard2Suit` |
| `Player` | `Name` | string? | — | "P0".."P5" |
| `Player` | `Alias` | string? | — | OCR del nombre del villano |
| `Player` | `Dealer / Active / SitOut / Empty / HasFolded / BigBlind / SmallBlind` | bool | false | Estados |
| `Player` | `Bet / Stack` | decimal | 0 | OCR |
| `Player` | `Position` | TablePosition | None | Asignado por PositionCalculator |
| `Player` | `ValuePosition` | int | — | 0-5 (P0=hero) |
| `Player` | `WasPreflopAggressor` | bool | false | Para cross-street DonkBet detection |
| `BoardData` | `Name / Force / Suit / Position / Location` | string? / int / int / BoardPosition / int | — | Carta del board (Force=rank, Location 1-5) |
| `ResponseAction` | `Action / HandSituation / IsSecondAction` | string? / HandSituation / bool | — | Acción recomendada |
| `TableScrapeFlopResult.BoardTexture` | `IsCoordinated/Rainbow/Connected/Paired/Dry/HasAce/HasKing` | bool | false | Análisis legacy del board |
| `TableScrapeFlopResult.BoardTexture` | `HighestRank / LowestRank` | int [2,14] | — | Rangos extremos |
| `TableScrapeFlopResult.HeroHandStrength` | `Hand` | HeroHand | — | Required |
| `TableScrapeFlopResult.HeroHandStrength` | `Has*` (10 booleans) | bool | false | TopPair/MiddlePair/BottomPair/OverPair/TwoPair/Set/FullHouse/3ofKind/OverCards/Connected |
| `TableScrapeFlopResult.HeroHandStrength` | `HasTopPairOrBetter` | bool computed | — | Two pair OR Hand >= DoblePareja |
| `TableScrapeFlopResult.DrawingOpportunities` | `HasFlushDraw/StraightDraw/BackdoorFlush/DrawingHand` | bool | false | |
| `TableScrapeFlopResult` | `HasStrongHand/HasWeakHand/ShouldContinue` | bool computed | — | Helpers |
| `TurnBoardTexture` enum | values | — | — | Dry / Coordinated / Paired |
| `RiverBoardTexture` enum | values | — | — | Dry / Coordinated / Paired |

### Services/

| Tipo | Campo | Tipo | Default | Notas |
|------|-------|------|---------|-------|
| `OcrResult` | `Text` | string? | — | |
| `OcrResult` | `Image` | Bitmap? IDisposable | — | Caller dispose |
| `OcrResult` | `Confidence` | float | -1 | -1 = cache hit; 0..1 = Tesseract GetMeanConfidence |
| `OcrResult` | `Attempts` | int | 1 | Número de intentos exitosos |
| `OcrResult` | `IsHighConfidence` | bool computed | — | Confidence < 0 OR Confidence ≥ 0.70f |
| `OcrService` | `MaxBitmapCacheSize` | const int | 200 | LRU bitmap cache |
| `OcrService` | `MaxOcrCacheSize` | const int | 500 | LRU text cache |
| `ImageCropperService` | `DefaultTolerance` | const int | 10 | Pixel similarity tolerance |
| `ImageCropperService` | `SimilarityThreshold` | const double | 90.0 | % mínimo para match positivo |
| `ImageCropperService` | `MaxImageCacheSize` | const int | 500 | LRU bytes cache |
| `ImageCropperService` | `DHashMaxDistance` | const int | 15 | Hamming threshold pre-filtro |
| `ImageCropperService._dHashCache` | LruCache | — | 600 | Cache hashes perceptuales |
| `GameLoggerService` | `MaxHandsInMemory` | const int | 20 | Truncate older hands (already persisted) |
| `GameLoggerService` | `CurrentBigBlind` | decimal computed | 0.50 | Fallback si no hay sesión |
| `GameLoggerService` | `_dbWriteLock` | SemaphoreSlim(1,1) | — | Persistencia thread-safe |
| `GameLoopStateMachine` | `MaxOcrRetries` | const int | 2 | Reintentos ProcessFlop/Turn/River |
| `GameLoopStateMachine.GameState` enum | values | — | — | WaitingForHand/HandDetected/PreflopAction/FlopDetected/FlopAction/TurnDetected/TurnAction/RiverDetected/RiverAction/HandComplete |
| `TableLayoutService` | `_colorEmpty` | List\<int\> | {14,15,53,59,74} | Canal Blue para empty |
| `TableLayoutService` | `_colorPlaying` | List\<int\> | {17} | Canal Blue para playing |
| `TableLayoutService` | `DealerValuePosition` | int | -1 | -1 = no detectado |
| `TableLayoutService` | `DealerPosition / PreviousDealerPlayerName` | string | "" | Tracking inter-mano |
| `RegionLookupCache` | `_regionsByMapAndName` | Dictionary\<string, Dictionary\<string, Region\>\> | {} | OrdinalIgnoreCase |
| `RegionLookupCache` | `_regionsByMap` | Dictionary\<string, List\<Region\>\> | {} | OrdinalIgnoreCase |
| `LruCache<TKey,TValue>` | `_capacity / _map / _lruList / _lock` | int / Dictionary / LinkedList / object | — | Thread-safe |
| `CardCacheService` | `_cards` | List\<CardDTO\>? | null | Lazy 52 cartas |
| `CardCacheService` | `_semaphore` | SemaphoreSlim(1,1) | — | Double-check lazy init |
| `ColorDetectionService` | `_pixelData` | byte[]? | null | LockBits cached buffer |
| `ColorDetectionService` | `_handle` | GCHandle | — | Pinned para Marshal.Copy |
| `DetectionStatistics` | `Date / TotalColorDetections / TurnDetections / Errors / ScreenshotsSaved` | mixed | — | |
| `DetectionStatistics` | `SuccessRate` | double computed | — | TurnDetections / TotalColorDetections * 100 |
| `DetectionResult` (record nested FrmMain) | `ActionColor / FlopColor / AverageActionB / SampleCount / IsActionColorInRange / ShouldCapture / ShouldCaptureFlop / IsFlopVisible` | Color/double/int/bool×4 | — | Output PerformEnhancedDetection |
| `GameLoopResult` | `Empty / Error / Street / RecommendedAction / DecisionResult / LogText` | bool / Exception? / BoardPosition? / string? / DecisionResult? / string? | — | Emit por GameLoopCoordinator |

### Telemetry/

| Tipo | Campo | Tipo | Default | Notas |
|------|-------|------|---------|-------|
| `MetricsSnapshot` | `CurrentHandId / LastHand / Session` | string? / IReadOnlyDictionary\<string, CategoryStats\>×2 | — | Sealed record |
| `Histogram` | `BucketCount` | const int | 30 | |
| `Histogram` | `BucketBoundsTicks` | static long[] | precomputado | bound[i] = 1e-5 × 10^(i × 0.2) seg |
| `Histogram` | `_buckets` | long[BucketCount] | — | Counts por bucket |
| `Histogram` | `_count` | long | 0 | Total samples |
| `Histogram` | `_max` | TimeSpan | 0 | Max observado |
| `MetricsCollector.CategoryState` | `Lock / LastHand / Session` | object / Histogram / Histogram | — | Sync per-categoría |
| `TelemetryCategories` const strings | values | — | — | CycleTotal, CaptureScreenshot, OcrCards, OcrBets, OcrStacks, OcrHandNumber, OcrPlayerNames, LayoutDealer, LayoutPositions, DecisionTotal, DecisionEquity, DecisionTexture, DecisionProfile, DecisionDecisionService, DecisionSizing, OverlayRender, PersistenceSaveHand |
| `TelemetryCategories.SessionOnly` | HashSet | — | { PersistenceSaveHand } | Excluida de TelemetryAggregate por mano |
| `ScopedMeasurement` (struct) | `_collector / _category / _startTicks / _sessionOnly` | IMetricsCollector? / string? / long / bool | — | Stopwatch.GetTimestamp |

### Aplication/UseCases/

| Tipo | Campo | Tipo | Default | Notas |
|------|-------|------|---------|-------|
| `PokerCalculationResult` | `PotOddsPercentage / EquityPercentage` | double | 0 | % |
| `PokerCalculationResult` | `ShouldCall / FoldEquity / EVWithFoldEquity / ExpectedValue` | bool / double | — | Heurística básica |
| `PokerCalculationResult` | `DrawTypes` | List\<string\> | [] | "Flush Draw", "Straight Draw", etc. |
| `PokerCalculationResult` | `TotalOuts / EffectiveOuts` | int / double | 0 | EffectiveOuts S22.1 tainted descontados |
| `PokerCalculationResult` | `Street` | string | "" | Pre-Flop/Flop/Turn/River/Unknown |
| `PokerCalculationResult` | `RecommendedAction` | string | "" | "Fold" / "Call" / "Bet Xx pot" |
| `PokerCalculationResult` | `SuggestedBetSize` | double? | null | Como % del pot |
| `PokerCalculationResult` | `HeroHandRank / PairType / HeroKickerStrength` | HandRank / PairClassification / KickerStrength | — | |
| `PokerCalculationResult` | `HasComboDraw` | bool | false | flush + straight draw |
| `PokerCalculationResult` | `BoardTexture / BoardWetnessScore` | BoardTextureCategory? / double | — | Postflop ≥ 3 cartas |
| `UnifiedPokerCalculator` | `_equityCache` | ConcurrentDictionary\<string, double\> | {} | Cache equity por (hand|comm|opps|sit) |
| `UnifiedPokerCalculator` | `EquityCacheMaxSize` | const int | 2048 | Clear cuando se supera |

### Helpers/

| Tipo | Campo | Tipo / Valor | Notas |
|------|-------|--------------|-------|
| `AppThemeHelper` | `PrimaryDark / PrimaryLight / Accent` | Color (45,52,67) / (99,110,131) / (0,123,191) | Paleta principal |
| `AppThemeHelper` | `Success / Warning / Danger` | Color (40,167,69) / (255,193,7) / (220,53,69) | Estados |
| `AppThemeHelper` | `BackgroundMain / BackgroundCard / BorderLight` | Color (248,249,250) / White / (222,226,230) | Fondos |
| `CaptureWindowsHelper.User32.RECT` | left/top/right/bottom | int | Win32 P-Invoke |
| `CaptureWindowsHelper.User32` | `PW_RENDERFULLCONTENT / MONITOR_DEFAULTTONEAREST` | const uint 0x02 / 0x02 | PrintWindow flags |
| `CaptureWindowsHelper.GDI32` | `SRCCOPY` | const int 0x00CC0020 | BitBlt dwRop |
| `EncrypterHelper` | IV | byte[16] zeros | **Anomalía**: IV fija, key = SHA256(secret) |
| `ColorHelper.GetRGBColorRequest` | `Image / X / Y / IsColor` | Bitmap / int / int / bool | Request para extraer RGB |
| `ColorHelper.GetRGBColorResponse` | `RColor / GColor / BColor` | string | Hex 2-char por canal |
| `CoordinateScaler` | `_referenceWidth / _referenceHeight / _isInitialized` | int / int / bool | Init one-shot |
| `Hands` (en ObtainActionHelper) | `Hand / Action / Porcentajes` | string / string / int | Random weighted selection |

### FrmMain — campos mutables relevantes

> Documentado en FrmMain.cs:4448-4500 con plan de migración a Fase 7.3.

| Campo | Tipo | Migración planeada |
|-------|------|---------------------|
| `_executeCapture` | volatile bool | GameLoopCoordinator (CancellationToken) |
| `_backgroundExecute` | volatile bool | GameLoopCoordinator |
| `_speed` | int | GameLoopOptions.CaptureIntervalMs |
| `_heroStackPreRebuy` | decimal | PostflopGameContext.TrackHeroStack() |
| `_newHand` | bool | PostflopGameContext.NewHandDetected |
| `_newTableHand` | long | PostflopGameContext.CurrentHandNumber |
| `_tableHand` | string | PostflopGameContext.CurrentHandNumber |
| `_previousSBPlayerName / _previousBBPlayerName` | string | PostflopGameContext.PreviousBlinds |
| `_lastActivePlayerCount` | int | PostflopGameContext.LastActivePlayerCount |
| `_flopResult / _turnResult / _riverResult` | PokerCalculationResult | GameLoopResult por iteración |
| `_turnBoardTexture / _riverBoardTexture` | enum | GameLoopResult.BoardTexture |
| `_responseAction` | ResponseAction | GameLoopResult.DecisionResult |
| `_playerGameState` | PlayerGameState | ITableLayoutService |
| `_handle` | IntPtr | Permanece en FrmMain (ventana) |
| `_session` | string | GameLoggerService |
| `_pictureUmbralBet` | int | IScreenReaderService config |
| `_lastChecked / _img / _isClosing / _historialLoaded / _bankrollLoaded` | varios | Permanecen en FrmMain (UI pura) |

### Tipos consumidos del Domain (no propios)

| Tipo | Origen | Uso |
|------|--------|-----|
| `StrategyProfile / OverlayConfig` | `Domain.Entities` | `IOptions<T>` |
| `Card / GameSession / HandRecord / RegionTableMap / Table` | `Domain.Entities` | Marten queries |
| `OpponentProfile` | `Domain.Entities` | `GetActiveVillainProfile`, pasado al calculator |
| `Region / CardDataOuts / Hand / StreetDecision / VillainRange / TelemetryAggregate / CategoryStats / BankrollSnapshot` | `Domain.ValueObjects` | DTOs comunes |
| `BoardPosition / HandSituation / TablePosition / HandRank / PairClassification / KickerStrength / GameSituation / Rank / Suit / HandResult / OpponentType / BoardTextureCategory / RiverCardType / Positions` | `Domain.Enums` | Tipificación |
| `CardDTO / SessionStatsDto / TableDTO` | `Domain.Dtos` | Query results |
| `StrategyProfileValidationException` | `Domain.Exceptions` | Fail-fast en Program.cs |
