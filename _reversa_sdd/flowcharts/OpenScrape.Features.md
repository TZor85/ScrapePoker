# Flowchart — `OpenScrape.Features`

> Diagramas Mermaid del módulo de casos de uso scoped.
> Referencia cruzada: `_reversa_sdd/code-analysis.md` § "Módulo: OpenScrape.Features".

---

## 1. Vista de capas y dependencias

```mermaid
flowchart TB
    subgraph App["OpenScrape.App (consumidor)"]
        FrmMain[FrmMain.cs]
        SetPreflopUC[SetPreflopActionUseCase]
        ActionsWrap[GetAction*UseCase wrappers]
    end

    subgraph Features["OpenScrape.Features"]
        direction TB
        Services[Services.AddUseCases]
        subgraph ActionScenarioF["ActionScenario/"]
            ASUC[ActionScenarioUseCases record]
            GAS[GetActionScenario]
            ASR[ActionScenarioRequest]
        end
        subgraph TableF["Table/"]
            TUC[TableUseCases record]
            GT[GetTable]
            GAT[GetAllTables vacío]
        end
        subgraph CardsF["Cards/"]
            CUC[CardUseCases record]
            GAC[GetAllCards]
            GFC[GetFlopCards dead code]
        end
        subgraph RegionsF["RegionsTableMap/"]
            RUC[RegionTableMapUseCases record]
            URM[UpdateRegionTableMap]
            GARM[GetAllRegionTableMap método comentado]
            URR[UpdateRegionTableMapRequest]
        end
        subgraph GameRoundF["GameRound/"]
            GRUC[GameRoundUseCases record]
            GRGR[GetRecentGameRounds]
        end
    end

    subgraph Domain["OpenScrape.Domain"]
        DEnt[Entities Table Card RegionTableMap GameSession]
        DDto[DTOs CardDTO TableDTO]
        DMap[Mappers ToDto / ToEntity]
        DEnum[Enums GameSituation TablePosition HandSituation]
        DVO[ValueObjects Hand Region PlayerActionSequence]
    end

    subgraph Infra["OpenScrape.Infrastructure"]
        Marten[Marten IDocumentStore]
    end

    FrmMain --> TUC
    FrmMain --> CUC
    FrmMain --> RUC
    FrmMain --> GRUC
    SetPreflopUC --> ASUC
    ActionsWrap --> ASUC

    ASUC --> GAS
    TUC --> GT
    TUC --> GAT
    CUC --> GAC
    RUC --> URM
    RUC --> GARM
    GRUC --> GRGR

    GAS --> ASR
    GAS --> TUC
    URM --> URR
    GT --> DMap
    GAC --> DMap

    GAS -.->|GetDescription| DEnum
    GAS -.->|filtra| DVO
    GT --> DEnt
    GAC --> DEnt
    URM --> DEnt
    URM --> DVO
    GRGR --> DEnt

    GT --> Marten
    GAC --> Marten
    URM --> Marten
    GRGR --> Marten

    Services --> ASUC
    Services --> TUC
    Services --> CUC
    Services --> RUC
    Services --> GRUC

    classDef dead fill:#fbb,stroke:#900,stroke-dasharray:5 5
    class GAT,GFC,GARM dead
```

🔴 Nodos en rojo punteado: **dead code** registrado en DI (`GetAllTables`) o no registrado (`GetFlopCards`, `GetAllRegionTableMap` con método comentado).

---

## 2. Flujo de selección preflop (cadena de fallback)

Resumen del consumidor `SetPreflopActionUseCase` que **encadena** llamadas a `GetActionScenario` con distintas situaciones hasta encontrar una acción ≠ `"Fold"`.

```mermaid
flowchart TD
    Start([PlayerState válido]) --> IsSecond{IsSecondAction?}
    IsSecond -->|sí| Re[Re-evaluación segunda acción<br/>HeroCallOpenRaise+Squeeze /<br/>Hero3Bet+OR4Bet /<br/>OpenRaiseVs3Bet /<br/>OpenRaiseVs3BetAndCall]
    IsSecond -->|no o no resuelto| Sq[Squeeze]
    Re --> Resolved{¿Acción resuelta?}
    Resolved -->|sí| End([SetHandSituation + return])
    Resolved -->|no| Sq

    Sq --> SqOk{Acción ≠ vacío?}
    SqOk -->|sí| End
    SqOk -->|no| OR[OpenRaise]

    OR --> OROk{Acción ≠ vacío?}
    OROk -->|sí| End
    OROk -->|no| C4[Cold4Bet]

    C4 --> C4Ok{Acción ≠ vacío?}
    C4Ok -->|sí| End
    C4Ok -->|no| ROL[RaiseOverLimper]

    ROL --> ROLOk{Acción ≠ vacío?}
    ROLOk -->|sí| End
    ROLOk -->|no| TB[3Bet si bet > 1 ∧ no 4Bet]

    TB --> TBOk{Acción ≠ vacío?}
    TBOk -->|sí| End
    TBOk -->|no| None[Action = None / HandSituation = None]
    None --> End
```

🟢 **Patrón de cascada**: cada nivel evalúa una `HandSituation` distinta vía `GetActionScenario`. El primer match no-vacío gana. Si ninguna situación matchea → `"None"`.

---

## 3. Decisión de tipo de sesión Marten

```mermaid
flowchart LR
    UC[Use case Features] --> NeedsWrite{¿Escribe?}
    NeedsWrite -->|sí| LW[LightweightSession<br/>sin tracking]
    NeedsWrite -->|no| QS[QuerySession<br/>read-only]

    LW --> StoreSave[session.Store + SaveChangesAsync]
    QS --> Query[session.Query/Load]

    StoreSave --> Dispose1[Dispose síncrono o<br/>await using en disposal]
    Query --> Dispose2[Dispose síncrono o<br/>await using]

    classDef inconsistent fill:#fef9c3,stroke:#a16207
    class Dispose1,Dispose2 inconsistent
```

🟡 **Inconsistencia**: `GetTable` usa `using` síncrono; `GetRecentGameRounds` usa `await using` async. Ambos válidos pero no uniformes.

---

## 4. Update non-destructive de regiones

```mermaid
flowchart TD
    Start([UpdateRegionTableMapRequest]) --> Load[session.LoadAsync RegionTableMap por Category]
    Load --> Found{¿Encontrado?}
    Found -->|no| NotFound[Result.NotFound]
    Found -->|sí| Find[FirstOrDefault región<br/>con Name == request.Name]
    Find --> Existing{¿Existe región?}
    Existing -->|sí| Remove[Regions.Remove existing]
    Existing -->|no| NewOnly[regionToRemove = null]
    Remove --> Build
    NewOnly --> Build

    Build[Construir nueva Region:<br/>geometría = request<br/>flags Hash/Color/Board/OnlyNumber = preserved<br/>Color/Umbral/InactiveUmbral = request]

    Build --> Add[Regions.Add nueva]
    Add --> Store[session.Store region]
    Store --> Save[await SaveChangesAsync ct]
    Save --> Ok[Result.Success]

    classDef warn fill:#fef9c3,stroke:#a16207
    class Build,NewOnly warn
```

🟡 Si `regionToRemove == null` (creación inicial) → flags `IsHash/IsColor/IsBoard/IsOnlyNumber` quedan en `null` para siempre, aunque el request los traiga. Bug latente.
