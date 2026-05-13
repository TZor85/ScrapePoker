# Code-Spec Matrix — ScrapePoker

> Mapeo bidireccional entre archivos del legado y la unit (`<output_folder>/<unit>/`) que documenta su comportamiento. Granularidad de specs: **`module`** (configurada en `.reversa/config.toml § [specs]`). Cada módulo del legado tiene su unit con `requirements.md`, `design.md`, `tasks.md`, `decisions.md`, `edge-cases.md`, `legacy-mapping.md`, `questions.md`.
>
> **Cobertura:** 🟢 = la unit cubre el archivo (requirements/design/tasks). 🟡 = cobertura parcial (mencionado en algún artefacto pero sin spec dedicada). 🔴 = lacuna (archivo sin spec). **n/a** = artefacto generado o no productivo (`obj/`, designer files, properties).
>
> Los detalles línea-a-línea se encuentran en `<unit>/legacy-mapping.md`. Esta matriz es el índice de entrada.

---

## Resumen ejecutivo

| Métrica | Valor |
|---------|-------|
| Archivos productivos (.cs no `obj/`) | **228** |
| Archivos cubiertos 🟢 | **218** (95.6 %) |
| Archivos cobertura parcial 🟡 | **5** (2.2 %) |
| Archivos no cubiertos 🔴 | **0** |
| Archivos `n/a` (designer/obj/build) | **5** (2.2 %) |
| Archivos no-`.cs` cubiertos | 21 (JSON estrategia, config, csproj, traineddata) |
| **Cobertura efectiva del legado** | **🟢 ~96 %** |

**Distribución por unit:**

| Unit | Archivos `.cs` | Spec status |
|------|----------------|-------------|
| `OpenScrape.Domain` | 32 | 🟢 100 % |
| `OpenScrape.Infrastructure` | 1 | 🟢 100 % |
| `OpenScrape.Features` | 16 | 🟢 100 % |
| `OpenScrape.DecisionMaker` | 39 | 🟢 100 % |
| `OpenScrape.App` | 140 | 🟢 95 % (5 🟡: helpers FlopHelper sub-folder) |
| **Total productivo** | **228** | **🟢 96 %** |

---

## 1. OpenScrape.Domain ↔ unit `OpenScrape.Domain/`

**Spec folder:** `_reversa_sdd/OpenScrape.Domain/`
**Detalles línea-a-línea:** `OpenScrape.Domain/legacy-mapping.md`

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.Domain/Entities/Card.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Entities/GameSession.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Entities/OpponentProfile.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Entities/OverlayConfig.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Entities/RegionTableMap.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Entities/StrategyProfile.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Entities/Table.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Enums/ActionsResponse.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Enums/BluffConditionType.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Enums/ListRegions.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Enums/OutsDataEnum.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Enums/Positions.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Enums/Styles.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Exceptions/StrategyProfileValidationException.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Mappers/CardDTOMapper.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Mappers/TableDTOMapper.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Dtos/CardDTO.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Dtos/SessionStatsDto.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/Dtos/TableDTO.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/BankrollSnapshot.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/CardDataOuts.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/CategoryStats.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/Hand.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/HandEvaluation.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/HandStrenght.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/PlayerActionSequence.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/PotOddsResult.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/Region.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/StreetDecision.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/TelemetryAggregate.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/ThresholdKey.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/ValueObjects/VillainRange.cs` | `OpenScrape.Domain/` | 🟢 |
| `src/OpenScrape.Domain/OpenScrape.Domain.csproj` | `OpenScrape.Domain/` | 🟢 |

**Total:** 32 archivos `.cs` + 1 csproj. **Cobertura:** 🟢 100 %.

---

## 2. OpenScrape.Infrastructure ↔ unit `OpenScrape.Infrastructure/`

**Spec folder:** `_reversa_sdd/OpenScrape.Infrastructure/`
**Detalles línea-a-línea:** `OpenScrape.Infrastructure/legacy-mapping.md`

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.Infrastructure/Services.cs` | `OpenScrape.Infrastructure/` | 🟢 |
| `src/OpenScrape.Infrastructure/OpenScrape.Infrastructure.csproj` | `OpenScrape.Infrastructure/` | 🟢 |

**Total:** 1 archivo `.cs` + 1 csproj. **Cobertura:** 🟢 100 %. Módulo minimalista (solo `AddDataBase` extension).

---

## 3. OpenScrape.Features ↔ unit `OpenScrape.Features/`

**Spec folder:** `_reversa_sdd/OpenScrape.Features/`
**Detalles línea-a-línea:** `OpenScrape.Features/legacy-mapping.md`

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.Features/Services.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/ActionScenario/ActionScenarioRequest.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/ActionScenario/ActionScenarioUseCases.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/Cards/CardUseCases.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/Cards/GetAll/GetAllCards.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/Cards/GetFlop/GetFlopCards.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/GameRound/GameRoundUseCases.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/GameRound/GetRecentGameRounds.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/RegionsTableMap/GetAll/GetAllRegionTableMap.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/RegionsTableMap/RegionTableMapUseCases.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMap.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMapRequest.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/Table/Get/GetTable.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/Table/GetAll/GetAllTables.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/Table/TableUseCases.cs` | `OpenScrape.Features/` | 🟢 |
| `src/OpenScrape.Features/OpenScrape.Features.csproj` | `OpenScrape.Features/` | 🟢 |

**Total:** 16 archivos `.cs` + 1 csproj. **Cobertura:** 🟢 100 %.

---

## 4. OpenScrape.DecisionMaker ↔ unit `OpenScrape.DecisionMaker/`

**Spec folder:** `_reversa_sdd/OpenScrape.DecisionMaker/`
**Detalles línea-a-línea:** `OpenScrape.DecisionMaker/legacy-mapping.md`

### 4.1 Algorithms

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.DecisionMaker/Algorithms/BitHandEvaluator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/HandEvaluator.cs` | `OpenScrape.DecisionMaker/` | 🟡 (legacy duplicado — Q-DM-01 abierta sobre eliminar) |
| `src/OpenScrape.DecisionMaker/Algorithms/IBoardTextureAnalyzer.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/IHandEvaluator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/IMonteCarloSimulator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/IOutsCalculator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/MonteCarloSimulator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/OutsCalculator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/PreflopEquityCalculator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |

### 4.2 DTOs

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.DecisionMaker/DTOs/DecisionRequest.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/DTOs/PostflopDecisionInput.cs` | `OpenScrape.DecisionMaker/` | 🟢 |

### 4.3 Interfaces

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.DecisionMaker/Interfaces/IAutoCalibrationService.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IBankrollTrackerService.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IBetSizingService.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IDangerPenaltyCalculator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IEquityCalculatorService.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IExploitabilityCalculator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IImpliedOddsCalculator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IOpponentTracker.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IPostflopDecisionService.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IPreflopAnalyzer.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IRangePolarizer.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IStrategyAnalyzerService.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IStrategyBacktester.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/IThresholdsRegistry.cs` | `OpenScrape.DecisionMaker/` | 🟢 |

### 4.4 Services

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.DecisionMaker/Services/AutoCalibrationService.cs` | `OpenScrape.DecisionMaker/` | 🟢 (BUG Q-DM-06) |
| `src/OpenScrape.DecisionMaker/Services/BankrollTrackerService.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/BetSizingService.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/EquityCalculatorService.cs` | `OpenScrape.DecisionMaker/` | 🟢 (Q-DM-11 sobre `RecommendedAction`) |
| `src/OpenScrape.DecisionMaker/Services/ExploitabilityCalculator.cs` | `OpenScrape.DecisionMaker/` | 🟢 (BUG Q-DM-07 BigBlind hardcoded) |
| `src/OpenScrape.DecisionMaker/Services/ImpliedOddsCalculator.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs` | `OpenScrape.DecisionMaker/` | 🟢 (Q-DM-08 persistencia) |
| `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` | `OpenScrape.DecisionMaker/` | 🟢 (god-class 1893 LOC, Q-DM-05/Q-DM-12) |
| `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/PreflopAnalyzer.cs` | `OpenScrape.DecisionMaker/` | 🟢 (Q-DM-03 static vs interface) |
| `src/OpenScrape.DecisionMaker/Services/RangePolarizer.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/StrategyAnalyzerService.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/StrategyBacktester.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/ThresholdsRegistry.cs` | `OpenScrape.DecisionMaker/` | 🟢 |

### 4.5 Misc

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.DecisionMaker/PokerConstants.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj` | `OpenScrape.DecisionMaker/` | 🟢 |

**Total:** 39 archivos `.cs` + 1 csproj. **Cobertura:** 🟢 97 % (1 🟡: `HandEvaluator.cs` legacy con decisión pendiente).

---

## 5. OpenScrape.App ↔ unit `OpenScrape.App/`

**Spec folder:** `_reversa_sdd/OpenScrape.App/`
**Detalles línea-a-línea:** `OpenScrape.App/legacy-mapping.md`

### 5.1 Composition root

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Program.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Configuration/FeatureFlags.cs` | `OpenScrape.App/` | 🟢 (Q-APP-04 GameLoopCoordinator flag) |
| `src/OpenScrape.App/Configuration/GameLoopOptions.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Properties/launchSettings.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Properties/Resources.Designer.cs` | n/a | n/a (autogen) |
| `src/OpenScrape.App/Properties/Settings.Designer.cs` | n/a | n/a (autogen) |
| `src/OpenScrape.App/Properties/Resources.resx` | `OpenScrape.App/` | 🟢 (recurso embebido) |
| `src/OpenScrape.App/Properties/Settings.settings` | n/a | n/a (autogen) |
| `src/OpenScrape.App/appsettings.json` | `OpenScrape.App/` | 🟢 (anomalía DD-18 Q-APP-01) |
| `src/OpenScrape.App/appsettings.Development.json` | `OpenScrape.App/` | 🟢 (gitignored, secretos reales) |
| `src/OpenScrape.App/OpenScrape.App.csproj` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Interfaces/IAddImage.cs` | `OpenScrape.App/` | 🟢 |

### 5.2 Forms (UI)

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Forms/FrmMain.cs` | `OpenScrape.App/` | 🟢 (god-class 4502 LOC, Q-APP-07) |
| `src/OpenScrape.App/Forms/FrmMain.Designer.cs` | n/a | n/a (autogen WinForms) |
| `src/OpenScrape.App/Forms/FrmOverlay.cs` | `OpenScrape.App/` | 🟢 (DD-08 magenta-key) |
| `src/OpenScrape.App/Forms/FrmOverlay.Designer.cs` | n/a | n/a |
| `src/OpenScrape.App/Forms/FrmHandDetail.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Forms/FrmDetectionDebug.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Forms/FrmDetectionDebug.Designer.cs` | n/a | n/a |
| `src/OpenScrape.App/Forms/FormAction.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Forms/FormAction.Designer.cs` | n/a | n/a |
| `src/OpenScrape.App/Forms/FormImage.cs` | `OpenScrape.App/` | 🟢 (path hardcoded Q-APP-05) |
| `src/OpenScrape.App/Forms/FormImage.Designer.cs` | n/a | n/a |
| `src/OpenScrape.App/Forms/FormListApps.cs` | `OpenScrape.App/` | 🟢 (filtro `"NL H"` Q-APP-08) |
| `src/OpenScrape.App/Forms/FormListApps.Designer.cs` | n/a | n/a |

### 5.3 Helpers

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Helpers/AppThemeHelper.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/CaptureWindowsHelper.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/ColorHelper.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/CoordinateScaler.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/EncrypterHelper.cs` | `OpenScrape.App/` | 🟢 (anomalía DD-19 IV fija Q-APP-02) |
| `src/OpenScrape.App/Helpers/HandHelper.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/ImagePreprocessorHelper.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/ObtainActionHelper.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/PlayerRegionParser.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/UserHandHelper.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/WindowsInformationHelper.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Helpers/FlopHelper/FlopAnalyzerHelperReqest.cs` | `OpenScrape.App/` | 🟡 (mencionado, sin spec dedicada) |
| `src/OpenScrape.App/Helpers/FlopHelper/PreFlopRaiser/PreFlopRaiserIPAnalyzerHelper.cs` | `OpenScrape.App/` | 🟡 |
| `src/OpenScrape.App/Helpers/FlopHelper/RaiseOverLimper/RaiseOverLimperIPAnalyzerHelper.cs` | `OpenScrape.App/` | 🟡 |
| `src/OpenScrape.App/Helpers/FlopHelper/RaiseOverLimper/RaiseOverLimperOOPAnalyzerHelper.cs` | `OpenScrape.App/` | 🟡 |

### 5.4 Entities y Models

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Entities/BoardTextures.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Entities/Player.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Entities/PlayerGameState.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Entities/TableScrapeFlopResult.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Models/BestHandResult.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Models/HandEvaluationResult.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Models/NormalizedCard.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Models/Region.cs` | `OpenScrape.App/` | 🟢 |

### 5.5 Aplication (UseCases App-side)

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Aplication/GetCropImageUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/GetHashImageUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/IGetCardsFlopUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/IGetCardsRiverUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/IGetCardsTurnUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/IGetCropImageUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/IGetHashImageUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/ILoadTableMapUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/IOutsCalculatorUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/IPotOddsCalculator.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/ISaveTableMapUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/ISetFlopForceBoardUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/ISetMovementRegionUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/ISetPreflopActionUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/SetFlopForceBoardUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/SetPreflopActionUseCase.cs` | `OpenScrape.App/` | 🟢 (DD-16 scoped no singleton) |
| `src/OpenScrape.App/Aplication/UseCases/BaseRequest.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/BaseResponse.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/GetCardsFlopUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/GetCardsRiverUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/GetCardsTurnUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/GetWindowsScreenUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/LoadTableMapUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/OutsCalculatorUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/PotOddsCalculator.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/SaveTableMapUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/SetMovementRegionUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs` | `OpenScrape.App/` | 🟢 (DD-09 equity cache, DD-12 facade real) |

### 5.6 Aplication.UseCases.Actions (preflop tabla-driven)

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetAction3BetUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetActionCold4BetUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetActionHero3BetAndOpenRaiser4BetUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetActionHeroCallOpenRaiseAndGetSqueezeUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetActionOpenRaiseUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetActionRaiseOverLimperUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetActionRaiseVsSBLimpUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetActionSqueezeUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetActionVs3BetAndCallUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/GetActionVs3BetUseCaseUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetAction3BetUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetActionCold4BetUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetActionHero3BetAndOpenRaiser4BetUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetActionOpenRaiseUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetActionRaiseOverLimperUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetActionRaiseVsSBLimpUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetActionSqueezeUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetActionVs3BetAndCallUseCase.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/Actions/IGetActionVs3BetUseCaseUseCase.cs` | `OpenScrape.App/` | 🟢 |

### 5.7 Services (game loop, OCR, telemetría, persistencia)

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Services/ActionFormatter.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/CardCacheService.cs` | `OpenScrape.App/` | 🟢 (DD-14) |
| `src/OpenScrape.App/Services/ColorDetectionService.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/DetectionLoggerService.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/GameCoordinator.cs` | `OpenScrape.App/` | 🟢 (793 LOC, decision pipeline) |
| `src/OpenScrape.App/Services/GameLoggerService.cs` | `OpenScrape.App/` | 🟢 (DD-15 truncado + acumuladores) |
| `src/OpenScrape.App/Services/GameLoopCoordinator.cs` | `OpenScrape.App/` | 🟢 (DD-04 dormant, Q-APP-04) |
| `src/OpenScrape.App/Services/GameLoopResult.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/GameLoopStateMachine.cs` | `OpenScrape.App/` | 🟢 (DD-07 visibleBoardCards validation) |
| `src/OpenScrape.App/Services/IActionFormatter.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/ICoordinateScaler.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/IFrmOverlay.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/IGameCoordinator.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/IGameLoopCoordinator.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/IOverlayPositioner.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/IPokerDecisionFacade.cs` | `OpenScrape.App/` | 🟢 (DD-12 sin consumidores Q-APP-03) |
| `src/OpenScrape.App/Services/IPostflopContextHolder.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/IScreenReaderService.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/ITableLayoutService.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/IUiSyncService.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/ImageCropperService.cs` | `OpenScrape.App/` | 🟢 (DD-10 dHash 64-bit) |
| `src/OpenScrape.App/Services/LruCache.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/NullUiSyncService.cs` | `OpenScrape.App/` | 🟢 (test/headless) |
| `src/OpenScrape.App/Services/OcrService.cs` | `OpenScrape.App/` | 🟢 (DD-05 lock global, DD-10 cache bicapa) |
| `src/OpenScrape.App/Services/OverlayPositioner.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/PokerDecisionFacade.cs` | `OpenScrape.App/` | 🟢 (DD-12 muerto Q-APP-03) |
| `src/OpenScrape.App/Services/PokerHandEvaluator.cs` | `OpenScrape.App/` | 🟢 (legacy App-side, decisión Q-APP-XX) |
| `src/OpenScrape.App/Services/PositionCalculator.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/PostflopContextHolder.cs` | `OpenScrape.App/` | 🟢 (DD-13 thread-safe Volatile.Read + lock) |
| `src/OpenScrape.App/Services/RegionLookupCache.cs` | `OpenScrape.App/` | 🟢 (DD-14, EC-21 thread-safety reload) |
| `src/OpenScrape.App/Services/ScreenReaderService.cs` | `OpenScrape.App/` | 🟢 (495 LOC, OCR multi-lectura consenso) |
| `src/OpenScrape.App/Services/StrategyProfileService.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/StrategyProfileValidator.cs` | `OpenScrape.App/` | 🟢 (DD-03 fail-fast) |
| `src/OpenScrape.App/Services/TableLayoutService.cs` | `OpenScrape.App/` | 🟢 (624 LOC, dealer/players/positions) |
| `src/OpenScrape.App/Services/UiSyncService.cs` | `OpenScrape.App/` | 🟢 |

### 5.8 Services/Logging

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Services/Logging/TextBoxLogger.cs` | `OpenScrape.App/` | 🟢 (DD-17) |
| `src/OpenScrape.App/Services/Logging/TextBoxLoggerExtensions.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/Logging/TextBoxLoggerOptions.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Services/Logging/TextBoxLoggerProvider.cs` | `OpenScrape.App/` | 🟢 (DD-17 buffer one-shot, EC-16) |

### 5.9 Telemetry

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Telemetry/Histogram.cs` | `OpenScrape.App/` | 🟢 (DD-11 buckets log) |
| `src/OpenScrape.App/Telemetry/IMetricsCollector.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Telemetry/MetricsCollector.cs` | `OpenScrape.App/` | 🟢 (DD-11 17 categorías) |
| `src/OpenScrape.App/Telemetry/MetricsSnapshot.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Telemetry/ScopedMeasurement.cs` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Telemetry/TelemetryCategories.cs` | `OpenScrape.App/` | 🟢 (DD-11 contrato estable) |

### 5.10 Data (assets de estrategia + traineddata)

| Archivo del legado | Unit correspondiente | Cobertura |
|---|---|:-:|
| `src/OpenScrape.App/Data/OpenRaise.json` | `OpenScrape.App/` | 🟢 (estrategia preflop tabla-driven) |
| `src/OpenScrape.App/Data/BBvsSB.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/ThreeBet.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/VsThreeBet.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/Squeeze.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/RaiseOverLimpers.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/RaiseVsSbLimp.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/Cold4Bet.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/FourBet.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/VsSqueeze.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/VsThreeBetAndCall.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/Cartas2.json` | `OpenScrape.App/` | 🟢 (52 cartas) |
| `src/OpenScrape.App/Data/Regiones.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/Regiones3.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/RegionToTest.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/tableMap.json` | `OpenScrape.App/` | 🟢 |
| `src/OpenScrape.App/Data/RaiseOverLimpers.txt` | `OpenScrape.App/` | 🟡 (datos textuales auxiliares) |
| `src/OpenScrape.App/Data/Revision.txt` | `OpenScrape.App/` | 🟡 (notas internas) |
| `src/OpenScrape.App/Resources/tessdata/eng.traineddata` | `OpenScrape.App/` | 🟢 (Q-APP-06 duplicada con `tessdata/`) |
| `src/OpenScrape.App/tessdata/eng.traineddata` | `OpenScrape.App/` | 🟢 (Q-APP-06) |

**Total App:** 140 archivos `.cs` + ~21 assets. **Cobertura:** 🟢 95 % (5 🟡 en `Helpers/FlopHelper/` sub-folder + 2 .txt de datos auxiliares).

---

## 6. Tests y benchmarks (corpus de validación)

Los tests NO son units — son corpus de validación cruzado. Cada test cubre un servicio/algoritmo de las units anteriores. Mapeo informativo:

### 6.1 OpenScrape.App.Tests (50 archivos `.cs`, 645+ tests)

| Archivo de test | Unit cubierta | Cobertura |
|---|---|:-:|
| `OpenScrape.App.Tests/HandEvaluatorTests.cs` | `OpenScrape.DecisionMaker/` (BitHandEvaluator, HandEvaluator) | 🟢 |
| `OpenScrape.App.Tests/MonteCarloSimulatorTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/OutsCalculatorTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/BoardTextureAnalyzerTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/BetSizingServiceTests.cs` + `BetSizingServiceMultiProfileTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/EquityCalculatorServiceTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/DangerPenaltyCalculatorTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/ImpliedOddsCalculatorTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/AutoCalibrationServiceTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 (BUG Q-DM-06) |
| `OpenScrape.App.Tests/BankrollTrackerServiceTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/ExploitabilityCalculatorTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/OpponentTrackerTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/PostflopContextHolderTests.cs` | `OpenScrape.App/` | 🟢 |
| `OpenScrape.App.Tests/PostflopDecisionApiContractTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/PokerDecisionFacadeTests.cs` + `PokerDecisionFacadeTestBuilder.cs` | `OpenScrape.App/` | 🟢 (DD-12 muerto pero testeado) |
| `OpenScrape.App.Tests/DecisionIntegrationTests.cs` + `DecisionMatrixIntegrationTests.cs` | `OpenScrape.DecisionMaker/` + `OpenScrape.App/` | 🟢 (216-case matrix) |
| `OpenScrape.App.Tests/GameLoopStateMachineTests.cs` | `OpenScrape.App/` | 🟢 |
| `OpenScrape.App.Tests/GameLoopCoordinatorLifecycleTests.cs` | `OpenScrape.App/` | 🟢 |
| `OpenScrape.App.Tests/GameCoordinatorDonkBetTests.cs` | `OpenScrape.App/` | 🟢 |
| `OpenScrape.App.Tests/HistogramTests.cs` + `MetricsCollectorTests.cs` | `OpenScrape.App/` | 🟢 (telemetría) |
| `OpenScrape.App.Tests/HandRecordTelemetryPersistenceTests.cs` | `OpenScrape.App/` + `OpenScrape.Domain/` | 🟢 |
| `OpenScrape.App.Tests/OverlayPositionerTests.cs` | `OpenScrape.App/` | 🟢 |
| `OpenScrape.App.Tests/PairClassificationTests.cs` | `OpenScrape.DecisionMaker/` | 🟢 |
| `OpenScrape.App.Tests/PlayerRegionParserTests.cs` | `OpenScrape.App/` | 🟢 |
| `OpenScrape.App.Tests/PositionCalculatorTests.cs` | `OpenScrape.App/` | 🟢 |
| `OpenScrape.App.Tests/ActionFormatterTests.cs` | `OpenScrape.App/` | 🟢 |
| `OpenScrape.App.Tests/*` (resto: ~25 archivos) | varias units | 🟢 |

**Cobertura:** 🟢 100 %. Tests cubren todas las units relevantes del motor + game loop + telemetría.

### 6.2 BenchmarkSuite1 (BenchmarkDotNet)

| Archivo | Unit cubierta | Cobertura |
|---|---|:-:|
| `BenchmarkSuite1/ImageProcessingBenchmark.cs` | `OpenScrape.App/` (ImagePreprocessorHelper, OcrService) | 🟢 |
| `BenchmarkSuite1/BenchmarkSuite1.csproj` | `OpenScrape.App/` | 🟢 |

---

## 7. Artefactos no productivos

| Archivo / patrón | Status | Justificación |
|---|:-:|---|
| `src/**/obj/**` | n/a | Auto-generado por `dotnet build` |
| `src/**/bin/**` | n/a | Output binario |
| `**/Designer.cs` (FrmMain.Designer.cs, FrmOverlay.Designer.cs, etc.) | n/a | Auto-generado por VS WinForms designer; el código manual vive en `*.cs` correspondiente |
| `**/Properties/Resources.Designer.cs` | n/a | Auto-generado por `Resources.resx` |
| `**/Properties/Settings.Designer.cs` | n/a | Auto-generado por `Settings.settings` |
| `**/GlobalUsings.g.cs` | n/a | Auto-generado por SDK |
| `**/AssemblyInfo.cs` | n/a | Auto-generado por SDK |

---

## 8. Specs SDD globales y traceability cruzada

Las units de specs cubren el **comportamiento** del legado. Los siguientes artefactos globales del Reversa cubren la **arquitectura** y son referenciados desde las units:

| Artefacto global | Cubre | Status |
|---|---|:-:|
| `_reversa_sdd/inventory.md` (Scout) | Inventario inicial | 🟢 |
| `_reversa_sdd/dependencies.md` (Scout) | NuGet + transitive | 🟢 |
| `_reversa_sdd/code-analysis.md` (Archaeologist) | Análisis por módulo | 🟢 |
| `_reversa_sdd/data-dictionary.md` (Archaeologist) | Diccionario de datos | 🟢 |
| `_reversa_sdd/flowcharts/*.md` (Archaeologist) | Flowcharts por método clave | 🟢 |
| `_reversa_sdd/domain.md` (Detective) | 64 reglas de negocio | 🟢 |
| `_reversa_sdd/state-machines.md` (Detective) | 7 FSMs | 🟢 |
| `_reversa_sdd/permissions.md` (Detective) | RBAC (sin RBAC actual) | 🟢 |
| `_reversa_sdd/questions.md` (Detective, raíz) | 24 preguntas globales | 🟢 |
| `_reversa_sdd/adrs/*.md` (Detective) | 20 ADRs retroactivos | 🟢 |
| `_reversa_sdd/c4-context.md` / `c4-containers.md` / `c4-components.md` (Architect) | Diagramas C4 | 🟢 |
| `_reversa_sdd/erd-complete.md` (Architect) | ERD Marten + relaciones | 🟢 |
| `_reversa_sdd/architecture.md` (Architect) | Síntesis arquitectural | 🟢 |
| `_reversa_sdd/deployment.md` (Architect) | Deployment Windows desktop | 🟢 |
| `_reversa_sdd/traceability/spec-impact-matrix.md` (Architect) | Capability ↔ Component | 🟢 |
| `_reversa_sdd/traceability/code-spec-matrix.md` (Writer, este archivo) | Archivo legado ↔ Unit | 🟢 |

---

## 9. Lacunas y resolución

### 9.1 Cobertura parcial 🟡 — 5 archivos en `Helpers/FlopHelper/`

| Archivo | Razón | Acción recomendada |
|---|---|---|
| `Helpers/FlopHelper/FlopAnalyzerHelperReqest.cs` | Helper de soporte para análisis de flop pre-DecisionMaker; mencionado en `OpenScrape.App/legacy-mapping.md` pero sin spec dedicada (heurísticas locales que el motor moderno reemplaza) | Validar si el motor postflop ya cubre 100 % la funcionalidad. Si sí, candidato a eliminación; documentar en Q-APP-XX. |
| `Helpers/FlopHelper/PreFlopRaiser/PreFlopRaiserIPAnalyzerHelper.cs` | Idem | Idem |
| `Helpers/FlopHelper/RaiseOverLimper/RaiseOverLimperIPAnalyzerHelper.cs` | Idem | Idem |
| `Helpers/FlopHelper/RaiseOverLimper/RaiseOverLimperOOPAnalyzerHelper.cs` | Idem | Idem |
| `Data/RaiseOverLimpers.txt` y `Data/Revision.txt` | Datos textuales auxiliares (notas, raw data); no parte del runtime | Verificar uso real — posible candidato a eliminar. |

### 9.2 Anomalías documentadas en specs (no son lacunas, son decisiones pendientes)

Cada anomalía 🔴 detectada vive en:
- `_reversa_sdd/<unit>/decisions.md` (DD-NN documenta la anomalía como decisión de facto).
- `_reversa_sdd/<unit>/edge-cases.md` (EC-NN documenta el comportamiento real).
- `_reversa_sdd/<unit>/questions.md` (Q-XX-NN bloquea la decisión humana).

| Anomalía | Unit | DD | EC | Q |
|---|---|---|---|---|
| Credenciales reales en `appsettings.json` | `OpenScrape.App/` | DD-18 | EC-03 | Q-APP-01 |
| `EncrypterHelper` IV fija de 16 ceros | `OpenScrape.App/` | DD-19 | — | Q-APP-02 |
| `PokerDecisionFacade` registrado pero sin consumidores | `OpenScrape.App/` | DD-12 | — | Q-APP-03 |
| `GameLoopCoordinator` apagado por feature flag | `OpenScrape.App/` | DD-04 | — | Q-APP-04 |
| `FormImage` path hardcoded | `OpenScrape.App/` | — | — | Q-APP-05 |
| `eng.traineddata` duplicada (embebida + output) | `OpenScrape.App/` | — | — | Q-APP-06 |
| `FrmMain` god-class 4502 LOC | `OpenScrape.App/` | — | — | Q-APP-07 |
| `HandEvaluator.cs` legacy en DM | `OpenScrape.DecisionMaker/` | — | — | Q-DM-01 |
| `PostflopDecisionService` 1893 LOC + `goto` | `OpenScrape.DecisionMaker/` | — | EC-07/EC-08 | Q-DM-05/Q-DM-12 |
| `AutoCalibrationService.PreviewAndApply` BUG | `OpenScrape.DecisionMaker/` | — | EC-09 | Q-DM-06 |
| `ExploitabilityCalculator.BigBlind=1.0` hardcoded | `OpenScrape.DecisionMaker/` | — | EC-10 | Q-DM-07 |
| `OpponentTracker` no persistente | `OpenScrape.DecisionMaker/` | — | — | Q-DM-08 / Q-FSM-02 (raíz) |
| Sin validación de input en `DetermineAction` | `OpenScrape.DecisionMaker/` | — | EC-01/EC-02/EC-16 | Q-DM-09 |
| `obj/Debug/net{8,9}.0/` versionados | `OpenScrape.DecisionMaker/` | — | — | Q-DM-13 |

---

## 10. Cobertura por confianza global

| Confianza | Archivos | % |
|-----------|----------|---|
| 🟢 CONFIRMADO (cubierto por unit + verificado en código) | 218 | 95.6 % |
| 🟡 INFERIDO / parcial (mencionado, sin spec dedicada) | 5 | 2.2 % |
| 🔴 LACUNA (sin cobertura) | 0 | 0 % |
| **n/a** (auto-gen / build) | 5 explícitos + N implícitos en `obj/` | n/a |

**Cobertura efectiva del legado productivo: 🟢 ~96 %.**

---

## 11. Cómo usar esta matriz

**Para reimplementar:** comienza por `<unit>/requirements.md`, luego `design.md`, luego `tasks.md`. Cada task referencia el archivo legado de origen.

**Para refactorizar un archivo del legado:** búscalo en esta matriz, abre su unit, revisa `decisions.md` (porqués) y `edge-cases.md` (qué no romper), antes de tocar el código.

**Para auditar cobertura:** filtra por 🟡/🔴 — son los archivos con riesgo de drift entre código y spec. Resuélvelos vía `questions.md` de la unit o ampliando el `legacy-mapping.md`.

**Para validar fidelidad tras reimplementación:** la batería de 645+ tests en `OpenScrape.App.Tests/` actúa como contrato cruzado. Cualquier reimpl debe mantener todos los tests verdes.
