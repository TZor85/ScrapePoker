## Context

El log muestra Players.Count=1 aunque P5 tiene bet=1. El problema es que P5 no se detecta como jugador activo.

## Goals

- Agregar debug logging para ver qué regiones se procesan
- Detectar por qué P5 no se agrega a la lista de jugadores

## Decisions

- Agregar logging en SetEmptyPlayer y SetActivePlayer

## Risks

- Sin riesgos