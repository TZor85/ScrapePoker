# ADR-0013 — Detección de auto-rebuy con umbral 50 BB en `PostflopGameContext`

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** ~2026-03-22 (commit `5e21f14 feat/session: handle auto-rebuy and improve seat logic`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto

## Contexto

Las salas de poker cash game ofrecen **auto-rebuy**: cuando el stack del hero cae bajo cierto umbral (típicamente 100 BB inicial - drawdown), la sala recarga automáticamente a 100 BB sin que el jugador haga nada.

Sin detección de auto-rebuy, el cálculo de `NetProfit` por mano fallaba:

- Hero stack al inicio mano: 60 BB (tras drawdown).
- Hero pierde mano: stack baja a 40 BB.
- Sala detecta drawdown → auto-rebuy a 100 BB.
- Lectura final del stack: 100 BB.
- Cálculo naive: `NetProfit = 100 − 60 = +40`. **Incorrecto.** El hero perdió 20 BB pero el cálculo dice +40.

## Decisión

1. **`PostflopGameContext.HeroStackPreRebuy`** rastrea el stack del hero al inicio de la mano y se mantiene **monotónicamente decreciente** (excepto rebuy).
2. **`AutoRebuyThreshold = 50m` BB** (constante privada). Si entre dos lecturas consecutivas dentro de la misma mano `currentStack - HeroStackPreRebuy ≥ 50`, se interpreta como rebuy.
3. **`TrackHeroStack(currentStack)`** (método del record):
   - Si `HeroStackPreRebuy ≤ 0` → primera lectura: setea `HeroStackPreRebuy = currentStack` y retorna `(this with { HeroStackPreRebuy = currentStack }, currentStack)`.
   - Si `currentStack > HeroStackPreRebuy && currentStack - HeroStackPreRebuy ≥ 50` → rebuy detectado: **preserva** `HeroStackPreRebuy` (no lo actualiza al alza) y retorna `(this, HeroStackPreRebuy)` como `EffectiveStack`.
   - En cualquier otro caso (descenso o incremento pequeño): actualiza `HeroStackPreRebuy = currentStack`.
4. **Reset en cada `NewHand()`.**
5. **`HandRecord.AutoRebuy`** persiste el monto detectado por mano. **`NetProfit = (HeroStackEnd - HeroStackStart) - AutoRebuy + BlindPosted`** excluye el rebuy del profit reportado.
6. **Stack inicial = 100 BB si la primera lectura es 0** (OCR fallido) — fallback de seguridad.

## Alternativas consideradas

1. **Detectar rebuy al cierre de la mano (no in-mano).** Rechazado: si el rebuy ocurre antes de la decisión final, la equity calculada con stack erróneo da una decisión equivocada. Hay que detectar **en el momento**.
2. **Threshold 30 BB.** Considerado. Rechazado: oscilaciones normales de un pot grande pueden cruzar 30 BB en una sola lectura. 50 BB es seguro.
3. **Threshold 90 BB (cerca del rebuy completo).** Rechazado: si el hero perdió un pot enorme y el rebuy lo lleva de 5 BB a 100 BB, el delta es 95 BB — pero si perdió "solo" un pot mediano y queda en 70 BB → 100 BB, el delta es 30 BB. **Threshold 50 BB cubre casi todos los casos.**
4. **Detectar rebuy comparando con `BigBlind` * 100 (asumiendo siempre 100 BB).** Rechazado: las salas tienen tablas con buy-in distinto del 100 BB clásico (40 BB, 250 BB).
5. **Pedir al usuario que confirme manualmente cada rebuy.** Rechazado: rompe el flujo automático.
6. **Hook a la API de la sala.** Rechazado por ADR-0004 (scraping pasivo).

## Consecuencias

**Positivas:**

- **NetProfit correcto** post-rebuy. Sin sobrestimar ni perder profit real.
- **`HeroStackPreRebuy`** se preserva durante la mano: si el hero hace dos rebuys (extremo), el cálculo sigue siendo conservador.
- 6 tests dedicados a `PostflopGameContext.TrackHeroStack` cubren casos edge.
- Funciona con cualquier tabla cash — el threshold 50 BB es genérico, no específico a un buy-in.

**Negativas:**

- **Si el hero gana un bote enorme legítimamente** (ej: all-in × 4 jugadores en mismo pot), el delta puede superar 50 BB en una lectura. **Falso positivo.** Mitigado parcialmente porque ese delta requiere stack increase **antes** de cierre de mano, lo cual no debería pasar (el pot no se recoge hasta el final).
- **No detecta el monto exacto del rebuy** — solo lo "amortigua". Si el rebuy es 80 BB, la diferencia se imputa al `EffectiveStack` calculado, no al `HandRecord.AutoRebuy`. 🔴 Q-AR-01: validar cómo se rellena `HandRecord.AutoRebuy`.
- **Threshold hardcoded.** No expuesto en `StrategyProfile` — si el usuario juega tablas con un buy-in atípico, no puede ajustarlo.

**Implicaciones para una migración:**

- El concepto "stack del hero al inicio efectivo" debe preservarse. La heurística 50 BB es razonable pero replicable.
- En sistemas más sofisticados, podría hacerse cross-check con eventos de la mano (showdown, fold) para confirmar que la mano no se ha cerrado todavía.

## Referencias

- Commit `5e21f14 feat/session: handle auto-rebuy and improve seat logic`.
- Commit `c788e9e feat/game-logging: Track hero stack pre auto-rebuy`.
- `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs:80-150`.
- `src/OpenScrape.Domain/Entities/GameSession.cs:73-95` — `HandRecord.NetProfit` documentado.
- ADR-0007 (PostflopGameContext inmutable — `TrackHeroStack` retorna tupla).
