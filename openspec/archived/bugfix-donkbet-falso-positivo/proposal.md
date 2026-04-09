# Proposal: Bugfix DonkBet Falso Positivo en Pots Limpeados

## Contexto

En manos donde nadie raiseó preflop (pot limpeado), cualquier bet postflop se clasifica incorrectamente como DonkBet. Esto activa thresholds permisivos (`LowEquityAction: "Call"`) que causan calls con equity mínima (10%) contra pot odds de 43.8%.

## Bugs Identificados

### BUG 1: DonkBet sin agresor (CRÍTICO)

**Causa raíz:** `DetectDonkBet` solo verifica `!villainWasPreflopAggressor` sin confirmar que hero sea el agresor. En pots limpeados nadie tiene `WasPreflopAggressor = true`, así que `!villainWasPreflopAggressor = true` → DonkBet detectado.

**Consecuencia:** Hero llama con 10% equity vs 43.8% pot odds porque `Turn_DonkBet.LowEquityAction = "Call"`.

**Archivo:** `GameCoordinator.cs:183-193`

### BUG 2: villainWasPreflopAggressor incluye hero (MEDIO)

**Causa raíz:** `state.Players.Any(p => p.Active && p.WasPreflopAggressor)` busca entre TODOS los jugadores, incluyendo hero (P0, `ValuePosition == 0`). Si hero raiseó preflop, hero.WasPreflopAggressor = true → `villainWasPreflopAggressor = true` → DonkBet NO detectado cuando debería.

**Consecuencia:** Cuando hero es el agresor real y villain donk-betea, no se detecta como DonkBet, perdiendo ajustes de threshold específicos.

**Archivo:** `GameCoordinator.cs:185-186`

### BUG 3: C-bet adj aplicado sin ser agresor real (BAJO)

**Causa raíz:** `IsPreflopAggressor(RaiseOverLimper) = true` aunque hero foldeó en ese spot. La HandSituation describe el tipo de spot, no la acción tomada.

**Consecuencia:** +12 puntos de equity injustificados en flop (15.4% → 27.4%). No afecta la decisión final (Check) pero distorsiona el log y la equity reportada.

**Archivo:** `GameCoordinator.cs:355-358`

## Alcance

Fix de BUG 1 y BUG 2 (ambos en la misma función `DetectDonkBet`). BUG 3 es cosmético en test mode y se deja para análisis posterior.

## Impacto Esperado

- Elimina calls con equity < 20% en pots limpeados (pérdida directa de ~7BB/mano afectada)
- Habilita DonkBet detection correcta cuando hero es el agresor real
