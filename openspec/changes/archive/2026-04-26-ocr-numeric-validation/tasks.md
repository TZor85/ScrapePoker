## 1. Implementación de Regex de Validación

- [x] 1.1 Añadir campo estático `NumericPattern` en `ScreenReaderService`
- [x] 1.2 Crear método privado `IsValidNumericInput(string input)`

## 2. Modificación de NormalizeBetValue

- [x] 2.1 Añadir llamada a `IsValidNumericInput` al inicio del método
- [x] 2.2 Si no es válido, loggear warning y devolver 0

## 3. Modificación de NormalizeStackValue

- [x] 3.1 Añadir llamada a `IsValidNumericInput` al inicio del método
- [x] 3.2 Si no es válido, loggear warning y devolver 0

## 4. Validación de Nombres de Jugadores

- [x] 4.1 Revisar `TableLayoutService.SetAliasVillain` para añadir validación
- [x] 4.2 Añadir método auxiliar para validar nombres (solo alfabéticos)

## 5. Pruebas y Verificación

- [x] 5.1 Verificar que el build compila sin errores
- [x] 5.2 Ejecutar tests existentes para verificar que no hay regresiones