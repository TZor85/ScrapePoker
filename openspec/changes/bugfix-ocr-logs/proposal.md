## Why

Análisis de logs reales reveló 4 bugs críticos de OCR/parsing que causan decisiones incorrectas:

1. **Bet OCR pierde decimal separator** — "5.93" se lee como "593", generando pot odds de 96% y folds incorrectos. `SetBetValue` carecía de toda la normalización que `SetStackValue` sí tiene (preprocessing, 3-read consensus, CleanOcrNumericText, NormalizeValue).
2. **River card no detectada pero procesada** — GetCardsRiverUseCase retorna BoardData con nombre vacío, `dataBoard.Count >= 5` pasa pero la carta no tiene valor. El log muestra `Board: [...] + ` (vacío).
3. **Squeeze → 4-bet no detectado** — Cuando hero hace Squeeze y villain re-raises, SetPreflopActionUseCase no tiene caso para `HandSituation.Squeeze` → no transiciona a FourBet. Hero re-apuesta como Squeeze en vez de evaluar como 4-bet pot.

## What Changes

1. **NormalizeBetValue()** — Nuevo método análogo a NormalizeStackValue: detecta decimal separator perdido (>=300 sin decimales o >5× pot), artefacto "8" espurio. Aplicado en SetBetPlayer.
2. **SetBetValue mejorado** — 3-read consensus (preprocessing + inactiveUmbral + directa), CleanOcrNumericText, try/finally dispose.
3. **River card validation** — Contar solo cartas con `!string.IsNullOrEmpty(Name)` para retry, no `dataBoard.Count`.
4. **Squeeze → FourBet** — SetPreflopActionUseCase: condición ampliada para incluir `HandSituation.Squeeze` junto a ThreeBet al detectar 4-bet del villain. Null check en raiser.

## Impact

- **FrmMain.cs**: NormalizeBetValue, SetBetValue reescrito, river card validation.
- **SetPreflopActionUseCase.cs**: Squeeze → FourBet detection.
