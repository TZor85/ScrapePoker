## Why

Tres fuentes de errores silenciosos degradan la fiabilidad del bot:

1. **OCR sin scoring de confianza** — Tesseract ofrece `GetMeanConfidence()` pero no se usaba. Stacks y bets pueden leerse como basura y aceptarse sin validación.
2. **State machine sin validación de board cards** — La transición de estados no verifica que el nº de cartas visibles sea coherente con el state destino. Puede decidir en la street equivocada.
3. **OpponentTracker con threshold global** — `IsReliable >= 20 manos` ignora datos útiles de stats específicos. Con 5 c-bets ya se puede inferir el patrón, pero el tracker lo descarta.

## What Changes

1. **OCR Confidence** — `OcrResult` incluye `Confidence` (float 0-1) y `IsHighConfidence` (>= 0.70). `SetTextOCR` retry con umbral inactivo cuando confianza < 70%. `SetBetValue` usa confianza para elegir entre los dos OCR.
2. **Street Validation** — Nuevo overload `TryTransition(GameState, int visibleBoardCards)` que bloquea transición si cartas < mínimo esperado y warn si cartas > máximo.
3. **Granular Sample Size** — Nuevas propiedades: `HasReliableCBetData` (>= 5), `HasReliableAFData` (>= 10), `HasReliableFoldData` (>= 8), `HasReliablePreflopData` (>= 10). `OpponentTracker` usa estas en vez de `IsReliable`.

## Impact

- **`src/OpenScrape.App/Services/OcrService.cs`**: Confidence en OcrResult, GetMeanConfidence en ExtractTextFromRegionAndDebug.
- **`src/OpenScrape.App/Forms/FrmMain.cs`**: SetTextOCR y SetBetValue usan confianza para retry/selección.
- **`src/OpenScrape.App/Services/GameLoopStateMachine.cs`**: Nuevo overload TryTransition con board card validation.
- **`src/OpenScrape.Domain/Entities/OpponentProfile.cs`**: 4 nuevas propiedades stat-specific.
- **`src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs`**: GetAdjustedFoldEquity y GetFoldToBetPct usan thresholds granulares.
