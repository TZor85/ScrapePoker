## 1. Float Exit Abort on Bad Runout

- [ ] 1.1 En `PostflopDecisionService.cs` líneas 600-607, antes de retornar float exit, verificar:
  - `boardChange?.OvercardAppeared == true` → Check (bad runout)
  - `boardChange?.FlushCompleted == true && !heroBlocksDangerSuit` → Check (draw completado)
  - `boardChange?.StraightCompleted == true` → Check (draw completado)
  - `heroHandRank >= HandRank.OnePair` → convertir float exit en value bet (ya no es bluff)
- [ ] 1.2 Añadir parámetros `boardChange`, `heroHandRank`, `heroBlocksDangerSuit` al bloque float exit (ya disponibles en el método).
- [ ] 1.3 Tests: float normal en brick, abort en overcard, abort en flush complete, value bet cuando hero mejoró.

## 2. Multiway Penalty Exponencial + Posicional

- [ ] 2.1 En `PostflopDecisionService.cs` líneas 185-197, reemplazar penalty lineal por:
  - OOP: `penalty = extraOpponents² × MultiwayFoldBelowOOP × 0.5` (cuadrática)
  - IP: mantener lineal `extraOpponents × MultiwayFoldBelowIP`
- [ ] 2.2 Añadir multiplicador de street: flop ×1.0, turn ×1.2, river ×1.4.
- [ ] 2.3 Añadir `MultiwayStreetMultiplierTurn` (1.2) y `MultiwayStreetMultiplierRiver` (1.4) a `StrategyProfile.cs`.
- [ ] 2.4 Tests: 3-way IP vs OOP, 4-way OOP cuadrático, turn/river multiplicadores, heads-up sin penalty.

## 3. 3-Bet Pot Postflop Adjustment

- [ ] 3.1 En `PostflopDecisionService.cs` después de línea 183 (tras multiway), añadir bloque que detecte `situation` como ThreeBet/OpenRaiseVs3Bet/FourBet/Squeeze.
- [ ] 3.2 Aplicar ajustes:
  - ThreeBet/OpenRaiseVs3Bet/Squeeze: `adjustedFoldBelow += ThreeBetPostflopFoldIncrease`, `adjustedThinValueAbove += ThreeBetPostflopValueIncrease`
  - FourBet: `adjustedFoldBelow += FourBetPostflopFoldIncrease`, `adjustedThinValueAbove += FourBetPostflopValueIncrease`
- [ ] 3.3 Añadir 4 parámetros a `StrategyProfile.cs`: `ThreeBetPostflopFoldIncrease` (5.0), `ThreeBetPostflopValueIncrease` (3.0), `FourBetPostflopFoldIncrease` (8.0), `FourBetPostflopValueIncrease` (5.0).
- [ ] 3.4 Añadir los mismos 4 parámetros a `appsettings.json` bajo StrategyProfile.
- [ ] 3.5 Tests: 3bet pot vs open raise, 4bet pot más estricto, squeeze = 3bet, acumulativo con range narrowing.

## 4. All-In Detection

- [ ] 4.1 Añadir `bool IsAnyoneAllIn` a `PostflopGameContext.cs` con reset en `Reset()`.
- [ ] 4.2 En `FrmMain.cs`, en el game loop postflop, detectar all-in: `villainBet >= villainStack` o `villainStack == 0` → `_postflopContext.IsAnyoneAllIn = true`.
- [ ] 4.3 Pasar `isAnyoneAllIn` como parámetro a `DetermineAction()` (añadir al signature).
- [ ] 4.4 En `PostflopDecisionService.cs`:
  - Si `isAnyoneAllIn`: `foldEquity = 0` (no puede foldear).
  - Si `isAnyoneAllIn`: `reverseImpliedPenalty = 0` (no puede apostar más).
  - Ajustar SPR: `effectiveSPR = min(heroStack, villainStack) / potSize` cuando all-in.
- [ ] 4.5 Tests: fold equity desactivada, reverse implied desactivada, SPR ajustado, reset entre manos.

## 5. Check-Raise SPR Guard

- [ ] 5.1 En `PostflopDecisionService.cs` líneas 546-582, antes de los bloques OOP/IP, añadir guard:
  ```
  double spr = heroStack > 0 && potSize > 0 ? (double)(heroStack / potSize) : 99;
  bool lowSPRCommit = spr < _profile.CheckRaiseSPRMinThreshold;
  if (lowSPRCommit && equity < _profile.CheckRaiseLowSPRMinEquity)
      → skip check-raise (no ejecutar, continuar al siguiente decision path)
  ```
- [ ] 5.2 Añadir `CheckRaiseSPRMinThreshold` (1.5) y `CheckRaiseLowSPRMinEquity` (60.0) a `StrategyProfile.cs`.
- [ ] 5.3 Añadir parámetros a `appsettings.json`.
- [ ] 5.4 Tests: SPR >= 2.0 normal, SPR < 1.5 con equity alta permitido, SPR < 1.5 con equity baja bloqueado, draw semi-bluff bloqueado con SPR bajo.

## 6. SPR Smooth Interpolation

- [ ] 6.1 En `GetSPRAdjustment()` (líneas 889-904), reemplazar buckets discretos por interpolación:
  - Push/fold zone (SPR < threshold): `factor = 1 - spr / SPRPushFoldThreshold` → `foldAdjust = -reduction × factor`
  - Deep zone (SPR > threshold): `factor = min((spr - SPRDeepCautionThreshold) / 2.0, 1.0)` → `foldAdjust = increase × factor`
  - Normal zone: sin ajuste (como antes)
- [ ] 6.2 `isPushFold` flag: true solo cuando `spr < SPRPushFoldThreshold × 0.5` (mitad inferior de la zona).
- [ ] 6.3 Aplicar interpolación también a `valueAdjust` en push/fold zone.
- [ ] 6.4 Tests: SPR = 0 (máximo ajuste), SPR = 1.0 (mitad), SPR = 1.9 (casi normal), SPR = 3.0 (normal), SPR = 4.1 (casi deep), SPR = 6.0 (deep máximo).

## 7. Verificación

- [ ] 7.1 Compilar con `dotnet build OpenScrape.sln`.
- [ ] 7.2 Ejecutar tests con `dotnet test OpenScrape.sln`.
- [ ] 7.3 Verificar manualmente que decisiones postflop no regresan en escenarios comunes.
