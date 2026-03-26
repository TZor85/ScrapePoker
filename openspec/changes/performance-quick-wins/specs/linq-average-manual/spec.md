## ADDED Requirements

### Requirement: Cálculo de promedio RGB sin allocations
El cálculo de promedios de canales RGB SHALL usar aritmética manual con un bucle `foreach` en lugar de LINQ `.Average()`, eliminando allocations de delegados y enumeradores.

#### Scenario: Promedio manual equivalente a LINQ
- **GIVEN** una colección de 9 colores muestreados `[c1, c2, ..., c9]`
- **WHEN** se calculan los promedios con `sumB/count`, `sumR/count`, `sumG/count`
- **THEN** los valores `avgB`, `avgR`, `avgG` son idénticos a los que produciría `sampleColors.Average(c => c.B/R/G)`

#### Scenario: Colección de un solo color
- **GIVEN** que solo el píxel principal pasa el bounds check (los 8 offsets caen fuera)
- **WHEN** se calcula el promedio con count = 1
- **THEN** el resultado es el valor exacto del canal de ese único color (sin división por cero)

#### Scenario: Zero allocations en el cálculo
- **GIVEN** que el bucle de detección corre ~10 veces por segundo
- **WHEN** se ejecuta el cálculo de promedios
- **THEN** no se crean objetos en el heap (no `Func<>`, no `IEnumerator`, no boxing)

## REMOVED Requirements

### Requirement: LINQ Average para promedios de color
Se ELIMINA el uso de `sampleColors.Average(c => c.B)`, `sampleColors.Average(c => c.R)` y `sampleColors.Average(c => c.G)` del método de detección.
