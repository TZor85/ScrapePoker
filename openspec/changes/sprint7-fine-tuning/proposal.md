# Sprint 7 — Fine-tuning

## Why

Cuatro ajustes de bajo impacto individual pero acumulativo que refinan la precisión del motor:

1. **Implied odds interpolación lineal**: Entre SPR 2.0 y 4.0 la interpolación es lineal, pero los implied odds reales crecen más rápido entre SPR 3.0-4.0 que entre 2.0-3.0. Una interpolación cuadrática (`Math.Sqrt`) captura mejor la curva real.

2. **Tainted outs descuento uniforme**: El descuento es siempre 0.5× sin importar qué mano completa cada out. Un out que da trips al villano cuando hero hace flush merece ~0.7× (hero gana). Uno que da flush al villano cuando hero hace trips merece ~0.3× (hero pierde).

3. **Bluff frequency sin modular por SPR**: Con SPR corto (<2) los bluffs son menos rentables (menos fold equity, más committed); con SPR profundo (>4) los bluffs son más rentables. La frecuencia debería escalar.

4. **FoldBelow=0 en RaiseOverLimper**: `Turn_RaiseOverLimper` y `River_RaiseOverLimper` tienen `FoldBelow: 0`, lo que significa que el bot nunca foldea postflop contra limpers incluso con 5% equity. Es excesivamente agresivo.

## What Changes

- `ImpliedOddsCalculator`: Interpolación cuadrática (`Math.Sqrt`) en vez de lineal para SPR 2.0-4.0.
- `OutsCalculator`: Descuento de tainted outs variable según contexto (hero mejora más o menos que villano).
- `PostflopDecisionService`: Bluff frequency multiplicada por factor SPR.
- `appsettings.json`: Corregir FoldBelow en Turn/River_RaiseOverLimper.

## Capabilities

### Modified Capabilities
- `implied-odds-factor`: Interpolación cuadrática para SPR medio (2.0-4.0).
- `tainted-outs-discount`: Descuento variable según relación de mejora hero/villano.
- `bluff-frequency`: Modulada por SPR (corto ×0.5, profundo ×1.2).
- `raise-over-limper-config`: FoldBelow corregido a valores razonables.

## Impact

- **`ImpliedOddsCalculator.cs`** — S7.1
- **`OutsCalculator.cs`** — S7.2
- **`PostflopDecisionService.cs`** — S7.3
- **`StrategyProfile.cs`** — S7.3 (nuevos parámetros SPR bluff)
- **`appsettings.json`** — S7.4
- **Sin nuevas dependencias externas.**
