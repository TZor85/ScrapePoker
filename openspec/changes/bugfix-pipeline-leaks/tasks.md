# Bugfix Pipeline Leaks — Tareas

## Tasks

### 1. BF1 — Fix preflop equity log
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs:2085`
- **Acción:** Cambiar `_playerGameState.Players.Count(p => p.Active)` → agregar `- 1`.
- **Verificación:** Build OK.

### 2. BF2 — DetectFoldedPlayers en preflop
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs:122-125`
- **Acción:** Eliminar el guard `if (!IsFlop && !IsTurn && !IsRiver) return;` → permitir detección en cualquier estado postflop + preflop.
- **Verificación:** Build OK. Folds preflop detectados por color.

### 3. BF3 — Actualizar VillainBetSize en reprocess mid-street
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs:1148-1163`
- **Acción:** Al reprocesar flop (línea 1150) y turn (línea 1162), reclasificar betSize y actualizar `_postflopContext.VillainBetSizeFlop/Turn` antes de llamar a ProcessFlopAsync/ProcessTurnAsync.
- **Verificación:** Si villain raise en misma calle, betSize se actualiza.

### 4. BF4 — IP/OOP relativo al bettor
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs` (SetIsInPosition o callers)
- **Acción:** Cuando hay bet activa (maxBet > 0), recalcular si hero actúa después del bettor (IP) o antes (OOP). En heads-up ya es correcto; en multiway con bettor en posición diferente, ajustar.
- **Verificación:** Hero en BTN con bet desde MP → IP (correcto ya). Hero en MP con bet desde CO → OOP.

### 5. BF5 — Preflop equity display con ajuste posicional
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs:2085-2097`
- **Acción:** Después de calcular preflopEquity, aplicar bonus/penalty por TablePosition: BTN +3, CO +1, MP 0, EP -2, SB -3, BB -1.
- **Verificación:** Equity display varía por posición.

### 6. BF7 — Kicker strength en thin value
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- **Acción:** Agregar parámetro `KickerStrength heroKickerStrength = KickerStrength.None` a DetermineAction. Propagar a HandleNoBet. En thin value: TPTK (Strong) → bet. TPWK (Weak) → check OOP o bet sizing menor.
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Propagar `_flopResult.HeroKickerStrength` / `_turnResult.HeroKickerStrength` / `_riverResult.HeroKickerStrength` a DetermineAction.
- **Verificación:** TPTK thin value → bet. TPWK thin value → check.

### 7. Tests nuevos + build final
- ~10-15 tests.
- Build OK, format OK, all tests pass.
