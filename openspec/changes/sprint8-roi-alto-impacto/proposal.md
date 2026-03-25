# Sprint 8 — Mejoras ROI Alto Impacto

## Why

Análisis exhaustivo del motor de decisión post-Sprint 7 identifica 6 leaks estratégicos de alto impacto que generan pérdidas de EV estimadas en +7-12% ROI combinado:

1. **All-In EV sin calcular para SPR < 1.5**: El bot usa thresholds binarios (equity > ValueAbove && HandRank >= OnePair) para decidir all-in. Con SPR 1.2 y equity 42%, pot odds son 33% → debería ir all-in (+EV), pero foldea. Peor: AK sin par con equity 65% en SPR 0.8 foldea por no tener OnePair.
2. **Bluff catch ignora tipo de oponente**: El bluff catching no diferencia LAG (bluffea ~40-50%) de Nit (~10-15%). Bluff-catchear contra Nit es -EV, contra LAG debería ser más amplio.
3. **Underbet tell no detectado**: Villain bet < 1/4 pot se clasifica como "Small" (+1 penalty). Un underbet indica debilidad o trampa — requiere tratamiento diferenciado.
4. **Double barrel sin evaluar runout**: El bot barrelea si tiene equity marginal como agresor, sin importar si la carta del turn/river ayudó al villano (overcard, draw completado).
5. **Thin value river demasiado agresivo**: El bot apuesta thin value con ~45% equity en river sin considerar si draws se completaron. En boards con flush/straight completado, thin value frecuentemente pierde contra el rango que llega al river.
6. **Float IP sin exit strategy**: El bot floatea en flop (call con aire + posición) pero en turn no reconoce que fue un float → no apuesta cuando villano chequea, perdiendo la inversión.

## What Changes

- `PostflopDecisionService.GetSPRAdjustment()`: Cálculo explícito de EV(allin) = equity × totalPot - (1-equity) × stackRestante. Si EV > 0 → all-in independiente de HandRank.
- `PostflopDecisionService.HandleLowEquity()` bluff catch: Multiplicador de `bluffCatchThreshold` por `villainType`.
- `BetSizeCategory`: Nuevo valor `Underbet` para bets < 1/4 pot. Lógica especial en `HandleFacingBet`.
- `PostflopDecisionService.HandleNoBet()` double barrel: Evaluar `boardChange` antes de barrelear. Bad runout (overcard, draw completado) → check.
- `PostflopDecisionService.HandleNoBet()` thin value river: Subir umbral cuando board tiene draws completados y hero no los tiene.
- `PostflopDecisionResult.IsFloating`: Propagar a turn vía nuevo parámetro `heroFloatedFlop`. Si villain chequea + heroFloatedFlop → bet automático.

## Capabilities

### Modified Capabilities

- `allin-ev-explicito`: Cálculo EV all-in explícito para SPR < 1.5, reemplaza check binario de HandRank.
- `bluff-catch-por-oponente`: Multiplicador de bluff catch threshold por tipo de oponente (LAG, LP, TAG, TP).
- `underbet-tell`: Nueva categoría `BetSizeCategory.Underbet` con penalty reducida y lógica de raise en facing bet.
- `barrel-vs-runout`: Double barrel condicionado a runout favorable (brick) vs desfavorable (overcard/draw completado).
- `thin-value-river-conservador`: Thin value en river bloqueado cuando board tiene draws completados y hero no los tiene.
- `float-exit-strategy`: Nuevo parámetro `heroFloatedFlop` que activa bet automático en turn si villano chequea.

## Impact

- **`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`**: S8.1, S8.2, S8.3, S8.4, S8.5, S8.6.
- **`src/OpenScrape.DecisionMaker/PokerConstants.cs`**: S8.2, S8.3.
- **`src/OpenScrape.Domain/Enums/BetSizeCategory.cs`** (o donde esté): S8.3.
- **`src/OpenScrape.App/Forms/FrmMain.cs`**: S8.3 (clasificación underbet), S8.6 (propagar IsFloating).
- **`src/OpenScrape.Domain/Entities/StrategyProfile.cs`**: S8.2 parámetros bluff catch por oponente.
- **Tests**: +20-25 tests nuevos cubriendo los 6 cambios.
- **Sin nuevas dependencias externas.**
