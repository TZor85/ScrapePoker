# Flowchart — `MonteCarloSimulator.CalculateEquity`

> Selección híbrida entre enumeración exacta y Monte Carlo paralelizado.
> Ver `_reversa_sdd/code-analysis.md` sección "OpenScrape.DecisionMaker" para descripción narrativa.

## Selección de método

```mermaid
flowchart TD
    Start([CalculateEquity myCards, community, numOpp, iterations, villainRange]) --> BuildCombos{villainRange != null?}
    BuildCombos -- sí --> Build[BuildVillainCombos<br/>expandir AKs/QQ/JTo a combos<br/>descartar combos bloqueados por hero/board]
    BuildCombos -- no --> NoCombos[villainCombos = null]
    Build --> PreWeight[ComputeTotalWeight<br/>una sola vez no por iteración]
    NoCombos --> Blocked
    PreWeight --> Blocked

    Blocked{villainRange != null?}
    Blocked -- sí --> CalcBlocked[CalculateBlockedComboPercentage<br/>iterar todos combos vs hero/board]
    Blocked -- no --> ZeroBlocked[blockedPercentage = 0]
    CalcBlocked --> CommCount
    ZeroBlocked --> CommCount

    CommCount{communityCount?}
    CommCount -- 5 river --> ExactRiver[ExactEnumerationRiver<br/>C 45,2 = 990 manos<br/>determinístico]
    CommCount -- 4 turn --> ExactTurn[ExactEnumerationTurn<br/>45 rivers × C 44,2 ≈ 42K<br/>determinístico]
    CommCount -- 3 flop --> AdaptIter1[GetAdaptiveIterations 3 = 50_000]
    CommCount -- 0 preflop --> AdaptIter2[GetAdaptiveIterations 0 = 30_000]
    CommCount -- otro --> AdaptIter3[default 10_000]

    AdaptIter1 --> MC[RunMonteCarloSimulation paralelizado]
    AdaptIter2 --> MC
    AdaptIter3 --> MC

    ExactRiver --> SetReliable
    ExactTurn --> SetReliable
    MC --> SetReliable

    SetReliable[result.IsReliable = blockedPercentage <= 20%]
    SetReliable --> Final([EquityResult])

    classDef startEnd fill:#c8e6c9,stroke:#388e3c
    classDef exact fill:#e1f5ff,stroke:#0288d1
    classDef mcStep fill:#fff9c4,stroke:#f9a825
    classDef checkNode fill:#e3f2fd,stroke:#1976d2

    class Start,Final startEnd
    class ExactRiver,ExactTurn exact
    class MC mcStep
    class BuildCombos,Blocked,CommCount checkNode
```

## `ExactEnumerationRiver` (5 community cards)

```mermaid
flowchart TD
    Start([ExactEnumerationRiver]) --> HeroHand[Construir heroHand 7 cartas]
    HeroHand --> HeroEval[BitHandEvaluator.EvaluateHandScore<br/>HandScore struct zero-alloc]
    HeroEval --> Blocked[Construir HashSet blocked: hero + community]
    Blocked --> Remaining[Filtrar deck residual: 47 - 5 = 42 a 45 cartas]
    Remaining --> Distribution[handDistribution heroScore.Rank = 1<br/>hero siempre tiene esta mano]

    Distribution --> RangeCheck{villainCombos != null?}

    RangeCheck -- sí, con rango --> WeightedLoop[foreach combo en villainCombos]
    WeightedLoop --> SkipBlocked{combo.Card1 o Card2<br/>en blocked?}
    SkipBlocked -- sí --> NextCombo[continue]
    SkipBlocked -- no --> OppEval[Construir oppHand 7 cartas<br/>EvaluateHandScore]
    OppEval --> Compare[heroScore.CompareTo oppScore]
    Compare --> Accumulate[wins/ties/total += combo.Weight]
    Accumulate --> NextCombo
    NextCombo --> EndLoop1{Más combos?}
    EndLoop1 -- sí --> WeightedLoop
    EndLoop1 -- no --> CalcResult1[Equity = wins+ties/2 / total]

    RangeCheck -- no, sin rango --> EnumLoop[Para i,j en remaining<br/>C remaining,2 hands]
    EnumLoop --> OppEval2[Construir oppHand 7 cartas<br/>EvaluateHandScore]
    OppEval2 --> Compare2[heroScore.CompareTo oppScore]
    Compare2 --> Accumulate2[totalWins/totalTies += 1]
    Accumulate2 --> EndLoop2{Más pares?}
    EndLoop2 -- sí --> EnumLoop
    EndLoop2 -- no --> CalcResult2[Equity = totalWins+totalTies/2 / totalHands]

    CalcResult1 --> Final([EquityResult])
    CalcResult2 --> Final

    classDef startEnd fill:#c8e6c9,stroke:#388e3c
    classDef setupNode fill:#fff3e0,stroke:#f57c00
    classDef loopNode fill:#fff9c4,stroke:#f9a825
    classDef calcNode fill:#e3f2fd,stroke:#1976d2

    class Start,Final startEnd
    class HeroHand,HeroEval,Blocked,Remaining,Distribution setupNode
    class WeightedLoop,SkipBlocked,OppEval,Compare,Accumulate,NextCombo,EnumLoop,OppEval2,Compare2,Accumulate2 loopNode
    class CalcResult1,CalcResult2 calcNode
```

## `RunMonteCarloSimulation` (Flop/Preflop con paralelismo)

```mermaid
flowchart TD
    Start([RunMonteCarloSimulation simulationCount, villainCombos]) --> ParallelFor[Parallel.For 0, simulationCount<br/>localInit: int 3+HandRankCount<br/>localFinally: Interlocked.Add]

    ParallelFor --> SingleSim[RunSingleSimulation por iteración]

    subgraph Single[RunSingleSimulation]
        SinglyStart([RunSingleSimulation]) --> ThreadDeck[ThreadLocal CardDataOuts deck]
        ThreadDeck --> CopyDeck[Array.Copy DeckTemplate, deck]
        CopyDeck --> RemoveKnown[RemoveKnownCards myCards + community]
        RemoveKnown --> CompleteCommunity[Completar 5-community.Count cartas<br/>random del deck residual]
        CompleteCommunity --> HeroEval[ThreadHandBuffer<br/>EvaluateHandScore]
        HeroEval --> OppLoop[Para i = 0; i < numOpponents]

        OppLoop --> RangeOrRandom{villainCombos != null?}
        RangeOrRandom -- sí --> TryDraw[TryDrawFromRange precomputedTotalWeight<br/>20 intentos máximo]
        TryDraw --> DrawSuccess{Draw exitoso?}
        DrawSuccess -- no --> SkipReturn[Return skipped=true<br/>NO contaminar con random]
        DrawSuccess -- sí --> RemoveCards[RemoveCard card1, card2 del deck]
        RangeOrRandom -- no --> RandomDraw[DrawRandomCard ×2]
        RemoveCards --> OppEval
        RandomDraw --> OppEval[OppBuffer<br/>EvaluateHandScore]
        OppEval --> CompareOpp[heroScore.CompareTo oppScore]
        CompareOpp --> CheckLose{cmp < 0?}
        CheckLose -- sí --> Lose[heroLost=true<br/>break]
        CheckLose -- no + cmp=0 --> SetTied[heroTied=true]
        CheckLose -- no + cmp>0 --> Continue
        SetTied --> Continue
        Continue --> NextOpp{Más opponents?}
        NextOpp -- sí --> OppLoop
        NextOpp -- no --> Return
        Lose --> Return
        SkipReturn --> SinglyEnd
        Return[Return wins, ties, bestRank, skipped=false] --> SinglyEnd([fin])
    end

    SingleSim --> AccumLocal[Acumular en local 3+HandRankCount<br/>local 0=wins, 1=ties, 2=skipped, 3+=distribution]

    AccumLocal --> NextIter{Más iteraciones?}
    NextIter -- sí --> ParallelFor
    NextIter -- no --> Aggregate

    Aggregate[localFinally: Interlocked.Add a totalWins/Ties/Skipped/Distribution]
    Aggregate --> EffectiveCount[effectiveCount = simulationCount - totalSkipped]
    EffectiveCount --> CalcEquity[Equity = totalWins+totalTies/2 / effectiveCount]
    CalcEquity --> Final([EquityResult con SkippedSimulations tracking])

    classDef startEnd fill:#c8e6c9,stroke:#388e3c
    classDef parallelNode fill:#ffe0b2,stroke:#fb8c00
    classDef threadLocal fill:#dcedc8,stroke:#689f38
    classDef checkNode fill:#e3f2fd,stroke:#1976d2

    class Start,Final,SinglyStart,SinglyEnd startEnd
    class ParallelFor,AccumLocal,Aggregate parallelNode
    class ThreadDeck,CopyDeck threadLocal
    class RangeOrRandom,DrawSuccess,CheckLose,NextOpp,NextIter checkNode
```

## `BitHandEvaluator.EvaluateHandScore` (zero-alloc)

```mermaid
flowchart TD
    Start([EvaluateHandScore cards]) --> StackAlloc[Span int 15 rankCount<br/>Span int 5 suitCount<br/>int rankBits<br/>Span int 5 suitRankBits]
    StackAlloc --> Scan[Fase 1: escaneo único<br/>foreach card<br/>rankCount r++, suitCount s++,<br/>rankBits |= 1 LL r,<br/>suitRankBits s |= 1 LL r]

    Scan --> FlushDetect[Fase 2: flushSuit = -1<br/>for s = 1..4<br/>si suitCount s >= 5 → flushSuit = s, break]

    FlushDetect --> FlushBranch{flushSuit >= 0?}

    FlushBranch -- sí --> SFCheck[FindStraightHigh suitRankBits flushSuit]
    SFCheck --> SFFound{sfHigh > 0?}
    SFFound -- sí --> SFReturn[Return HandScore StraightFlush, BuildComposite SF, sfHigh]
    SFFound -- no --> FlushReturn[Flush: 5 kickers más altos del flush suit<br/>BuildComposite Flush, fk1..fk5]

    FlushBranch -- no --> Groups[Fase 4: encontrar grupos<br/>for r = 14..2<br/>quadsRank/tripsRank/highPair/lowPair]
    Groups --> StraightDetect[Fase 5: straightHigh = FindStraightHigh rankBits<br/>mask 0x1F LL high-4 desde 14..6, luego WheelMask]

    StraightDetect --> Quads{quadsRank > 0?}
    Quads -- sí --> QuadsReturn[FourOfAKind con kicker]
    Quads -- no --> FullHouse

    FullHouse{tripsRank > 0?}
    FullHouse -- sí + pairRank --> FHReturn[FullHouse tripsRank, pairRank]
    FullHouse -- no --> Straight1

    Straight1{straightHigh > 0 + tripsRank=0?}
    Straight1 -- sí --> StrReturn1[Straight straightHigh]
    Straight1 -- no --> Trips

    Trips{tripsRank > 0?}
    Trips -- sí + straight prio --> StrReturn2[Straight gana sobre trips]
    Trips -- sí --> TripsReturn[ThreeOfAKind con 2 kickers]
    Trips -- no --> Straight2

    Straight2{straightHigh > 0?}
    Straight2 -- sí --> StrReturn3[Straight straightHigh]
    Straight2 -- no --> TwoPair

    TwoPair{highPair > 0 + lowPair > 0?}
    TwoPair -- sí --> TPReturn[TwoPair con kicker]
    TwoPair -- no --> OnePair

    OnePair{highPair > 0?}
    OnePair -- sí --> OPReturn[OnePair con 3 kickers]
    OnePair -- no --> HighCard

    HighCard[HighCard con 5 kickers]
    HighCard --> HCReturn[Return HighCard composite]

    SFReturn --> Final
    FlushReturn --> Final
    QuadsReturn --> Final
    FHReturn --> Final
    StrReturn1 --> Final
    StrReturn2 --> Final
    TripsReturn --> Final
    StrReturn3 --> Final
    TPReturn --> Final
    OPReturn --> Final
    HCReturn --> Final([HandScore struct]) 

    classDef startEnd fill:#c8e6c9,stroke:#388e3c
    classDef stackAllocNode fill:#dcedc8,stroke:#689f38
    classDef bitOps fill:#fff3e0,stroke:#f57c00
    classDef returnNode fill:#ffccbc,stroke:#d84315

    class Start,Final startEnd
    class StackAlloc,Scan stackAllocNode
    class FlushDetect,SFCheck,Groups,StraightDetect bitOps
    class SFReturn,FlushReturn,QuadsReturn,FHReturn,StrReturn1,StrReturn2,StrReturn3,TripsReturn,TPReturn,OPReturn,HCReturn returnNode
```

> **HandScore.BuildComposite** codifica `rank<<20 | k1<<16 | k2<<12 | k3<<8 | k4<<4 | k5` en un único `long`. Comparar manos = `long.CompareTo` → O(1). Crítico para MC con ~42K evaluaciones.
