## Bugfixes aplicados

- [x] OCR-1: NormalizeBetValue — decimal separator perdido (593 → 5,93), artefacto "8", validación vs pot size.
- [x] OCR-2: SetBetValue reescrito — 3-read consensus, PreprocessImageForOCR, CleanOcrNumericText, try/finally dispose.
- [x] OCR-3: River card validation — contar solo cartas con nombre válido para retry.
- [x] OCR-4: Squeeze → FourBet — SetPreflopActionUseCase amplía condición ThreeBet|Squeeze para detectar 4-bet del villain.

## Verificación

- [x] Build: 0 errores.
- [x] Tests: 519/519 pasados.
