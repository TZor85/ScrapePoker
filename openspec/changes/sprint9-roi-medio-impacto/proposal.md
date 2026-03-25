# Sprint 9 — Mejoras ROI Medio Impacto

## Why

Complementando el Sprint 8, se identifican 4 leaks estratégicos de medio impacto que generan pérdidas de EV estimadas en +3-6% ROI combinado:

1. **Fold equity calculada con multipliers estáticos**: El `OpponentTracker` recopila stats granulares (VPIP, PFR, CBet%, Fold-to-Steal%) pero el decision engine solo usa `OpponentType` con multipliers fijos. Un LAG con 80% fold-to-3bet se trata igual que uno con 20%.
2. **Semi-bluff sin verificar fold equity**: Los semi-bluffs con draws se ejecutan sin comprobar si el villano foldea lo suficiente. Contra un LP que nunca foldea, semi-bluff infla el pot con mano débil → -EV.
3. **Check-raise limitado a TwoPair+**: Excluye combo draws fuertes (12+ outs) y flush draws en flop. Un check-raise semi-bluff OOP con flush draw + overcard (52% equity) es un play estándar GTO que el bot nunca hace.
4. **Pot commitment no calculado**: Hero con SPR 0.3 restante puede tener EV(call) > EV(fold) pero el bot foldea porque equity < adjustedFoldBelow. No calcula si ya está pot committed.

## What Changes

- `PostflopDecisionService.DetermineAction()`: Nuevo parámetro opcional `villainFoldToBetPct` del OpponentTracker. Si disponible, reemplaza fold equity estática.
- `PostflopDecisionService.HandleLowEquity()`: Semi-bluff verifica `breakevenFoldEquity` antes de apostar, igual que bluff puro.
- `PostflopDecisionService.HandleNoBet()`: Check-raise ampliado a combo draws (12+ outs) o flush draw con equity suficiente.
- `PostflopDecisionService.HandleFacingBet()`: Antes de fold, calcular `potCommitRatio`. Si > 0.6 y equity > potOdds × 0.8 → call.

## Capabilities

### Modified Capabilities

- `fold-equity-stats-reales`: Fold equity calculada con stats reales del OpponentTracker (VPIP, Fold-to-Steal%) en vez de multipliers fijos.
- `semibluff-fold-equity-check`: Semi-bluffs verifican fold equity mínima antes de ejecutar, evitando -EV contra calling stations.
- `checkraise-con-draws`: Check-raise habilitado para combo draws (12+ outs) y flush draws en flop, no solo TwoPair+.
- `pot-commitment-detection`: Cálculo de pot commitment ratio para evitar folds -EV cuando hero ya invirtió >60% de su stack.

## Impact

- **`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`**: S9.1, S9.2, S9.3, S9.4.
- **`src/OpenScrape.App/Forms/FrmMain.cs`**: S9.1 (propagar villainFoldToBetPct).
- **`src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs`**: S9.1 (nuevo método GetFoldToBetPct).
- **`src/OpenScrape.Domain/Entities/StrategyProfile.cs`**: S9.3, S9.4 parámetros nuevos.
- **Tests**: +15-20 tests nuevos cubriendo los 4 cambios.
- **Sin nuevas dependencias externas.**
