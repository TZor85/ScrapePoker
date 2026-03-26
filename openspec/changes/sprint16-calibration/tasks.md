## 1. C-Bet Frequency

- [x] 1.1 Añadir CbetFrequencyFlop (0.65), CbetFrequencyTurn (0.45), CbetFrequencyRiver (0.30) a StrategyProfile.
- [x] 1.2 Implementar GetCbetFrequency(street) en PostflopDecisionService.
- [x] 1.3 En DetermineAction, antes de HandleLowEquity: si agresor + no facing bet + HU + equity dentro de margen → c-bet a su frecuencia.

## 2. Kicker Quality en Facing Bet

- [x] 2.1 Añadir KickerStrongEquityBonus (3.0) y KickerWeakEquityPenalty (2.0) a StrategyProfile.
- [x] 2.2 En DetermineAction, cuando isFacingBet + OnePair: TPTK reduce FoldBelow, TPWK OOP aumenta FoldBelow.

## 3. Opponent Aggression por Posición

- [x] 3.1 Añadir TimesAggressiveIP/OOP y TimesPassiveIP/OOP a OpponentProfile.
- [x] 3.2 Añadir propiedades calculadas AggressionFactorIP y AggressionFactorOOP (min 5 acciones, fallback -1).
- [x] 3.3 Implementar GetTypeForPosition(bool villainIsIP) con fallback a AF global.

## 4. Randomización Adaptativa

- [x] 4.1 En PostflopDecisionService, reemplazar RandomizationBetFrequency fijo por switch de villainType: LAG 0.85, LP 0.80, TP 0.55, TAG 0.60, Unknown 0.70.

## 5. Verificación

- [x] 5.1 Build sin errores.
- [x] 5.2 514 tests existentes pasan.
