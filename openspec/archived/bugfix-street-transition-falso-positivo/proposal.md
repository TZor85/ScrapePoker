# Proposal: Bugfix Transición de Calle por Falso Positivo

## Contexto

Cuando hay más de una acción en la misma calle (villain raise, re-raise, etc.), el game loop transiciona incorrectamente a la siguiente calle en vez de reprocesar la calle actual con la bet actualizada.

## Causa Raíz

En `ProcessPostFlopAsync`, la transición `FlopAction → TurnDetected` se basa en `IsBoardCardVisible("Card4")` con un umbral de 80% de confianza. Si esta función retorna un falso positivo (artefactos visuales, splash de fichas, textura de mesa similar), el game loop transiciona a turn inmediatamente.

El problema se agrava porque `TryTransition(GameState.TurnDetected, 4)` pasa un `4` hardcodeado — no valida que haya 4 cartas realmente visibles en el board.

## Fix

1. Crear método `CountVisibleBoardCards()` que verifica Card1-Card5 y cuenta cuántas son realmente visibles.
2. Antes de transicionar, pasar el conteo real a `TryTransition(state, visibleCards)`.
3. El validador existente en `TryTransition` bloquea si `visibleCards < expectedMinCards`.

## Impacto

- Elimina transiciones de calle por falsos positivos de una sola región
- Cross-valida: Card4 "visible" pero Card1-3 no visibles → conteo < 4 → transición bloqueada
- Sin impacto en transiciones legítimas (4+ cartas visibles = turn real)
