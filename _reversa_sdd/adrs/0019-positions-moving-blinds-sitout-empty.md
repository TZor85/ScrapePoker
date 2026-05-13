# ADR-0019 — Algoritmo de posiciones con moving blinds: SB/BB saltan SitOut, Empty fuera del anillo

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** 2026-04-15 (commit `1654e09 fix(positions): ciegas con moving-blinds para SitOut y asientos vacios`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto + Claude Opus 4.7

## Contexto

En las salas de poker, las **moving blinds** son una regla estándar para mesas con asientos en sit-out o vacíos:

- El **SmallBlind (SB)** se postea en el asiento inmediatamente a la izquierda del dealer.
- El **BigBlind (BB)** se postea en el asiento inmediatamente a la izquierda del SB.
- Si esos asientos están en SitOut (ausentes pero asiento reservado), las ciegas **saltan** al siguiente asiento ocupado. El SitOut queda **sin posición** asignada esa mano.
- Los asientos **Empty** (sin jugador) se ignoran completamente — no participan en el cálculo.

Antes del fix, el cálculo de posiciones en `PositionCalculator` no seguía esta semántica fielmente:

- En tablas con SitOut consecutivos a la izquierda del dealer, se asignaba ciega al SitOut (incorrecto).
- AssignVillainPositions usaba `heroPosition` como ancla en vez del dealer, lo que producía rotaciones inconsistentes.
- Algunos SitOut **fuera** del tramo dealer-SB-BB perdían posición cuando deberían mantener su seat asignado.

## Decisión

Reescribir `PositionCalculator` con un **algoritmo de anillo físico**:

1. **Empty seats están fuera del anillo.** Se eliminan del `players[]` antes de iterar.
2. **SitOut seats permanecen en el anillo.** Su posición depende de si caen en el tramo dealer-SB-BB:
   - Dentro del tramo (los siguientes 1 o 2 a la izquierda del dealer): SB/BB saltan; el SitOut consumido por el salto queda **sin posición** (Position.None).
   - Fuera del tramo: mantienen su posición normal (Early/Middle/CutOff/etc.).
3. **`AssignVillainPositions(dealerPosition, ...)`** recibe ahora `dealerPosition` (no `heroPosition`).
4. **`TableLayoutService` recorre `allPlayers`** al resetear/asignar para que los SitOut con posición también reciban su etiqueta.
5. **Hero (P0) siempre activo** (commit `4dc9075`): aunque el OCR no detecte su nombre, P0 se considera Active.
6. **Recalcular posiciones cuando cambia el conteo de Active.** (Commit `5e21f14`).

## Alternativas consideradas

1. **Recolocar SitOut como Empty.** Atractivo por simplicidad pero rompe historicidad — el jugador volverá al mismo seat. Rechazado.
2. **Asignar siempre SB/BB a `dealerSeat+1` y `dealerSeat+2` físicamente.** Rechazado: incorrecto vs. la regla de la sala.
3. **Mantener `heroPosition` como ancla.** Estado anterior. Rechazado: cuando hero rota, las posiciones del resto cambian incorrectamente entre manos.
4. **Detectar la regla específica de cada sala.** Sobre-ingeniería: las moving blinds son universales en cash NLHE.
5. **Pedir al usuario que indique manualmente la posición de cada jugador.** Rechazado por UX.

## Consecuencias

**Positivas:**

- **Posiciones correctas** en mesas con SitOut. Bug histórico cerrado.
- **Hero siempre Active** — no se pierde aunque OCR de su nombre falle.
- **32 tests** en `PositionCalculatorTests` cubren SitOut dentro/fuera del tramo, asientos vacíos en SB/BB, combinaciones Empty+SitOut. Suite 814/814 verde.
- Algoritmo testeable en aislamiento (no depende de `FrmMain`).
- Logging detallado en debug si una posición sale `None` (alerta para diagnóstico).

**Negativas:**

- **Sutileza del "tramo dealer-SB-BB"** requiere entender la regla. Los tests cubren los casos pero la documentación inline ayuda.
- Si el OCR detecta mal el dealer (la base del cálculo), todo se desplaza.
- En mesas FullRing (9 jugadores) con muchos SitOut, el cálculo es más expensivo (iteraciones extras del anillo). Despreciable.

**Implicaciones para una migración:**

- El algoritmo de anillo + reglas de SitOut/Empty es **portable**. La spec textual basta para reimplementarlo.
- En cualquier reimplementación, **mantener el contract**: `AssignVillainPositions(dealerPosition, players[])` retornando `players[]` con `Position` rellenada.

## Referencias

- Commit `1654e09 fix(positions): ciegas con moving-blinds para SitOut y asientos vacios`.
- Commit `4dc9075 fix/positions: Correct hero position & player detection`.
- `src/OpenScrape.App/Helpers/PositionCalculator.cs`.
- `OpenScrape.App.Tests/PositionCalculatorTests.cs` (32 casos).
- Ver `_reversa_sdd/state-machines.md §6 PlayerState`.
