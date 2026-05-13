## Why

El sistema OCR actualmente acepta valores numéricos mal formateados o con caracteres erróneos que pasan como válidos. Por ejemplo, "10O" (letra O mayúscula) se interpreta como "100" en stacks y bets, generando errores en los cálculos de decisiones de poker. Esta falta de validación causa datos incorrectos en el pipeline de decisión.

## What Changes

- Añadir validación de patrón numérico con regex `^\d+[.,]?\d*$` en la normalización de stacks y bets
- Implementar validación de nombres de jugadores contra lista de aliases conocida
- Rechazar valores que no coincidan con el patrón numérico válido y marcar como error de OCR

## Capabilities

### New Capabilities

- `ocr-input-validation`: Validación de inputs OCR (stacks, bets, nombres) antes de procesarlos en el pipeline de decisión

### Modified Capabilities

- Ninguna existente con cambios de requisitos

## Impact

- Servicios de normalización de OCR (`ScreenReaderService`)
- Pipeline de procesamiento de datos de mesa (`TableLayoutService`)
- Componentes de validación de entrada