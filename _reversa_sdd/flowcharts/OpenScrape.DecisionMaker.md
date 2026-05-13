# Flowchart — `OpenScrape.DecisionMaker`

> Diagrama de dependencias y flujo del motor de decisión.
> Generado por el Arqueólogo del Reversa.

## Vista de capas y orquestación

```mermaid
flowchart TB
    subgraph App["OpenScrape.App (consumer)"]
        UPC[UnifiedPokerCalculator<br/>IPokerCalculator]
        PDF[PokerDecisionFacade]
        FrmMain[FrmMain UI]
        GC[GameCoordinator]
    end

    subgraph DM["OpenScrape.DecisionMaker"]
        subgraph Algos["Algorithms/"]
            MC[MonteCarloSimulator]
            BHE[BitHandEvaluator]
            HE[HandEvaluator legacy]
            OC[OutsCalculator]
            BTA[BoardTextureAnalyzer]
            PEC[PreflopEquityCalculator]
        end

        subgraph Decision["Services/ – decisión"]
            PDS[PostflopDecisionService<br/>1893 LOC]
            PA[PreflopAnalyzer]
            BSS[BetSizingService]
            DPC[DangerPenaltyCalculator]
            IOC[ImpliedOddsCalculator]
            RP[RangePolarizer]
            TR[ThresholdsRegistry]
            PGC[PostflopGameContext<br/>record inmutable]
        end

        subgraph Tracking["Services/ – tracking"]
            OT[OpponentTracker<br/>thread-safe]
            ECS[EquityCalculatorService<br/>orquestador]
            SB[StrategyBacktester]
            SAS[StrategyAnalyzerService]
            BTS[BankrollTrackerService]
        end

        subgraph Telem["Services/ – telemetría GTO"]
            EC[ExploitabilityCalculator<br/>queue 10K records]
            ACS[AutoCalibrationService]
        end

        subgraph DTOs["DTOs/ y record context"]
            PDI[PostflopDecisionInput<br/>6 required + 36 defaults]
            PDR[PostflopDecisionResult]
            DR[DecisionRequest/Result]
        end

        PK[PokerConstants<br/>~25 const algorítmicas]
    end

    subgraph Domain["OpenScrape.Domain"]
        StrProfile[StrategyProfile<br/>~150 params]
        OpProfile[OpponentProfile]
        VR[VillainRange]
        Thresh[StreetThresholds<br/>+ ThresholdKey]
        SD[StreetDecision]
        BR[BankrollSnapshot]
        Cards[CardDataOuts]
    end

    subgraph Infra["OpenScrape.Infrastructure"]
        Marten[(Marten/Postgres)]
    end

    %% App → DecisionMaker (consumo principal)
    FrmMain --> GC
    GC --> UPC
    UPC --> PDF
    PDF --> PDS
    PDF --> MC
    PDF --> BTA
    PDF --> OC
    PDF --> OT
    PDF --> EC

    %% PostflopDecisionService dependencies
    PDS --> BSS
    PDS --> RP
    PDS --> TR
    PDS --> DPC
    PDS --> IOC
    PDS --> PK
    PDS -.consume.-> PGC
    PDS -.input.-> PDI
    PDS -.output.-> PDR

    %% MC + Outs
    MC --> BHE
    HE --> BHE
    MC --> PK

    %% EquityCalculatorService orquestador
    ECS --> MC
    ECS --> OC
    ECS --> PEC

    %% StrategyBacktester depende de IPostflopDecisionService
    SB --> PDS

    %% AutoCalibrationService depende de ExploitabilityCalculator
    ACS --> EC

    %% BankrollTrackerService consume Marten
    BTS --> Marten

    %% Tracking
    OT -.profile lookup.-> PDS

    %% Domain consumption
    PDS --> StrProfile
    PDS --> OpProfile
    TR --> Thresh
    MC --> VR
    MC --> Cards
    OC --> Cards
    BHE --> Cards
    BTA --> Cards
    PEC --> Cards
    SB --> SD
    BTS --> BR
    OT --> OpProfile

    %% Estilos
    classDef appLayer fill:#e1f5ff,stroke:#0288d1
    classDef algorithmLayer fill:#fff3e0,stroke:#f57c00
    classDef serviceLayer fill:#f3e5f5,stroke:#7b1fa2
    classDef telemetryLayer fill:#fce4ec,stroke:#c2185b
    classDef domainLayer fill:#e8f5e9,stroke:#388e3c
    classDef dtoLayer fill:#f5f5f5,stroke:#616161

    class FrmMain,GC,UPC,PDF appLayer
    class MC,BHE,HE,OC,BTA,PEC algorithmLayer
    class PDS,PA,BSS,DPC,IOC,RP,TR,PGC,OT,ECS,SB,SAS,BTS serviceLayer
    class EC,ACS telemetryLayer
    class StrProfile,OpProfile,VR,Thresh,SD,BR,Cards domainLayer
    class PDI,PDR,DR,PK dtoLayer
```

## Flujo de una decisión postflop completa

```mermaid
sequenceDiagram
    participant FrmMain
    participant GameCoordinator
    participant UnifiedPokerCalc
    participant ECS as EquityCalculatorService
    participant MC as MonteCarloSimulator
    participant OC as OutsCalculator
    participant BTA as BoardTextureAnalyzer
    participant OT as OpponentTracker
    participant PDS as PostflopDecisionService
    participant DPC as DangerPenaltyCalculator
    participant IOC as ImpliedOddsCalculator
    participant TR as ThresholdsRegistry

    FrmMain->>GameCoordinator: ProcessPostFlopAsync(image)
    GameCoordinator->>UnifiedPokerCalc: Calculate(DecisionRequest)
    UnifiedPokerCalc->>OT: GetProfile(villainId)
    OT-->>UnifiedPokerCalc: OpponentProfile (con stats)
    UnifiedPokerCalc->>ECS: CalculateFullEquity(cards, board, opp, pot, call)
    activate ECS
    ECS->>MC: CalculateEquity(...) [MonteCarlo o exact]
    activate MC
    MC->>MC: BuildVillainCombos(rangeOpp)
    alt Community = 5
        MC->>MC: ExactEnumerationRiver C(45,2)=990
    else Community = 4
        MC->>MC: ExactEnumerationTurn 45×C(44,2)=42K
    else Community = 3 o 0
        MC->>MC: Parallel.For MC (50K o 30K)
    end
    MC-->>ECS: EquityResult (equity, distribution, IsReliable)
    deactivate MC
    ECS->>OC: CalculateOuts(cards, board, texture, blocker)
    OC-->>ECS: OutsResult (TotalOuts, EffectiveOuts, draw flags)
    deactivate ECS
    ECS-->>UnifiedPokerCalc: FullEquityAnalysis

    UnifiedPokerCalc->>BTA: Analyze(board)
    BTA-->>UnifiedPokerCalc: BoardTextureResult (Wet/Dry/...)
    UnifiedPokerCalc->>BTA: AnalyzeBoardChange(prev, newCard)
    BTA-->>UnifiedPokerCalc: BoardChangeResult (FlushCompleted, DangerLevel)
    UnifiedPokerCalc->>BTA: ClassifyRiverCard (S22.2)
    BTA-->>UnifiedPokerCalc: RiverCardType (Blank/Neutral/Scare)

    UnifiedPokerCalc->>PDS: DetermineAction(PostflopDecisionInput)
    activate PDS
    PDS->>TR: Get(ThresholdKey(street, situation))
    TR-->>PDS: StreetThresholds
    PDS->>DPC: Calculate(equity, boardChange, blocker, ...)
    DPC-->>PDS: dangerPenalty
    PDS->>IOC: CalculateImpliedOddsFactor(street, IP, flushDraw, SPR)
    IOC-->>PDS: impliedOddsFactor [0.5, 1.0]
    PDS->>IOC: CalculateReverseImpliedOdds(...)
    IOC-->>PDS: reverseImpliedPenalty
    PDS->>PDS: effectiveEquity = equity - danger + comboBonus - reverseImplied (cap, floor)
    PDS->>PDS: Apply ajustes (multiway, 3bet pot, BvB, broadway, kicker, sizing escalation, SPR)
    alt equity < FoldBelow
        PDS->>PDS: HandleLowEquity (semi-bluff, draw call, bluff puro, bluff catch, pot commit)
    else facing bet
        PDS->>PDS: HandleFacingBet (push/fold, raise/call/fold, pot control)
    else no facing bet
        PDS->>PDS: HandleNoBet (check-raise, slow play, probe, value bet, thin value, double barrel)
    end
    PDS-->>UnifiedPokerCalc: PostflopDecisionResult (Action, Reason, IsBluff/IsBarrel/...)
    deactivate PDS
    UnifiedPokerCalc-->>GameCoordinator: DecisionResult
    GameCoordinator-->>FrmMain: actualizar overlay
```

## Path de mixing aleatorio en `PostflopDecisionService`

```mermaid
flowchart LR
    Start([DetermineAction equity calculada]) --> CBet{Hero agresor<br/>+ equity baja<br/>+ HU?}
    CBet -- sí --> Freq1[GetCbetFrequency street<br/>Flop 65% Turn 45% River 30%]
    Freq1 --> Mod1[Modular por textura runout<br/>broadway-wet<br/>villain CheckRaise%]
    Mod1 --> Roll1{Random.Shared<br/>< freq?}
    Roll1 -- sí --> CBetAction[C-Bet IsBarrel=prevBet]
    Roll1 -- no --> Continue1[Continuar]

    CBet -- no --> Continue1
    Continue1 --> CBetMix{Hero agresor<br/>+ equity media<br/>+ HU + no facing bet?}
    CBetMix -- sí --> Roll2{Random<br/>>= freq?}
    Roll2 -- sí --> CheckProtect[Check protección de range]
    Roll2 -- no --> Continue2[Continuar]
    CBetMix -- no --> Continue2

    Continue2 --> CR{CanCheckRaise<br/>+ no agresor<br/>+ no facing bet?}
    CR -- sí --> CRType{Tipo de hand}
    CRType -- TopPair+FlushDraw --> Freq2[CRMixFreqOOPTopPairDraw 35%]
    CRType -- Draw puro --> Freq3[CRMixFreqOOPDraw 30%]
    CRType -- TwoPair+ --> Freq4[CRMixFreqOOPStrong 40%]
    Freq2 & Freq3 & Freq4 --> Roll3{Random<br/>< freq?}
    Roll3 -- sí --> CheckRaise[Check Check-Raise]
    Roll3 -- no --> CheckMix[Check CR mixing]

    CR -- no --> Cont3[Continuar...]
    Cont3 --> Random{equity en<br/>±RandomMargin<br/>de ThinValueAbove?}
    Random -- sí --> Adapt[Bet freq por villainType<br/>LAG 85%, LP 80%,<br/>TAG 60%, TP 55%, Unk 70%]
    Adapt --> Roll4{Random<br/>> freq?}
    Roll4 -- sí --> CheckRand[Check randomización]
    Roll4 -- no --> Bet[Bet thin value]

    Random -- no --> Bet

    classDef randomStep fill:#fff9c4,stroke:#f9a825
    classDef actionStep fill:#c8e6c9,stroke:#388e3c
    classDef checkStep fill:#bbdefb,stroke:#1976d2

    class Roll1,Roll2,Roll3,Roll4 randomStep
    class CBetAction,CheckRaise,Bet,CheckProtect,CheckMix,CheckRand checkStep
```

## Flujo de cálculo de `effectiveEquity`

```mermaid
flowchart TD
    Start([equity raw del MC]) --> Danger[DangerPenaltyCalculator<br/>flushPct/straightPct max<br/>× streetMult Flop 1.3 Turn 1.0 River 0.8]
    Danger --> Skip1{Hero completó<br/>flush/straight?}
    Skip1 -- sí --> NoDanger[penalty = 0 L3]
    Skip1 -- no --> ApplyDanger[effEq = equity - dangerPenalty]
    NoDanger --> ApplyDanger

    ApplyDanger --> Combo{HasComboDraw<br/>+ no River<br/>+ HandRank < Straight?}
    Combo -- sí --> Bonus[+ ComboDrawEquityBonus<br/>× textureMul Dry 1.2 Monotone 0.5 L5]
    Combo -- no --> NoCombo[sin bonus]
    Bonus --> Cap
    NoCombo --> Cap

    Cap{Board flush/straight completed<br/>hero no lo tiene<br/>sin facing bet?}
    Cap -- sí --> ApplyCap[effEq = min effEq, DangerCompletedDrawNoBetCap=45]
    Cap -- no --> NoCap[sin cap]
    ApplyCap --> RIO
    NoCap --> RIO

    RIO[ReverseImpliedOdds<br/>turn/river facing bet OnePair/TwoPair<br/>en draw board]
    RIO --> AllIn{IsAnyoneAllIn?}
    AllIn -- sí --> ZeroRIO[reverseImpliedPenalty = 0]
    AllIn -- no --> CalcRIO[Calcular RIO con multiplier por PairClassification<br/>+ blocker reduction + bluff risk S20.3]
    ZeroRIO --> SubRIO
    CalcRIO --> SubRIO

    SubRIO[effEq -= reverseImpliedPenalty]
    SubRIO --> Floor[effEq = Math.Max 0, effEq]
    Floor --> Final([effectiveEquity final])

    classDef calcStep fill:#e3f2fd,stroke:#1976d2
    classDef checkStep fill:#fff3e0,stroke:#f57c00
    classDef finalStep fill:#c8e6c9,stroke:#388e3c

    class Start,Final finalStep
    class Skip1,Combo,Cap,AllIn checkStep
    class Danger,ApplyDanger,Bonus,ApplyCap,RIO,CalcRIO,SubRIO,Floor calcStep
```
