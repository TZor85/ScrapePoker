# Plan de Mejoras — Motor de Decisión y Estrategia

Análisis realizado: 2026-03-23
Sprint 1 y 2 completados: 2026-03-23
Sprint 3 completado: 2026-03-24
Sprint 4 completado: 2026-03-24

Objetivo: maximizar EV (Expected Value) corrigiendo bugs, calibrando parámetros y mejorando la lógica de decisión.

---

## Sprint 1: Bugs Directos (Máximo Impacto) ✅

### S1.1 — Bluff condition `IPCoordinatedSmallOnly` nunca se dispara ✅
- **Problema:** En `HandleLowEquity`, cuando `!isFacingBet`, `villainBetSize` es siempre `NoBet`. Se pasa a `ShouldBluff()` donde la condición `betSize == BetSizeCategory.Small` nunca es true. El bot nunca bluffea en boards coordinated IP.
- **Ubicación:** `PostflopDecisionService.cs:679` (pasa `villainBetSize`) → `ShouldBluff():744` (chequea `betSize == Small`)
- **Fix:** En `HandleLowEquity`, NO pasar `villainBetSize` a `ShouldBluff`. Reemplazar el parámetro `betSize` en `ShouldBluff` por condiciones relevantes al board sin facing bet (ej: `boardTexture == "Coordinated" && isInPosition`).
- **Impacto:** Alto — recupera frecuencia de bluffs en coordinated boards IP que actualmente es 0%.

### S1.2 — Danger penalty suma porcentuales cuando flush Y straight completan ✅
- **Problema:** Si flush Y straight completan en el mismo turn/river, aplica `equity×25% + equity×18% = 43%` de penalty. En la práctica, el villano tiene UNA de las dos manos (no ambas simultáneamente). La suma duplica el riesgo.
- **Ubicación:** `DangerPenaltyCalculator.cs:28-34`
- **Fix:** Usar `Math.Max(flushPenalty, straightPenalty)` en vez de sumarlas. El villano representará el draw más fuerte, no ambos.
- **Impacto:** Alto — evita foldear manos ganadoras en boards doble-completados (ej: 2h3h4d5h turn completa flush Y straight).

### S1.3 — Calibración: `DangerFlushCompletePct` demasiado bajo ✅
- **Problema:** Con 25%, si hero tiene 70% equity y flush completa, penalty = 17.5 puntos → effectiveEquity = 52.5%. Esto es insuficiente: cuando 4+ cartas del mismo palo están en board, el villano tiene flush ~35-40% del tiempo en rangos normales.
- **Ubicación:** `StrategyProfile.cs:44`
- **Fix:** Subir `DangerFlushCompletePct` de 25 a 35.
- **Impacto:** Alto — evita value bets suicidas en boards con flush completado.

### S1.4 — Calibración: Reverse implied odds penalties insuficientes ✅
- **Problema:** `ReverseImpliedFlushDrawPenalty = 4.0` y `ReverseImpliedCoordinatedPenalty = 2.0` son demasiado bajos. Con OnePair/MiddlePair facing turn bet en board con flush draw aparecido, hero pierde mucho más de 6 puntos de equity efectiva en calles futuras.
- **Ubicación:** `StrategyProfile.cs:95-96`
- **Fix:** Subir `ReverseImpliedFlushDrawPenalty` de 4.0 a 7.0 y `ReverseImpliedCoordinatedPenalty` de 2.0 a 4.0.
- **Impacto:** Alto — reduce calls marginales con manos vulnerables que terminan pagando rivers caros.

---

## Sprint 2: Calibración + Lógica ✅

### S2.1 — Combo draw bonus se aplica cuando draw ya completó ✅
- **Problema:** Si hero tiene flush completado (HandRank >= Flush) y `hasComboDraw = true`, el bonus +6% se suma a equity ya alta. El bonus es para semi-bluffs con draws NO completados; si ya completaste, el bonus es redundante y distorsiona sizing.
- **Ubicación:** `PostflopDecisionService.cs:117-119`
- **Fix:** Agregar condición: solo aplicar si hero NO completó el draw:
  ```
  if (hasComboDraw && street != River &&
      heroHandRank < HandRank.Straight)
  ```
- **Impacto:** Medio — evita overbets innecesarios con nuts (el valor ya está en la equity base).

### S2.2 — `villainAggressorCheckedPreviousStreet` no se pasa en river ✅
- **Problema:** En river, la llamada a `DetermineAction` no incluye el parámetro `villainAggressorCheckedPreviousStreet`. El probe bet path (path 4 del PostflopDecisionService) nunca se activa en river, perdiendo oportunidades de bet cuando el villano agresor mostró debilidad en turn.
- **Ubicación:** `FrmMain.cs` — llamada a `DetermineAction` en `ProcessRiverAsync`, falta el parámetro.
- **Fix:** Agregar `villainAggressorCheckedPreviousStreet: _postflopContext.VillainBetTurn == false && !isPreflopAggressor` a la llamada de river.
- **Impacto:** Medio — recupera probe bets en river que actualmente se pierden.

### S2.3 — Facing bet penalty escalado por street ✅
- **Problema:** Las penalizaciones por tamaño de bet del villano (`Small+1, Medium+4, Large+8`) son iguales en flop, turn y river. En flop, una large bet es común (range amplio); en river, una large bet es rara (range estrecho, más value). La penalty debería ser mayor en river.
- **Ubicación:** `PostflopDecisionService.cs:139-151`
- **Fix:** Multiplicar penalty por factor de street: Flop ×1.0, Turn ×1.15, River ×1.3. Nuevas constantes `FacingBetTurnMultiplier` y `FacingBetRiverMultiplier` en `PokerConstants.cs`.
- **Impacto:** Alto — foldea menos en flop (donde bets grandes son normales) y más en river (donde representan manos fuertes).

### S2.4 — Calibración de parámetros menores ✅
- **`BluffCatchFoldBelowMultiplier`:** De 0.85 a 0.75. Con 0.85, hero paga bluff catches con equity ~25% contra rangos que son 75%+ value. Con 0.75, el threshold sube a ~22.5%, más selectivo.
  - `StrategyProfile.cs:101`
- **`FloatingIPMinEquity`:** De 20% a 25%. Con 20% equity, hero pierde contra cualquier par; 25% requiere al menos outs reales.
  - `StrategyProfile.cs:112`
- **`ImpliedOddsSPRShallowFactor`:** De 0.95 a 0.98. Con SPR < 2 (casi committed), implied odds prácticamente no existen.
  - `StrategyProfile.cs:61`
- **`SlowPlayMinEquity`:** De 80% a 72%. Con 80%, slow play casi nunca ocurre; 72% permite slow play con sets en boards secos.
  - `StrategyProfile.cs:116`

---

## Sprint 3: Mejoras Estructurales ✅

### S3.1 — Bluff catch en turn (no solo river) ✅
- **Problema:** `HandleLowEquity` solo permite bluff catch en river (línea 698). En turn, facing small bet con OnePair+ y equity cercana a FoldBelow, hero debería poder call para ver river barato.
- **Ubicación:** `PostflopDecisionService.cs:698`
- **Fix:** Extender bluff catch a turn con condiciones más estrictas: `street == Turn && villainBetSize == Small && heroHandRank >= OnePair && equity >= FoldBelow * 0.90`. Nuevo parámetro `BluffCatchTurnEquityMultiplier = 0.90` en `StrategyProfile`. BottomPair excluido en turn (demasiado débil con 1 calle por venir).
- **Impacto:** Medio — atrapa más bluffs sin exponer a rivers caros (condición estricta).

### S3.2 — Floating IP requiere draw real ✅
- **Problema:** Floating permite `totalOuts >= 4` que incluye overcards débiles sin draw real. Hero float-calls con 9h2d en AcKcQc board porque tiene "4 outs" (overcards), pero no tiene ningún draw legítimo.
- **Ubicación:** `PostflopDecisionService.cs:337-343`
- **Fix:** Requerir `hasRealDraw = (hasFlushDraw || hasComboDraw || totalOuts >= FloatingIPMinOuts)` además de `totalOuts >= 4`. Nuevo parámetro `FloatingIPMinOuts = 6` en `StrategyProfile`. `hasFlushDraw` y `hasComboDraw` propagados a `HandleFacingBet`.
- **Impacto:** Medio — evita floats -EV con manos sin potencial real.

### S3.3 — Fold equity check en bluffs puros ✅
- **Problema:** `ShouldBluff()` decide bluffear basado solo en frecuencia aleatoria, sin verificar si el bluff es matemáticamente rentable. Un bluff de 1/2 pot necesita ~33% fold equity para ser break-even.
- **Ubicación:** `PostflopDecisionService.cs:677-685`
- **Fix:** Antes de retornar bluff, verificar: `foldEquity >= betFraction / (1 + betFraction)` usando `BetStringToFraction()`. Si fold equity insuficiente → check. Parámetro `foldEquity` propagado desde `DetermineAction` → `HandleLowEquity`, y desde `FrmMain` (3 llamadas: flop/turn/river) usando `_streetResult.FoldEquity`.
- **Impacto:** Medio — elimina bluffs -EV contra calling stations.

### S3.4 — Board texture "Monotone" con sizing específico ✅
- **Problema:** Boards con 3+ cartas del mismo palo se clasifican como "Dry" o "Coordinated" sin sizing propio. C-bet en monotone board debería ser menor (1/4-1/3 pot) porque flush draw es muy probable para el villano.
- **Ubicación:** `PostflopDecisionService.cs:422-434`, `BoardTextureAnalyzer.cs`
- **Fix:** Agregar case `"Monotone"` en el switch de baseBetThreshold con sizing `"Bet 1/4"`. Nuevo campo `MonotoneBoardBetSize` en `StreetThresholds`. `SimplifiedTexture` ahora mapea `IsMonotone → "Monotone"` (antes caía a "Coordinated"). Actualizado `appsettings.json` con `MonotoneBoardBetSize` en todos los thresholds.
- **Impacto:** Medio — sizing más eficiente en boards mono-palo.

### S3.5 — Preflop equity vs VillainRange (no vs random) ✅
- **Problema:** La equity preflop siempre se calcula contra rango aleatorio (`_preflopEquityCalculator.GetEquity(hand, numOpponents)`). En situaciones 3Bet/4Bet, el rango del villano es mucho más estrecho, y la equity debería reflejar eso.
- **Ubicación:** `UnifiedPokerCalculator.cs:189-195`
- **Fix:** En `CalculateEquity`, cuando preflop y `VillainRange.GetForSituation(situation)` retorna rango, usar `MonteCarloSimulator.CalculateEquity()` con community cards vacía y el rango filtrado. Situaciones sin rango definido (OpenRaise) siguen usando lookup table.
- **Impacto:** Alto — mejora significativamente decisiones preflop en pots 3bet+.

---

## Sprint 4: Contexto Cross-Street ✅

### S4.1 — Rastrear villain bet SIZE por street ✅
- **Problema:** `PostflopGameContext` solo guarda `VillainBetFlop: bool` / `VillainBetTurn: bool`. No guarda el tamaño (`BetSizeCategory`). Turn/River no pueden detectar "villain siempre bet small" vs "villain escaló sizing" (sizing tells).
- **Ubicación:** `PostflopGameContext.cs:13-22`
- **Fix:** Agregados `VillainBetSizeFlop` y `VillainBetSizeTurn` como `BetSizeCategory` en `PostflopGameContext`. FrmMain guarda betSize al actualizar contexto. `PostflopDecisionService` detecta sizing tells: villain escaló bet size → `adjustedFoldBelow += VillainSizingEscalationPenalty` (4.0). Nuevos parámetros `villainBetSizeFlop`/`villainBetSizeTurn` en `DetermineAction`. Propagados desde FrmMain en turn y river.
- **Impacto:** Medio — mejor lectura del oponente.

### S4.2 — Propagar board danger desde flop ✅
- **Problema:** `CombineBoardChanges()` solo combina turn + river. El flop siempre pasa `BoardChangeResult.Safe`, así que peligros del flop (ej: board paired desde el inicio, 2-tone flop) no se propagan.
- **Ubicación:** `FrmMain.cs:1316` (flop hardcoded Safe), `PostflopGameContext.CombineBoardChanges`
- **Fix:** Nuevo método `BoardTextureAnalyzer.AnalyzeInitialBoard()` genera `BoardChangeResult` para el estado base del flop (flush draw, paired, connected). Nuevo campo `PostflopGameContext.InitialBoardDanger`. FrmMain analiza flop con `AnalyzeInitialBoard()` y combina con turn via `CombineBoardChanges(InitialBoardDanger, turnChange)`. River ya combina `LastBoardChange` que ahora incluye peligro acumulado desde flop. Interfaz `IBoardTextureAnalyzer` actualizada.
- **Impacto:** Medio — mejora acumulación de peligro entre streets.

### S4.3 — Extraer `villainStack` de Players ✅
- **Problema:** Siempre se pasa `villainStack: 0` al calculador. El stack del villano es relevante para decisiones SPR y all-in sizing.
- **Ubicación:** `FrmMain.cs:1263, 1576, 1654, 1935` (parameter `villainStack: 0`)
- **Fix:** Nuevo helper `GetVillainStack()` extrae max stack de oponentes activos. Reemplazados los 4 puntos de `villainStack: 0` por `villainStack: GetVillainStack()`. Habilita cálculo SPR con effective stack real en `UnifiedPokerCalculator`.
- **Impacto:** Bajo-Medio — complementa cálculos SPR existentes.

---

## Resumen de Impacto Estimado

| Sprint | Items | Impacto EV estimado | Esfuerzo |
|--------|-------|---------------------|----------|
| Sprint 1 ✅ | S1.1-S1.4 | +15-20% ROI | Medio |
| Sprint 2 ✅ | S2.1-S2.4 | +8-12% ROI | Medio |
| Sprint 3 ✅ | S2.3, S3.1-S3.5 | +5-8% ROI | Alto |
| Sprint 4 ✅ | S4.1-S4.3 | +2-4% ROI | Medio |
| **Total** | **16 items** | **+30-44% ROI** | |

---

## Notas Técnicas

### Archivos principales afectados
- `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` — S1.1, S2.1, S2.3, S3.1-S3.4
- `src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs` — S1.2
- `src/OpenScrape.DecisionMaker/Services/ImpliedOddsCalculator.cs` — (calibración)
- `src/OpenScrape.Domain/Entities/StrategyProfile.cs` — S1.3, S1.4, S2.4
- `src/OpenScrape.App/Forms/FrmMain.cs` — S2.2, S4.2, S4.3
- `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs` — S4.1
- `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs` — S3.5

### Tests creados/actualizados
- `DangerPenaltyCalculatorTests.cs` — Actualizar test de flush+straight (S1.2) ✅
- `PostflopDecisionServiceTests.cs` — Tests para bluff condition fix (S1.1), combo draw bonus (S2.1), bluff catch turn (S3.1), facing bet scaling (S2.3), floating IP draw real (S3.2), fold equity bluff (S3.3), monotone sizing (S3.4) ✅ (+11 tests Sprint 3)
- `BoardTextureAnalyzerTests.cs` — Actualizado SimplifiedTexture monotone → "Monotone" ✅
- `ImpliedOddsCalculatorTests.cs` — Actualizar por cambios en calibración ✅
- `PreflopAnalyzerTests.cs` — Tests para preflop vs VillainRange (S3.5) — pendiente test dedicado
- `StrategyProfileTests.cs` — Validar nuevos valores de parámetros ✅
- **Total: 403 tests (392 base + 11 Sprint 3), 0 fallos**

### Riesgo de regresión
- **S1.2 (Math.Max penalty):** Puede cambiar decisiones en ~5% de manos con doble-completion. Verificar con tests de integración.
- **S2.3 (facing bet scaling):** Puede causar más folds en river. Monitorizar winrate por street.
- **S2.4 (parámetros):** Cambios acumulativos. Aplicar uno a uno y medir impacto.
- **S3.4 (Monotone):** `SimplifiedTexture` cambió de "Coordinated" a "Monotone" para boards monotone. Verificar que no haya dependencias hardcodeadas a "Coordinated" para estos boards.
- **S3.5 (Preflop MC):** Monte Carlo preflop es más lento que lookup table (~50ms). Monitorizar latencia en game loop.
