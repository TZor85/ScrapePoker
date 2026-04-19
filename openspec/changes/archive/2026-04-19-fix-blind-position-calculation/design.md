## Context

El cálculo de posición usa la distancia desde el dealer. La fórmula actual está matemáticamente invertida.

## Goals / Non-Goals

**Goals:** Corregir la fórmula para que la distancia se calcule correctamente

**Non-Goals:** No cambiar otras funciones

## Decisions

- **Invertir la fórmula**: Cambiar `heroIndex - dealerIndex` a `dealerIndex - heroIndex`

## Risks / Trade-offs

- Sin riesgos