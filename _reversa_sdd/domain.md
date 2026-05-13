# Dominio — ScrapePoker

> Glosario, reglas de negocio implícitas y conocimiento de dominio extraído del código.
> Generado por el Detective del Reversa el 2026-05-06.
>
> **Escala de confianza:** 🟢 CONFIRMADO (extraído del código) · 🟡 INFERIDO (deducido por patrones) · 🔴 LACUNA (pendiente de validar con el usuario en `_reversa_sdd/questions.md`).

---

## 1. Visión del producto 🟢

**ScrapePoker / OpenScrape** es un **bot asistente de poker NLHE cash game** que opera **sin acceso a la API del cliente de poker**. Su mecanismo es de **scraping pasivo**: captura imágenes de la mesa que el usuario juega en otra ventana de Windows, las analiza con OCR/visión artificial, modela el estado del juego y recomienda en tiempo real una jugada (Fold / Call / Bet / Raise / Check) mostrándola en un overlay sobre la mesa.

- **Modelo de despliegue:** binario WinForms standalone (.NET 10 Windows). Sin cliente/servidor — todo corre en la máquina del jugador.
- **Persistencia:** PostgreSQL (Marten) — guarda sesiones, manos individuales, mapas de regiones por sala, perfil de estrategia, perfiles de oponentes.
- **Audiencia:** un único usuario humano (el jugador) por instalación. No es multi-tenant.
- **Estado actual del producto:** funcional, en evolución continua de sprints S18..S22 con mejoras al motor de decisión (~+5.5–9.5 BB/100 estimado en el último sprint S22).
- **Distribución comercial:** prevista pero no implementada — existe spec abierta `login-sistema-licencias` que añade login, licencias temporales por hardware y rol Admin (ver §10).

**Restricciones implícitas inferidas:** 🟡

- El bot **no actúa**: la spec confirma que solo *recomienda*. El usuario sigue siendo el agente que pulsa los botones del cliente. Esto evita el escenario "bot que juega solo" — relevante para el TOS de las salas.
- 🔴 **LACUNA**: el código contiene `Helpers/AutoIt` y commits "Capture auto" / "Add autoit to show form action" en la prehistoria del repo (commits `3db442c`, `7562772`). Falta confirmar si en algún momento hubo automatización de input vía AutoIt y si fue eliminada o queda dormida. **Pregunta para el usuario.**

---

## 2. Glosario de poker (jerga del dominio) 🟢

> Todos los términos siguientes aparecen literalmente en el código (enums, propiedades, parámetros). Una migración a otra stack debe preservar la semántica exacta.

### 2.1 Roles y posiciones en la mesa

| Término | Tipo en código | Significado |
|---|---|---|
| **Hero** | nombre convencional | El jugador humano que usa el bot. En el motor es siempre el observador. En `OpponentTracker`, `Hero` no se rastrea (solo villanos). |
| **Villain** | nombre convencional | Cualquier oponente. Cuando hay HU (heads-up) hay un único villain; en multiway, hasta 8. |
| **Dealer / Button (BTN)** | `TablePosition.Button` | El jugador con la "rueda" (botón). Detección crítica: sin dealer, todas las posiciones derivan mal. |
| **Small Blind (SB)** | `TablePosition.SmallBlind` | A la izquierda del dealer. Paga ciega pequeña obligatoria. |
| **Big Blind (BB)** | `TablePosition.BigBlind` | A la izquierda del SB. Paga ciega grande obligatoria (= unidad de medida — `BBPer100`). |
| **Cut-Off (CO)** | `TablePosition.CutOff` | A la derecha del BTN. Posición tardía favorable. |
| **Middle (MP)** | `TablePosition.Middle` | Posición intermedia. |
| **Early (EP)** | `TablePosition.Early` | Posición temprana. La más desventajosa al hacer open. |
| **In Position (IP) / Out Of Position (OOP)** | `Positions.InPosition / OutOfPosition` | Indica si hero actúa después (IP) o antes (OOP) del villain en cada calle postflop. Modula casi toda decisión postflop. |
| **Hero seat 0 (P0)** | convención | En todas las regiones (`P0_Card1`, `P0_Stack`, `P0_Bet`…) Hero ocupa el slot 0. P1..P8 son villanos. |

### 2.2 Acciones y situaciones preflop

| Término | Tipo | Definición operativa |
|---|---|---|
| **Open / OpenRaise** | `HandSituation.OpenRaise` | Hero es el primer raiser preflop. Ningún jugador antes ha apostado por encima de BB. |
| **Limper** | concepto | Jugador que solo pagó la BB (call sin raise). |
| **RaiseOverLimper** | `HandSituation.RaiseOverLimper` | Hero hace raise estando ya un limper en bote. **Modo simplificado**: la decisión postflop ignora la textura del board y usa sizings IP/OOP fijos (`StreetThresholds.IsSimplified=true`). |
| **3-Bet** | `HandSituation.ThreeBet` | Hero re-sube sobre un open ajeno. |
| **vs 3-Bet** | `HandSituation.OpenRaiseVs3Bet` | Hero abrió y enfrenta un 3-bet. |
| **vs 3-Bet & Call** | `HandSituation.OpenRaiseVs3BetAndCall` | Hero abrió, otro 3-betteó, un tercero pagó (rango medio caller). |
| **4-Bet** | `HandSituation.FourBet` | Hero re-sube sobre un 3-bet del villain. |
| **Cold 4-Bet** | `HandSituation.Cold4Bet` | Hero hace 4-bet sin haber participado en el preflop antes (no abrió). Rango premium implicado. |
| **Squeeze** | `HandSituation.Squeeze` | Hero re-sube cuando hay 1+ raiser y 1+ caller (apretar a varios oponentes). |
| **vs Squeeze** | `HandSituation.VsSqueeze` | Hero recibe un squeeze. |
| **Limp-Raise** | `HandSituation.LimpRaise` | Patrón añadido en S20.2: hero/villano limpea y luego sube — rango muy fuerte. |
| **Donk Bet** | `HandSituation.DonkBet` | Apuesta postflop hecha **OOP** por quien **no** fue el agresor preflop. Históricamente señal de debilidad/caprichoso, en algunos pools señal de fuerza. **Bug histórico (commit `a554b4a`):** se detectaba donk en pots sin raise preflop (limpeados); ahora exige agresor real. |
| **DonkBet vs OpenRaise** | `HandSituation.DonkBetVsOpenRaise` | Donk específicamente en respuesta a un open hero. |
| **Blind vs Blind (BvB)** | concepto / S20.1 | Hand-to-hand SB vs BB. Rango y dinámicas distintas (más amplio, más bluff). Tres ajustes en `StrategyProfile`: `BvBSBvsBBFoldBelowAdj=-3`, `BvBBBvsSBFoldBelowAdj=-5`, `BvBBBvsBTNFoldBelowAdj=-1`. |

🟢 Confianza alta — todos confirmados en `Domain/Enums/Positions.cs` y datos de `src/OpenScrape.App/Data/*.json`.

### 2.3 Calles, board y manos

| Término | Tipo | Significado |
|---|---|---|
| **Hand / Preflop** | `BoardPosition.Hand` | Calle 0: hero recibe sus 2 hole cards. **Importante:** el enum se llama `Hand`, no `Preflop`, históricamente porque también designa la mano de hero. |
| **Flop** | `BoardPosition.Flop` | Calle 1: 3 cartas comunitarias reveladas. |
| **Turn** | `BoardPosition.Turn` | Calle 2: 4ª carta. |
| **River** | `BoardPosition.River` | Calle 3 y final: 5ª carta. Decisión definitiva sin más cartas. |
| **Hole cards** | `HeroCard1`, `HeroCard2` | Cartas privadas del jugador. |
| **Board / Community cards** | `FlopCards`, `TurnCard`, `RiverCard` en `HandRecord` | Cartas comunes a todos. |
| **HandRank** | `HandRank` enum | Ranking estándar de poker: HighCard < OnePair < TwoPair < ThreeOfAKind < Straight < Flush < FullHouse < FourOfAKind < StraightFlush < RoyalFlush. Ordinal numérico permite comparación directa. |
| **KickerStrength** | `KickerStrength` enum (None/Weak/Medium/Strong) | Calidad del kicker en TopPair. **Regla de negocio**: TPTK (Top Pair Top Kicker = Strong) → +3 sizing y -3 FoldBelow facing bet; TPWK (Top Pair Weak Kicker) OOP → +2 FoldBelow. |
| **PairClassification** | `PairClassification` enum | Sub-clasificación específica para postflop turn/river: BoardPaired < BottomPair < PocketPairUnder < MiddlePair < TopPair < Overpair. Ordinal byte permite ordenamiento. |

### 2.4 Draws (proyectos)

| Término | Significado |
|---|---|
| **Outs** | Cartas que mejoran la mano de hero. Se cuentan con inclusión-exclusión en `OutsCalculator`. |
| **Flush draw** | Hero tiene 4 cartas del mismo palo. ~9 outs estándar. |
| **Straight draw (OESD)** | Open-ended straight draw. ~8 outs. |
| **Gutshot** | Inside straight draw. ~4 outs. |
| **Combo draw** | Flush draw **+** straight draw simultáneos. Tratado como semi-bluff premium con **bonus de equity ajustado por textura** (S L5): `ComboDrawTextureDry=1.2 → ComboDrawTextureMonotone=0.5`. |
| **Backdoor draw** | Necesita **dos** cartas para completarse (turn y river). 🟢 Calibración (L1, commit `bf839be`): backdoor flush vale **1.5 outs**, backdoor straight vale **1.0 outs**. |
| **Tainted out** | Out que también mejora la mano del villano. Se descuenta: hero con flush draw → ×0.7 (mejora más); sin flush draw → ×0.3. |
| **Overcard out** | Carta más alta que el board. Calibrado por textura S21.1: base 3 outs, board conectado 2, board emparejado 2; con blocker × 1.2. |
| **Effective outs** | `Math.Max(0, totalOuts - tainted - overlapping)`. |

### 2.5 Equity, pot odds y EV

| Término | Significado |
|---|---|
| **Equity** | % esperado del bote. Calculado por `MonteCarloSimulator` (preflop 30K iter MC, flop 50K MC, turn enumeración exacta 42K, river enumeración exacta C(45,2)=990). |
| **Effective equity** | `equity - dangerPenalty + comboDrawBonus, after Math.Max(0, …) - reverseImpliedPenalty`. Es la equity *para decidir*, no la cruda. |
| **Pot odds** | % de equity necesario para que call sea +EV. = `betToCall / (pot + betToCall)`. |
| **SPR (Stack-to-Pot Ratio)** | `effectiveStack / pot`. Modula commitment: SPR<0.5 + EV(call)>0 → forzar call; SPR<1.5 → all-in EV explícito; check-raise prohibido si SPR<1.5 + equity<60. |
| **EV (Expected Value)** | Esperanza monetaria. `equity × (pot+stack) − (1−equity) × stack` para all-in (`CalculateAllinEV`). |
| **Fold equity (FE)** | Probabilidad de que el villain foldee a una apuesta. Base 20% + bonificaciones por street/posición/tipo de oponente. Si hay all-in detectado → FE = 0. |
| **Implied odds** | Equity adicional ganada en calles futuras si se completa el draw. Factor multiplicativo sobre pot odds, dependiente de SPR y posición. |
| **Reverse implied odds** | Pérdida esperada en calles futuras cuando se mejora a una mano dominada. Penalización aditiva en turn facing bet. |

### 2.6 Métricas de oponente

| Term | Definición operativa |
|---|---|
| **VPIP** | Voluntarily Put $ In Pot — % manos jugadas voluntariamente preflop. Default 50%. |
| **PFR** | Pre-Flop Raise — % manos con raise preflop. Default 15%. |
| **3-Bet %** | % manos con 3bet preflop. Default 5%. |
| **AF (Aggression Factor)** | `(bet + raise + 1) / (call + 1)` (Laplace smoothing — L4 commit `72ade14` — evita cliff cuando passive=0). |
| **AF IP / AF OOP** | Versiones posicionales. -1 si <5 muestras. |
| **C-Bet %** | `TimesCBet / TimesCBetOpportunity`. Default 50%. |
| **Fold-to-CBet %** | Default 50%. |
| **WTSD %** | Went to Showdown — `TimesWentToShowdown / TimesReachedRiver`. Default 35%. |
| **WSD %** | Won at Showdown — `TimesWonAtShowdown / TimesWentToShowdown`. Default 50%. |
| **Check-Raise %** | Default 8%. |
| **Donk-Bet %** | Default 10%. |
| **Barrel Frequency** | `TimesBarreled / TimesBarrelOpportunity`. Default por tipo: LAG 60, TAG 30, LP 20, TP 10. |
| **OpponentType** | `LAG` (Loose-Aggressive, fish/maniac), `TAG` (Tight-Aggressive, regular), `LP` (Loose-Passive, fish), `TP` (Tight-Passive, nit). Clasificación: `isLoose = VPIP > 30`, `isAggressive = AF > 1.5`. **`Unknown` cuando HandsPlayed < 10**. |

🟢 Todas confirmadas en `OpponentProfile.cs` con thresholds y defaults exactos.

---

## 3. Reglas de negocio principales 🟢

> Todas las reglas que aparecen abajo están confirmadas en código. La sección §6 lista las inferidas.

### 3.1 Manejo del dinero

| ID | Regla | Lugar | Confianza |
|----|-------|-------|-----------|
| BR-001 | Toda métrica monetaria se expresa en BB (Big Blinds). La unidad es la `BigBlind` de la sesión, leída en cada hand record. | `GameSession.BBPer100` | 🟢 |
| BR-002 | El profit neto **excluye** la ciega obligatoria pagada y el auto-rebuy. `NetProfit = (HeroStackEnd - HeroStackStart) - AutoRebuy + BlindPosted`. | `HandRecord.NetProfit` | 🟢 |
| BR-003 | Si el hero paga BB (1.00) y foldea, NetProfit = 0 (no -1.00). Esto refleja que la ciega es coste estructural, no error. | `HandRecord.NetProfit` doc | 🟢 |
| BR-004 | Auto-rebuy se detecta cuando el stack del hero aumenta repentinamente ≥ **50 BB** entre lecturas consecutivas dentro de la misma mano. Se asume que la sala recargó automáticamente a 100 BB. | `PostflopGameContext.AutoRebuyThreshold = 50m` y `TrackHeroStack` | 🟢 |
| BR-005 | El stack inicial del hero se fija a **100 BB** si la primera lectura es 0 (OCR fallido). | commit `5e21f14` (`feat/session: handle auto-rebuy and improve seat logic`) | 🟢 |
| BR-006 | El bankroll inicial se persiste en `appsettings.json:StrategyProfile.InitialBankroll`. La sesión almacena `StartingBankroll`, `EndingBankroll`, `PeakBankroll` para tracking de drawdown. | `GameSession`, `BankrollSnapshot` | 🟢 |
| BR-007 | El "Risk of Ruin" default umbral es 5% (`RiskOfRuinThreshold=0.05`). Recomendación de límites de buy-in se calcula con la fórmula clásica: `RoR = exp(−2 × winRate × bankroll / variance)`. | `BankrollTrackerService` | 🟢 |

### 3.2 Detección de mesa y jugadores

| ID | Regla | Lugar |
|----|-------|-------|
| BR-010 | El dealer se detecta por color/forma. Si la primera captura no lo encuentra (`Position == None`), `SetDealerPlayer()` reintenta en cada iteración del loop hasta detectarlo. | `TableLayoutService` |
| BR-011 | Las **9 posiciones** del esquema (`P0..P8`) son fijas; el slot `P0` siempre corresponde al hero. Los villanos se detectan en `P1..P8` según asientos ocupados/vacíos/sit-out. | regiones JSON |
| BR-012 | Las ciegas usan **moving-blinds**: SB y BB saltan los SitOut consecutivos a su izquierda; los Empty quedan completamente fuera del anillo. Los SitOut consumidos por el salto quedan sin posición; los que caen fuera del tramo dealer-SB-BB mantienen su asiento. | `PositionCalculator` (commit `1654e09`) |
| BR-013 | Hero (P0) siempre se considera **active**, incluso si OCR no detecta su nombre. | commit `4dc9075` |
| BR-014 | Cuando cambia el número de jugadores activos entre manos, se **recalculan todas las posiciones** desde el dealer. | commit `5e21f14` |

### 3.3 Detección de calle (street detection)

| ID | Regla | Lugar |
|----|-------|-------|
| BR-020 | El paso de Preflop a Flop se decide por `ShouldCaptureFlop` en el detection loop (suma de pixeles activos en regiones de carta). | `FrmMain` |
| BR-021 | El paso a Turn / River se decide por `IsBoardCardVisible("Card4"/"Card5")` usando comparación de hash de imagen (dHash) con umbral **>80%** de confianza. | `FrmMain.ProcessPostFlopAsync` |
| BR-022 | **Contar cartas reales antes de transicionar.** `CountVisibleBoardCards()` verifica Card1-Card5 secuencialmente y bloquea la transición si `visibleCards < expectedMin` (Flop=3, Turn=4, River=5). Bug fix histórico (commit `2538e55`): falsos positivos hacían transitar a turn sin carta nueva. | `GameLoopStateMachine.TryTransition(state, visibleBoardCards)` |
| BR-023 | Cuando hero está en `*Action` y la siguiente carta no aparece, se reprocesa la calle actual con bet info actualizada (escenario "villain raise mid-street"). | `ProcessPostFlopAsync` |
| BR-024 | El conteo del río (River) cuenta solo cartas con **nombre válido** (no string vacío). Bug fix OCR-3 (commit `6726f0e`). | `FrmMain` |

### 3.4 Estrategia preflop

| ID | Regla | Lugar |
|----|-------|-------|
| BR-030 | La estrategia preflop es **tabla-driven**, no algorítmica: 16 archivos JSON en `src/OpenScrape.App/Data/` mapean (Position × Situation × Action) → `List<Hand>` permitidas. Ej: `OpenRaise.json`, `BBvsSB.json`, `ThreeBet.json`. | `Data/*.json` |
| BR-031 | Cada `Hand` tiene `Name` (ej "AKs"), `Suited` (true/false/null para pairs), `Action` (Raise/Call/3Bet/Fold), y `Percentage` (0..100, frecuencia de mezcla). | `Hand.cs` |
| BR-032 | Squeeze por hero/villain dispara la lógica `FourBet` (commit `6726f0e`). | `SetPreflopActionUseCase` |
| BR-033 | Numero de oponentes preflop usa solo `voluntaryBettors` (jugadores con `Bet > 1m` BB), no todos los activos. Bug fix HF5. | `FrmMain` |

### 3.5 Estrategia postflop — pipeline de equity

🟢 Pipeline canónico de `PostflopDecisionService.DetermineAction(input)`:

```
1. Look up StreetThresholds via ThresholdsRegistry[Street, Situation]
2. Categorizar villain bet (NoBet | Underbet<15% | Small<33% | Medium<66% | Large)
3. Calcular pot odds → necesaria equity de call
4. Equity raw (Monte Carlo / enumeración exacta)
5. Outs + draws + tainted outs + combo draw detection
6. Hand evaluation (HandRank + KickerStrength + PairClassification)
7. Fold equity (stats reales OpponentTracker o fallback estático; 0 si all-in)
8. Danger penalty (proportional flush/straight; flat board paired/overcard; 
   blocker reduction nut/non-nut; street multiplier flop×1.3, river×0.8)
9. Combo draw bonus × textura (S L5)
10. Reverse implied odds (turn/river facing bet, OnePair/TwoPair, draw-heavy)
11. effectiveEquity = max(0, raw - danger + combo) - reverseImplied
12. Decisión por path (10+ paths — ver §3.6)
```

### 3.6 Postflop — los 10+ paths de decisión

🟢 Confirmados en `PostflopDecisionService.DetermineAction` y CLAUDE.md. La prelación es importante: el primer path que matchea decide.

1. **Facing Bet** → Call/Raise/Fold según equity vs pot odds + categoría de bet villain. Raise solo TwoPair+; OnePair en board flush-posible sin blocker → call no raise. Bet-size penalties: Underbet 0, Small +1, Medium +4, Large +8; villain agresivo +3.
2. **No Bet (check)** → Check / Bet con sizing por textura (Dry / Coordinated / Paired / Monotone / Wet). Overbet en boards secos turbo (TwoPair+ con `OverbetMinEquity≥80`). River merged sizing: OnePair → ReduceBetSize, TwoPair+ → polarizado.
3. **Check-Raise** → OOP TwoPair+ o draws fuertes (combo draw / flush draw 9+ outs, `CheckRaiseDrawMinEquity=40`). IP TwoPair+ trap en board no Wet/Monotone. Solo si `!heroIsAggressor && !multiway`. **SPR guard:** si SPR<1.5 y equity<60% → skip.
4. **Float Exit** → `heroFloatedFlop && turn && villain check && !multiway` → Bet 1/2. Se aborta si `bad runout` (overcard / flush completa / straight completa).
5. **Probe Bet** → Villain agresor checkeó la calle anterior + `!multiway` + equity ≥ `ProbeBetMinEquity`. IP bet 1/2, OOP bet 1/3.
6. **Pot Control** → Turn equity 40-55% en Coordinated/Wet/Monotone + `!heroIsAggressor` → check-back.
7. **Delayed Value** → River + `HeroCheckedAllStreets` + OnePair TopPair+ → Bet 1/3.
8. **Low Equity (Bluff / Bluff Catch)** → Semi-bluff con check de fold equity (breakevenFE ajustado por draw equity); bluff puro si FE ≥ breakeven; bluff catching multiplicado por villain type / runout / blocker.
9. **Randomización** → Equity ±3% del threshold → check adaptativo por villain type (LAG 85%, LP 80%, TAG 60%, TP 55%, Unknown 70%) — anti-exploit.
10. **C-Bet (agresor)** → Agresor preflop con equity baja (< FoldBelow dentro de -15) → c-bet a frecuencia propia (Flop 65%, Turn 45%, River 30%). C-bet mixing: agresor con equity media [FoldBelow, ThinValueAbove) chequea a frecuencia (1−cbetFreq) para proteger checking range — solo HU.

🟡 **Inferido**: el orden y prelación entre paths busca **maximizar EV** y **proteger rangos** (anti-explotación). El c-bet mixing solo en HU porque multiway hace inviable defender un check con la misma frecuencia.

### 3.7 Sizing dinámico

🟢 `BetSizingService.CalculateDynamicBetSize` modula por SPR/multiway/textura/posición/street según estos factores:

| Factor | Multiplicador |
|---|---|
| SPR profundo (>3) | × 1.25 |
| SPR superficial (<1) | × 0.75 |
| Board paired | × 1.15 |
| Board coordinated | × 0.90 |
| OOP | × 0.90 |
| Multi-opponent | × 0.85 |

Sizing de bluff modulado por SPR (`BluffSPRShort 0.5×`, `BluffSPRDeep 1.2×`) y por tipo de oponente.

### 3.8 Persistencia y telemetría

| ID | Regla | Lugar |
|----|-------|-------|
| BR-040 | Una sesión = un `GameSession` document con FK a múltiples `HandRecord` documents. La lista `GameSession.Hands` es solo en memoria (`[JsonIgnore]`). | `GameSession.cs` |
| BR-041 | `HandResult` ∈ {Unknown, Won, Lost, Push}. `Unknown` indica que la mano no llegó a showdown ni se pudo determinar resultado por OCR — se excluye de cálculos de profit y BB/100. | `HandResult` enum, `GameSession.TotalProfit` |
| BR-042 | Para que una sesión sea válida (`IsValid`): `SessionId` y `TableName` no vacíos + `BigBlind > 0`. Sino, no se persiste. | `GameSession.IsValid` |
| BR-043 | Cada `StreetDecision` registra: street, EquityPercent, PotOddsPercent, ExpectedValue, RecommendedAction (motor), ActionTaken (jugador), pot/bet sizes, situation, IsInPosition. Permite divergencia bot vs humano para análisis posterior. | `StreetDecision.cs` |
| BR-044 | Carpetas de session storage usan formato `AAAAMMDD_Game` (commit `6699702`), antes era `Game_YYYY_MM_DD`. Migración manual no documentada en código — folders viejos quedan huérfanos. 🟡 |
| BR-045 | Telemetría es opcional: `HandRecord.Telemetry` es nullable; manos persistidas antes de la feature de métricas no la tienen. La medición `Persistence.SaveHand` se captura *después* del snapshot del hand, por diseño solo agregada por sesión. | `HandRecord` doc |

### 3.9 Configuración y validación

| ID | Regla | Lugar |
|----|-------|-------|
| BR-050 | Al arrancar, `StrategyProfileValidator` valida coherencia de los **36 thresholds requeridos** (4 streets × 9 situations + extras) y que `FoldBelow < ThinValueAbove < ValueAbove < StrongValueAbove`. **Fail-fast:** si falla, MessageBox + exit. | `StrategyProfileValidator`, commit `3aa4c06` |
| BR-051 | El `ThresholdKey(BoardPosition, HandSituation)` se registra como singleton tipado en `IThresholdsRegistry`. **No hay fallback hardcoded** desde el commit `3aa4c06` (eliminó dos fallbacks silenciosos que enmascaraban typos de `appsettings.json`). | `ThresholdsRegistry` |
| BR-052 | `StrategyProfile.Validate()` retorna lista de errores; si no está vacía, el host aborta con `StrategyProfileValidationException`. | `StrategyProfile.Validate` |
| BR-053 | Frecuencias y multiplicadores del `StrategyProfile` están **acotados**: bluff/cbet en `[0,1]`, tainted en `[0,1]`, danger pct en `[0,100]`. La validación lanza error si se sale. | `StrategyProfile.Validate` |
| BR-054 | `appsettings.Development.json` (con credenciales reales) es `gitignored`. `appsettings.json` debe contener placeholders `CHANGE_ME`. **Anomalía detectada por el Scout:** actualmente `appsettings.json` tiene una connection string Neon real y una `Encrypter.Key` real (validar con usuario). | CLAUDE.md, Scout `surface.json` |

### 3.10 OCR — robustez

| ID | Regla | Lugar |
|----|-------|-------|
| BR-060 | Las lecturas OCR críticas (bets, stacks) usan **3-read consensus** + preprocesamiento + `CleanOcrNumericText`. | `ScreenReaderService`, commit `6726f0e` |
| BR-061 | `NormalizeBetValue` corrige separadores decimales perdidos (ej "593" → "5.93") y filtra artefactos (un "8" suelto, etc.) validando contra el pot. | `ScreenReaderService` |
| BR-062 | OCR de baja confianza (`< 0.70`) dispara reintento. | `OcrService.GetMeanConfidence` |
| BR-063 | Si OCR del villain stack devuelve 0, **fallback al stack del hero** (asume mismo tamaño de stack en cash). | bug fix BF8 |
| BR-064 | El nombre/alias del villain se cachea por seat para mantener identidad entre manos cuando OCR fluctúa. | `TableLayoutService` |

---

## 4. Conceptos derivados / cross-cutting 🟢

### 4.1 Estado cross-street (`PostflopGameContext`)

`PostflopGameContext` es un **record inmutable** (commit `127a2f5`) con propiedades cross-street:

- `VillainBetFlop / VillainBetTurn / HeroBetFlop / HeroBetTurn` (booleanos por calle)
- `VillainBetSizeFlop / VillainBetSizeTurn` (categoría de bet)
- `IsVillainBarreling` (apostó en 2+ calles consecutivas → rango más estrecho)
- `VillainCheckedMiddleStreet` (apostó flop pero no turn → patrón débil)
- `HeroFloatedFlop` (call con aire en flop + IP)
- `TurnCalledWithFlushDanger` (preserva plan de check río si flush completa)
- `HeroCheckedAllStreets` (deriva delayed value river)
- `IsAnyoneAllIn` (desactiva FE y reverse implied)
- `TurnBetCommitsToRiver` (S22.4 stackoff plan)
- `HeroStackPreRebuy` + `AutoRebuyThreshold = 50 BB` (S detección de auto-rebuy)
- `InitialBoardDanger / LastBoardChange` (acumulan peligro de board entre calles)

**Regla:** **un único holder scoped** (`IPostflopContextHolder`) garantiza coherencia (commit `127a2f5` arregló bug donde `FrmMain._postflopContext` y `GameCoordinator.PostflopContext` divergían). Toda transición es `holder.Update(c => c with { ... })`.

### 4.2 BoardChange y BoardTexture

| Categoría | Condición |
|---|---|
| **Dry** | DangerLevel < 15 (board sin draws ni paired) |
| **SemiDry** | 15-35 |
| **SemiWet** | 35-60 |
| **Wet** | 60+ (board peligroso, muchos draws) |
| **Paired** | Hay un par en el board (categoría especial) |

**RiverCardType** (S22.2): `Brick / Scare`. Brick (carta neutra) reduce thinValue threshold; Scare (completa flush/straight obvio) aumenta facilidad de bluff catch (`RiverScareBluffCatchReduction=0.90`).

### 4.3 Multiway penalty

🟢 Penalización por bote multiway:

| Posición / situación | Penalty |
|---|---|
| IP | Lineal |
| OOP | **Cuadrático**: `n² × penalty × MultiwayOOPQuadraticDamping (=0.5)` |
| OOP SB | × 0.70 |
| OOP BB | × 0.50 |
| OOP EP | × 0.60 |
| Turn | × 1.2 |
| River | × 1.4 |
| IP agresor | amplificador × 1.3 |

### 4.4 Range advantage

🟢 Hero tiene range advantage en boards con 1+ carta alta (Q, K, A) en pots 3bet (S HF1: antes exigía ≥2 high cards). Modula `CbetRangeAdvantageBonus=8` puntos de equity para c-bet.

### 4.5 Anti-exploit (mixing y randomización)

- **C-Bet mixing** (agresor preflop, equity media, HU): chequea a frecuencia `(1 − cbetFreq)` para que su rango de check no sea capturable.
- **Check-Raise mixing** S19.1: 4 frecuencias por contexto (`CRMixFreqOOPStrong=0.40`, `CRMixFreqOOPTopPairDraw=0.35`, etc.).
- **Randomización threshold ±3%** (S13.2 + S21.5): cuando equity ≈ FoldBelow, se chequea a frecuencia adaptativa por villain type para evitar pattern recognition.

---

## 5. Invariantes de negocio 🟢

| INV | Invariante |
|-----|------------|
| INV-1 | Una `HandRecord` siempre pertenece a una `GameSession` (`GameSessionId` no vacío). Persistencia falla sin esto. |
| INV-2 | El motor **nunca** recomienda Fold sin que haya bet pendiente; en ese caso siempre Check (BR §3.6 path No Bet). |
| INV-3 | `effectiveEquity ≥ 0` siempre tras `Math.Max(0, …)` — bug fix BF5. |
| INV-4 | Cuando `IsAnyoneAllIn = true`, `foldEquity = 0` y `reverseImplied = 0`. Sin excepción. |
| INV-5 | El `HeroStackPreRebuy` se preserva durante toda la mano si se detectó auto-rebuy; nunca se actualiza al alza ≥50 BB durante la misma mano. |
| INV-6 | Hero (P0) no se rastrea en `OpponentTracker`. Solo villains. |
| INV-7 | Las transiciones del `GameLoopStateMachine` son atómicas (lock `_stateLock`). |
| INV-8 | `StrategyProfile.Validate()` retorna lista vacía durante toda la ejecución del programa (validado al arrancar; no se permite reload de threshold sin re-validar). |
| INV-9 | Los Hand `Name` no pueden ser vacíos (validación en constructor del record `Hand`); `Percentage` debe estar en [0, 100]. |
| INV-10 | Las decisiones se loguean **siempre** que estén en `*Action` y haya cambio de bet, incluso si la decisión final del jugador difiere (`StreetDecision.RecommendedAction` vs `ActionTaken`). |

---

## 6. Reglas inferidas — sin certeza absoluta 🟡

| ID | Regla inferida | Evidencia | Pregunta abierta |
|----|----------------|-----------|------------------|
| INF-1 | El bot está pensado para **NLHE 6-max y FullRing** (hasta 9 jugadores). | 9 slots P0..P8 en regiones; tablas `OpenRaise.json` con posiciones EP/MP/CO/BTN/SB/BB | ¿Soporta también 4-max o HU dedicado? |
| INF-2 | El bot opera **en una única ventana al mismo tiempo**. | `FormListApps` selecciona una sola ventana; no hay multi-table coordinator | ¿Existe plan para multi-table? |
| INF-3 | El motor recomienda solo, nunca actúa. | Sin import de `SendInput`, `SendKeys.SendWait` ni similar en código activo | ¿Se descartó AutoIt? Hay rastros legacy. |
| INF-4 | Las salas soportadas son **PokerStars, GG, party / general** — el sistema de regiones es genérico. | `TableLayoutService` lee regiones desde JSON; no hay rama por sala detectada | ¿Cuáles son las salas calibradas en producción? |
| INF-5 | El idioma del cliente de poker debe ser **inglés** para que el OCR Tesseract con `eng.traineddata` funcione. | Solo eng.traineddata embebido, antes era spa según commit `1343857` | Confirmar idioma esperado. |
| INF-6 | La calibración del motor (S18..S22, L1..L6) está **sintonizada para 0.50 BB** (cash micro/low). | `BigBlind` default 0.50m, `BuyInMax = 2.0m` | ¿El motor mantiene calidad en 1/2 o 5/10? |
| INF-7 | El `StrategyProfile` debería permitir **A/B test entre perfiles** (existe `StrategyBacktester`). | `StrategyBacktester` y `BacktestResult` | ¿Usa ya en producción o sigue siendo solo CLI? |
| INF-8 | Los archivos JSON de tablas preflop son **editables por el usuario avanzado**. | Sin código que regenere los JSON; existen 16 archivos en Data/ | ¿Cuál es el flujo de tuning preflop? |
| INF-9 | El sistema de licencias futuro (spec abierta) implica que el bot se **distribuirá comercialmente**. | `openspec/changes/login-sistema-licencias/` | Estado y plan: ¿quién mantiene la BD de licencias? |

---

## 7. Lacunas identificadas 🔴

> Preguntas para `_reversa_sdd/questions.md`. Listadas aquí para visibilidad.

1. 🔴 **Q-DOM-01:** ¿Cuál es el público objetivo y modelo comercial? ¿Uso personal del propietario, distribución a un círculo cerrado, o producto comercial abierto?
2. 🔴 **Q-DOM-02:** ¿Qué salas de poker están **oficialmente** soportadas / calibradas? ¿Hay alguna lista de mesas testadas? (Necesario para no dar falsa promesa de portabilidad).
3. 🔴 **Q-DOM-03:** Limites de stake objetivos: ¿el motor está calibrado para micro (NL2-NL10), low (NL25-NL100), mid? Distintos pools requieren parámetros distintos.
4. 🔴 **Q-DOM-04:** El `BankrollTracker` calcula `RiskOfRuin` con varianza estimada de muestra. ¿Quién decide cuándo subir/bajar de stake? (Hay `RiskOfRuinThreshold=0.05` pero no veo trigger automático).
5. 🔴 **Q-DOM-05:** ¿La feature `FrmHistorial` permite editar manos (corregir `ActionTaken` a posteriori)? El UI existe pero no he validado los handlers.
6. 🔴 **Q-DOM-06:** El sistema legacy `Helpers/AutoIt` y commits "Capture auto" — ¿alguna vez automatizó el clic? ¿Se eliminó deliberadamente o queda dormido?
7. 🔴 **Q-DOM-07:** ¿El motor siempre asume que el hero **respeta** la recomendación, o hay tracking de divergencia para coaching? Existe `RecommendedAction` vs `ActionTaken` en `StreetDecision` pero no he visto reporte.
8. 🔴 **Q-DOM-08:** ¿La spec `login-sistema-licencias` está priorizada o queda diferida? La memoria indica que es spec pendiente desde hace meses.
9. 🔴 **Q-DOM-09:** El proyecto auxiliar `Extractor/ExtractorTablas/` (.NET 8.0) — ¿qué papel juega? ¿Genera los `*.json` de tablas preflop a partir de capturas de PokerSnowie / equivalentes?
10. 🔴 **Q-DOM-10:** Anomalía Scout: `appsettings.json` contiene una connection string Neon real y una `Encrypter.Key`. ¿Es un olvido de rotación o decisión consciente (cualquiera con el binario tiene acceso a una BD compartida)?

---

## 8. Configuración crítica del sistema 🟢

Resumen accionable de parámetros ajustables (extraído de `StrategyProfile` y `appsettings.json`):

| Categoría | Parámetros clave |
|---|---|
| **Fold equity** | `FoldEquityBase=20`, IPBonus=10, ThreeBetPenalty=−10, range [5, 60] |
| **Bet sizing SPR** | Deep (>3) ×1.25, Shallow (<1) ×0.75 |
| **Bluff freq** | Flop 15%, Turn 12%, River 10% |
| **C-Bet freq** | Flop 65%, Turn 45%, River 30% |
| **Danger penalties** | Flush complete 35%, Straight complete 18%, Board paired 5pt, Overcard 3pt, FacingBet ×1.4 |
| **SPR push/fold** | Threshold 2.0, Deep caution 4.0, FoldReduction 8, ValueIncrease 10 |
| **Multiway** | Turn ×1.2, River ×1.4, OOP cuadrático damping 0.5 |
| **3-bet/4-bet pot** | FoldBelow +5/+8, ThinValue +3/+5 |
| **Combo draw bonus** | 6pt × textura (Dry 1.2 → Monotone 0.5) |
| **Tainted outs** | Hero strong ×0.7, weak ×0.3 |
| **Bluff catch** | FoldBelow ×0.75 base, Turn ×0.90 más estricto |
| **Bankroll** | InitialBankroll 100€, BuyInMax 2€, RoR threshold 0.05 |

> Todos estos valores son **ajustables en runtime** modificando `appsettings.json` y reiniciando — no requieren recompilar.

---

## 9. Estilo de juego implícito 🟡

Combinando todos los parámetros y paths, el motor implementa un estilo que se puede caracterizar como:

- **Exploitative-leaning con base GTO defensiva.** Tiene `RangePolarizer` + `ExploitabilityCalculator` (telemetría GTO) pero todas las decisiones específicas usan stats reales del villano (`OpponentTracker`).
- **Tight-Aggressive (TAG)** preflop: 16 archivos JSON con frecuencias, no all-in random.
- **Position-aware fuerte:** todas las situaciones tienen IP/OOP fallback, y SPR se interpola suavemente (no hay buckets duros).
- **Anti-exploit consciente:** mixing en c-bet, check-raise y randomización ±3% del threshold.
- **Conservador en multiway OOP:** penalización cuadrática + reducción especial por blinds.
- **Adaptativo a tipos de oponente:** LAG/TAG/LP/TP/Unknown modulan ~10 parámetros distintos.
- **Bankroll-management built-in:** hace recomendación de buy-in según drawdown histórico.

🟡 Todo lo anterior es síntesis. **Validar con el usuario si refleja la intención original.**

---

## 10. Sistema de licencias (spec pendiente) 🟢

**No implementado todavía.** La spec viva en `openspec/changes/login-sistema-licencias/` define:

- **2 roles**: `User` y `Admin` (`LicenseRole` enum a crear).
- **License** entity (Marten): `LicenseKey` (formato `XXXX-XXXX-XXXX-XXXX`), `Role`, `HardwareId`, `ExpiresAt`, `IsActive`, `LastValidation`.
- **Hardware ID**: SHA256 de `MachineName + DiskSerial + FirstActiveMAC`. Componentes faltantes se sustituyen por cadena vacía (no falla).
- **Validación al arrancar**: existe + activa + no expirada + hardware coincide.
- **Bypass Admin**: ignora hardware (puede usarse en cualquier máquina) y expiración (nunca caduca).
- **Vinculación hardware**: la primera vez que una licencia se usa, se asigna su `HardwareId`. Después, esa licencia solo funciona en ese equipo.
- **Seed admin idempotente**: si no hay licencia admin en BD, crea una con clave de `appsettings.json:AdminLicense:Key`, `ExpiresAt=DateTime.MaxValue`.
- **`ICurrentLicenseService`** singleton: estado de la licencia activa durante la sesión, expone `Role` y `IsAdmin`.
- **UI**: `FrmLogin` modal previo a `FrmMain`. Si se cancela → app termina. "Recordar licencia" guarda clave en `Properties/Settings`.

Ver `permissions.md` para la matriz RBAC propuesta.

---

## Confianza global

- 🟢 **Alta:** glosario, reglas BR-001..BR-064, invariantes INV-1..INV-10, paths del motor, configuración del `StrategyProfile`. Todo extraído del código actual sin ambigüedad.
- 🟡 **Media:** estilo de juego inferido, audiencia, puntos de evolución (anti-exploit, multiway, BvB). Conjeturas razonables pero validar.
- 🔴 **Lacunas:** preguntas Q-DOM-01..Q-DOM-10 listadas en §7.
