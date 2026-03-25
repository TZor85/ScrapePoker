# Sprint 11 — Tareas

## Tasks

### 1. S11.1 — River value bet sizing reducido en board peligroso
- **Archivo:** `PostflopDecisionService.cs` HandleNoBet, sección value/strong value
- **Acción:** En river, si `dangerousFlushBoard` (ya calculado en HandleFacingBet, reusar lógica) → reducir sizing un nivel con `ReduceBetSize()`. Aplica a Value y StrongValue (no a thin value que ya checkea).
- **Verificación:** River con 3 same suit, hero OnePair → sizing menor. River sin flush → sizing normal.

### 2. S11.2 — VillainRange ajustado por posición del villain
- **Archivo:** `VillainRange.cs`
- **Acción:** Nuevo método `GetForSituation(HandSituation, TablePosition villainPosition)`. Si villain EP → rango más estrecho (×0.7 frecuencias). Si villain BTN → rango más amplio (×1.2). Propagar en FrmMain.
- **Verificación:** EP open → equity hero más baja. BTN open → equity hero más alta.

### 3. S11.3 — River sizing merged vs polarizado
- **Archivo:** `PostflopDecisionService.cs` HandleNoBet
- **Acción:** En river value bet: OnePair → ReduceBetSize (merged, sizing menor). TwoPair+ → sizing normal o IncreaseBetSize (polarizado). Complementa S11.1.
- **Verificación:** River OnePair → sizing menor. River ThreeOfAKind → sizing mayor.

### 4. S11.4 — Turn call con danger → river auto-check si draw completa
- **Archivo:** `PostflopGameContext.cs` + `PostflopDecisionService.cs` + `FrmMain.cs`
- **Acción:** Nuevo campo `TurnCalledWithFlushDanger`. Se marca cuando hero call turn con `dangerousFlushBoard`. En river, si draw completó (FlushCompleted) y `TurnCalledWithFlushDanger` → check (no value bet, potencialmente fold si villain apuesta).
- **Verificación:** Turn call con 3 hearts → river 4th heart → check. River brick → bet normal.

### 5. Tests + build final
- ~10-12 tests nuevos. Build OK, format OK, all tests pass.
