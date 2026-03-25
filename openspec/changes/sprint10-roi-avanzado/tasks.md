# Sprint 10 — Tareas de Implementación

## Tasks

### 1. S10.1 — Preflop equity con VillainRange fallback
- **Archivo:** `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs`
- **Acción:** En `CalculateEquity()`, cuando preflop y `handSituation` es null o no parseable, usar `VillainRange.GetForSituation(HandSituation.OpenRaise)` como fallback en vez de lookup table.
- **Verificación:** Test: preflop equity con situación null usa MC vs OpenRaise range.

### 2. S10.1b — Fold equity ajustada por numOpponents
- **Archivo:** `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs`
- **Acción:** Agregar `int numOpponents = 1` a `CalculateFoldEquity()`. Si numOpponents >= 2, multiplicar baseFoldEquity por `1.0 / (1.0 + 0.3 * (numOpponents - 1))`. Propagar numOpponents en la llamada.
- **Verificación:** Test: fold equity 3-way < fold equity heads-up.

### 3. S10.2 — Bluff catch modulado por runout
- **Archivo:** `src/OpenScrape.DecisionMaker/PokerConstants.cs`
- **Acción:** Agregar `BluffCatchBrickRunoutMultiplier = 0.85` y `BluffCatchScareRunoutMultiplier = 1.15`.
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En bluff catch river, después de `opponentBluffMultiplier`, evaluar boardChange: brick → ×0.85, draw completado → ×1.15. Propagar `boardChange` al bloque (ya disponible en HandleLowEquity).
- **Verificación:** Test: bluff catch brick river → call más amplio. Bluff catch flush completado → fold más.

### 4. S10.3 — Implied odds ajustadas por numOpponents
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/ImpliedOddsCalculator.cs`
- **Acción:** Agregar `int numOpponents = 1` a `CalculateImpliedOddsFactor()`. Multiway OOP: sprFactor × (1.0 + 0.05 × (n-1)). Multiway IP con draw: sprFactor × (1.0 - 0.03 × (n-1)).
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Propagar `numOpponents` a `CalculateImpliedOddsFactor()` y al call interno.
- **Verificación:** Test: implied odds 3-way OOP peores que HU. Implied odds 3-way IP con draw mejores.

### 5. S10.4 — Check-raise IP como trap
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** En HandleNoBet, ampliar check-raise para IP con TwoPair+ en flop/turn, board no Wet/Monotone, como trap. Mantener prioridad OOP > IP.
- **Verificación:** Test: IP TwoPair flop Dry → check-raise. IP OnePair flop → no check-raise. IP TwoPair Wet → no check-raise.

### 6. Tests nuevos Sprint 10
- ~15-18 tests cubriendo S10.1-S10.4.
- **Verificación:** Todos pasan.

### 7. Build y test final
- `dotnet build + dotnet test + dotnet format`
- **Verificación:** 0 errores, todos tests pasan, formato OK.
