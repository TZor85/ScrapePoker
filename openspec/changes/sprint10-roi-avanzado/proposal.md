# Sprint 10 — Mejoras ROI Avanzadas

## Why

Análisis post-Sprint 9 identifica 4 leaks estratégicos que combinados estiman +8-15% ROI:

1. **Preflop equity vs random en vez de VillainRange**: En situaciones sin HandSituation definida (limp pots, raise standard), la equity preflop usa lookup table genérica (vs random). Esto infla la equity 5-15% respecto a la realidad.
2. **Bluff catch ignora runout del river**: Si river es brick (no completa draws), villain probablemente falló su draw → bluff catch más amplio. Si completa flush/straight → restringir. Actualmente el threshold es estático.
3. **Implied odds no ajustan por numOpponents**: En multiway OOP, implied odds son peores (villain detrás puede raise). El ImpliedOddsCalculator no recibe numOpponents.
4. **Check-raise IP nunca ocurre**: Check-raise solo permite OOP. En IP con TwoPair+ o draw fuerte, check-raise es un play premium de valor que el bot pierde.

## What Changes

- `UnifiedPokerCalculator.CalculateFoldEquity()`: Recibe `numOpponents`, reduce fold equity en multiway.
- `UnifiedPokerCalculator.CalculateEquity()`: Preflop fallback usa VillainRange genérico OpenRaise cuando no hay situación específica.
- `PostflopDecisionService.HandleLowEquity()`: Bluff catch modula threshold por `boardChange` del river (brick → ×0.85, draw completado → ×1.15).
- `ImpliedOddsCalculator.CalculateImpliedOddsFactor()`: Recibe `numOpponents`, penaliza implied odds en multiway OOP.
- `PostflopDecisionService.HandleNoBet()`: Check-raise permite IP con TwoPair+ en flop/turn.

## Capabilities

### Modified Capabilities
- `preflop-equity-villainrange`: Preflop fallback usa VillainRange OpenRaise genérico en vez de random.
- `bluff-catch-runout`: Bluff catch threshold modulado por runout (brick vs draw completado).
- `implied-odds-multiway`: Implied odds penalizadas en multiway OOP, bonificadas en multiway IP con draws.
- `checkraise-ip`: Check-raise habilitado IP con mano premium (TwoPair+) como trap.

## Impact

- **`src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs`**: S10.1, S10.1b fold equity multiway.
- **`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`**: S10.2, S10.4.
- **`src/OpenScrape.DecisionMaker/Services/ImpliedOddsCalculator.cs`**: S10.3.
- **`src/OpenScrape.DecisionMaker/PokerConstants.cs`**: S10.2, S10.3.
- **Sin nuevas dependencias externas.**
