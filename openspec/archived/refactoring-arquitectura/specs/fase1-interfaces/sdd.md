# SDD — Fase 1: Interfaces para Servicios de DecisionMaker

## 1. Propósito

Extraer interfaces para los 13 servicios de `OpenScrape.DecisionMaker` que carecen de abstracción, permitiendo:
- Desacoplamiento del consumidor (FrmMain) respecto a implementaciones concretas
- Testeabilidad via mocks/stubs en fases posteriores
- Sustitución de implementaciones sin modificar consumidores

## 2. Alcance

### En alcance
- Crear 13 archivos de interfaz en `src/OpenScrape.DecisionMaker/Interfaces/`
- Hacer que cada clase concreta implemente su interfaz
- Actualizar registro DI en `Program.cs` con patrón forwarding
- Actualizar constructor de `FrmMain` para recibir interfaces
- Actualizar constructor de `StrategyBacktester` para recibir interfaz

### Fuera de alcance
- No se modifica lógica interna de ningún servicio
- No se crean tests nuevos en esta fase
- No se tocan los 4 algoritmos que ya tienen interfaz (IBoardTextureAnalyzer, IHandEvaluator, IMonteCarloSimulator, IOutsCalculator)

## 3. Decisiones de Diseño

### 3.1 Ubicación: directorio `Interfaces/` dentro de DecisionMaker

Los servicios usan dos namespaces distintos:
- `OpenScrape.DecisionMaker.Services` — 10 servicios
- `OpenScrape.DecisionMaker` — 3 servicios (ExploitabilityCalculator, AutoCalibrationService, PokerConstants)

Las interfaces se ubican en `Interfaces/` con namespace `OpenScrape.DecisionMaker.Interfaces` para evitar ambigüedad y facilitar imports.

### 3.2 Métodos estáticos → instancia en interfaz

`DangerPenaltyCalculator`, `ImpliedOddsCalculator` y `PreflopAnalyzer` tienen métodos `static`. Las interfaces los declaran como instancia. Las clases mantienen el método `static` y agregan un wrapper de instancia que delega al estático.

**Razón:** Las interfaces C# no admiten miembros estáticos abstractos útiles para DI. El wrapper es trivial y permite mockear.

### 3.3 Patrón DI: forwarding (clase concreta + interfaz → misma instancia)

```csharp
services.AddSingleton<PostflopDecisionService>();
services.AddSingleton<IPostflopDecisionService>(sp => sp.GetRequiredService<PostflopDecisionService>());
```

**Razón:** Mantiene retrocompatibilidad — cualquier código que resuelva por clase concreta sigue funcionando. Patrón ya usado para los 4 algoritmos existentes.

### 3.4 Dependencias internas mantienen tipo concreto temporalmente

`PostflopDecisionService` recibe `BetSizingService` y `RangePolarizer` en constructor. En esta fase, estos constructores internos NO cambian a interfaz — solo cambian los consumidores externos (FrmMain, StrategyBacktester). Las dependencias internas se migrarán cuando se actualicen los constructores en fase posterior.

**Excepción:** `StrategyBacktester` recibe `PostflopDecisionService` → cambia a `IPostflopDecisionService` porque es un consumidor externo.

## 4. Inventario de Interfaces

| # | Interfaz | Namespace Clase | Métodos | Propiedades |
|---|----------|-----------------|---------|-------------|
| 1 | IPostflopDecisionService | Services | GetThresholds, CalculateDangerPenalty, CalculateImpliedOddsFactor, CalculateReverseImpliedOdds, DetermineAction | — |
| 2 | IBetSizingService | Services | GetValueBetSizes, GetBluffSizes, GetThinValueThreshold, CalculateDynamicBetSize | — |
| 3 | IOpponentTracker | Services | GetProfile, RecordHandPlayed, RecordVPIP, RecordPFR, RecordThreeBet, RecordPostflopAction, RecordCBetOpportunity, RecordFacedCBet, GetAdjustedFoldEquity, GetFoldToBetPct, RegisterSeatAlias, ResolveName, Reset | AllProfiles |
| 4 | IDangerPenaltyCalculator | Services | Calculate | — |
| 5 | IImpliedOddsCalculator | Services | CalculateImpliedOddsFactor, CalculateReverseImpliedOdds | — |
| 6 | IPreflopAnalyzer | Services | IsPreflopAggressor, HasRangeAdvantageOnBoard, CalculateCbetAdjustment, DetectDonkBet, CategorizeOpponentBet | — |
| 7 | IRangePolarizer | Services | GetOptimalRangeType, GetOptimalRangeTypeByWetness, GetThresholdAdjustment, GetThresholdAdjustmentBySituation | — |
| 8 | IStrategyAnalyzerService | Services | AnalyzeSessions, Analyze, GenerateReport | — |
| 9 | IExploitabilityCalculator | DecisionMaker | AnalyzeDecision, IsBetExploitable, IsCallExploitable, RecordDecision, CalculateSessionAnalysis, CalculateGTODistance, ClearRecords | — |
| 10 | IAutoCalibrationService | DecisionMaker | Calibrate, ShouldRecalibrate, RecordDecision, GetPreview, ResetCalibrationHistory, GetDecisionsSinceLastCalibration | — |
| 11 | IBankrollTrackerService | Services | GetBankrollStats, RecordSessionEnd, GetCurrentBankroll, GetPeakBankroll, CalculateRiskOfRuin, GetLimitRecommendation, GetHistory, SetInitialBankroll | — |
| 12 | IEquityCalculatorService | Services | CalculateFullEquity, GetEquityAnalysisSummary | — |
| 13 | IStrategyBacktester | Services | RunBacktest | — |

## 5. Impacto en Archivos

### Nuevos (13)
- `src/OpenScrape.DecisionMaker/Interfaces/IPostflopDecisionService.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IBetSizingService.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IOpponentTracker.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IDangerPenaltyCalculator.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IImpliedOddsCalculator.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IPreflopAnalyzer.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IRangePolarizer.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IStrategyAnalyzerService.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IExploitabilityCalculator.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IAutoCalibrationService.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IBankrollTrackerService.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IEquityCalculatorService.cs`
- `src/OpenScrape.DecisionMaker/Interfaces/IStrategyBacktester.cs`

### Modificados
- 13 clases de servicio — agregar `: IXxxService` a la declaración
- `src/OpenScrape.App/Program.cs` — agregar forwarding por interfaz
- `src/OpenScrape.App/Forms/FrmMain.cs` — campos e inyección por interfaz
- `src/OpenScrape.DecisionMaker/Services/StrategyBacktester.cs` — constructor por interfaz

## 6. Criterios de Verificación

- `dotnet build OpenScrape.sln` — 0 errores
- `dotnet test OpenScrape.sln` — todos los tests pasan
- Cada servicio implementa exactamente su interfaz
- FrmMain no referencia tipos concretos de los 13 servicios (excepto en `using`)
