## Why

Las decisiones marginales (equity cerca de thresholds) son donde más dinero se gana o pierde. Cuatro áreas de calibración necesitan ajuste fino:

1. **C-bet sin frecuencia propia** — La c-bet como agresor preflop comparte frecuencia con bluff genérico (15% flop). La c-bet real debería ser ~65% en flop.
2. **Kicker ignorado en facing bet** — TPTK y TPWK reciben el mismo threshold de call. TPTK debería ser más fácil de call (+3%), TPWK OOP más difícil (-2%).
3. **AF global para todos los spots** — Villain puede ser LAG en posición pero TAG fuera de posición. Un solo AF no lo captura.
4. **Randomización fija anti-exploit** — El 70% bet frequency en boundary es igual contra todos. Debería ser más agresivo vs LAG (explotar su call freq) y más pasivo vs TAG (proteger rango).

## What Changes

1. **C-bet frequency** — 3 nuevos params (CbetFrequencyFlop=65%, Turn=45%, River=30%). Cuando hero es agresor con equity baja (< FoldBelow pero dentro de -15), c-bet a su frecuencia antes de ir a HandleLowEquity.
2. **Kicker facing bet** — TPTK: `adjustedFoldBelow -= KickerStrongEquityBonus` (3.0). TPWK OOP: `adjustedFoldBelow += KickerWeakEquityPenalty` (2.0). Solo aplica con OnePair facing bet.
3. **AF por posición** — 4 nuevos contadores IP/OOP en OpponentProfile. `GetTypeForPosition(bool villainIsIP)` usa AF posicional con fallback a AF global. `AggressionFactorIP/OOP` propiedades calculadas.
4. **Randomización adaptativa** — Reemplaza `RandomizationBetFrequency` fijo por switch de villain type: LAG 85%, LP 80%, TP 55%, TAG 60%, Unknown 70%.

## Impact

- **`src/OpenScrape.Domain/Entities/StrategyProfile.cs`**: +5 nuevos parámetros (3 c-bet freq, 2 kicker).
- **`src/OpenScrape.Domain/Entities/OpponentProfile.cs`**: +4 contadores IP/OOP, +2 propiedades AF, +1 método GetTypeForPosition.
- **`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`**: C-bet block, kicker adjustment en facing bet, randomización adaptativa por villain type, método GetCbetFrequency.
