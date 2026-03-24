# Plan de Mejoras Avanzadas — Motor de Decisión

Análisis realizado: 2026-03-24
Sprint 5 completado: 2026-03-24
Basado en análisis exhaustivo del código tras completar Sprints 1-4.

Objetivo: corregir leaks estratégicos que el modelo actual no captura, maximizando EV en spots reales de cash game.

---

## Sprint 5: Alto Impacto — Leaks significativos de EV ✅

### S5.1 — Overcards no contadas con OESD/draws ✅

- **Problema:** Si hero tiene AK en board 9-8-7, `OutsCalculator` detecta OESD y deja de contar A/K como overcards (`hasMainDraw = true → skip overcards`). AK en 9-8-7 tiene ~55% equity vs top pair, pero el modelo calcula ~35% (solo straight outs). Subestima equity de manos premium con draws.
- **Ubicación:** `OutsCalculator.cs:86` — `if (!hasMainDraw && !hasMadeHand) { // contar overcards }`
- **Fix:** Separar "clean overcards" (solo overcard, no contribuye al draw) de "overlap outs" (overcard que también es straight out). Contar overcards siempre pero sin doble-contar outs que ya están en el straight draw. Agregar `OvercardOuts` al `DrawResult` para desglosar.
- **Impacto:** Alto — AK, AQ, KQ con draws recuperan 6-10 puntos de equity que actualmente se ignoran.

### S5.2 — Danger penalty no escalada por street ✅

- **Problema:** `DangerFlushCompletePct = 35%` se aplica igual en flop, turn y river. En flop quedan 2 calles de variancia (el villano puede mejorar más), en river es definitivo. El modelo sobre-penaliza en river y sub-penaliza en flop.
- **Ubicación:** `DangerPenaltyCalculator.cs:20-28`
- **Fix:** Multiplicador por street en las penalizaciones porcentuales: Flop ×1.3, Turn ×1.0, River ×0.8. No afecta a las penalizaciones flat (BoardPaired, Overcard, FlushDraw) que son fijas.
- **Impacto:** Alto — corrige ~5% de decisiones donde hero debería proteger más en flop o pagar más en river.

### S5.3 — Multiway sin awareness de posición ✅

- **Problema:** Con 3+ oponentes, el ajuste es plano: `+4 FoldBelow por oponente extra`. Pero IP en 3-way permite aislar; OOP en 3-way es extremadamente vulnerable. El bot juega igual en ambas.
- **Ubicación:** `PostflopDecisionService.cs:173-178` — `MultiwayFoldBelowPerOpponent = 4.0`
- **Fix:** Diferenciar: Multiway OOP → +6/oponente. Multiway IP → +2/oponente. Bluff frequency → 0 con 3+ oponentes OOP (no bluffear multiway sin posición). Nuevas constantes `MultiwayFoldBelowOOP` y `MultiwayFoldBelowIP` en `PokerConstants`.
- **Impacto:** Alto — OOP multiway es el spot más costoso del poker; reducir leaks aquí tiene impacto desproporcionado.

### S5.4 — Separar SemiWet y Wet en board texture sizing ✅

- **Problema:** `SimplifiedTexture` mapea SemiWet (35-60 wetness) y Wet (60+) a "Coordinated" con el mismo sizing. En boards Wet (60+), los draws del villano son mucho más frecuentes → sizing debería ser menor para no inflar pote contra ranges con equity real.
- **Ubicación:** `BoardTextureAnalyzer.cs:23-28` — `SimplifiedTexture`, `PostflopDecisionService.cs:453-459` — switch de boardTexture
- **Fix:** Nuevo valor "Wet" en `SimplifiedTexture`. Nuevo campo `WetBoardBetSize` en `StreetThresholds` (default "Bet 1/3"). Agregar case "Wet" en HandleNoBet. SemiWet sigue siendo "Coordinated" (Bet 1/2).
- **Impacto:** Alto — boards 60+ wetness representan ~15% de los flops; sizing incorrecto en todos ellos.

### S5.5 — Completar configuraciones faltantes de Flop ✅

- **Problema:** Flop tiene 10 configs pero le faltan `Flop_DonkBet` y `Flop_DonkBetVsOpenRaise` que sí existen en Turn y River. Cuando el villano hace donk bet en flop, el sistema usa el fallback genérico (FoldBelow=40) que no tiene calibración para este spot. El donk bet en flop es un spot común (~10-15% de manos) con estrategia muy diferente al OpenRaise.
- **Ubicación:** `appsettings.json` — falta `Flop_DonkBet` y `Flop_DonkBetVsOpenRaise`
- **Fix:** Agregar ambas configuraciones con thresholds calibrados:
  - `Flop_DonkBet`: FoldBelow bajo (32), CanBluff=true, CanCheckRaise=true (donk bets suelen ser débiles)
  - `Flop_DonkBetVsOpenRaise`: FoldBelow medio (38), hero fue agresor → puede raise vs donk
- **Impacto:** Alto — cada mano con donk bet en flop usa fallback subóptimo.

---

## Sprint 6: Medio Impacto — Optimizaciones concretas

### S6.1 — Bet-Check-Bet ≠ Barrel

- **Problema:** `villainBarreling` se activa si villano apostó 2+ calles, pero no distingue bet-bet (barrel real, rango fuerte) de bet-check-bet (draw fallido que reintenta). La penalty (+5 FoldBelow) se aplica incorrectamente al patrón bet-check-bet que indica rango diferente (más débil, a menudo missed draw bluffeando river).
- **Ubicación:** `PostflopDecisionService.cs:193-198`, `FrmMain.cs:1423,1022`
- **Fix:** Agregar `VillainCheckedMiddleStreet` flag en `PostflopGameContext`. Detectar patrón bet-check-bet y aplicar penalización distinta (menor, ~+2 en vez de +5) o incluso bonus de call (villain probablemente bluffeando).
- **Impacto:** Medio — ~8% de manos en river tienen patrón bet-check-bet.

### S6.2 — Reverse implied odds con contexto de blockers

- **Problema:** Penalización reverse implied odds (7.0 flush / 4.0 coordinated) se aplica sin verificar si hero bloquea el draw del villano. Si hero tiene A♠ y el board tiene flush draw en spades, hero reduce combinaciones de flush del villano → penalización debería ser menor.
- **Ubicación:** `ImpliedOddsCalculator.cs:62-75`
- **Fix:** Recibir `heroBlocksDangerSuit` como parámetro. Si hero bloquea palo del draw → penalización ×0.5. Requiere propagar `heroBlocksDangerSuit` desde `DetermineAction` a `CalculateReverseImpliedOdds`.
- **Impacto:** Medio — evita folds incorrectos cuando hero bloquea el draw.

### S6.3 — Hero blocker effect granular (no plano 0.5×)

- **Problema:** Si hero tiene una carta del palo peligroso, la reducción es siempre 50%. Pero el impacto real depende de si es nut blocker (As del palo), cuántas cartas del palo hay en board, y si el board tiene 3 o 4 del palo.
- **Ubicación:** `DangerPenaltyCalculator.cs:45-46`
- **Fix:** Nut blocker (A del palo completado): ×0.35. Non-nut blocker: ×0.55. Board con 4+ del palo (flush visible): ×0.7 (villano casi seguro tiene flush, blocker menos relevante). Nuevo parámetro `DangerNutBlockerReduction` en `StrategyProfile`.
- **Impacto:** Medio — refina ~5% de decisiones en boards con flush completado.

### S6.4 — Probe bet extendido a IP + sizing variable

- **Problema:** Solo permite probe bet OOP (`!isInPosition`). Pero IP leading cuando oponente checkeó es rentable en turn/river. Además, sizing es fijo (1/3) sin adaptar al tipo de oponente.
- **Ubicación:** `PostflopDecisionService.cs:443-451`
- **Fix:** Permitir IP probe bets (eliminar `!isInPosition`). Sizing variable: vs TP/nit → 1/4 (inducir call mínimo), vs LAG → 1/2 (definir mano). Nuevo parámetro `ProbeBetIPSize` en `StreetThresholds`.
- **Impacto:** Medio — recupera value en spots actualmente perdidos con check.

### S6.5 — Slowplay extendida a turn + adaptar a OpponentType

- **Problema:** Solo en flop, solo Dry, solo ThreeOfAKind+, solo no-agresor. En turn con nuts en board seco contra villano agresivo, slowplay induciría bet en river que se paga.
- **Ubicación:** `PostflopDecisionService.cs:408-418`
- **Fix:** Extender a turn con condiciones: board Dry + heroHandRank >= ThreeOfAKind + equity >= 80% + villainType == LAG o Unknown. No permitir slowplay en river (última calle, siempre apostar con nuts).
- **Impacto:** Medio — ~2% de manos tienen nuts en dry turn.

---

## Sprint 7: Bajo Impacto — Fine-tuning

### S7.1 — Implied odds interpolación curva (no lineal)

- **Problema:** Interpolación entre SPR 2.0 y 4.0 es lineal, pero implied odds crece más rápido entre SPR 3.0-4.0.
- **Ubicación:** `ImpliedOddsCalculator.cs:30-36`
- **Fix:** Reemplazar interpolación lineal por `Math.Sqrt((spr - shallow) / (deep - shallow))` × rango.
- **Impacto:** Bajo — refina marginalmente calls de draw en SPR 2.5-3.5.

### S7.2 — Tainted outs descuento variable

- **Problema:** Descuento siempre 0.5× sin importar qué completa cada jugador. Un out que da trips al villano cuando hero hace flush no es igual que viceversa.
- **Ubicación:** `OutsCalculator.cs` — `TaintedOutsDiscount = 0.5`
- **Fix:** Descuento por tipo: hero mejora más que villano → 0.7×; villano mejora más → 0.3×. Requiere analizar qué draw completa cada out para hero vs villano.
- **Impacto:** Bajo — afecta draws poco frecuentes.

### S7.3 — Bluff frequency modulada por SPR

- **Problema:** Con SPR corto (<2), bluffs pierden menos si pagan pero también ganan menos. La frecuencia debería reducirse.
- **Ubicación:** `PostflopDecisionService.cs:678-686`
- **Fix:** Multiplicar bluff frequency por factor SPR: `spr < 2 → freq × 0.5`, `spr > 4 → freq × 1.2`.
- **Impacto:** Bajo — afecta ~5% de manos con SPR extremo.

### S7.4 — FoldBelow=0 en RaiseOverLimper (bug de config)

- **Problema:** `Turn_RaiseOverLimper` y `River_RaiseOverLimper` tienen `FoldBelow: 0`. El bot nunca foldea contra limpers post-flop, incluso con 5% equity. Es excesivamente agresivo.
- **Ubicación:** `appsettings.json:337,596`
- **Fix:** Subir a `FoldBelow: 25` (turn) y `FoldBelow: 30` (river). Mantener modo simplificado.
- **Impacto:** Bajo (situación poco frecuente) pero corrige decisiones claramente -EV.

---

## Resumen de Impacto Estimado

| Sprint | Items | Impacto EV estimado | Esfuerzo |
|--------|-------|---------------------|----------|
| Sprint 5 ✅ | S5.1-S5.5 | +8-12% ROI | Alto |
| Sprint 6 | S6.1-S6.5 | +4-6% ROI | Medio |
| Sprint 7 | S7.1-S7.4 | +1-2% ROI | Bajo |
| **Total** | **14 items** | **+13-20% ROI** | |

---

## Orden de Implementación Recomendado

```
S5.5 Flop_DonkBet configs     → Fácil, impacto inmediato
S5.2 Danger penalty por street → Corrige over/under penalización
S5.1 Overcards con draws       → Subestima AK, AQ, KQ
S5.4 Separar SemiWet/Wet       → Sizing más preciso 15% de flops
S5.3 Multiway position         → Evita leaks OOP multiway
S7.4 FoldBelow=0 bug           → Fix trivial
S6.1 Bet-Check-Bet vs Barrel   → Hand reading más preciso
S6.2 Reverse implied blockers  → Reduce overcalls
S6.4 Probe bet IP              → Recupera value
S6.5 Slowplay turn             → Valor perdido con nuts
S6.3 Hero blocker granular     → Refina decisiones flush boards
S7.1 Implied odds curva        → Fine-tuning SPR
S7.2 Tainted outs variable     → Fine-tuning draws
S7.3 Bluff frequency SPR       → Fine-tuning extremos
```

---

## Notas Técnicas

### Archivos principales afectados
- `OutsCalculator.cs` — S5.1, S7.2
- `DangerPenaltyCalculator.cs` — S5.2, S6.3
- `PostflopDecisionService.cs` — S5.3, S6.1, S6.4, S6.5, S7.3
- `BoardTextureAnalyzer.cs` — S5.4
- `StreetThresholds.cs` — S5.4
- `ImpliedOddsCalculator.cs` — S6.2, S7.1
- `PostflopGameContext.cs` — S6.1
- `PokerConstants.cs` — S5.3
- `appsettings.json` — S5.4, S5.5, S7.4

### Dependencias entre items
- S5.4 (Wet texture) requiere S5.5 o se hace en conjunto (ambos tocan appsettings)
- S6.2 (reverse implied blockers) beneficia de S5.2 (danger penalty por street)
- El resto son independientes entre sí
