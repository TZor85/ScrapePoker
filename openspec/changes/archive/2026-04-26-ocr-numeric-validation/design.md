## Context

El sistema OCR del poker bot utiliza `ScreenReaderService` para leer valores de stacks y bets de la pantalla. Los métodos `NormalizeBetValue` y `NormalizeStackValue` en `ScreenReaderService.cs:289-373` aplican correcciones para artefactos OCR comunes (ej: "8" espurio, separadores decimales perdidos), pero no validan que el input contenga solo caracteres numéricos válidos.

**Problema actual:** 
- "10O" (letra O mayúscula) se interpreta como "100"
- No hay validación de patrón numérico
- Nombres de jugadores pueden contener caracteres erróneos de OCR

## Goals / Non-Goals

**Goals:**
- Añadir regex `^\d+[.,]?\d*$` para validar inputs numéricos
- Rechazar valores con caracteres no numéricos antes del procesamiento
- Validar nombres de jugadores contra lista de aliases conocida

**Non-Goals:**
- No modificar la lógica de corrección de artefactos existente
- No añadir machine learning o modelos predictivos
- No cambiar el formato de salida de los métodos de normalización

## Decisions

### D1: Regex de Validación Numérica

Se usará `^\d+[.,]?\d*$` que acepta:
- Solo dígitos, opcionalmente con separador decimal
- Separadores: coma (,) o punto (.)
- Ejemplos válidos: "100", "100.50", "100,50", "50"

**Alternativa considerada:** `[0-9.,]+` - rechazada porque permite caracteres adicionales no válidos.

### D2: Ubicación de la Validación

La validación se añadirá en `ScreenReaderService` como método privado auxiliar:
- `private static bool IsValidNumericInput(string input)`
- Se llamará al inicio de `NormalizeBetValue` y `NormalizeStackValue`
- Si no es válido, loggear warning y devolver 0

### D3: Validación de Nombres

Los nombres de jugadores se validarán contra la lista de aliases conocida en `OpponentTracker._seatAliasCache`. Si un nombre OCR no coincide con ningún alias y contiene caracteres no alfabéticos, se marcará como error.

## Risks / Trade-offs

- [Riesgo] False positives - usuarios legítimos con caracteres especiales → Mitigación: solo rechazar caracteres claramente erróneos (dígitos en nombres)
- [Riesgo] Performance - regex en cada llamada → Mitigación: usar Regex compilado estático

## Migration Plan

1. Añadir campo estático `NumericPattern` en `ScreenReaderService`
2. Añadir método `IsValidNumericInput`
3. Modificar `NormalizeBetValue` para validar antes de procesar
4. Modificar `NormalizeStackValue` para validar antes de procesar
5. Añadir validación en `SetAliasVillain` para nombres

Sin migración necesaria - cambio hacia adelante compatible.