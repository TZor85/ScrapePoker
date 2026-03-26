## ADDED Requirements

### Requirement: OcrResult incluye scoring de confianza
El resultado OCR SHALL incluir la confianza media de Tesseract (0.0 a 1.0) para cada extracción.

#### Scenario: Extracción con confianza alta
- **GIVEN** una imagen clara de un stack "97"
- **WHEN** se ejecuta ExtractTextFromRegionAndDebug
- **THEN** OcrResult.Confidence >= 0.70 y OcrResult.IsHighConfidence == true

#### Scenario: Extracción desde cache
- **GIVEN** una imagen ya procesada (cache hit por dHash)
- **WHEN** se retorna resultado cacheado
- **THEN** OcrResult.Confidence == -1 (no disponible) y IsHighConfidence == true (beneficio de la duda)

### Requirement: Retry con umbral degradado en confianza baja
Cuando la confianza del OCR es < 70% y hay texto no vacío, SetTextOCR SHALL reintentar con umbral inactivo y usar el resultado de mayor confianza.

#### Scenario: Confianza baja → retry mejora
- **GIVEN** primer OCR retorna "9T" con confianza 0.45
- **WHEN** se reintenta con umbral inactivo y retorna "97" con confianza 0.85
- **THEN** se usa "97" (mayor confianza)

#### Scenario: Confianza baja → retry no mejora
- **GIVEN** primer OCR retorna "97" con confianza 0.55, retry retorna "9" con confianza 0.40
- **WHEN** se comparan confianzas
- **THEN** se mantiene "97" del primer intento

### Requirement: SetBetValue usa confianza para selección
Cuando ambos OCR (umbral activo e inactivo) retornan valores numéricos, SHALL preferir el de mayor confianza.

#### Scenario: Ambos OCR retornan valor, uno con más confianza
- **GIVEN** OCR1 retorna "5" con confianza 0.90, OCR2 retorna "15" con confianza 0.60
- **WHEN** se selecciona el resultado
- **THEN** se usa "5" (mayor confianza, más fiable)
