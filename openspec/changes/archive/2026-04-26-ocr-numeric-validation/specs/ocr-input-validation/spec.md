## ADDED Requirements

### Requirement: Validación de valores numéricos OCR
El sistema SHALL validar que los valores de stacks y bets contengan solo caracteres numéricos válidos (dígitos, opcionalmente con separador decimal comma o punto) antes de procesarlos.

#### Scenario: Input numérico válido
- **WHEN** se recibe un valor como "100", "100.50", "100,50" o "50"
- **THEN** el sistema SHALL procesar el valor normalmente

#### Scenario: Input con caracteres no numéricos
- **WHEN** se recibe un valor como "10O" (letra O), "5O0", "100A"
- **THEN** el sistema SHALL rechazar el valor, loggear warning con el input inválido, y devolver 0

#### Scenario: Input vacío o negativo
- **WHEN** se recibe un valor vacío, 0 o negativo
- **THEN** el sistema SHALL devolver 0 sin error

### Requirement: Validación de nombres de jugadores
El sistema SHALL validar que los nombres de jugadores leídos por OCR contengan solo caracteres alfabéticos válidos. Nombres con dígitos o símbolos deberán marcarse como error de OCR.

#### Scenario: Nombre con caracteres válidos
- **WHEN** se recibe un nombre como "PlayerOne", "Hero", "Villain123" (si está en lista de aliases)
- **THEN** el sistema SHALL procesar el nombre normalmente

#### Scenario: Nombre con caracteres inválidos
- **WHEN** se recibe un nombre con dígitos o símbolos no válidos (ej: "P1ayer", "Play€r")
- **THEN** el sistema SHALL marcar como error de OCR y usar el seat name (P1, P2, etc.) como fallback

#### Scenario: Nombre no encontrado en lista de aliases
- **WHEN** se recibe un nombre que no está en la lista de aliases conocida
- **THEN** el sistema SHALL intentar normalizar el nombre (limpiar caracteres) y registrarlo si es válido