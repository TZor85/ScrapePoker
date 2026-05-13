# Flowchart — `GameCoordinator.DetermineFlopAction / Turn / River`

> Coordinador del game loop. Funcionalmente idénticos los 3 métodos: equity → contexto → decisión → exploitabilidad → log.
> Generado por el Arqueólogo del Reversa.

## Contexto del componente

`GameCoordinator` (793 LOC) centraliza la **fase de decisión postflop** delegando en `IPostflopDecisionService` (motor del paquete `OpenScrape.DecisionMaker`). Es **scoped** en DI y sostiene el `PostflopContextHolder` cross-street.

Tres métodos públicos análogos: `DetermineFlopAction`, `DetermineTurnAction`, `DetermineRiverAction`. Las diferencias son:
- **Flop:** `cbetAdjustment`, sin `dangerPenalty` previo, `previousCardCount=0`
- **Turn:** `dangerPenalty` calculado, `combinedBoardChange = InitialBoardDanger + boardChange`, `previousCardCount=3`
- **River:** `dangerPenalty`, `combinedBoardChange = LastBoardChange + boardChange`, `previousCardCount=4`, all-in detection

## Flujo de `DetermineFlopAction` (canónico)

```mermaid
flowchart TD
    Start([DetermineFlopAction state]) --> Guard{FlopResult null?}
    Guard -->|Yes| Err([Return Check, Error: sin resultado de flop])
    Guard -->|No| Vars[Extraer maxBet/potSize/inPosition/numOpponents/villainStack]

    Vars --> SizeCat[GetOpponentBetSize maxBet, potSize<br/>Underbet ≤15% / Small ≤30% / Medium ≤70% / Large >70%]
    SizeCat --> AnalyzeBoard[boardTextureAnalyzer.Analyze flopRanks, flopSuits<br/>Category Dry/SemiDry/SemiWet/Wet/Paired]

    AnalyzeBoard --> InitialDanger[boardTextureAnalyzer.AnalyzeInitialBoard<br/>contextHolder.Update InitialBoardDanger]
    InitialDanger --> BoardChange[AnalyzeBoardChange boardCards, prev=0]

    BoardChange --> DonkCheck[DetectDonkBet maxBet, inPosition, situation]
    DonkCheck --> RangeAdv[PreflopAnalyzer.HasRangeAdvantageOnBoard]
    RangeAdv --> CbetAdj[CalculateCbetAdjustment isPreflopAggressor + rangeAdv + texture + IP + opp + profile]

    CbetAdj --> EffEquity[effectiveEquity = rawEquity + cbetAdjustment]

    EffEquity --> BuildInput[PostflopDecisionInput<br/>30+ campos: equity, street, situation, texture<br/>villainBetSize, potOdds, outs effective<br/>boardChange, hasFlushDraw, comboDraw<br/>heroIsAggressor, kicker, blocksTopCard<br/>FoldEquity adjusted by tracker<br/>VillainType + foldToBetPct]

    BuildInput --> Decide[postflopDecisionService.DetermineAction input<br/>10+ paths del motor]
    Decide --> Track[TrackVillainPostflopAction<br/>OpponentTracker.RecordPostflopAction Bet/Check<br/>RecordCBetOpportunity si preflopAggressor]

    Track --> UpdateCtx[contextHolder.Update<br/>HeroBetFlop, VillainBetFlop, VillainBetSizeFlop<br/>HeroFloatedFlop si Call vs bet<br/>VillainAggressorCheckedFlop]

    UpdateCtx --> Exploit[exploitabilityCalculator.AnalyzeDecision<br/>RecordDecision con OurDecisionEV/BestResponseEV/Mbb]
    Exploit --> Log[StringBuilder formatea bloque ═══ FLOP ═══<br/>cards / pot / situación / equity / mano / draws / decisión]

    Log --> Logger[gameLoggerService.LogStreetDecision<br/>UpdateSituation]
    Logger --> Return([GameDecisionResult action, logText, decision, boardChange, texture])
```

## Diferencias específicas por calle

### Turn-only

```mermaid
flowchart LR
    Common[Cálculo común] --> CombineBoard[combinedBoardChange = CombineBoardChanges<br/>InitialBoardDanger + boardChange]
    CombineBoard --> DangerPenalty[postflopDecisionService.CalculateDangerPenalty<br/>equity, boardChange, heroBlocks, isFacingBet, Turn, nutBlocker, hand]
    DangerPenalty --> AggCross[turnIsAggressor = isPreflopAggressor OR HeroBetFlop]
    AggCross --> SetCtx[Update HeroBetTurn, VillainBetTurn<br/>VillainBarreling = VillainBetFlop AND maxBet>0<br/>VillainAggressorCheckedPreviousStreet = VillainAggressorCheckedFlop<br/>HeroFloatedFlop pasa a input]
    SetCtx --> NewFlushDanger[Update TurnCalledWithFlushDanger<br/>si maxBet>0 + Call + FlushDrawAppeared]
```

### River-only

```mermaid
flowchart LR
    Common[Cálculo común] --> AllinCheck{villainStack ≤ 0?}
    AllinCheck -->|Yes| MarkAllin[contextHolder.Update IsAnyoneAllIn=true]
    AllinCheck -->|No| RiverCard

    MarkAllin --> RiverCard[boardTextureAnalyzer.ClassifyRiverCard boardChange<br/>RiverCardType S22.2: Brick/Scare/Improving/Neutral]
    RiverCard --> CombineRiver[combinedBoardChange = CombineBoardChanges<br/>LastBoardChange + boardChange]
    CombineRiver --> AggRiver[riverIsAggressor = preflopAgg OR HeroBetFlop OR HeroBetTurn]
    AggRiver --> InputRiver[Pasa a input:<br/>VillainBetSizeTurn, VillainCheckedMiddleStreet<br/>TurnCalledWithFlushDanger, HeroCheckedAllStreets]
    InputRiver --> NoTrackUpdate[NO actualiza HeroBet/VillainBet de calle siguiente — es la última]
```

## Helpers compartidos del coordinator

```mermaid
flowchart TD
    GAVI[GetActiveVillainId state]
    GAVI --> Filter[Active + Name no vacío]
    Filter --> Order[OrderByDescending Bet → primero]
    Order --> Resolve{Alias presente?}
    Resolve -->|Yes| RetAlias[return Alias]
    Resolve -->|No| Tracker[opponentTracker.ResolveName Name<br/>fallback Name]

    GVT[GetVillainType state, heroIsInPosition?]
    GVT --> ProfId[GetActiveVillainId]
    ProfId --> ProfReliable{HasReliablePreflopData?}
    ProfReliable -->|No| Unknown[OpponentType.Unknown]
    ProfReliable -->|Yes & heroIP set| GetForPos[profile.GetTypeForPosition villainIsIP=NOT heroIP]
    ProfReliable -->|Yes & no IP info| GlobalType[profile.Type]

    GAP[GetActiveVillainProfile state]
    GAP --> Tracker2[tracker.GetProfile id]
    Tracker2 -->|HasReliablePreflopData| RetProf[return profile]
    Tracker2 -->|else| Null[null]

    GVS[GetVillainStack state]
    GVS --> ActiveV[Active + Name no vacío + != P0]
    ActiveV --> MaxStack[Max Stack]
    MaxStack --> Fallback{villainStack ≤ 0 AND HeroStack > 0?}
    Fallback -->|Yes| HeroFB[return HeroStack fallback]
    Fallback -->|No| RetStack[return villainStack]

    HBTC[HeroBlocksTopBoardCard state]
    HBTC --> TopRank[topBoardRank = boardCards.Max Force]
    TopRank --> RankCheck{topBoardRank < 10?}
    RankCheck -->|Yes| FalseRet[false sin blocker relevante]
    RankCheck -->|No| BlockerCheck[Hole1Rank == top OR Hole2Rank == top]
```

## DonkBet Detection

```mermaid
flowchart TD
    Start([DetectDonkBet state, maxBet, isHeroIP, situation]) --> VWasAgg[villainWasPreflopAggressor =<br/>any Active + WasPreflopAggressor + ValuePosition != 0]

    VWasAgg --> HeroAgg[heroPlayer = state.Players P0<br/>heroWasPreflopAggressor = heroPlayer.WasPreflopAggressor]
    HeroAgg --> HeroPrev[heroWasPreviousStreetAggressor =<br/>IsTurn AND HeroBetFlop OR<br/>IsRiver AND HeroBetTurn]

    HeroPrev --> HeroIs[heroIsAggressor = heroWasPreflopAgg OR heroWasPrevStreetAgg]
    HeroIs --> Guard{heroIsAggressor?}
    Guard -->|No| RetFalse[return false, currentSituation<br/>sin agresor no hay donk]
    Guard -->|Yes| EffectiveV[effectiveVillainAggressor =<br/>villainWasPreflopAgg AND NOT heroWasPrevStreetAgg]

    EffectiveV --> Delegate[PreflopAnalyzer.DetectDonkBet<br/>maxBet, effectiveVillainAggressor, currentSituation]
    Delegate --> Result([Tuple IsDonkBet, DonkBetSituation])
```

## Análisis de board (post-OCR cards)

```mermaid
flowchart TD
    ABC([AnalyzeBoardChange boardCards, previousCardCount])
    ABC --> Filter[community = boardCards Position != Hand]
    Filter --> Check{community.Count ≤ previousCardCount?}
    Check -->|Yes| Safe[Return BoardChangeResult.Safe]
    Check -->|No| Extract[previousRanks = take previousCardCount<br/>previousSuits = take previousCardCount<br/>newCard = community at previousCardCount]
    Extract --> Delegate[boardTextureAnalyzer.AnalyzeBoardChange<br/>previousRanks, previousSuits, newCard.Force, newCard.Suit]

    ATBT([AnalyzeTurnBoardTexture boardCards])
    ATBT --> TurnCards[turnCards = boardCards Position != Hand]
    TurnCards --> CountCheck{count < 4?}
    CountCheck -->|Yes| Dry[Return Dry]
    CountCheck -->|No| Detect[Paired si rank duplicado<br/>Coordinated si flushDraw OR straightDraw<br/>else Dry]

    ARBT([AnalyzeRiverBoardTexture boardCards])
    ARBT --> RiverCards[community = boardCards Position != Hand]
    RiverCards --> CountCheck5{count < 5?}
    CountCheck5 -->|Yes| Dry5[Return Dry]
    CountCheck5 -->|No| Detect5[Paired si trips OR 2+ pairs<br/>Coordinated si flush completo OR straight diff≤4<br/>else Dry]
```
